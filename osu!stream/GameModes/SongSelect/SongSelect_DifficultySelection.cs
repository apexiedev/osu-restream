﻿using OpenTK;
using OpenTK.Graphics;
using osum.Audio;
using osum.GameModes.Play;
using osum.GameplayElements;
using osum.GameplayElements.Scoring;
using osum.Graphics;
using osum.Graphics.Renderers;
using osum.Graphics.Sprites;
using osum.Helpers;
using osum.Input;
using osum.Localisation;
using System;

namespace osum.GameModes.SongSelect
{
    public partial class SongSelectMode : GameMode
    {
        private pSprite s_ModeButtonStream;

        private pSprite s_ModeArrowLeft;
        private pSprite s_ModeArrowRight;
        private pSprite s_ModeButtonEasy;
        private pSprite s_ModeButtonExpert;
        private pText s_ModeDescriptionText;

        private DifficultyScoreInfo bmi;

        /// <summary>
        /// True when expert mode is not yet unlocked for the current map.
        /// </summary>
        private bool mapRequiresUnlock
        {
            get
            {
#if !DIST
                return false;
#else
                DifficultyScoreInfo sc = Player.Beatmap.BeatmapInfo.DifficultyScores[Difficulty.Normal];
                return sc.HighScore == null || sc.HighScore.Ranking < Rank.A;
#endif
            }
        }

        private void showDifficultySelection(BeatmapPanel panel, bool instant = false)
        {
            if (!instant && State != SelectState.SongSelect && State != SelectState.SongInfo) return;

            if (Clock.ModeTime - lastDownTime > time_to_hover) return;

            cancelHoverPreview();
            cancelLockedHoverPreview();

            SelectedPanel = panel;
            Player.Beatmap = panel.Beatmap;

            panel.s_BackingPlate2.Alpha = 1;
            panel.s_BackingPlate2.AdditiveFlash(400, 1, true);
            panel.s_BackingPlate2.FadeColour(Color4.White, 0);

            foreach (BeatmapPanel p in panels)
            {
                p.s_BackingPlate.HandleInput = false;

                if (p == panel) continue;

                foreach (pDrawable s in p.Sprites)
                {
                    s.FadeOut(instant ? 0 : 400);
                    s.MoveTo(new Vector2(200, s.Position.Y), instant ? 0 : 500, EasingTypes.In);
                }
            }

            if (instant)
            {
                if (AudioEngine.Music != null && (AudioEngine.Music.LastLoaded != panel.Beatmap.PackageIdentifier))
                    AudioEngine.Music.Load(panel.Beatmap.GetFileBytes(panel.Beatmap.AudioFilename), false, panel.Beatmap.PackageIdentifier);
                showDifficultySelection2(true);
            }
            else
            {
                AudioEngine.PlaySample(OsuSamples.MenuHit);

                State = SelectState.LoadingPreview;

                GameBase.Scheduler.Add(delegate
                {
                    if (State != SelectState.LoadingPreview) return;

                    if (AudioEngine.Music != null && (AudioEngine.Music.LastLoaded != panel.Beatmap.PackageIdentifier))
                        AudioEngine.Music.Load(panel.Beatmap.GetFileBytes(panel.Beatmap.AudioFilename), false, panel.Beatmap.PackageIdentifier);

                    GameBase.Scheduler.Add(delegate { showDifficultySelection2(); }, true);
                }, 400);
            }
        }

