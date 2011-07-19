﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using osu_common.Bancho;
using osu_common.Helpers;
using osum.Graphics;
using osum.Graphics.Skins;

namespace osum.GameplayElements.Scoring
{
    public class Score : bSerializable
    {
        public ushort count100;
        public ushort count300;
        public ushort count50;
        public ushort countMiss;
        public DateTime date;
        public ushort maxCombo;
        public int spinnerBonusScore;
        public int hitOffsetMilliseconds;
        public int hitOffsetCount;
        public int comboBonusScore;
        public int accuracyBonusScore
        {
            get
            {
                if (!UseAccuracyBonus) return 0;
                return (int)Math.Round(Math.Max(0, accuracy - 0.75) / 0.25 * 300000);
            }
        }

        public int hitScore;
        public bool UseAccuracyBonus = true;

        public int totalScore
        {
            get { return spinnerBonusScore + hitScore + comboBonusScore + accuracyBonusScore; }
        }

        public Rank Ranking
        {
            get
            {
                if (accuracy == 1)
                    return Rank.SS;
                if (totalScore > 900000)
                    return Rank.S;
                if (totalScore > 800000)
                    return Rank.A;
                if (totalScore > 650000)
                    return Rank.B;
                if (totalScore > 500000)
                    return Rank.C;
                if (totalScore > 0)
                    return Rank.D;
                return Rank.N;
            }
        }

        public pTexture RankingTexture
        {
            get
            {
                pTexture rankLetter = null;

                switch (Ranking)
                {
                    case Rank.SS:
                        rankLetter = TextureManager.Load(OsuTexture.rank_x);
                        break;
                    case Rank.S:
                        rankLetter = TextureManager.Load(OsuTexture.rank_s);
                        break;
                    case Rank.A:
                        rankLetter = TextureManager.Load(OsuTexture.rank_a);
                        break;
                    case Rank.B:
                        rankLetter = TextureManager.Load(OsuTexture.rank_b);
                        break;
                    case Rank.C:
                        rankLetter = TextureManager.Load(OsuTexture.rank_c);
                        break;
                    case Rank.D:
                        rankLetter = TextureManager.Load(OsuTexture.rank_d);
                        break;
                }

                return rankLetter;
            }
        }

        public pTexture RankingTextureSmall
        {
            get
            {
                pTexture rankLetter = null;

                switch (Ranking)
                {
                    case Rank.SS:
                        rankLetter = TextureManager.Load(OsuTexture.rank_x_small);
                        break;
                    case Rank.S:
                        rankLetter = TextureManager.Load(OsuTexture.rank_s_small);
                        break;
                    case Rank.A:
                        rankLetter = TextureManager.Load(OsuTexture.rank_a_small);
                        break;
                    case Rank.B:
                        rankLetter = TextureManager.Load(OsuTexture.rank_b_small);
                        break;
                    case Rank.C:
                        rankLetter = TextureManager.Load(OsuTexture.rank_c_small);
                        break;
                    case Rank.D:
                        rankLetter = TextureManager.Load(OsuTexture.rank_d_small);
                        break;
                }

                return rankLetter;
            }
        }

        internal virtual float accuracy
        {
            get { return totalHits > 0 ? (float)(count50 * 1 + count100 * 2 + count300 * 4) / (totalHits * 4) : 0; }
        }

        internal virtual int totalHits
        {
            get { return count50 + count100 + count300 + countMiss; }
        }

        internal virtual int totalSuccessfulHits
        {
            get { return count50 + count100 + count300; }
        }

        #region bSerializable implementation
        public void ReadFromStream (SerializationReader sr)
        {
            count300 = sr.ReadUInt16();
            count100 = sr.ReadUInt16();
            count50 = sr.ReadUInt16();
            countMiss = sr.ReadUInt16();
            //date = sr.ReadDateTime();
            maxCombo = sr.ReadUInt16();
            spinnerBonusScore = sr.ReadInt32();
            comboBonusScore = sr.ReadInt32();
            hitScore = sr.ReadInt32();
        }

        public void WriteToStream (SerializationWriter sw)
        {
            sw.Write(count300);
            sw.Write(count100);
            sw.Write(count50);
            sw.Write(countMiss);
            sw.Write(maxCombo);
            sw.Write(spinnerBonusScore);
            sw.Write(comboBonusScore);
            sw.Write(hitScore);
        }
        #endregion
    }

    public enum Rank
    {
        N,
        D,
        C,
        B,
        A,
        S,
        SS
    }
}
