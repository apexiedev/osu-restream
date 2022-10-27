//  Play.cs
//  Author: Dean Herbert <pe@ppy.sh>
//  Copyright (c) 2010 2010 Dean Herbert

using OpenTK;
using OpenTK.Graphics;
using osum.Audio;
using osum.GameModes.Play.Components;
using osum.GameModes.Results;
using osum.GameplayElements;
using osum.GameplayElements.Beatmaps;
using osum.GameplayElements.HitObjects;
using osum.GameplayElements.HitObjects.Osu;
using osum.GameplayElements.Scoring;
using osum.Graphics;
using osum.Graphics.Sprites;
using osum.Helpers;
using osum.Input;
using osum.Input.Sources;
using osum.Support;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

//using osu.Graphics.Renderers;

namespace osum.GameModes.Play
{
    public class Player : GameMode
    {
        private HitObjectManager hitObjectManager;

        public HitObjectManager HitObjectManager
        {
            get => hitObjectManager;
            set
            {
                hitObjectManager = value;
                if (GuideFingers != null) GuideFingers.HitObjectManager = value;
            }
        }

        public HealthBar healthBar;

        internal ScoreDisplay scoreDisplay;

        internal ComboCounter comboCounter;

        private int firstObjectTime;
        private int lastObjectTime;

        internal static bool AllowStreamSwitches = true;

        /// <summary>
        /// Score which is being played (or watched?)
        /// </summary>
        public Score CurrentScore;

        /// <summary>
        /// The beatmap currently being played.
        /// </summary>
        public static Beatmap Beatmap;

        /// <summary>
        /// The difficulty which will be used for play mode (Easy/Standard/Expert).
        /// </summary>
        public static Difficulty Difficulty;

        /// <summary>
        /// Is autoplay activated?
        /// </summary>
        public static bool Autoplay;

        internal PauseMenu menu;

        internal PlayfieldBackground playfieldBackground;

        internal CountdownDisplay countdown;

        public bool Completed; //todo: make this an enum state

        /// <summary>
        /// If we are currently in the process of switching to another stream, this is when it should happen.
        /// </summary>
        protected int queuedStreamSwitchTime;

        /// <summary>
        /// Warning graphic which appears when a stream change is in process.
        /// </summary>
        private pSprite s_streamSwitchWarningArrow;

        internal SpriteManager topMostSpriteManager;

        internal StreamSwitchDisplay streamSwitchDisplay;

        internal TouchBurster touchBurster;

        protected bool isIncreasingStream;
        protected bool Failed;

        private int frameCount;

#if SCORE_TESTING
        string scoreTestFilename = "score-" + DateTime.Now.Ticks + ".txt";
#endif

#if HP_TESTING
        string healthTestFilename = "health-" + DateTime.Now.Ticks + ".txt";
#endif

