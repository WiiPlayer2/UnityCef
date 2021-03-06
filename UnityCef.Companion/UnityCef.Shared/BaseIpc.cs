﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityCef.Shared.Ipc;

namespace UnityCef.Shared
{
    public abstract class BaseIpc : IDisposable
    {
        public BaseIpc(MessageIpc ipc, string prefix = null)
        {
            Prefix = prefix;

            IPC = ipc ?? throw new ArgumentNullException(nameof(ipc));
            IPC.RegisterObject(this, prefix);
        }

        public MessageIpc IPC { get; private set; }

        public string Prefix { get; private set; }

        protected string MethodName([CallerMemberName]string methodName = "")
        {
            return Prefix != null ? $"{Prefix}.{methodName}" : methodName;
        }

        public virtual void Dispose(bool disposeIpc)
        {
            IPC.UnregisterObject(this);
            if (disposeIpc)
                IPC.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
