using System;
using System.Runtime.InteropServices;
using UnityCef.Shared;
using UnityCef.Shared.Ipc;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.IO.MemoryMappedFiles;

namespace UnityCef.Unity.Ipc
{
    public class BrowserIpc : BaseIpc, IBrowserIpc
    {
        private UnityCef.Shared.SharedBuffer textureBuffer;
        private byte[] textureData;
        private string textureName;
        private int textureWidth;
        private int textureHeight;

        public BrowserIpc(MessageIpc ipc, int identifier, int width, int height)
            : base(ipc, identifier.ToString())
        {
            textureWidth = width;
            textureHeight = height;
            textureData = new byte[textureWidth * textureHeight * 4];
        }

        public Texture2D Texture { get; private set; }

        public void Update()
        {
            if (textureName == null)
                textureName = GetSharedName();
            if (Texture == null)
            {
                Texture = new Texture2D(textureWidth, textureHeight, TextureFormat.BGRA32, false, true);
                textureBuffer = new UnityCef.Shared.SharedBuffer(textureName, textureData.Length);
                textureBuffer.Open();
            }

            textureBuffer.CopyTo(textureData);

            Texture.LoadRawTextureData(textureData);
            Texture.Apply();
        }

        public string GetSharedName()
        {
            return (string)IPC.Request(Ipc("GetSharedName"))[0];
        }
    }
}
