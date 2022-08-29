﻿using OpenTK;
using OpenTK.Graphics;
using osum.Audio;
using osum.GameModes.Play;
using osum.GameplayElements.Beatmaps;
using osum.GameplayElements.HitObjects.Osu;
using osum.Graphics;
using osum.Graphics.Sprites;
using osum.Helpers;
using osum.Input;
using osum.Support;
using System;

namespace osum.GameplayElements.HitObjects
{
    internal delegate void HitCircleDelegate(HitObject h);

    internal class HitObjectDummy : HitObject
    {
        public HitObjectDummy(int time)
        {
            StartTime = time;
        }

        protected override ScoreChange HitActionInitial()
        {
            throw new NotImplementedException();
        }

        public override bool IsVisible => throw new NotImplementedException();
    }

    public abstract class HitObject : pSpriteCollection, IComparable<HitObject>, IComparable<int>, IUpdateable
    {
        protected HitObjectManager m_HitObjectManager;

        public const int DIMMABLE_TAG = 1293;

        protected HitObject()
        {
        }

        public HitObject(HitObjectManager hitObjectManager, Vector2 position, int startTime, HitObjectSoundType soundType, bool newCombo, int comboOffset)
        {
            m_HitObjectManager = hitObjectManager;
            this.position = position;
            StartTime = startTime;
            EndTime = StartTime;
            SoundType = soundType;
            NewCombo = newCombo;
            ComboOffset = comboOffset;
        }

        #region General & Timing

        public int StartTime;
        public int EndTime;

        public int JudgeTime;

        internal ScoreChange hitValue;

        internal HitObjectType Type;

        internal int ComboOffset;

        internal HitObject connectedObject;
        internal pSprite connectionSprite;

        /// <summary>
        /// Do any arbitrary updates for this hitObject.
        /// </summary>
        public virtual void Update()
        {
            UpdateDimming();
        }

        private bool isDimmed;

        //todo: this is horribly memory inefficient.
        protected void UpdateDimming()
        {
            bool shouldDim = StartTime - Clock.AudioTime > m_HitObjectManager.FirstBeatLength;

            if (shouldDim != isDimmed)
            {
                isDimmed = shouldDim;

                if (isDimmed)
                {
                    foreach (pDrawable p in Sprites)
                        if (p.TagNumeric == DIMMABLE_TAG)
                            p.FadeColour(new Color4(0.7f, 0.7f, 0.7f, 1), 0, true);
                }
                else
                {
                    foreach (pDrawable p in Sprites)
                        if (p.TagNumeric == DIMMABLE_TAG)
                            p.FadeColour(Color4.White, (int)m_HitObjectManager.FirstBeatLength / 2);
                }
            }
        }

        public bool NewCombo { get; }

        private ClockTypes clocking;

        internal ClockTypes Clocking
        {
            get => clocking;
            set
            {
                if (value == clocking)
                    return;

                clocking = value;

                foreach (pDrawable d in Sprites)
                    d.Clocking = clocking;
            }
        }

        private Color4 colour;

        internal virtual Color4 Colour
        {
            get => colour;

            set => colour = value;
        }

        private int colour_index;

        internal virtual int ColourIndex
        {
            get => colour_index;
            set
            {
                if (value >= 4) throw new ArgumentOutOfRangeException();

                colour_index = value;
                Colour = TextureManager.DefaultColours[value];
            }
        }

        public virtual bool IsHit { get; set; }

        internal int ClockingNow => Sprites[0].ClockingNow;

        /// <summary>
        /// This will cause the hitObject to get hit and scored.
        /// </summary>
        /// <returns>
        /// A <see cref="ScoreChange"/> representing what action was taken.
        /// </returns>
        internal ScoreChange Hit()
        {
            if (Clock.AudioTimeInputAdjust < StartTime - DifficultyManager.HitWindow50 * 1.5f)
            {
                Shake();
                return ScoreChange.Ignore;
            }

            if (IsHit)
                return ScoreChange.Ignore;

            ScoreChange action = HitActionInitial();

            if (action != ScoreChange.Ignore)
            {
                IsHit = true;
                JudgeTime = Clock.AudioTime;
            }

            return action;
        }

        /// <summary>
        /// This is called every frame that this object is visible to pick up any intermediary scoring that is not associated with the initial hit.
        /// </summary>
        /// <returns></returns>
        internal virtual ScoreChange CheckScoring()
        {
            if (IsHit)
                return ScoreChange.Ignore;

            //check for miss
            if (Player.Autoplay)
            {
                if (ClockingNow > StartTime)
                    return Hit(); //force a "hit" if we haven't yet. todo: check if we ever get here
            }
            else if (Clock.AudioTimeInputAdjust > HittableEndTime)
                return Hit(); //force a "hit" if we haven't yet.

            return ScoreChange.Ignore;
        }