        private void showDifficultySelection2(bool instant = false)
        {
            if (!instant && State != SelectState.LoadingPreview && State != SelectState.SongInfo && State != SelectState.RankingDisplay) return;

            if (!AudioEngine.Music.IsElapsing)
                playFromPreview();

            if (s_ModeButtonStream == null)
                initializeDifficultySelection();

            s_ModeButtonExpert.Colour = mapRequiresUnlock ? Color4.Gray : Color4.White;

            int animationTime = instant ? 0 : 500;

            SelectedPanel.HideRankings(instant);

            foreach (pDrawable s in SelectedPanel.Sprites)
                s.MoveTo(new Vector2(0, 0), animationTime, EasingTypes.InDouble);

            s_Header.Transform(new TransformationV(s_Header.Position, new Vector2(0, -63), Clock.ModeTime, Clock.ModeTime + animationTime, EasingTypes.In));
            s_Header.Transform(new TransformationF(TransformationType.Rotation, s_Header.Rotation, 0.03f, Clock.ModeTime, Clock.ModeTime + animationTime, EasingTypes.In));

            s_Footer.Transformations.Clear();
            s_Footer.Transform(new TransformationV(s_Footer.Position, Vector2.Zero, Clock.ModeTime, Clock.ModeTime + animationTime, EasingTypes.In));
            s_Footer.Transform(new TransformationF(TransformationType.Rotation, s_Footer.Rotation, 0, Clock.ModeTime, Clock.ModeTime + animationTime, EasingTypes.In));
            s_Footer.Alpha = 1;

            s_SongInfo.Transform(new TransformationF(TransformationType.Fade, s_SongInfo.Alpha, 1, Clock.ModeTime + animationTime, Clock.ModeTime + (animationTime * 3) / 2));

            spriteManagerDifficultySelect.ScaleScalar = 1;
            spriteManagerDifficultySelect.Transformations.Clear();
            spriteManagerDifficultySelect.FadeInFromZero(animationTime / 2);

            if (State == SelectState.LoadingPreview && !instant)
                SetDifficulty(GameBase.Config.GetValue("EasyMode", false) ? Difficulty.Easy : Difficulty.Normal, true);
            else
                SetDifficulty(Player.Difficulty, true);

            //preview has finished loading.
            State = SelectState.DifficultySelect;
        }

        private void playFromPreview()
        {
            if (Player.Beatmap != null)
            {
                AudioEngine.Music.DimmableVolume = 0;
                AudioEngine.Music.SeekTo(Player.Beatmap.PreviewPoint);
                AudioEngine.Music.Play();
            }
        }

        private void initializeDifficultySelection()
        {
            const float yOffset = 0;

            s_ModeButtonStream = new pSprite(TextureManager.Load(OsuTexture.songselect_mode_stream), FieldTypes.StandardSnapCentre, OriginTypes.Centre, ClockTypes.Mode, new Vector2(0, 0), 0.4f, true, Color4.White) { Offset = new Vector2(0, yOffset), HandleClickOnUp = true };
            s_ModeButtonStream.OnClick += onModeButtonClick;
            spriteManagerDifficultySelect.Add(s_ModeButtonStream);

            s_SongInfo = new pSprite(TextureManager.Load(OsuTexture.songselect_songinfo), FieldTypes.StandardSnapRight, OriginTypes.TopRight, ClockTypes.Mode, new Vector2(0, 0), 0.95f, true, Color4.White);
            s_SongInfo.Alpha = 0;
            s_SongInfo.OnClick += onSongInfoClick;
            topmostSpriteManager.Add(s_SongInfo);

            s_ModeButtonEasy = new pSprite(TextureManager.Load(OsuTexture.songselect_mode_easy), FieldTypes.StandardSnapCentre, OriginTypes.Centre, ClockTypes.Mode, new Vector2(0, 0), 0.4f, true, Color4.White) { Offset = new Vector2(-mode_button_width, yOffset), HandleClickOnUp = true };
            s_ModeButtonEasy.OnClick += onModeButtonClick;
            spriteManagerDifficultySelect.Add(s_ModeButtonEasy);

            s_ModeButtonExpert = new pSprite(TextureManager.Load(OsuTexture.songselect_mode_expert), FieldTypes.StandardSnapCentre, OriginTypes.Centre, ClockTypes.Mode, new Vector2(0, 0), 0.4f, true, Color4.White) { Offset = new Vector2(mode_button_width, yOffset), HandleClickOnUp = true };
            s_ModeButtonExpert.OnClick += onModeButtonClick;
            spriteManagerDifficultySelect.Add(s_ModeButtonExpert);

            const float arrow_spread = 180;

            s_ModeArrowLeft = new pSprite(TextureManager.Load(OsuTexture.songselect_mode_arrow), FieldTypes.StandardSnapCentre, OriginTypes.Centre, ClockTypes.Mode, new Vector2(-arrow_spread, yOffset), 0.45f, true, Color4.White);
            s_ModeArrowLeft.ClickableMargin = 30;
            s_ModeArrowLeft.OnHover += delegate { s_ModeArrowLeft.ScaleTo(1.2f, 100, EasingTypes.In); };
            s_ModeArrowLeft.OnHoverLost += delegate { s_ModeArrowLeft.ScaleTo(1f, 100, EasingTypes.In); };
            s_ModeArrowLeft.OnClick += onSelectPreviousMode;

            spriteManagerDifficultySelect.Add(s_ModeArrowLeft);

            s_ModeArrowRight = new pSprite(TextureManager.Load(OsuTexture.songselect_mode_arrow), FieldTypes.StandardSnapCentre, OriginTypes.Centre, ClockTypes.Mode, new Vector2(arrow_spread, yOffset), 0.45f, true, Color4.DarkGray);
            s_ModeArrowRight.ClickableMargin = 30;
            s_ModeArrowRight.OnHover += delegate { s_ModeArrowRight.ScaleTo(1.2f, 100, EasingTypes.In); };
            s_ModeArrowRight.OnHoverLost += delegate { s_ModeArrowRight.ScaleTo(1f, 100, EasingTypes.In); };
            s_ModeArrowRight.OnClick += onSelectNextMode;

            s_ModeArrowRight.Rotation = 1;
            spriteManagerDifficultySelect.Add(s_ModeArrowRight);

            s_ModeDescriptionText = new pText(string.Empty, 30, new Vector2(0, 110), 1, true, Color4.White) { Field = FieldTypes.StandardSnapCentre, Origin = OriginTypes.Centre, TextAlignment = TextAlignment.Centre };
            spriteManagerDifficultySelect.Add(s_ModeDescriptionText);

            s_ScoreInfo = new pText(null, 24, new Vector2(GameBase.SuperWidePadding, 64), Vector2.Zero, 1, true, Color4.White, true);
            s_ScoreInfo.OnClick += Handle_ScoreInfoOnClick;
            spriteManagerDifficultySelect.Add(s_ScoreInfo);

            s_ScoreRank = new pSprite(null, new Vector2(GameBase.SuperWidePadding, 72)) { DrawDepth = 0.95f };
            s_ScoreRank.OnClick += Handle_ScoreInfoOnClick;
            spriteManagerDifficultySelect.Add(s_ScoreRank);
        }

