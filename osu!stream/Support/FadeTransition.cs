﻿using osum.Graphics.Sprites;
using osum.Helpers;
using System;

namespace osum.Support
{
    internal class FadeTransition : Transition
    {
        public const int DEFAULT_FADE_OUT = 300;
        public const int DEFAULT_FADE_IN = 300;

        private readonly int FadeOutTime;
        private readonly int FadeInTime;

        public FadeTransition()
            : this(DEFAULT_FADE_OUT, DEFAULT_FADE_IN)
        {
        }

        public FadeTransition(int fadeOut, int fadeIn)
        {
            FadeOutTime = fadeOut;
            FadeInTime = fadeIn;
        }

        private FadeState fadeState = FadeState.FadeOut;

        private float currentValue; //todo: yucky.
        private float drawDim;
        public override float CurrentValue => currentValue;

        public override bool Draw()
        {
            drawDim = SpriteManager.UniversalDim;

            return base.Draw();
        }

        public override void Update()
        {
            switch (fadeState)
            {
                case FadeState.FadeIn:
                    if (FadeInTime == 0)
                        SpriteManager.UniversalDim = 0;
                    else
                        SpriteManager.UniversalDim = (float)Math.Max(0, SpriteManager.UniversalDim - Clock.ElapsedMilliseconds / FadeInTime);
                    break;
                case FadeState.FadeOut:
                    if (FadeOutTime == 0)
                        SpriteManager.UniversalDim = 1;
                    else
                        SpriteManager.UniversalDim = (float)Math.Min(1, SpriteManager.UniversalDim + Clock.ElapsedMilliseconds / FadeOutTime);
                    break;
            }

            currentValue = 1 - SpriteManager.UniversalDim; //todo: yucky.

            base.Update();
        }

        internal override void FadeIn()
        {
            fadeState = FadeState.FadeIn;
            base.FadeIn();
        }

        public override bool FadeInDone => drawDim == 0 && fadeState == FadeState.FadeIn;

        public override bool FadeOutDone => (drawDim == 1 && fadeState == FadeState.FadeOut) || fadeState == FadeState.FadeIn;
    }

    internal enum FadeState
    {
        FadeOut,
        FadeIn
    }
}