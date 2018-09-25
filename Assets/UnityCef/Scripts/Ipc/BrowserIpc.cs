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

        public void Init(Texture2D texture = null)
        {
            if (texture == null)
                Texture = new Texture2D(textureWidth, textureHeight, TextureFormat.BGRA32, false, true);
            else
                Texture = texture;

            if (Texture.width != textureWidth
                || Texture.height != textureHeight)
                Texture.Resize(textureWidth, textureHeight);

            textureName = GetSharedName();
            textureBuffer = new SharedBuffer(textureName, textureData.Length);
            textureBuffer.Open();
        }

        public Texture2D Texture { get; private set; }

        public void Update()
        {
            textureBuffer.CopyTo(textureData);

            Texture.LoadRawTextureData(textureData);
            Texture.Apply();
        }

        public string GetSharedName()
        {
            return (string)IPC.Request(MethodName())[0];
        }

        public void Close()
        {
            IPC.Send(MethodName());
        }

        public void Navigate(string url)
        {
            IPC.Send(MethodName(), url);
        }

        public void ExecuteJS(string code)
        {
            IPC.Send(MethodName(), code);
        }

        public void SetFramerate(int framerate)
        {
            IPC.Send(MethodName(), framerate);
        }

        //TODO: Instead of implementing it, call an event
        [MessageIpc.Method]
        public void OnConsoleMessage(LogLevel level, string message, string source, int line)
        {
            var output = string.Format("{0}:{1}\n{2}", source, line, message);
            switch (level)
            {
                case LogLevel.Info:
                case LogLevel.Debug:
                    Debug.Log(output);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(output);
                    break;
                case LogLevel.Error:
                    Debug.LogError(output);
                    break;
                default:
                    Debug.LogWarningFormat("Log level {0} not implemented.\n{1}", level, output);
                    break;
            }
        }
    }
}
