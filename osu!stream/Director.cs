using osum.Audio;
using osum.GameModes;
using osum.GameModes.MainMenu;
using osum.GameModes.Options;
using osum.GameModes.Play;
using osum.GameModes.Results;
using osum.GameModes.SongSelect;
using osum.GameModes.Store;
using osum.Graphics;
using osum.Graphics.Sprites;
using osum.Helpers;
using osum.Support;
using System;
using System.Collections.Generic;

namespace osum
{
    /// <summary>
    /// Handles display and transitioning of game modes.
    /// </summary>
    public static class Director
    {
        /// <summary>
        /// The active game mode, which is being drawn to screen.
        /// </summary>
        internal static GameMode CurrentMode;

        public static OsuMode CurrentOsuMode;
        internal static OsuMode LastOsuMode;

        /// <summary>
        /// The next game mode to be displayed (after a possible transition). OsuMode.Unknown when no mode is pending
        /// </summary>
        internal static OsuMode PendingOsuMode;

        /// <summary>
        /// The transition being used to introduce a pending mode.
        /// </summary>
        internal static Transition ActiveTransition;

        /// <summary>
        /// Actions to perform when transition finishes. NOTE: Is cleared after each transition.
        /// </summary>
        public static event VoidDelegate OnTransitionEnded;

        private static void TriggerOnTransitionEnded()
        {
            if (OnTransitionEnded != null)
            {
                OnTransitionEnded();
                OnTransitionEnded = null;
            }
        }

        public static bool ChangeMode(OsuMode mode, bool retainState = false)
        {
            return ChangeMode(mode, new FadeTransition(), retainState);
        }

        private static readonly Dictionary<OsuMode, GameMode> savedStates = new Dictionary<OsuMode, GameMode>();

        /// <summary>
        /// Changes the active game mode to a new requested mode, with a possible transition.
        /// </summary>
        /// <param name="mode">The new mode.</param>
        /// <param name="transition">The transition (null for instant switching).</param>
        /// <param name="retainState">Whether we want to save some kind of state.</param>
        /// <returns></returns>
        public static bool ChangeMode(OsuMode mode, Transition transition, bool retainState = false)
        {
            if (mode == OsuMode.Unknown || mode == PendingOsuMode) return false;

            if (retainState)
                //store a reference to the current mode to show that we want to keep state.
                savedStates[CurrentOsuMode] = CurrentMode;
            else
                savedStates.Remove(CurrentOsuMode);

            if (transition == null)
            {
                changeMode(mode);

                //force a transition-end in this case.
                TriggerOnTransitionEnded();

                return true;
            }

            PendingOsuMode = mode;
            ActiveTransition = transition;

            return true;
        }

        /// <summary>
        /// Handles switching to a new OsuMode. Acts as a fatory to create the material GameMode instance and dispose of any previous mode.
        /// </summary>
        /// <param name="newMode">The new mode specification.</param>
        private static void changeMode(OsuMode newMode)
        {
#if MONO
            if (Environment.CommandLine.Contains("Tester"))
            {
                switch (newMode)
                {
                    case OsuMode.PlayTest:
                    case OsuMode.PositioningTest:

                        break;
                    default:
                        PendingOsuMode = OsuMode.PositioningTest;
                        newMode = OsuMode.PositioningTest;
                        break;
                }
            }
#endif

            if (CurrentMode != null)
            {
                //check whether we want to retain state before outright disposing.
                GameMode check = null;

                if (!savedStates.TryGetValue(CurrentOsuMode, out check) || check != CurrentMode)
                {
                    if (check != null)
                        savedStates.Remove(CurrentOsuMode); //we should never get here, since only one state should be saved.
                    CurrentMode.Dispose();
                }
            }

            TextureManager.ModeChange();

            bool restored = false;

            Clock.ModeTimeReset();

            if (PendingMode == null)
            {
                //try to restore a saved state before loading fresh.
                if (savedStates.TryGetValue(newMode, out PendingMode))
                    restored = true;
                else
                    loadNewMode(newMode);
            }

            AudioEngine.Reset();

            CurrentMode = PendingMode;
            PendingMode = null;

            //enable dimming in case it got left on somewhere.
            if (GameBase.Instance != null) GameBase.Instance.DisableDimming = true;
            GameBase.ShowLoadingOverlay = false;

            AudioDimming = true;

            LastOsuMode = CurrentOsuMode; //the case for the main menu on first load.

            if (restored)
                CurrentMode.Restore();
            else
                CurrentMode.Initialize();

            if (PendingOsuMode != OsuMode.Unknown) //can be unknown on first startup
            {
                if (PendingOsuMode != newMode)
                {
                    changeMode(PendingOsuMode);
                    //we got a new request to load a *different* mode during initialisation...
                    return;
                }

                modeChangePending = true;
            }

            PendingOsuMode = OsuMode.Unknown;
            CurrentOsuMode = newMode;

            if (PendingOsuMode == OsuMode.Play)
                GC.Collect(); //force a full collect before we start displaying the new mode.

            GameBase.ThrottleExecution = false;
            //reset this here just in case it got stuck.
        }

