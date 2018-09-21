using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UnityCef.Shared.Ipc
{
    class IpcNegotiater
    {
        public IpcNegotiater(bool isServer, int negotiatingPort = 9000)
        {
            IsServer = isServer;
            NegotiationPort = negotiatingPort;
        }

        public bool IsServer { get; private set; }

        public int NegotiationPort { get; private set; }

        public int Port { get; private set; }

        public async Task WaitAsServer(int port)
        {
            Port = port;
            var listener = new TcpListener(IPAddress.Loopback, NegotiationPort);
            listener.Start();
            var client = await listener.AcceptTcpClientAsync();
            var buffer = BitConverter.GetBytes(port);
            var stream = client.GetStream();
            await stream.WriteAsync(buffer, 0, 4);
            await stream.FlushAsync();
            client.Close();
            listener.Stop();
        }

        public async Task<int> WaitAsClient()
        {
            var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, NegotiationPort);
            var stream = client.GetStream();
            var buffer = new byte[4];
            await stream.ReadAsync(buffer, 0, 4);
            Port = BitConverter.ToInt32(buffer, 0);
            client.Close();
            return Port;
        }
    }
}