        internal SpriteManager usableSpriteManager
        {
            get
            {
                SpriteManager sm = m_HitObjectManager.ActiveStreamSpriteManager;
                if (sm != null) return sm;

                return m_HitObjectManager.spriteManager;
            }
        }

        /// <summary>
        /// Trigger a hit animation showing the score overlay above the object.
        /// </summary>
        internal virtual void HitAnimation(ScoreChange action, bool animateNumber = false)
        {
            if (m_HitObjectManager == null) return; //is the case for sliders start circles, where we don't want to display this stuff.

            float depth = this is Spinner ? SpriteManager.drawOrderBwd(EndTime - 4) : SpriteManager.drawOrderFwdPrio(EndTime + 4);

            OsuTexture texture = OsuTexture.None;

            switch (action & ScoreChange.HitValuesOnly)
            {
                case ScoreChange.Hit300g:
                    texture = OsuTexture.hit300g;
                    break;
                case ScoreChange.Hit300k:
                    texture = OsuTexture.hit300k;
                    break;
                case ScoreChange.Hit300:
                    texture = OsuTexture.hit300;
                    break;
                case ScoreChange.Hit100k:
                    texture = OsuTexture.hit100k;
                    break;
                case ScoreChange.Hit100:
                    texture = OsuTexture.hit100;
                    break;
                case ScoreChange.Hit50:
                    texture = OsuTexture.hit50;
                    break;
                case ScoreChange.Miss:
                    texture = OsuTexture.hit0;
                    break;
            }

            if (texture == OsuTexture.None)
                return;

            //Draw the hit value
            pSprite p =
                new pSprite(TextureManager.Load(texture),
                    FieldTypes.GamefieldSprites,
                    OriginTypes.Centre,
                    ClockTypes.Audio, EndPosition, depth, false, Color4.White);

            Sprites.Add(p);

            usableSpriteManager.Add(p);

            const int HitFadeIn = 120;
            const int HitFadeOutDuration = 400;
            const int HitFadeOutStart = 400;
            int now = p.ClockingNow;

            if (action > ScoreChange.Miss)
            {
                p.Transform(
                    new TransformationBounce(now, now + (HitFadeIn * 2), 1, 0.2f, 3));
                p.Transform(
                    new TransformationF(TransformationType.Fade, 1, 0,
                        now + HitFadeOutStart, now + HitFadeOutStart + HitFadeOutDuration));
            }
            else
            {
                p.Transform(
                    new TransformationF(TransformationType.Scale, 2, 1, now,
                        now + HitFadeIn));
                p.Transform(
                    new TransformationF(TransformationType.Fade, 1, 0, now + HitFadeOutStart,
                        now + HitFadeOutStart + HitFadeOutDuration));

                p.Transform(
                    new TransformationF(TransformationType.Rotation, 0,
                        (float)((GameBase.Random.NextDouble() - 0.5) * 0.2), now,
                        now + HitFadeIn));
            }
        }

        /// <summary>
        /// Internal judging of a Hit() call. Is only called after preliminary checks have been completed.
        /// </summary>
        /// <returns>
        /// A <see cref="ScoreChange"/>
        /// </returns>
        protected abstract ScoreChange HitActionInitial();

        internal virtual void Dispose()
        {
            StopSound();
        }

        /// <summary>
        /// Is this object currently within an active range?
        /// </summary>
        internal virtual bool IsActive => false;

        #endregion

        #region Drawing

        protected Vector2 position;

        public virtual Vector2 Position
        {
            get => position;
            set
            {
                Vector2 change = value - position;

                Sprites.ForEach(s => { s.Position += change; });
                position = value;
            }
        }

        public virtual Vector2 EndPosition => Position;

        /// <summary>
        /// For objects with two distinct endpoints, this will be the "far" one.
        /// </summary>
        public virtual Vector2 Position2 => Position;

        internal int StackCount;

        internal virtual int ComboNumber { get; set; }

        /// <summary>
        /// Id this hitObject visible at the current audio time?
        /// </summary>
        public abstract bool IsVisible { get; }

        #endregion

        #region Sound

        internal HitObjectSoundType SoundType;

        /// <summary>
        /// Whether to add this object's score to the counters (hit300 count etc.)
        /// </summary>
        public bool IsScorable = true;

        internal bool Whistle
        {
            get => (HitObjectSoundType.Whistle & SoundType) > 0;
            set
            {
                if (value)
                    SoundType |= HitObjectSoundType.Whistle;
                else
                    SoundType &= ~HitObjectSoundType.Whistle;
            }
        }


        internal bool Finish
        {
            get => (HitObjectSoundType.Finish & SoundType) > 0;
            set
            {
                if (value)
                    SoundType |= HitObjectSoundType.Finish;
                else
                    SoundType &= ~HitObjectSoundType.Finish;
            }
        }

