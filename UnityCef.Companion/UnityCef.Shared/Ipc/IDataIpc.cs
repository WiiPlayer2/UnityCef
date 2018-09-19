using System;
using System.Collections.Generic;
using System.Text;

namespace UnityCef.Shared.Ipc
{
    public interface IDataIpc : IDisposable
    {
        void SetCallback(Func<byte[], Tuple<bool, byte[]>> onCall);
        
        void RemoteSend(byte[] data);

        Tuple<bool, byte[]> RemoteRequest(byte[] data);
    }
}