        private void onSongInfoClick(object sender, EventArgs e)
        {
            s_SongInfo.AdditiveFlash(1000, 0.8f);

            switch (State)
            {
                case SelectState.DifficultySelect:
                    AudioEngine.PlaySample(OsuSamples.MenuBling);
                    SongInfo_Show();
                    break;
                case SelectState.SongInfo:
                    SongInfo_Hide();
                    break;
            }
        }

        private void footer_onClick(object sender, EventArgs e)
        {
            if (InputManager.PrimaryTrackingPoint.BasePosition.X / GameBase.BaseSizeFixedWidth.X > (GameBase.BaseSize.X - 214) / GameBase.BaseSize.X)
            {
                Player.Autoplay = true;
                onStartButtonPressed(null, null);
            }
            else if (InputManager.PrimaryTrackingPoint.BasePosition.X / GameBase.BaseSizeFixedWidth.X > (GameBase.BaseSize.X - 214 * 2) / GameBase.BaseSize.X)
            {
                Ranking_Show();
            }
#if !DIST
            else if (GameBase.Mapper)
            {
                Player.AllowStreamSwitches = !Player.AllowStreamSwitches;
                GameBase.Notify("Stream switches are now " + (Player.AllowStreamSwitches ? "allowed" : "disallowed"));
            }
#endif
        }

        private void Handle_ScoreInfoOnClick(object sender, EventArgs e)
        {
            if (bmi.HighScore == null || bmi.HighScore.totalScore == 0)
                return;

            AudioEngine.PlaySample(OsuSamples.MenuHit);
            Results.Results.RankableScore = bmi.HighScore;
            Director.ChangeMode(OsuMode.Results);
        }

        private void onModeButtonClick(object sender, EventArgs e)
        {
            if (State == SelectState.Starting) return;

            if (!(sender is pDrawable d)) return;

            Difficulty newDifficulty;

            if (sender == s_ModeButtonEasy)
                newDifficulty = Difficulty.Easy;
            else if (sender == s_ModeButtonExpert)
                newDifficulty = Difficulty.Expert;
            else
                newDifficulty = Difficulty.Normal;

            if (newDifficulty == Player.Difficulty)
                onStartButtonPressed(sender, e);
            else
            {
                SetDifficulty(newDifficulty);
            }
        }

