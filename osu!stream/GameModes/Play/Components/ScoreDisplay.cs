﻿using OpenTK;
using OpenTK.Graphics;
using osum.Graphics.Sprites;
using osum.Helpers;
using System;

namespace osum.GameModes.Play.Components
{
    internal class ScoreDisplay : GameComponent
    {
        internal readonly pSpriteText s_Score;
        internal readonly pSpriteText s_Accuracy;
        private int displayScore;
        private double displayAccuracy;
        internal int currentScore;
        internal double currentAccuracy;
        protected Vector2 textMeasure;
        protected float scale;

        internal ScoreDisplay()
            : this(Vector2.Zero, true, 1, true, true)
        {
        }

        internal ScoreDisplay(Vector2 position, bool alignRight, float scale, bool showScore, bool showAccuracy)
        {
            this.scale = scale;

            float vpos = position.Y;
            float hOffset = 40 + GameBase.SuperWidePadding;

            textMeasure = Vector2.Zero;

            if (showScore)
            {
                s_Score =
                    new pSpriteText("000000", "score", 1,
                        alignRight ? FieldTypes.StandardSnapRight : FieldTypes.Standard, alignRight ? OriginTypes.TopRight : OriginTypes.TopLeft, ClockTypes.Game,
                        new Vector2(hOffset, 0), 0.95F, true, Color4.White);
                s_Score.TextConstantSpacing = true;
                s_Score.Position = new Vector2(hOffset, vpos);
                s_Score.ScaleScalar = scale;
                textMeasure = s_Score.MeasureText() * 0.625f * scale;

                vpos += textMeasure.Y + 4;
            }

            if (showAccuracy)
            {
                s_Accuracy =
                    new pSpriteText("00.00%", "score", 1,
                        alignRight ? FieldTypes.StandardSnapRight : FieldTypes.Standard, alignRight ? OriginTypes.TopRight : OriginTypes.TopLeft, ClockTypes.Game,
                        new Vector2(hOffset, 0), 0.95F, true, Color4.White);
                s_Accuracy.TextConstantSpacing = true;
                s_Accuracy.ScaleScalar = scale * (showScore ? 0.7f : 1);
                s_Accuracy.Position = new Vector2(hOffset - 35, vpos);
            }


            spriteManager.Add(s_Score);
            spriteManager.Add(s_Accuracy);
        }

        public override void Update()
        {
            if (s_Accuracy != null && Math.Abs(displayAccuracy - currentAccuracy) > 0.005)
            {
                if (displayAccuracy - currentAccuracy <= -0.005)
                    displayAccuracy = Math.Round(displayAccuracy + Math.Max(0.01, (currentAccuracy - displayAccuracy) / 5), 2);
                else if (displayAccuracy - currentAccuracy >= 0.005)
                    displayAccuracy = Math.Round(displayAccuracy - Math.Max(0.01, (displayAccuracy - currentAccuracy) / 5), 2);
                s_Accuracy.ShowDouble(displayAccuracy, 2, 2, '%');
            }

            if (s_Score != null)
            {
                if (displayScore != currentScore)
                {
                    int change = (int)((currentScore - displayScore) / (6f / Clock.ElapsedRatioToSixty));

                    //in case it gets rounded too close to zero.
                    if (change == 0) change = currentScore - displayScore;

                    displayScore += change;
                    s_Score.ShowInt(displayScore, 6);
                }
            }

            base.Update();
        }

        internal virtual void SetScore(int score)
        {
            currentScore = score;
        }

        internal void SetAccuracy(float accuracy)
        {
            currentAccuracy = Math.Round(accuracy, 2);
        }

        internal void Hide()
        {
            s_Score?.FadeOut(0);
            s_Accuracy?.FadeOut(0);
        }
    }
}