        public override void Initialize()
        {
            if (GameBase.Instance != null) GameBase.Instance.DisableDimming = true;

#if SCORE_TESTING
            File.WriteAllText(scoreTestFilename, "");
#endif
#if HP_TESTING
            File.WriteAllText(healthTestFilename, "");
#endif

            InputManager.OnDown += InputManager_OnDown;

            if (GameBase.Instance != null)
                TextureManager.RequireSurfaces = true;

            if (!GameBase.IsSlowDevice && GameBase.Instance != null)
                touchBurster = new TouchBurster(!Autoplay);

            loadBeatmap();
            
            PhotosensitiveMode = GameBase.Config.GetValue(@"PhotosensitiveMode", false);

            topMostSpriteManager = new SpriteManager();

            initializeUIElements();

            if (HitObjectManager != null)
            {
                ShowGuideFingers = GameBase.Instance != null && (this is Tutorial || Autoplay || (GameBase.Config != null && GameBase.Config.GetValue("GuideFingers", false)));
                if (ShowGuideFingers)
                    GuideFingers = new GuideFinger { TouchBurster = touchBurster, HitObjectManager = hitObjectManager };

                InitializeStream();

                BeatmapDifficultyInfo diff = null;
                if (Beatmap.DifficultyInfo.TryGetValue(Difficulty, out diff))
                    DifficultyComboMultiplier = diff.ComboMultiplier;
                //isn't being read. either i'm playing with the wrong osz2 file or the loading code isn't working

                if (HitObjectManager.ActiveStreamObjects == null)
                {
                    //GameBase.Scheduler.Add(delegate { GameBase.Notify("Could not load difficulty!\nIt has likely not been mapped yet."); }, 500);
                    Director.ChangeMode(OsuMode.SongSelect);
                    //error while loading.
                    return;
                }

                if (GameBase.Instance != null && AudioEngine.Music != null)
                {
                    AudioEngine.Music.Stop();

                    if (AudioEngine.Music.LastLoaded != Beatmap.PackageIdentifier)
                        //could have switched to the results screen bgm.
                        AudioEngine.Music.Load(Beatmap.GetFileBytes(Beatmap.AudioFilename), false, Beatmap.PackageIdentifier);
                    else
                        AudioEngine.Music.Prepare();
                }

                Clock.ModeTimeReset();

                List<HitObject> objects = hitObjectManager.ActiveStreamObjects;

                firstObjectTime = objects[0].StartTime;
                lastObjectTime = objects[objects.Count - 1].EndTime;
            }

            resetScore();

            if (Beatmap != null && GameBase.Instance != null)
            {
                mapBackgroundImage = new pSpriteDynamic
                {
                    LoadDelegate = delegate
                    {
                        float availableHeight = 344;
                        pTexture thumb = null;

                        byte[] bytes = Beatmap.GetFileBytes("thumb-512.jpg");

                        if (bytes == null)
                        {
                            bytes = Beatmap.GetFileBytes("thumb-256.jpg");
                            availableHeight = 172;
                        }

                        if (bytes != null)
                            thumb = pTexture.FromBytes(bytes);

                        mapBackgroundImage.ScaleScalar = GameBase.BaseSize.Y / (availableHeight * GameBase.SpriteToBaseRatio);

                        mapBackgroundImage.FadeIn(3000, 0.05f);
                        mapBackgroundImage.ScaleTo(mapBackgroundImage.ScaleScalar + 0.0001f, 1, EasingTypes.Out);

                        return thumb;
                    },
                    DrawDepth = 0.005f,
                    Field = FieldTypes.StandardSnapCentre,
                    Origin = OriginTypes.Centre,
                    Alpha = 0.0005f,
                    Additive = true,
                    RemoveOldTransformations = false
                };


                spriteManager.Add(mapBackgroundImage);
            }

            playfieldBackground = new PlayfieldBackground();
            playfieldBackground.ChangeColour(Difficulty);
            spriteManager.Add(playfieldBackground);

            Director.OnTransitionEnded += Director_OnTransitionEnded;

            //if (fpsTotalCount != null)
            //{
            //    fpsTotalCount.AlwaysDraw = false;
            //    fpsTotalCount = null;
            //}

            //gcAtStart = GC.CollectionCount(0);

            s_streamSwitchWarningArrow = new pSprite(TextureManager.Load(OsuTexture.stream_changing_down), FieldTypes.StandardSnapBottomRight, OriginTypes.Centre, ClockTypes.Audio, new Vector2(50, GameBase.BaseSizeHalf.Y), 1, true, Color.White);
            s_streamSwitchWarningArrow.Additive = true;
            s_streamSwitchWarningArrow.Alpha = 0;

            spriteManager.Add(s_streamSwitchWarningArrow);

            Clock.AudioTime = 0;
            //hack: because seek doesn't update iOS player's internal time correctly.
            //in theory the Clock.ModeTimeReset() above should handle this.

            if (hitObjectManager != null)
                Resume(hitObjectManager.CountdownTime, 8, true, Beatmap.CountdownOffset);
        }

        protected virtual void InitializeStream()
        {
            switch (Difficulty)
            {
                default:
                    HitObjectManager.SetActiveStream();
                    break;
                case Difficulty.Easy:
                    HitObjectManager.SetActiveStream(Difficulty.Easy);
                    break;
                case Difficulty.Expert:
                    HitObjectManager.SetActiveStream(Difficulty.Expert);
                    break;
            }
        }

