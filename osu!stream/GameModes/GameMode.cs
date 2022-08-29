﻿using osum.Graphics.Sprites;
using osum.Support;
using System;

namespace osum.GameModes
{
    public enum OsuMode
    {
        Unknown = 0,
        MainMenu,
        SongSelect,
        Play,
        Results,
        Store,
        Options,
        Tutorial,
        Credits,
        PositioningTest,
        Empty,
        VideoPreview,
        PlayTest,
        Mods
    }

    /// <summary>
    /// A specific scene/screen that is to be displayed in the game.
    /// </summary>
    public abstract class GameMode : IDrawable, IDisposable
    {
        /// <summary>
        /// Do all initialization here.
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// Called when restoring from a saved state.
        /// </summary>
        public virtual void Restore()
        {
        }

        /// <summary>
        /// A spriteManager provided free of charge.
        /// </summary>
        internal SpriteManager spriteManager;

        internal GameMode()
        {
            spriteManager = new SpriteManager();
        }

        /// <summary>
        /// Clean-up this instance.
        /// </summary>
        public virtual void Dispose()
        {
            spriteManager.Dispose();
        }

        /// <summary>
        /// Updates this instance. Called every frame when loaded as a component.
        /// </summary>
        public virtual void Update()
        {
            spriteManager.Update();
        }

        /// <summary>
        /// Draws this object to screen.
        /// </summary>
        public virtual bool Draw()
        {
            spriteManager.Draw();
            return true;
        }

        public virtual void OnFirstUpdate()
        {
        }
    }
}