using System;
using tiesky.com;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace UnityCef.Shared
{
    public class IPC : IDisposable
    {
        public enum ValueType : byte
        {
            Null = 0x00,
            Integer,
            String,
            Data,
            Array,
        }

        private static readonly Guid ipcGuid = Guid.Parse("{1A96751A-9E69-41D2-8DCA-6C9926990458}");

        private readonly SharmIpc ipc;
        private readonly Dictionary<string, Func<object[], object[]>> methods;

        public IPC()
        {
            methods = new Dictionary<string, Func<object[], object[]>>();
            ipc = new SharmIpc(ipcGuid.ToString(), OnCall);
        }

        public void RegisterMethod(string methodName, Func<object[], object[]> method)
        {
            if (methods.ContainsKey(methodName))
                throw new Exception($"{methodName} already registered.");
            methods[methodName] = method;
        }

        public void RegisterMethod(string methodName, object obj, MethodInfo method)
        {
            RegisterMethod(methodName, new Func<object[], object[]>(objs =>
            {
                var ret = method.Invoke(obj, objs);
                
                if (ret is Array && !(ret is byte[]))
                    return ((Array)ret)
                        .Cast<object>()
                        .ToArray();
                return new object[] { ret };
            }));
        }

        public void RegisterMethod(string methodName, Delegate method)
        {
            RegisterMethod(methodName, method.Target, method.Method);
        }

        #region I/O
        private object ReadValue(byte[] buffer, ref int index)
        {
            var ret = ReadValue(buffer, index);
            index += ret.Item2;
            return ret.Item1;
        }

        private (object, int) ReadValue(byte[] buffer, int index)
        {
            object ret = null;
            var len = 1;

            var type = (ValueType)buffer[index];
            switch(type)
            {
                case ValueType.Null:
                    break;
                case ValueType.Integer:
                    ret = BitConverter.ToInt32(buffer, index + 1);
                    len += 4;
                    break;
                case ValueType.String:
                    var length = BitConverter.ToInt32(buffer, index + 1);
                    ret = Encoding.UTF8.GetString(buffer, index + 5, length);
                    len += 4 + length;
                    break;
                case ValueType.Data:
                    length = BitConverter.ToInt32(buffer, index + 1);
                    ret = new byte[length];
                    Array.Copy(buffer, index + 5, (byte[])ret, 0, length);
                    len += 4 + length;
                    break;
                case ValueType.Array:
                    length = BitConverter.ToInt32(buffer, index + 1);
                    ret = new object[length];
                    var offset = 5;
                    for(var i = 0; i < length; i++)
                    {
                        var val = ReadValue(buffer, index + offset);
                        ((object[])ret)[i] = val.Item1;
                        offset += val.Item2;
                    }
                    len += offset - 1;
                    break;
                default:
                    throw new NotImplementedException($"{type} is not implemented.");
            }
            return (ret, len);
        }

        private (T, int) ReadValue<T>(byte[] buffer, int index)
        {
            var ret = ReadValue(buffer, index);
            return ((T)ret.Item1, ret.Item2);
        }

        private T ReadValue<T>(byte[] buffer, ref int index)
        {
            return (T)ReadValue(buffer, ref index);
        }

        private void WriteValue(BinaryWriter writer, object value)
        {
            if(value == null)
            {
                writer.Write((byte)ValueType.Null);
            }
            else if(value is int)
            {
                writer.Write((byte)ValueType.Integer);
                writer.Write((int)value);
            }
            else if(value is string)
            {
                var str = (string)value;
                writer.Write((byte)ValueType.String);
                var data = Encoding.UTF8.GetBytes(str);
                writer.Write(data.Length);
                writer.Write(data);
            }
            else if(value is byte[])
            {
                writer.Write((byte)ValueType.Data);
                var data = (byte[])value;
                writer.Write(data.Length);
                writer.Write(data);
            }
            else if(value is Array)
            {
                writer.Write((byte)ValueType.Array);
                var objs = (Array)value;
                writer.Write(objs.Length);
                for(var i = 0; i < objs.Length; i++)
                {
                    WriteValue(writer, objs.GetValue(i));
                }
            }
            else
            {
                throw new NotImplementedException($"{value.GetType()} is not implemented.");
            }
        }
        #endregion

        private Tuple<bool, byte[]> OnCall(byte[] data)
        {
            var index = 0;
            var methodName = ReadValue<string>(data, ref index);
            var args = ReadValue<object[]>(data, ref index);

            try
            {
                var objs = methods[methodName](args);

                using (var stream = new MemoryStream())
                using (var writer = new BinaryWriter(stream))
                {
                    WriteValue(writer, objs);
                    writer.Flush();
                    return Tuple.Create(true, stream.ToArray());
                }
            }
            catch(Exception e)
            {
                return Tuple.Create(false, new byte[0]);
            }
        }

        private object[] InternalCall(string method, object[] args, Func<byte[], Tuple<bool, byte[]>> call)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                WriteValue(writer, method);
                WriteValue(writer, args);
                writer.Flush();

                var result = call(stream.ToArray());
                if (!result.Item1)
                    throw new Exception("IPC Exception");
                return ReadValue<object[]>(result.Item2, 0).Item1;
            }
        }

        public object[] Call(string method, params object[] args)
        {
            return InternalCall(method, args, data => ipc.RemoteRequest(data));
        }

        public object[] LocalCall(string method, params object[] args)
        {
            return InternalCall(method, args, OnCall);
        }

        public void Dispose()
        {
            ipc.Dispose();
        }
    }
}