        protected virtual void initializeUIElements()
        {
            if (Difficulty != Difficulty.Easy) healthBar = new HealthBar();
            scoreDisplay = new ScoreDisplay();

            comboCounter = new ComboCounter();
            streamSwitchDisplay = new StreamSwitchDisplay();
            countdown = new CountdownDisplay();
            countdown.OnPulse += countdown_OnPulse;

            menu = new PauseMenu();

            menuPauseButton = new pSprite(TextureManager.Load(OsuTexture.pausebutton), FieldTypes.StandardSnapRight, OriginTypes.Centre,
                ClockTypes.Game,
                new Vector2(19 + GameBase.SuperWidePadding, 16.5f), 1, true, Color4.White);
            menuPauseButton.ClickableMargin = 5;
            menuPauseButton.OnClick += delegate { menu.Toggle(); };
            topMostSpriteManager.Add(menuPauseButton);

            menuPauseButton.Alpha = 0;
            menuPauseButton.Transform(new TransformationBounce(Clock.Time, Clock.Time + 1500, 1, 0.4f, 7));
            menuPauseButton.FadeIn(500);

            if (healthBar != null)
            {
                healthBar.spriteManager.Position = new Vector2(0, -40);
                healthBar.spriteManager.Rotation = -0.01f;
                healthBar.spriteManager.MoveTo(Vector2.Zero, 1000, EasingTypes.In);
                healthBar.spriteManager.RotateTo(0, 1000, EasingTypes.In);
            }

            progressDisplay = new ProgressDisplay();
        }

        private void countdown_OnPulse()
        {
            if(!PhotosensitiveMode) PulseBackground(true);
        }

        protected virtual void resetScore()
        {
            comboCounter?.SetCombo(0);

            healthBar?.SetCurrentHp(DifficultyManager.InitialHp, true);

            if (scoreDisplay != null)
            {
                scoreDisplay.SetAccuracy(0);
                scoreDisplay.SetScore(0);
            }

            CurrentScore = new Score { UseAccuracyBonus = false };
        }

        protected virtual void loadBeatmap()
        {
            if (Beatmap == null)
                return;

            HitObjectManager?.Dispose();

            if (Beatmap.Package != null && (GameBase.Instance != null && Beatmap.Package.GetMetadata(MapMetaType.Revision) == "preview"))
                return; //can't load preview in this mode.

            HitObjectManager = new HitObjectManager(Beatmap);

            HitObjectManager.OnScoreChanged += hitObjectManager_OnScoreChanged;
            HitObjectManager.OnStreamChanged += hitObjectManager_OnStreamChanged;

            try
            {
                if (Beatmap.Package != null)
                    HitObjectManager.LoadFile();
            }
            catch
            {
                HitObjectManager?.Dispose();
                HitObjectManager = null;
                //if this fails, it will be handled later on in Initialize()
            }
        }

        /// <summary>
        /// Setup a new countdown process.
        /// </summary>
        /// <param name="startTime">AudioTime of the point at which the countdown finishes (the "go"+1 beat)</param>
        /// <param name="beats">How many beats we should count in.</param>
        internal void Resume(int startTime = 0, int beats = 0, bool forceCountdown = false, int countdownOffset = 0)
        {
            if (countdown == null) return;

            if (Beatmap != null && startTime != 0 && beats != 0)
            {
                double beatLength = Beatmap.beatLengthAt(startTime);
                double offset = Beatmap.controlPointAt(startTime).offset;
                int actualStartTime = (int)(offset + (int)((startTime - offset) / beatLength + 0.5d - countdownOffset) * beatLength);

                int countdownStartTime;
                if (!countdown.HasFinished)
                    countdownStartTime = countdown.StartTime - (int)(beatLength * beats);
                else
                {
                    countdown.SetStartTime(actualStartTime, beatLength);
                    countdownStartTime = actualStartTime - (int)(beatLength * beats);
                }
            }

            if (GameBase.Instance != null && AudioEngine.Music != null)
            {
                AudioEngine.Music.Play();
            }

            if (menuPauseButton != null) menuPauseButton.HandleInput = true;
        }

        //static pSprite fpsTotalCount;
        //int gcAtStart;

        public override void Dispose()
        {
#if !DIST && !MONO
            Console.WriteLine("Player.cs produced " + frameCount + " frames.");
#endif
            if (GameBase.Instance != null) GameBase.Instance.DisableDimming = false;

            if (Beatmap != null)
            {
                if (Clock.ModeTime > 5000 && !Autoplay)
                {
                    BeatmapDatabase.GetDifficultyInfo(Beatmap, Difficulty).Playcount++;
                    BeatmapDatabase.Write();
                }
            }

            InputManager.OnDown -= InputManager_OnDown;
            Director.OnTransitionEnded -= Director_OnTransitionEnded;

            HitObjectManager?.Dispose();

            TextureManager.RequireSurfaces = false;

            healthBar?.Dispose();
            scoreDisplay?.Dispose();
            countdown?.Dispose();
            GuideFingers?.Dispose();
            menu?.Dispose();
            comboCounter?.Dispose();
            touchBurster?.Dispose();
            streamSwitchDisplay?.Dispose();
            playfieldBackground?.Dispose();
            topMostSpriteManager?.Dispose();

            progressDisplay?.Dispose();

            if (Director.PendingOsuMode != OsuMode.Play)
            {
                RestartCount = 0;
                Autoplay = false;
            }
            else
                RestartCount++;

            base.Dispose();

            //Performance testing code.
            //fpsTotalCount = new pText("Total Player.cs frames: " + frameCount + " of " + Math.Round(msCount / 16.666667f) + " (GC: " + (GC.CollectionCount(0) - gcAtStart) + ")", 16, new Vector2(0, 100), new Vector2(512, 256), 0, false, Color4.White, false);
            //fpsTotalCount.FadeOutFromOne(15000);
            //GameBase.MainSpriteManager.Add(fpsTotalCount);
        }