        private void SetDifficulty(Difficulty newDifficulty, bool force = false)
        {
            bool isNewDifficulty = Player.Difficulty != newDifficulty || force;
            velocity = 0;

            if (Player.Beatmap == null) return;

            MapPackage package = Player.Beatmap.Package;

            if (package == null) return;

            if (isNewDifficulty)
            {
                if (!force) AudioEngine.PlaySample(OsuSamples.ButtonTap);

                string versions = package.GetMetadata(MapMetaType.Version);
                if (versions != null && !versions.Contains(newDifficulty.ToString()))
                {
                    /*if (Player.Difficulty == Difficulty.Easy)
                        //came from easy -> expert; drop back on normal!
                        Player.Difficulty = Difficulty.Normal;
                    else*/
                    {
                        isNewDifficulty = false;
                        pendingModeChange = false;
                    }
                }
                else
                    Player.Difficulty = newDifficulty;
            }

            updateModeSelectionArrows(isNewDifficulty);
        }

        private void onSelectPreviousMode(object sender, EventArgs e)
        {
            if (State == SelectState.Starting) return;

            switch (Player.Difficulty)
            {
                case Difficulty.Normal:
                    SetDifficulty(Difficulty.Easy);
                    break;
                case Difficulty.Expert:
                    SetDifficulty(Difficulty.Normal);
                    break;
            }
        }

        private void onSelectNextMode(object sender, EventArgs e)
        {
            if (State == SelectState.Starting) return;

            switch (Player.Difficulty)
            {
                case Difficulty.Easy:
                    SetDifficulty(Difficulty.Normal);
                    break;
                case Difficulty.Normal:
                    SetDifficulty(Difficulty.Expert);
                    break;
            }
        }

        private const float mode_button_width = 300;
        private pText s_ScoreInfo;
        private pSprite s_ScoreRank;
        private pSprite s_SongInfo;

        /// <summary>
        /// Updates the states of mode selection arrows depending on the current mode selection.
        /// </summary>
        private void updateModeSelectionArrows(bool isNewDifficulty = true)
        {
            bool hasPrevious = false;
            bool hasNext = false;

            string text = null;

            velocity = 0;

            switch (Player.Difficulty)
            {
                case Difficulty.Easy:
                    hasNext = true;
                    difficultySelectOffset = mode_button_width;
                    text = LocalisationManager.GetString(OsuString.YouCantFail);
                    background.FadeColour(new Color4(110, 110, 110, 255), 500);
                    break;
                case Difficulty.Normal:
                    hasPrevious = true;
                    hasNext = true;
                    difficultySelectOffset = 0;
                    text = LocalisationManager.GetString(OsuString.DynamicStreamSwitching);
                    background.FadeColour(new Color4(70, 70, 70, 255), 500);
                    break;
                case Difficulty.Expert:
                    hasPrevious = true;
                    difficultySelectOffset = -mode_button_width;
                    text = LocalisationManager.GetString(OsuString.NotForTheFaintHearted);
                    background.FadeColour(new Color4(30, 30, 30, 255), 500);
                    break;
            }

            s_ModeArrowLeft.Colour = hasPrevious ? Color4.White : Color4.DarkGray;
            s_ModeArrowRight.Colour = hasNext ? Color4.White : Color4.DarkGray;

            if (isNewDifficulty || bmi == null)
            {
                if (s_ModeDescriptionText.Text != text)
                {
                    s_ModeDescriptionText.MoveTo(new Vector2(-20, s_ModeDescriptionText.Position.Y), 100, EasingTypes.Out);
                    s_ModeDescriptionText.FadeOut(100);
                }

                GameBase.Scheduler.Add(delegate
                {
                    //could have hit the back button really fast.
                    if (State == SelectState.DifficultySelect)
                    {
                        if (s_ModeDescriptionText.Text != text)
                        {
                            s_ModeDescriptionText.Text = text;
                            s_ModeDescriptionText.Position = new Vector2(20, s_ModeDescriptionText.Position.Y);
                            s_ModeDescriptionText.MoveTo(new Vector2(0, s_ModeDescriptionText.Position.Y), 100, EasingTypes.In);
                            s_ModeDescriptionText.FadeInFromZero(300);
                        }

                        bmi = BeatmapDatabase.GetDifficultyInfo(Player.Beatmap, Player.Difficulty);
                        s_ScoreInfo.Transform(new TransformationBounce(Clock.ModeTime, Clock.ModeTime + 200, 1, 0.05f, 2));
                        s_ScoreInfo.Text = LocalisationManager.GetString(OsuString.PlayCount) + " " + bmi.Playcount.ToString().PadLeft(3, '0') + '\n' + LocalisationManager.GetString(OsuString.HighScore) + " ";
                        if (bmi.HighScore == null)
                        {
                            s_ScoreInfo.Text += @"000000";
                            s_ScoreRank.Texture = null;
                        }
                        else
                        {
                            s_ScoreInfo.Text += bmi.HighScore.totalScore.ToString().PadLeft(6, '0');
                            s_ScoreRank.Texture = bmi.HighScore.RankingTextureSmall;
                        }

                        if (s_ScoreRank.Texture != null)
                        {
                            s_ScoreRank.AdditiveFlash(500, 0.5f);
                            s_ScoreInfo.MoveTo(new Vector2(GameBase.SuperWidePadding + 40, 64), 200, EasingTypes.In);
                        }
                        else
                            s_ScoreInfo.MoveTo(new Vector2(GameBase.SuperWidePadding, 64), 200, EasingTypes.In);
                    }
                }, 100);
            }
        }

