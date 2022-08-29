﻿using OpenTK;
using OpenTK.Graphics;
using osum.Audio;
using System;

namespace osum.Graphics.Sprites
{
    internal class pButton : pSpriteCollection
    {
        internal pSprite s_BackingPlate;
        internal pText s_Text;

        private readonly float base_depth = 0.5f;

        private Color4 colour;
        private Color4 colourNormal = new Color4(28, 139, 242, 255);

        internal const int PANEL_HEIGHT = 80;

        private readonly EventHandler action;

        public bool Enabled = true;

        private bool pendingUnhover;
        private pDrawable additiveButton;
        private readonly pSprite s_Status;

        internal pButton(string text, Vector2 position, Vector2 size, Color4 colour, EventHandler action)
        {
            this.action = action;

            Colour = colour;

            s_BackingPlate = new pSprite(TextureManager.Load(OsuTexture.notification_button_ok), position)
            {
                Origin = OriginTypes.Centre,
                DrawDepth = base_depth,
                HandleClickOnUp = true
            };
            Sprites.Add(s_BackingPlate);

            s_BackingPlate.OnHover += delegate
            {
                if (!Enabled) return;

                additiveButton = s_BackingPlate.AdditiveFlash(10000, 0.4f);
                pendingUnhover = true;
            };

            s_BackingPlate.OnHoverLost += delegate
            {
                if (!Enabled || !pendingUnhover) return;

                additiveButton?.FadeOut(100);
            };


            s_BackingPlate.OnClick += s_BackingPlate_OnClick;

            s_BackingPlate.HandleClickOnUp = true;

            s_Text = new pText(text, 25, position, base_depth + 0.01f, true, Color4.White);
            s_Text.Origin = OriginTypes.Centre;
            Sprites.Add(s_Text);

            s_Status = new pSprite(TextureManager.Load(OsuTexture.notification_button_toggle), position + new Vector2(-185, 0))
            {
                Origin = OriginTypes.Centre,
                DrawDepth = base_depth + 0.005f,
                Bypass = true
            };

            s_Status.OnClick += s_BackingPlate_OnClick;

            Sprites.Add(s_Status);
        }

        internal Color4 Colour
        {
            get => colour;
            set
            {
                colour = value;

                //colourNormal = ColourHelper.Darken(colour, 0.2f);
                //colourHover = colour;
                colourNormal = colour;

                if (s_BackingPlate != null)
                    s_BackingPlate.Colour = colourNormal;
            }
        }

        private void s_BackingPlate_OnClick(object sender, EventArgs e)
        {
            if (!Enabled) return;

            additiveButton?.FadeOut(100);

            AudioEngine.PlaySample(OsuSamples.MenuHit);

            pendingUnhover = false;

            action?.Invoke(this, null);
        }

        public Vector2 Position => s_BackingPlate.Position;

        internal void SetStatus(bool status)
        {
            if (s_Status.Bypass)
            {
                s_Status.Bypass = false;
                Sprites.ForEach(s => s.Position.X += 15);
            }

            Color4 col = status ? new Color4(184, 234, 0, 255) : new Color4(255, 72, 1, 255);

            if (s_Status.Colour != col)
            {
                s_Status.Colour = col;
                s_Status.FlashColour(Color4.White, 400);
            }
        }
    }
}