        protected virtual void InputManager_OnDown(InputSource source, TrackingPoint point)
        {
            if (GameBase.Mapper)
            {
                if (Autoplay)
                {
                    if (IsPaused)
                        Resume();
                    else
                        Pause(false);
                }
            }

            if (menu != null && menu.MenuDisplayed)
            {
                menu.handleInput(source, point);
                return;
            }

            if (!(Clock.AudioTime > 0 && (AudioEngine.Music == null || !AudioEngine.Music.IsElapsing)))
            {
                //pass on the event to hitObjectManager for handling.
                if (HitObjectManager != null && !Failed && !Autoplay && HitObjectManager.HandlePressAt(point))
                    return;
            }

            if (menu != null)
            {
                //before passing on input to the menu, do some other checks to make sure we don't accidentally trigger.
                if (hitObjectManager != null && !Autoplay)
                {
                    if (hitObjectManager.ActiveObject is Slider s && s.IsTracking)
                        return;

                    List<HitObject> objects = hitObjectManager.ActiveStreamObjects;
                    for (int i = hitObjectManager.ProcessFrom; i <= hitObjectManager.ProcessTo; i++)
                    {
                        HitObject h = objects[i];
                        if (h.IsVisible && menu.CheckHitObjectBlocksMenu(h.TrackingPosition.Y))
                            return;
                    }
                }

                menu.handleInput(source, point);
            }
        }

        protected virtual void hitObjectManager_OnStreamChanged(Difficulty newStream)
        {
            playfieldBackground.ChangeColour(HitObjectManager.ActiveStream);

            //don't change HP here when combinating. There could be a race condition between hitting the stream change note and resetting the health, which results in
            //incorrect calculations.
            if (healthBar != null && GameBase.Instance != null) healthBar.SetCurrentHp(DifficultyManager.InitialHp);

            streamSwitchDisplay?.EndSwitch();

#if SCORE_TESTING
            File.AppendAllText(scoreTestFilename, "Stream switched at " + Clock.AudioTime + "\n");
#endif

            queuedStreamSwitchTime = 0;
        }

        private void Director_OnTransitionEnded()
        {
        }

        private void comboPain(bool harsh)
        {
            if (Failed) return;

            playfieldBackground.FlashColour(Color4.Red, 500).Offset(-250);

            if (harsh)
            {
                AudioEngine.PlaySample(OsuSamples.miss);

                HitObjectManager.ActiveStreamSpriteManager.ScaleScalar = 0.9f;
                HitObjectManager.ActiveStreamSpriteManager.ScaleTo(1, 400, EasingTypes.In);
            }
        }