        private void leaveDifficultySelection(object sender, EventArgs args)
        {
            Player.Beatmap = null;
            bmi = null;

            touchingBegun = false;
            velocity = 0;

            State = SelectState.SongSelect;

            InitializeBgm();

            GameBase.Scheduler.Add(delegate
            {
                background.FadeColour(new Color4(56, 56, 56, 255), 200);

                if (SelectedPanel != null)
                {
                    SelectedPanel.ShowRankings();
                    SelectedPanel.s_BackingPlate2.FadeColour(Color4.Transparent, 150);
                }

                foreach (BeatmapPanel p in panels)
                {
                    p.s_BackingPlate.HandleInput = true;

                    foreach (pDrawable d in p.Sprites)
                        d.FadeIn(500);
                }

                spriteManagerDifficultySelect.FadeOut(250);

                s_Header.Transform(new TransformationV(s_Header.Position, Vector2.Zero, Clock.ModeTime, Clock.ModeTime + 500, EasingTypes.In));
                s_Header.Transform(new TransformationF(TransformationType.Rotation, s_Header.Rotation, 0, Clock.ModeTime, Clock.ModeTime + 500, EasingTypes.In));

                s_SongInfo?.FadeOut(250);

                footerHide();
            }, true);
        }

        private void onStartButtonPressed(object sender, EventArgs args)
        {
            if (State == SelectState.Starting) return;

            if (Player.Difficulty == Difficulty.Expert && mapRequiresUnlock && !Player.Autoplay)
            {
                GameBase.Notify(LocalisationManager.GetString(OsuString.ExpertUnlock), delegate { pendingModeChange = false; });
                return;
            }

            AudioEngine.PlaySample(OsuSamples.MenuHit);

            State = SelectState.Starting;

            pDrawable activatedSprite = null;

            switch (Player.Difficulty)
            {
                case Difficulty.Easy:
                    s_ModeButtonStream.FadeOut(200);
                    s_ModeButtonExpert.FadeOut(200);
                    activatedSprite = s_ModeButtonEasy;
                    break;
                case Difficulty.Normal:
                    s_ModeButtonEasy.FadeOut(200);
                    s_ModeButtonExpert.FadeOut(200);
                    activatedSprite = s_ModeButtonStream;
                    break;
                case Difficulty.Expert:
                    s_ModeButtonStream.FadeOut(200);
                    s_ModeButtonEasy.FadeOut(200);
                    activatedSprite = s_ModeButtonExpert;
                    break;
            }

            activatedSprite.Transform(new TransformationBounce(Clock.ModeTime, Clock.ModeTime + 500, 1.2f, 0.4f, 1));

            activatedSprite.AdditiveFlash(800, 0.8f).Transform(new TransformationF(TransformationType.Scale, 1, 1.5f, Clock.Time, Clock.Time + 800, EasingTypes.In));

            background.FlashColour(new Color4(200, 200, 200, 255), 800);

            s_ModeArrowLeft.FadeOut(200);
            s_ModeArrowRight.FadeOut(200);

            s_Footer.AdditiveFlash(500, 0.5f);

            GameBase.Scheduler.Add(delegate { Director.ChangeMode(OsuMode.Play); }, 800);
        }
    }
}