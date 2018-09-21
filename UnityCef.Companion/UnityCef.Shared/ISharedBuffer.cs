using System;
using System.Collections.Generic;
using System.Text;

namespace UnityCef.Shared
{
    public interface ISharedBuffer : IDisposable
    {
        void Create();

        void Open();

        void CopyTo(byte[] buffer);

        void CopyFrom(byte[] buffer);

        byte Read(int offset);

        void Write(byte value, int offset);
    }
}