        protected virtual void hitObjectManager_OnScoreChanged(ScoreChange change, HitObject hitObject)
        {
            double healthChange = 0;
            bool increaseCombo = false;

            if (hitObject is HitCircle && change > 0)
            {
                CurrentScore.hitOffsetMilliseconds += (Clock.AudioTimeInputAdjust - hitObject.StartTime);
                CurrentScore.hitOffsetCount++;
            }

            bool comboMultiplier = true;
            bool addHitScore = true;

            int scoreChange = 0;

            //handle the score addition
            switch (change & ~ScoreChange.ComboAddition)
            {
                case ScoreChange.SpinnerBonus:
                    scoreChange = (int)hitObject.HpMultiplier;
                    comboMultiplier = false;
                    addHitScore = false;
                    CurrentScore.spinnerBonusScore += (int)hitObject.HpMultiplier;
                    healthChange = hitObject.HpMultiplier * 0.003f;
                    break;
                case ScoreChange.SpinnerSpinPoints:
                    scoreChange = 10;
                    comboMultiplier = false;
                    healthChange = 0.4f * hitObject.HpMultiplier;
                    break;
                case ScoreChange.SliderRepeat:
                    scoreChange = 30;
                    comboMultiplier = false;
                    increaseCombo = true;
                    healthChange = 1.2 * hitObject.HpMultiplier;
                    break;
                case ScoreChange.SliderEnd:
                    scoreChange = 30;
                    comboMultiplier = false;
                    increaseCombo = true;
                    healthChange = 1.6 * hitObject.HpMultiplier;
                    break;
                case ScoreChange.SliderTick:
                    scoreChange = 10;
                    comboMultiplier = false;
                    increaseCombo = true;
                    healthChange = 0.8 * hitObject.HpMultiplier;
                    break;
                case ScoreChange.Hit50:
                    scoreChange = 50;
                    CurrentScore.count50++;
                    lastJudgeType = ScoreChange.Hit50;
                    increaseCombo = true;
                    healthChange = -8;
                    break;
                case ScoreChange.Hit100:
                    scoreChange = 100;
                    CurrentScore.count100++;
                    lastJudgeType = ScoreChange.Hit100;
                    increaseCombo = true;
                    healthChange = 1;
                    break;
                case ScoreChange.Hit300:
                    scoreChange = 300;
                    CurrentScore.count300++;
                    lastJudgeType = ScoreChange.Hit300;
                    increaseCombo = true;
                    healthChange = 5;
                    break;
                case ScoreChange.MissMinor:
                    if (comboCounter != null)
                    {
                        comboPain(comboCounter.currentCombo >= 30);
                        comboCounter.SetCombo(0);
                    }

                    healthChange = -20 * hitObject.HpMultiplier;
                    break;
                case ScoreChange.Miss:
                    CurrentScore.countMiss++;
                    lastJudgeType = ScoreChange.Miss;
                    if (comboCounter != null)
                    {
                        //if (comboCounter.currentCombo >= 30)
                        comboPain(comboCounter.currentCombo >= 30);
                        comboCounter.SetCombo(0);
                    }

                    healthChange = -40;
                    break;
            }

#if !DIST
            /*if (hitObject is HitCircle || Math.Abs(Clock.AudioTime - hitObject.StartTime) < 30)
            {
                pSpriteText st = new pSpriteText(Math.Abs(Clock.AudioTime - hitObject.StartTime).ToString(), "default", 0, FieldTypes.GamefieldSprites, OriginTypes.TopCentre,
                    ClockTypes.Audio, hitObject.Position + new Vector2(0,60), 1, false, Clock.AudioTime > hitObject.StartTime ? Color.OrangeRed : Color4.YellowGreen);
                st.FadeOutFromOne(900);
                spriteManager.Add(st);
            }*/
#endif

            if (scoreChange > 0 && addHitScore)
                CurrentScore.hitScore += scoreChange;

            if (mapBackgroundImage != null)
                if(!PhotosensitiveMode) PulseBackground(scoreChange > 0);

            if (increaseCombo && comboCounter != null)
            {
                comboCounter.IncreaseCombo();
                CurrentScore.maxCombo = (ushort)Math.Max(comboCounter.currentCombo, CurrentScore.maxCombo);

                if (comboMultiplier)
                {
                    int comboAmount = (int)Math.Max(0, (scoreChange / 10 * Math.Min(comboCounter.currentCombo - 4, 60) / 2 * DifficultyComboMultiplier));

                    CurrentScore.comboBonusScore += comboAmount;


                    //check we don't exceed 0.6mil total (before accuracy bonus).
                    //null check makes sure we aren't doing score calculations via combinator.
                    if (GameBase.Instance != null && CurrentScore.hitScore + CurrentScore.comboBonusScore > Score.HIT_PLUS_COMBO_BONUS_AMOUNT)
                    {
                        if (CurrentScore.comboBonusScore + CurrentScore.hitScore > Score.HIT_PLUS_COMBO_BONUS_AMOUNT)
                        {
#if !DIST
                            Console.WriteLine("WARNING: Score exceeded limits at " + CurrentScore.totalScore);
#if SCORE_TESTING
                            File.AppendAllText(scoreTestFilename, "WARNING: Score exceeded limits at " + CurrentScore.totalScore + "\n");
#endif
#endif
                            CurrentScore.comboBonusScore = Math.Min(Score.HIT_PLUS_COMBO_BONUS_AMOUNT - CurrentScore.hitScore, CurrentScore.comboBonusScore);
                        }
                    }
                }
            }

#if SCORE_TESTING
            File.AppendAllText(scoreTestFilename, "at " + Clock.AudioTime + " : " + change + "\t" + CurrentScore.hitScore + "\t" + CurrentScore.comboBonusScore + "\t" + (healthBar != null ? healthBar.CurrentHp.ToString() : "") + "\n");
#endif

#if HP_TESTING
            File.AppendAllText(healthTestFilename, "at " + Clock.AudioTime + " : " + change + "\t" + healthChange + "\t" + (healthChange * Beatmap.HpStreamAdjustmentMultiplier) + "x\t" + (healthBar != null ? healthBar.CurrentHpUncapped.ToString() : "") + " -> " + (healthBar != null ? (healthBar.CurrentHpUncapped + healthChange * Beatmap.HpStreamAdjustmentMultiplier).ToString() : "") + "\n");
#endif

            if (healthBar != null)
            {
                //then handle the hp addition
                if (healthChange < 0)
                {
                    Difficulty streamDifficulty = hitObjectManager.ActiveStream;
                    float streamMultiplier = 1;

                    switch (streamDifficulty)
                    {
                        case Difficulty.Hard:
                            streamMultiplier = 1.3f;
                            break;
                        case Difficulty.Normal:
                            streamMultiplier = 1.1f;
                            break;
                    }


                    if (HitObjectManager == null || !HitObjectManager.StreamChanging)
                        healthBar.ReduceCurrentHp(DifficultyManager.HpAdjustment * -healthChange * streamMultiplier);
                }
                else
                    healthBar.IncreaseCurrentHp(healthChange * (Difficulty == Difficulty.Normal ? Beatmap.HpStreamAdjustmentMultiplier : 1));
            }

            if (scoreDisplay != null)
            {
                scoreDisplay.SetScore(CurrentScore.totalScore);
                scoreDisplay.SetAccuracy(CurrentScore.accuracy * 100);
            }
        }