        internal bool Clap
        {
            get => (HitObjectSoundType.Clap & SoundType) > 0;
            set
            {
                if (value)
                    SoundType |= HitObjectSoundType.Clap;
                else
                    SoundType &= ~HitObjectSoundType.Clap;
            }
        }

        internal virtual void PlaySound()
        {
            PlaySound(SoundType);
        }

        internal virtual void PlaySound(HitObjectSoundType type)
        {
            PlaySound(type, SampleSet);
        }

        internal virtual void PlaySound(HitObjectSoundType type, SampleSetInfo ssi)
        {
            if ((type & HitObjectSoundType.Finish) > 0)
                AudioEngine.PlaySample(OsuSamples.HitFinish, ssi.AdditionSampleSet, ssi.Volume);

            if ((type & HitObjectSoundType.Whistle) > 0)
                AudioEngine.PlaySample(OsuSamples.HitWhistle, ssi.AdditionSampleSet, ssi.Volume);

            if ((type & HitObjectSoundType.Clap) > 0)
                AudioEngine.PlaySample(OsuSamples.HitClap, ssi.AdditionSampleSet, ssi.Volume);

            AudioEngine.PlaySample(OsuSamples.HitNormal, ssi.SampleSet, ssi.Volume);
        }

        protected virtual float PositionalSound => Position.X / GameBase.GamefieldBaseSize.X - 0.5f;

        /// <summary>
        /// Gets the hittable end time (valid active object time for sliders etc. - used in taiko to extend when hits are valid).
        /// </summary>
        /// <value>The hittable end time.</value>
        internal virtual int HittableEndTime => EndTime + DifficultyManager.HitWindow50;

        /// <summary>
        /// Gets the hittable end time (valid active object time for sliders etc. - used in taiko to extend when hits are valid).
        /// </summary>
        /// <value>The hittable end time.</value>
        internal virtual int HittableStartTime => StartTime;

        #endregion

        #region IComparable<HitObject> Members

        public int CompareTo(HitObject other)
        {
            int c = StartTime.CompareTo(other.StartTime);
            if (c != 0) return c;

            if (NewCombo && !other.NewCombo) return -1;
            if (other.NewCombo && !NewCombo) return 1;

            return EndTime.CompareTo(other.EndTime);
        }

        public int CompareTo(int time)
        {
            return StartTime.CompareTo(time);
        }

        #endregion

        public virtual bool IncrementCombo => true;


        internal virtual void StopSound(bool done = true)
        {
        }

        internal virtual bool HitTestInitial(TrackingPoint tracking)
        {
            float radius = DifficultyManager.HitObjectRadiusSolidGamefieldHittable;

            return (StartTime - DifficultyManager.PreEmpt <= Clock.AudioTimeInputAdjust &&
                    StartTime + DifficultyManager.HitWindow50 >= Clock.AudioTimeInputAdjust &&
                    !IsHit &&
                    pMathHelper.DistanceSquared(tracking.GamefieldPosition, Position) <= radius * radius);
        }

        internal SampleSetInfo SampleSet = new SampleSetInfo { SampleSet = Beatmaps.SampleSet.Normal, CustomSampleSet = CustomSampleSet.Default, Volume = 1, AdditionSampleSet = Beatmaps.SampleSet.Normal };

        internal virtual void Shake()
        {
            foreach (pDrawable p in Sprites)
            {
                p.Transformations.RemoveAll(t => t.Type == TransformationType.OffsetX);

                const int shake_count = 6;
                const int shake_velocity = 8;
                const int shake_period = 40;

                for (int i = 0; i < shake_count; i++)
                {
                    int s = i == 0 ? 0 : shake_velocity;
                    if (i % 2 == 0) s = -s;

                    int e = i == shake_count - 1 ? 0 : -s;

                    p.Transform(new TransformationF(TransformationType.OffsetX, s, e, Clock.AudioTime + i * shake_period, Clock.AudioTime + (i + 1) * shake_period));
                }
            }

            if (connectedObject != null)
            {
                connectedObject.connectedObject = null;
                connectedObject.Shake();
                connectedObject.connectedObject = this;
            }
        }

        public override string ToString()
        {
            return Type + ": " + StartTime + "-" + EndTime + " stack:" + StackCount;
        }

        public virtual float HpMultiplier => 1;

        public virtual Vector2 TrackingPosition => Position;
    }

    [Flags]
    public enum HitObjectType
    {
        Circle = 1,
        Slider = 2,
        NewCombo = 4,
        NormalNewCombo = 5,
        SliderNewCombo = 6,
        Spinner = 8,
        ColourHax = 112,
        Hold = 128
    }

    [Flags]
    public enum HitObjectSoundType
    {
        None = 0,
        Normal = 1,
        Whistle = 2,
        Finish = 4,
        WhistleFinish = 6,
        Clap = 8
    }
}