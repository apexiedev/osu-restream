﻿using OpenTK;
using OpenTK.Graphics;
using osum.Graphics;
using osum.Graphics.Sprites;
using osum.Helpers;
using System;
using System.Collections.Generic;

namespace osum.GameModes.SongSelect
{
    internal class pTabController : GameComponent
    {
        private pSprite s_TabBarBackground;

        internal List<pDrawable> Sprites => spriteManager.Sprites;

        private readonly List<pDrawable> tabs = new List<pDrawable>();

        private readonly Dictionary<pDrawable, SpriteManager> spriteManagers = new Dictionary<pDrawable, SpriteManager>();
        private SpriteManager activeSpriteManager;

        public override void Dispose()
        {
            foreach (SpriteManager s in spriteManagers.Values)
                s.Dispose();
            spriteManagers.Clear();

            base.Dispose();
        }

        public override void Initialize()
        {
            s_TabBarBackground = new pSprite(TextureManager.Load(OsuTexture.songselect_tab_bar_background), FieldTypes.StandardSnapTopCentre, OriginTypes.TopCentre, ClockTypes.Mode, new Vector2(0, -100), 0.4f, true, Color4.White);
            s_TabBarBackground.Scale = new Vector2(GameBase.BaseSizeFixedWidth.X, 1); //this isn't perfectly window width, for what it's worth.
            spriteManager.Add(s_TabBarBackground);
        }

        internal pDrawable Add(OsuTexture tabTexture, List<pDrawable> sprites)
        {
            pSprite tab = new pSprite(TextureManager.Load(tabTexture), FieldTypes.StandardSnapTopCentre, OriginTypes.TopCentre, ClockTypes.Mode, new Vector2(0, -100), 0.41f, true, Color4.Gray);
            tab.OnClick += onTabClick;
            tab.OnHover += delegate { tab.FadeColour(Color4.White, 100); };
            tab.OnHoverLost += delegate { tab.FadeColour(Color4.Gray, 100); };

            spriteManager.Add(tab);

            spriteManagers.Add(tab, sprites != null ? new SpriteManager(sprites) : new SpriteManager());

            if (tabs.Count == 0) tab.Click();

            tabs.Add(tab);

            float x = -(Math.Max(0, tabs.Count - 1) * 200f) / 2;
            for (int i = 0; i < tabs.Count; i++)
            {
                tabs[i].Offset = new Vector2(x, 0);
                x += 200;
            }

            return tab;
        }

        internal pSprite SelectedTab;

        private void onTabClick(object sender, EventArgs e)
        {
            if (SelectedTab != null)
            {
                if (SelectedTab == sender) return;

                SelectedTab.HandleInput = true;
            }

            if (!(sender is pSprite s)) return;

            SelectedTab = s;
            activeSpriteManager = spriteManagers[SelectedTab];
            activeSpriteManager.Sprites.ForEach(d => d.FadeInFromZero(250));

            s.HandleInput = false;
        }

        public override void Update()
        {
            activeSpriteManager?.Update();

            base.Update();
        }

        public override bool Draw()
        {
            activeSpriteManager?.Draw();

            //return base.Draw();
            return true;
        }

        internal void Hide()
        {
            activeSpriteManager?.Sprites.ForEach(s => s.FadeOut(250));
            spriteManager.Sprites.ForEach(s => s.FadeOut(250));
        }

        internal void Show()
        {
            activeSpriteManager?.Sprites.ForEach(s => s.FadeInFromZero(250));
            spriteManager.Sprites.ForEach(s => s.FadeInFromZero(250));
        }
    }
}