        private void PulseBackground(bool positive)
        {
            List<Transformation> transforms = new List<Transformation>(mapBackgroundImage.Transformations);
            if (mapBackgroundImage == null || transforms.Count < 2) return;

            const float effect_magnitude = 1.3f;
            const float effect_limit = 1.4f;
            const float min = 0.05f;

            if (positive)
            {
                TransformationF t = mapBackgroundImage.Transformations[1] as TransformationF;
                t.StartFloat = Math.Min(t.CurrentFloat + 0.05f * effect_magnitude, 0.4f * effect_limit);
                t.StartTime = mapBackgroundImage.ClockingNow;
                t.EndTime = t.StartTime + 600;
                t.EndFloat = min;

                TransformationF t2 = mapBackgroundImage.Transformations[0] as TransformationF;
                t2.StartFloat = Math.Min(t2.CurrentFloat + 0.012f * effect_magnitude, t2.EndFloat + 0.3f * effect_limit);
                t2.StartTime = mapBackgroundImage.ClockingNow;
                t2.EndTime = t.StartTime + 600;
            }
            else
            {
                TransformationF t = mapBackgroundImage.Transformations[1] as TransformationF;
                t.StartTime = mapBackgroundImage.ClockingNow;
                t.EndTime = t.StartTime + 2000;
                t.StartFloat = 0;
                t.EndFloat = min;

                TransformationF t2 = mapBackgroundImage.Transformations[0] as TransformationF;
                t2.StartTime = mapBackgroundImage.ClockingNow;
                t2.EndTime = t.StartTime + 100;
            }
        }

        private pSprite failSprite;
        private double DifficultyComboMultiplier = 1;

        internal GuideFinger GuideFingers;
        public static int RestartCount;

        public override bool Draw()
        {
            base.Draw();

            frameCount++;

            streamSwitchDisplay?.Draw();

            comboCounter?.Draw();

            progressDisplay?.Draw();

            countdown?.Draw();

            if (HitObjectManager != null)
            {
                if (hitObjectManager.DrawBelowOverlay)
                {
                    HitObjectManager.Draw();
                    scoreDisplay?.Draw();
                }
                else
                {
                    scoreDisplay?.Draw();
                    HitObjectManager.Draw();
                }
            }
            else
            {
                scoreDisplay?.Draw();
            }

            healthBar?.Draw();

            if (GuideFingers != null && ShowGuideFingers) GuideFingers.Draw();

            topMostSpriteManager.Draw();

            menu?.Draw();

            touchBurster?.Draw();

            return true;
        }

