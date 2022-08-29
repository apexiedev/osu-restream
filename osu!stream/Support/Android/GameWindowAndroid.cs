﻿using Android.Content;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Platform.Android;
using osum.Graphics;
using System;

namespace osum.Support.Android
{
    internal class GameWindowAndroid : AndroidGameView
    {
        public GameWindowAndroid(Context context) : base(context)
        {
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            MakeCurrent();

            TextureManager.ReloadAll();

            if (!GameBaseAndroid.IsInitialized)
                GameBase.Instance.Initialize();
        }

        protected override void CreateFrameBuffer()
        {
            ContextRenderingApi = GLVersion.ES1;

            try
            {
                base.CreateFrameBuffer();
                return;
            }
            catch
            {
            }

            try
            {
                GraphicsMode = new AndroidGraphicsMode(ColorFormat.Empty, 0, 0, 0, 0, false);

                base.CreateFrameBuffer();
                return;
            }
            catch
            {
            }

            throw new Exception("Can't load EGL, aborting...");
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            if (GameBase.Instance != null) GameBase.Instance.Update();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            if (GameBase.Instance != null) GameBase.Instance.Draw();

            SwapBuffers();
        }
    }
}
