using OpenTK;
using OpenTK.Graphics;
using osum.Audio;
using osum.GameModes.SongSelect;
using osum.Graphics;
using osum.Graphics.Renderers;
using osum.Graphics.Sprites;
using osum.Helpers;
using osum.Localisation;
using osum.UI;
using System;

namespace osum.GameModes.Options
{
    public class Mods : GameMode
    {

        private BackButton s_ButtonBack;

        private readonly SpriteManagerDraggable smd = new SpriteManagerDraggable
        {
            Scrollbar = true
        };

        private readonly SpriteManager topMostSpriteManager = new SpriteManager();

        internal static float ScrollPosition;

        public override void Initialize()
        {
            s_Header = new pSprite(TextureManager.Load(OsuTexture.options_header), new Vector2(0, 0));
            s_Header.OnClick += delegate { };
            topMostSpriteManager.Add(s_Header);

            pDrawable background =
                new pSprite(TextureManager.Load(OsuTexture.songselect_background), FieldTypes.StandardSnapCentre, OriginTypes.Centre,
                    ClockTypes.Mode, Vector2.Zero, 0, true, new Color4(56, 56, 56, 255));
            background.AlphaBlend = false;
            spriteManager.Add(background);

            s_ButtonBack = new BackButton(delegate { Director.ChangeMode(OsuMode.Options); }, Director.LastOsuMode == OsuMode.Options);
            smd.AddNonDraggable(s_ButtonBack);

            if (MainMenu.MainMenu.InitializeBgm())
                AudioEngine.Music.Play();

            const int header_x_offset = 60;

            float button_x_offset = GameBase.BaseSize.X / 2;

            int vPos = 70;

            pText text = new pText("Mods", 36, new Vector2(header_x_offset, vPos), 1, true, Color4.White) { Bold = true, TextShadow = true };
            smd.Add(text);

            vPos += 50;

            text = new pText("Mods increase/decrease the difficulty of a certain beatmap.\nCertain mods can be incompatible between each other.", 24, new Vector2(0, vPos), 1, true, Color4.LightGray) { TextShadow = true };
            text.Field = FieldTypes.StandardSnapTopCentre;
            text.Origin = OriginTypes.TopCentre;
            text.TextAlignment = TextAlignment.Centre;
            text.MeasureText(); //force a measure as this is the last sprite to be added to the draggable area (need height to be precalculated)
            text.TextBounds.X = 600;
            smd.Add(text);

            vPos += (int)text.MeasureText().Y - 10;

            hiddenMod = new pButton("Hidden", new Vector2(button_x_offset, vPos), new Vector2(280, 50), Color4.SkyBlue, delegate { DisplayHiddenModDialog(); });
            smd.Add(hiddenMod);

            vPos += 50;
            
            text = new pText("Tweaks", 36, new Vector2(header_x_offset, vPos), 1, true, Color4.White) { Bold = true, TextShadow = true };
            smd.Add(text);

            vPos += 50;
            
            text = new pText("Tweaks let you customize the game to your likings.", 24, new Vector2(0, vPos), 1, true, Color4.LightGray) { TextShadow = true };
            text.Field = FieldTypes.StandardSnapTopCentre;
            text.Origin = OriginTypes.TopCentre;
            text.TextAlignment = TextAlignment.Centre;
            text.MeasureText(); //force a measure as this is the last sprite to be added to the draggable area (need height to be precalculated)
            text.TextBounds.X = 600;
            smd.Add(text);
            
            vPos += (int)text.MeasureText().Y + 10;
            
            oldSoundtrack = new pButton("Old Soundtrack", new Vector2(button_x_offset, vPos), new Vector2(280, 50), Color4.SkyBlue, delegate { DisplayOldSoundtrackDialog(); });
            smd.Add(oldSoundtrack);

            vPos += 60;

            welcomeToOsu = new pButton("osu! 2015 theme", new Vector2(button_x_offset, vPos), new Vector2(280, 50), Color4.SkyBlue, delegate { DisplayWelcomeToOsuDialog(); });
            smd.Add(welcomeToOsu);

            vPos += 60;
            
            photosensitiveMode = new pButton("Photosensitive Mode", new Vector2(button_x_offset, vPos), new Vector2(280, 50), Color4.SkyBlue, delegate { DisplayPhotosensitiveModeDialog(); });
            smd.Add(photosensitiveMode);

            // doubleTimeMod = new pButton("Double Time", new Vector2(button_x_offset, vPos), new Vector2(280, 50), Color4.SkyBlue, delegate { DisplayDoubleTimeModDialog(); });
            // smd.Add(doubleTimeMod);

            UpdateButtons();

            vPos += 50;

            smd.ScrollTo(ScrollPosition);
        }

