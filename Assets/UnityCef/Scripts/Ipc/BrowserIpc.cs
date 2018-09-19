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

        public BrowserIpc(MessageIpc ipc, int identifier)
            : base(ipc, identifier.ToString()) { }

        public Texture2D Texture { get; private set; }

        public void Update()
        {
            if(textureName != null)
            {
                if(Texture == null)
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
        }

        [MessageIpc.Method]
        public void OnPaint(int width, int height, string sharedName)
        {
            textureWidth = width;
            textureHeight = height;
            textureName = sharedName;
        }
    }
}
