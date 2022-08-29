﻿using OpenTK;
using OpenTK.Graphics;
using osum.Audio;
using osum.Graphics;
using osum.Graphics.Sprites;
using osum.Helpers;
using System;

namespace osum.GameModes.Play.Components
{
    internal class CountdownDisplay : GameComponent
    {
        private pSprite background;

        private pSprite text;

        private const float distance_from_bottom = 0;

        internal event VoidDelegate OnPulse;

        public override void Initialize()
        {
            background = new pSprite(TextureManager.Load(OsuTexture.countdown_background), FieldTypes.StandardSnapCentre, OriginTypes.Centre, ClockTypes.Audio, new Vector2(0, distance_from_bottom), 0.99f, true, Color4.White);
            spriteManager.Add(background);

            text = new pSprite(null, FieldTypes.StandardSnapCentre, OriginTypes.Centre, ClockTypes.Audio, new Vector2(0, distance_from_bottom), 1, true, Color4.White);
            spriteManager.Add(text);

            spriteManager.Alpha = 0;

            base.Initialize();
        }

        public override void Dispose()
        {
            OnPulse = null;
            base.Dispose();
        }

        internal int StartTime = -1;
        private double BeatLength;

        internal void SetStartTime(int start, double beatLength)
        {
            StartTime = start;
            BeatLength = beatLength;
            spriteManager.Alpha = 0;
            HasFinished = false;
        }

        internal void Hide()
        {
            HasFinished = true;
            StartTime = -1;
            spriteManager.Alpha = 0;
        }

        internal void SetDisplay(int countdown)
        {
            bool didChangeTexture = true;

            switch (countdown)
            {
                case 0:
                    text.Texture = TextureManager.Load(OsuTexture.countdown_go);
                    spriteManager.FadeOut(400);
                    AudioEngine.PlaySample(OsuSamples.countgo);
                    HasFinished = true;
                    break;
                case 1:
                    text.Texture = TextureManager.Load(OsuTexture.countdown_1);
                    AudioEngine.PlaySample(OsuSamples.count1);
                    break;
                case 2:
                    text.Texture = TextureManager.Load(OsuTexture.countdown_2);
                    AudioEngine.PlaySample(OsuSamples.count2);
                    break;
                case 3:
                    text.Texture = TextureManager.Load(OsuTexture.countdown_3);
                    AudioEngine.PlaySample(OsuSamples.count3);
                    break;
                case 4:
                case 5:
                case 6:
                case 7:
                    text.Texture = TextureManager.Load(OsuTexture.countdown_ready);
                    if (spriteManager.Alpha == 0) spriteManager.FadeIn(200);
                    didChangeTexture = countdown == 7;
                    break;
                default:
                    return;
            }

            if (countdown < 4)
            {
                text.Transformations.RemoveAll(t => t is TransformationBounce);
                background.Transformations.RemoveAll(t => t is TransformationBounce);

                text.Transform(new TransformationBounce(Clock.AudioTime, Clock.AudioTime + 300, Math.Max(1, text.ScaleScalar), 0.2f, 3));
                background.Transform(new TransformationBounce(Clock.AudioTime, Clock.AudioTime + 300, Math.Max(1, background.ScaleScalar), 0.2f, 3));

                OnPulse?.Invoke();
            }

            if (didChangeTexture)
            {
                pDrawable flash = text.AdditiveFlash(300, 0.5f);
                flash.Transform(new TransformationF(TransformationType.Scale, 1, 1.2f, Clock.AudioTime, Clock.AudioTime + 400));
                flash.Transform(new TransformationF(TransformationType.Rotation, 0, 0.1f, Clock.AudioTime, Clock.AudioTime + 400));
            }
        }

        private int lastCountdownUpdate = -1;
        public bool HasFinished = true;

        public override void Update()
        {
            if (StartTime < 0 || (Clock.AudioTime > StartTime && lastCountdownUpdate < 0)) return;

            if (!HasFinished)
            {
                int countdown = (int)Math.Max(0, (StartTime - Clock.AudioTime) / BeatLength);

                if (countdown != lastCountdownUpdate)
                {
                    lastCountdownUpdate = countdown;
                    SetDisplay(countdown);
                }
            }

            base.Update();
        }
    }
}