        private pSprite s_Header;
        private pButton hiddenMod;
        private pButton oldSoundtrack;
        private pButton welcomeToOsu;
        private pButton photosensitiveMode;

        internal static void DisplayHiddenModDialog()
        {
            Notification notification = new Notification("Use the hidden mod?", "The hidden mod removes the approach circles and makes the objects fade after a bit of time, requiring you to memorize the map. Do you want to enable it?",
                NotificationStyle.YesNo,
                delegate (bool yes)
                {
                    GameBase.Config.SetValue(@"HiddenMod", yes);
                    if (Director.CurrentMode is Mods o) o.UpdateButtons();
                });
            GameBase.Notify(notification);
        }

        internal static void DisplayOldSoundtrackDialog()
        {
            Notification notification = new Notification("Enable the old OST?", "Feeling nostalgic? Enable the old soundtrack to feel like it was 2012 again for osu!stream.",
                NotificationStyle.YesNo,
                delegate (bool yes)
                {
                    GameBase.Config.SetValue(@"OldSoundtrack", yes);
                    if (Director.CurrentMode is Mods o) o.UpdateButtons();
                });
            GameBase.Notify(notification);
        }
        
        internal static void DisplayWelcomeToOsuDialog()
        {
            Notification notification = new Notification("Enable the osu! 2015 theme?", "Want to have the osu! 2015 theme? Enable it here.",
                NotificationStyle.YesNo,
                delegate (bool yes)
                {
                    GameBase.Config.SetValue(@"WelcomeToOsuTheme", yes);
                    if (Director.CurrentMode is Mods o) o.UpdateButtons();
                });
            GameBase.Notify(notification);
        }
        
        internal static void DisplayPhotosensitiveModeDialog()
        {
            Notification notification = new Notification("Enable photosensitive mode?", "If you suffer from epileptic seizures, you can enable photosensitive mode to reduce the amount of flashing lights in the game.",
                NotificationStyle.YesNo,
                delegate (bool yes)
                {
                    GameBase.Config.SetValue(@"PhotosensitiveMode", yes);
                    if (Director.CurrentMode is Mods o) o.UpdateButtons();
                });
            GameBase.Notify(notification);
        }

        private void UpdateButtons()
        {
            hiddenMod.SetStatus(GameBase.Config.GetValue(@"HiddenMod", false));
            oldSoundtrack.SetStatus(GameBase.Config.GetValue(@"OldSoundtrack", false));
            welcomeToOsu.SetStatus(GameBase.Config.GetValue(@"WelcomeToOsuTheme", false));
            photosensitiveMode.SetStatus(GameBase.Config.GetValue(@"PhotosensitiveMode", false));
            // doubleTimeMod.SetStatus(GameBase.Config.GetValue(@"DoubleTimeMod", false));
        }        

        public override void Dispose()
        {
            ScrollPosition = smd.ScrollPosition;

            GameBase.Config.SaveConfig();

            topMostSpriteManager.Dispose();

            smd.Dispose();
            base.Dispose();
        }

        public override bool Draw()
        {
            base.Draw();
            smd.Draw();
            topMostSpriteManager.Draw();
            return true;
        }

        public override void Update()
        {
            s_Header.Position.Y = Math.Min(0, -smd.ScrollPercentage * 20);

            smd.Update();
            base.Update();
            topMostSpriteManager.Update();
        }
    }
}