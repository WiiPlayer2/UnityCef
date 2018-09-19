using System;
using SharedMemory;
using UnityCef.Shared;
using UnityCef.Shared.Ipc;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityCef.Unity.Ipc
{
    public class BrowserIpc : BaseIpc, IBrowserIpc
    {
        private SharedArray<byte> textureBuffer;
        private byte[] textureData;
        private string textureName;
        private int textureWidth;
        private int textureHeight;

        public BrowserIpc(MessageIpc ipc, int identifier, int width, int height)
            : base(ipc, identifier.ToString())
        {
            textureWidth = width;
            textureHeight = height;
        }

        public Texture2D Texture { get; private set; }

        public void Update()
        {
            if (textureName == null)
                textureName = GetSharedName();
            if (Texture == null)
            {
                Texture = new Texture2D(textureWidth, textureHeight, TextureFormat.BGRA32, false, true);
                textureBuffer = new SharedArray<byte>(textureName);
                textureData = new byte[textureBuffer.Length];
            }

            textureBuffer.AcquireReadLock();
            textureBuffer.CopyTo(textureData);
            textureBuffer.ReleaseReadLock();

            Texture.LoadRawTextureData(textureData);
            Texture.Apply();
        }

        public string GetSharedName()
        {
            return (string)IPC.Request(Ipc("GetSharedName"))[0];
        }
    }
}