        private static void loadNewMode(OsuMode newMode)
        {
            //Create the actual mode
            GameMode mode = null;

            switch (newMode)
            {
                case OsuMode.MainMenu:
                    mode = new MainMenu();
                    break;
                case OsuMode.SongSelect:
                    mode = new SongSelectMode();
                    break;
                case OsuMode.Results:
                    mode = new Results();
                    break;
#if MONO
                case OsuMode.PlayTest:
                    mode = new PlayTest();
                    break;
#endif
                case OsuMode.Play:
                    mode = CurrentOsuMode == OsuMode.VideoPreview ? new PreviewPlayer() : new Player();
                    break;
                case OsuMode.Store:
#if iOS
                    mode = new StoreModeIphone();
#else
                    mode = new StoreMode();
#endif
                    break;
                case OsuMode.Options:
                    mode = new Options();
                    break;
                case OsuMode.Tutorial:
                    mode = new Tutorial();
                    break;
                case OsuMode.Credits:
                    mode = new Credits();
                    break;
                case OsuMode.VideoPreview:
                    mode = new VideoPreview();
                    break;
                case OsuMode.Empty:
                    mode = new Empty();
                    break;
                case OsuMode.Mods:
                    mode = new Mods();
                    break;
#if MONO
                case OsuMode.PositioningTest:
                    mode = new PositioningTest();
                    break;
#endif
            }

            PendingMode = mode;

            GameBase.Instance.SetViewport();
        }

        private static bool modeChangePending;
        private static GameMode PendingMode;

        public static bool AudioDimming = true;


        /// <summary>
        /// Updates the director, along with current game mode.
        /// </summary>
        internal static bool Update()
        {
            if (modeChangePending)
            {
                //There was a mode change last frame.
                //See below for where this is set.
                Clock.ModeTimeReset();
                ActiveTransition?.FadeIn();
                CurrentMode.OnFirstUpdate();

                modeChangePending = false;
            }

            if (ActiveTransition != null)
            {
                ActiveTransition.Update();

                if (AudioDimming && !ActiveTransition.FadeOutDone && AudioEngine.Music != null)
                    AudioEngine.Music.DimmableVolume = 0.2f + ActiveTransition.CurrentValue * 0.8f;

                if (ActiveTransition.FadeOutDone)
                {
                    if (PendingOsuMode != OsuMode.Unknown)
                        changeMode(PendingOsuMode);
                    else if (ActiveTransition.FadeInDone)
                    {
                        TriggerOnTransitionEnded();

                        ActiveTransition.Dispose();
                        ActiveTransition = null;
                    }
                }
            }
            else if (GameBase.ActiveNotification != null)
                SpriteManager.UniversalDim = GameBase.ActiveNotification.Alpha * 0.7f;
            else if (GameBase.GloballyDisableInput)
                SpriteManager.UniversalDim = Math.Min(0.8f, SpriteManager.UniversalDim + 0.06f);
            else
                SpriteManager.UniversalDim = 0;

            //audio dimming
            if (AudioDimming && AudioEngine.Music != null)
            {
                if (SpriteManager.UniversalDim > 0)
                    AudioEngine.Music.DimmableVolume = Math.Min(1 - SpriteManager.UniversalDim * 0.8f, AudioEngine.Music.DimmableVolume);
                if (AudioEngine.Music.DimmableVolume < 1)
                    AudioEngine.Music.DimmableVolume = Math.Min(1, AudioEngine.Music.DimmableVolume + 0.02f);
            }

            if (modeChangePending) return true;
            //Save the first mode updates after we purge this frame away.
            //Initialising a mode usually takes a fair amount of time and will throw off timings,
            //so we count this as a null frame.

            CurrentMode?.Update();

            return false;
        }

        /// <summary>
        /// Draws the current game mode.
        /// </summary>
        internal static bool Draw()
        {
            if (CurrentMode == null)
                return false;

            CurrentMode.Draw();

            ActiveTransition?.Draw();
            return true;
        }

        public static bool IsTransitioning => ActiveTransition != null && GameBase.Instance != null;
    }
}
