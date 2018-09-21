using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using UnityCef.Shared.Native;
using System.IO.MemoryMappedFiles;

namespace UnityCef.Shared.Memory
{
    public class SharedMemoryWin : ISharedBuffer
    {
        private MemoryMappedFile mmf;
        private MemoryMappedViewAccessor view;

        public SharedMemoryWin(string name, int size)
        {
            Name = name;
            Size = size;
        }

        public string Name { get; private set; }

        public int Size { get; set; }

        public void Dispose()
        {
            view?.Dispose();
            mmf?.Dispose();
        }

        public void Create()
        {
            mmf = MemoryMappedFile.CreateNew(Name, Size);
            view = mmf.CreateViewAccessor(0, Size, MemoryMappedFileAccess.ReadWrite);
        }

        public void Open()
        {
            mmf = MemoryMappedFile.OpenExisting(Name);
            view = mmf.CreateViewAccessor();
        }

        public void CopyTo(byte[] buffer)
        {
            view.ReadArray(0, buffer, 0, Size);
        }

        public byte Read(int offset)
        {
            view.Read<byte>(offset, out var ret);
            return ret;
        }

        public void CopyFrom(byte[] buffer)
        {
            view.WriteArray(0, buffer, 0, Size);
        }

        public void Write(byte value, int offset)
        {
            view.Write(offset, value);
        }
    }
}
