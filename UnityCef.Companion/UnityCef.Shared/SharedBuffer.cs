using System;
using System.Collections.Generic;
using System.Text;
using UnityCef.Shared.Memory;

namespace UnityCef.Shared
{
    public class SharedBuffer : ISharedBuffer
    {
        private readonly ISharedBuffer nativeBuffer;

        public SharedBuffer(string name, int size)
        {
            nativeBuffer = new SharedMemoryWin(name, size);
        }

        public void CopyFrom(byte[] buffer)
        {
            nativeBuffer.CopyFrom(buffer);
        }

        public void CopyTo(byte[] buffer)
        {
            nativeBuffer.CopyTo(buffer);
        }

        public void Create()
        {
            nativeBuffer.Create();
        }

        public void Dispose()
        {
            nativeBuffer.Dispose();
        }

        public void Open()
        {
            nativeBuffer.Open();
        }

        public byte Read(int offset)
        {
            return nativeBuffer.Read(offset);
        }

        public void Write(byte value, int offset)
        {
            nativeBuffer.Write(value, offset);
        }
    }
}