        public override void Update()
        {
            bool isElapsing = AudioEngine.Music == null || AudioEngine.Music.IsElapsing;

            if (Failed)
            {
                if (GameBase.Instance != null && AudioEngine.Music != null)
                {
                    Director.AudioDimming = false;

                    float vol = AudioEngine.Music.DimmableVolume;
                    if (vol == 0 && isElapsing)
                        AudioEngine.Music.Pause();
                    else
                        AudioEngine.Music.DimmableVolume -= (float)(Clock.ElapsedMilliseconds) * 0.001f;
                }
            }

            if (GuideFingers != null && ShowGuideFingers) GuideFingers.Update();

            if (HitObjectManager != null)
            {
                //this needs to be run even when paused to draw sliders on resuming from resign.
                HitObjectManager.Update();

                //check whether the map is finished
                CheckForCompletion();

                if (HitObjectManager.ActiveObject is Spinner s)
                    playfieldBackground.Alpha = 1 - s.SpriteBackground.Alpha;
                else
                    playfieldBackground.Alpha = 1;
            }

            healthBar?.Update();

            UpdateStream();

            scoreDisplay?.Update();
            comboCounter?.Update();

            touchBurster?.Update();

            countdown?.Update();

            topMostSpriteManager.Update();

            streamSwitchDisplay?.Update();

            if (menu != null && (!Completed || Failed)) menu.Update();

            if (progressDisplay != null)
            {
                if (!Failed) progressDisplay.SetProgress(Progress, lastJudgeType);
                progressDisplay.Update();
            }

            base.Update();
        }

        protected virtual bool CheckForCompletion()
        {
            if (HitObjectManager.AllNotesHit && !Director.IsTransitioning && !Completed)
            {
                Completed = true;

                if (GameBase.Instance != null) //combinator
                {
                    if (Autoplay)
                    {
                        Director.ChangeMode(OsuMode.SongSelect, new FadeTransition(3000, FadeTransition.DEFAULT_FADE_IN));
                    }
                    else
                    {
                        Results.Results.RankableScore = CurrentScore;
                        Results.Results.RankableScore.UseAccuracyBonus = true;

                        GameBase.Scheduler.Add(delegate { Director.ChangeMode(OsuMode.Results, new ResultTransition()); }, 500);
                    }
                }
            }

            return Completed;
        }

        protected virtual void UpdateStream()
        {
            if (HitObjectManager != null && healthBar != null && !HitObjectManager.StreamChanging)
            {
                if (HitObjectManager.IsLowestStream &&
                    CurrentScore.totalHits > 0 &&
                    healthBar.CurrentHp < HealthBar.HP_BAR_MAXIMUM)
                {
                    //we are on the lowest available stream difficulty and in failing territory.
                    if (healthBar.CurrentHp == 0 && !Autoplay)
                    {
                        playfieldBackground.ChangeColour(PlayfieldBackground.COLOUR_INTRO);

                        if (!Completed)
                        {
                            Completed = true;

                            showFailScreen();

                            menu.Failed = true; //set this now so the menu will be in fail state if interacted with early.

                            GameBase.Scheduler.Add(delegate
                            {
                                Results.Results.RankableScore = CurrentScore;
                                menu.ShowFailMenu();
                            }, 1500);
                        }
                    }
                    else if (healthBar.CurrentHp < HealthBar.HP_BAR_MAXIMUM / 2)
                        playfieldBackground.ChangeColour(HitObjectManager.ActiveStream, 1 - (float)healthBar.CurrentHp / (HealthBar.HP_BAR_MAXIMUM / 2));
                    else
                        playfieldBackground.ChangeColour(HitObjectManager.ActiveStream, false);
                }
                else if (healthBar.CurrentHp == HealthBar.HP_BAR_MAXIMUM)
                {
                    switchStream(true);
                }
                else if (healthBar.CurrentHp == 0)
                {
                    switchStream(false);
                }
            }
            else if (Difficulty != Difficulty.Easy)
            {
#if DEBUG
                DebugOverlay.AddLine("Stream changing at " + HitObjectManager.nextStreamChange + " to " + HitObjectManager.ActiveStream);
#endif
                playfieldBackground.Move((isIncreasingStream ? 1 : -1) * Math.Max(0, (2000f - (queuedStreamSwitchTime - Clock.AudioTime)) / 200));
            }
        }

        protected void hideFailScreen()
        {
            Failed = false;

            if (GameBase.Instance != null) GameBase.Instance.DisableDimming = true;

            if (HitObjectManager != null)
            {
                HitObjectManager.spriteManager.Transformations.Clear();
                HitObjectManager.spriteManager.Position = Vector2.Zero;
            }

            failSprite?.FadeOut(100);
        }

