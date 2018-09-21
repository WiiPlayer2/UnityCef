using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UnityCef.Shared.Ipc
{
    public class TcpDataIpc : IDataIpc
    {
        private enum PacketType : byte
        {
            Ping = 0x1,
            Request = 0x2,
            Response = 0x3,
        }

        private class ResponseInfo : IDisposable
        {
            private EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
            private Tuple<bool, byte[]> response;

            public ResponseInfo(long id)
            {
                ID = id;
            }

            public long ID { get; private set; }

            public static ResponseInfo Create(long id)
            {
                return new ResponseInfo(id);
            }

            public Tuple<bool, byte[]> Wait()
            {
                waitHandle.WaitOne();
                return response;
            }

            public void Signal(Tuple<bool, byte[]> response)
            {
                this.response = response;
                waitHandle.Set();
            }

            public void Dispose()
            {
                waitHandle.Dispose();
            }
        }

        private const string DEFAULT_UNIQUE_NAME = "{483D6641-6A40-4344-8C21-4DFE99F076A1}";
        private const int PIPE_TIMEOUT = 30000;
        private const int TCP_PORT_RANGE_START = 8000;
        private const int TCP_PORT_RANGE_END = 9000;

        private EventWaitHandle waitForConnection = new EventWaitHandle(false, EventResetMode.ManualReset);
        private EventWaitHandle waitForListener = new EventWaitHandle(false, EventResetMode.ManualReset);
        private Stream stream;
        private long packetId = 1;
        private readonly object packetIdLock = new object();
        private CancellationTokenSource readTaskCancellation = new CancellationTokenSource();
        private Func<byte[], Tuple<bool, byte[]>> callback;
        private Dictionary<long, ResponseInfo> responseInfos = new Dictionary<long, ResponseInfo>();

        public TcpDataIpc(bool isServer)
            : this(isServer, DEFAULT_UNIQUE_NAME) { }

        public TcpDataIpc(bool isServer, string uniqueName)
        {
            IsServer = isServer;
            UniqueName = uniqueName;

            //TODO: Make synchronous so the server is up and client is connected when object is created
            Task.Run(async () =>
            {
                var negotiater = new IpcNegotiater(IsServer);
                Console.WriteLine("Negotiating port...");
                if (isServer)
                {
                    var port = new Random().Next(TCP_PORT_RANGE_START, TCP_PORT_RANGE_END);
                    var listener = new TcpListener(IPAddress.Loopback, port);
                    listener.Start();
                    waitForListener.Set();

                    await negotiater.WaitAsServer(port);

                    var client = await listener.AcceptTcpClientAsync();
                    stream = client.GetStream();
                }
                else
                {
                    var port = await negotiater.WaitAsClient();
                    var client = new TcpClient();
                    await client.ConnectAsync(IPAddress.Loopback, port);

                    stream = client.GetStream();
                }
                Console.WriteLine("TCP connection established.");
                waitForConnection.Set();
                await RunReadLoop();
            }, readTaskCancellation.Token);
        }

        private void SendPing()
        {
            SendPacket(PacketType.Ping, 0, true, null);
        }

        private long SendRequest(byte[] data)
        {
            var id = 0L;
            lock (packetIdLock)
                id = packetId++;
            SendPacket(PacketType.Request, id, true, data);
            return id;
        }

        private void SendResponse(long id, bool ok, byte[] data)
        {
            SendPacket(PacketType.Response, id, ok, data);
        }

        private async void SendPacket(PacketType packetType, long id, bool ok, byte[] data)
        {
            await Task.Run(() =>
            {
                waitForConnection.WaitOne();

                using (var memStream = new MemoryStream())
                using (var writer = new BinaryWriter(memStream))
                {
                    writer.Write((byte)packetType);
                    if (packetType != PacketType.Ping)
                    {
                        writer.Write(id);
                        if (packetType == PacketType.Response)
                            writer.Write(ok);
                        if (ok)
                        {
                            writer.Write(data.Length);
                            writer.Write(data);
                        }
                    }
                    writer.Flush();

                    memStream.Position = 0;
                    lock (stream)
                    {
                        memStream.CopyTo(stream);
                        stream.Flush();
                    }
                }
            });
        }

        public bool IsServer { get; private set; }

        public string UniqueName { get; private set; }

        private async Task RunReadLoop()
        {
            while (true)
            {
                var read = 0;
                var buffer = new byte[8];
                do
                {
                    if (readTaskCancellation.IsCancellationRequested)
                        return;

                    read = await stream.ReadAsync(buffer, 0, 1);
                    if (read < 1)
                        await Task.Delay(100);
                }
                while (read < 1);

                var packetType = (PacketType)buffer[0];
                if (packetType != PacketType.Ping)
                {
                    await stream.ReadAsync(buffer, 0, 8);
                    var id = BitConverter.ToInt64(buffer, 0);
                    var ok = true;
                    byte[] data = null;

                    if (packetType == PacketType.Response)
                    {
                        await stream.ReadAsync(buffer, 0, 1);
                        ok = BitConverter.ToBoolean(buffer, 0);
                    }
                    if (ok)
                    {
                        await stream.ReadAsync(buffer, 0, 4);
                        var length = BitConverter.ToInt32(buffer, 0);
                        data = new byte[length];
                        await stream.ReadAsync(data, 0, length);
                    }

                    if (packetType == PacketType.Request)
                        HandleRequest(id, data);
                    else
                        HandleResponse(id, ok, data);
                }
            }
        }

        private async void HandleResponse(long id, bool ok, byte[] data)
        {
            await Task.Run(() =>
            {
                if (responseInfos.ContainsKey(id))
                {
                    var info = responseInfos[id];
                    lock (responseInfos)
                        responseInfos.Remove(id);
                    info.Signal(Tuple.Create(ok, data));
                }
            });
        }

        private async void HandleRequest(long id, byte[] data)
        {
            await Task.Run(() =>
            {
                var success = false;
                byte[] retData = null;
                try
                {
                    var ret = callback?.Invoke(data);
                    success = true;
                    if (ret != null && ret.Item1 && ret.Item2 != null)
                        retData = ret.Item2;
                    else
                        success = false;
                }
                catch (Exception e)
                {
                    Debug.Fail(e.Message, e.ToString());
                    if (Debugger.IsAttached)
                        Debugger.Break();
                }
                SendResponse(id, success, retData);
            });
        }

        public Tuple<bool, byte[]> RemoteRequest(byte[] data)
        {
            var id = SendRequest(data);
            lock (responseInfos)
                responseInfos[id] = ResponseInfo.Create(id);
            return responseInfos[id].Wait();
        }

        public void Dispose()
        {
            readTaskCancellation.Cancel();
            waitForConnection.Dispose();
            waitForListener.Dispose();
            stream.Dispose();
        }

        public void SetCallback(Func<byte[], Tuple<bool, byte[]>> onCall)
        {
            callback = onCall;
        }

        public void RemoteSend(byte[] data)
        {
            SendRequest(data);
        }

        public void WaitAsServer()
        {
            waitForListener.WaitOne();
        }
    }
}
