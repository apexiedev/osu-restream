using OpenTK;
using OpenTK.Graphics;
using osum.GameplayElements.Beatmaps;
using osum.Graphics;
using osum.Graphics.Sprites;
using osum.Helpers;

namespace osum.GameModes.SongSelect
{
    public partial class SongSelectMode : GameMode
    {
        private void SongInfo_Show()
        {
            spriteManagerDifficultySelect.ScaleTo(0.5f, 300, EasingTypes.Out);
            spriteManagerDifficultySelect.FadeOut(300);
            background.FadeOut(300);

            SelectedPanel.Sprites.ForEach(s => s.MoveTo(new Vector2(0, -100), 400));

            songInfoSpriteManager.Clear();

            songInfoSpriteManager.Alpha = 0;
            songInfoSpriteManager.Position = Vector2.Zero;
            songInfoSpriteManager.Transformations.Clear();
            songInfoSpriteManager.Transform(new TransformationBounce(Clock.ModeTime + 200, Clock.ModeTime + 700, 1, 0.5f, 2));
            songInfoSpriteManager.Transform(new TransformationF(TransformationType.Fade, 0, 1, Clock.ModeTime + 200, Clock.ModeTime + 500));

            Beatmap beatmap = SelectedPanel.Beatmap;

            //256x172
            float aspectAdjust = GameBase.BaseSize.Y / (172 * GameBase.SpriteToBaseRatio);

            pSprite thumbSprite = new pSpriteDynamic
            {
                LoadDelegate = delegate
                {
                    pTexture thumb = null;
                    byte[] bytes = beatmap.GetFileBytes("thumb-256.jpg");
                    if (bytes != null)
                        thumb = pTexture.FromBytes(bytes);
                    return thumb;
                },
                DrawDepth = 0.49f,
                Field = FieldTypes.StandardSnapCentre,
                Origin = OriginTypes.Centre,
                ScaleScalar = aspectAdjust,
                Alpha = 0.3f
            };
            songInfoSpriteManager.Add(thumbSprite);

            float vPos = 60;

            string unicodeTitle = beatmap.Package.GetMetadata(MapMetaType.TitleUnicode);
            string normalTitle = beatmap.Title;

            if (unicodeTitle != normalTitle)
            {
                pText titleUnicode = new pText(unicodeTitle, 30, new Vector2(0, vPos), 1, true, Color4.White)
                {
                    Field = FieldTypes.StandardSnapTopCentre,
                    Origin = OriginTypes.Centre
                };
                songInfoSpriteManager.Add(titleUnicode);
                vPos += 40;
            }

            pText title = new pText(normalTitle, 30, new Vector2(0, vPos), 1, true, Color4.White)
            {
                Field = FieldTypes.StandardSnapTopCentre,
                Origin = OriginTypes.Centre,
                TextShadow = true
            };
            songInfoSpriteManager.Add(title);

            vPos += 40;

            string unicodeArtist = beatmap.Package.GetMetadata(MapMetaType.ArtistUnicode);

            pText artist = new pText("by " + beatmap.Package.GetMetadata(MapMetaType.ArtistFullName), 24, new Vector2(0, vPos), 1, true, Color4.White)
            {
                Field = FieldTypes.StandardSnapTopCentre,
                Origin = OriginTypes.Centre,
                TextShadow = true
            };

            songInfoSpriteManager.Add(artist);

            vPos += 40;

            string artistTwitter = beatmap.Package.GetMetadata(MapMetaType.ArtistTwitter);
            string artistWeb = beatmap.Package.GetMetadata(MapMetaType.ArtistUrl);

            if (artistWeb != null)
            {
                pText info = new pText(artistWeb, 20, new Vector2(0, vPos), 1, true, Color4.SkyBlue)
                {
                    Field = FieldTypes.StandardSnapTopCentre,
                    Origin = OriginTypes.Centre
                };

                info.OnClick += delegate { GameBase.Instance.OpenUrl(artistWeb); };
                songInfoSpriteManager.Add(info);
                vPos += 40;
            }

            if (artistTwitter != null)
            {
                pText info = new pText(artistTwitter, 20, new Vector2(0, vPos), 1, true, Color4.SkyBlue)
                {
                    Field = FieldTypes.StandardSnapTopCentre,
                    Origin = OriginTypes.Centre
                };

                info.OnClick += delegate { GameBase.Instance.OpenUrl(artistTwitter.Replace(@"@", @"https://twitter.com/")); };
                songInfoSpriteManager.Add(info);
                vPos += 40;
            }

            string unicodeSource = beatmap.Package.GetMetadata(MapMetaType.SourceUnicode);
            string normalSource = beatmap.Package.GetMetadata(MapMetaType.Source);

            if (normalSource != null)
            {
                vPos += 40;
                pText source = new pText(normalSource, 24, new Vector2(0, vPos), 1, true, Color4.White)
                {
                    Field = FieldTypes.StandardSnapTopCentre,
                    Origin = OriginTypes.Centre,
                    TextShadow = true
                };
                songInfoSpriteManager.Add(source);
            }

            if (normalSource != unicodeSource)
            {
                vPos += 40;
                pText source = new pText(unicodeSource, 24, new Vector2(0, vPos), 1, true, Color4.White)
                {
                    Field = FieldTypes.StandardSnapTopCentre,
                    Origin = OriginTypes.Centre,
                    TextShadow = true
                };
                songInfoSpriteManager.Add(source);
            }

            pText mapper = new pText("Level design by " + beatmap.Creator, 18, new Vector2(0, 40), 1, true, Color4.White)
            {
                Field = FieldTypes.StandardSnapBottomCentre,
                Origin = OriginTypes.BottomCentre
            };
            songInfoSpriteManager.Add(mapper);

            State = SelectState.SongInfo;

            footerHide();
        }

        private void SongInfo_Hide()
        {
            showDifficultySelection2();

            background.FadeIn(300);
            songInfoSpriteManager.Transformations.Clear();
            songInfoSpriteManager.FadeOut(400);
            songInfoSpriteManager.MoveTo(new Vector2(0, 600), 1500, EasingTypes.Out);
        }
    }
}