        protected void showFailScreen()
        {
            if (menuPauseButton != null) menuPauseButton.HandleInput = false;

            Failed = true;
            playfieldBackground.ChangeColour(PlayfieldBackground.COLOUR_INTRO);
            AudioEngine.PlaySample(OsuSamples.fail);

            if (GameBase.Instance != null) GameBase.Instance.DisableDimming = false;

            HitObjectManager?.StopAllSounds();

            if (HitObjectManager != null)
            {
                HitObjectManager.spriteManager.MoveTo(new Vector2(0, 700), 5000, EasingTypes.OutDouble);
                HitObjectManager.spriteManager.RotateTo(0.1f, 5000);
                HitObjectManager.spriteManager.FadeOut(1000);
                HitObjectManager.ActiveStreamSpriteManager.MoveTo(new Vector2(0, 700), 5000, EasingTypes.OutDouble);
                HitObjectManager.ActiveStreamSpriteManager.FadeOut(5000);
                HitObjectManager.ActiveStreamSpriteManager.RotateTo(0.1f, 5000);
            }

            failSprite = new pSprite(TextureManager.Load(OsuTexture.failed), FieldTypes.StandardSnapCentre, OriginTypes.Centre, ClockTypes.Mode, Vector2.Zero, 0.5f, true, Color4.White);

            pDrawable failGlow = failSprite.Clone();

            failSprite.FadeInFromZero(500);
            failSprite.Transform(new TransformationF(TransformationType.Scale, 1.8f, 1, Clock.ModeTime, Clock.ModeTime + 500, EasingTypes.Out));
            failSprite.Transform(new TransformationF(TransformationType.Rotation, 0.1f, 0, Clock.ModeTime, Clock.ModeTime + 500, EasingTypes.Out));

            failGlow.DrawDepth = 0.51f;
            failGlow.AlwaysDraw = false;
            failGlow.ScaleScalar = 1.04f;
            failGlow.Additive = true;
            failGlow.Transform(new TransformationF(TransformationType.Fade, 0, 0, Clock.ModeTime, Clock.ModeTime + 500));
            failGlow.Transform(new TransformationF(TransformationType.Fade, 1, 0, Clock.ModeTime + 500, Clock.ModeTime + 2000));

            topMostSpriteManager.Add(failSprite);
            topMostSpriteManager.Add(failGlow);

            if (progressDisplay != null)
            {
                progressDisplay.SetProgress(Progress, ScoreChange.Ignore);
                progressDisplay.SetProgress(1, ScoreChange.Ignore);
                progressDisplay.ExtendHeight(1500, 50);
            }
        }

        internal bool IsPaused => GameBase.Mapper ? !AudioEngine.Music.IsElapsing : menu != null && menu.MenuDisplayed;

        internal virtual bool Pause(bool showMenu = true)
        {
            if (!Failed) AudioEngine.Music.Pause();

            HitObjectManager?.StopAllSounds();

            if (menu != null && showMenu) menu.MenuDisplayed = true;
            if (menuPauseButton != null) menuPauseButton.HandleInput = false;

            return true;
        }

        protected bool switchStream(bool increase)
        {
            if (!AllowStreamSwitches)
                return false;

            isIncreasingStream = increase;
            if (increase && HitObjectManager.IsHighestStream)
                return false;
            if (!increase && HitObjectManager.IsLowestStream)
                return false;

            int switchTime = HitObjectManager.SetActiveStream(HitObjectManager.ActiveStream + (increase ? 1 : -1));

            if (switchTime < 0)
                return false;

#if SCORE_TESTING
            File.AppendAllText(scoreTestFilename, "Switching stream at " + switchTime + "\n");
#endif

            streamSwitchDisplay?.BeginSwitch(increase);

            queuedStreamSwitchTime = switchTime;
            return true;
        }

        internal static string SubmitString => CryptoHelper.GetMd5String(Path.GetFileName(Beatmap.ContainerFilename) + "-" + Difficulty);

        public float Progress => pMathHelper.ClampToOne((float)Clock.AudioTime / lastObjectTime);

        protected bool ShowGuideFingers;
        protected bool PhotosensitiveMode;
        protected ProgressDisplay progressDisplay;
        private pSpriteDynamic mapBackgroundImage;
        private pSprite menuPauseButton;
        private ScoreChange lastJudgeType = ScoreChange.Ignore;
    }
}