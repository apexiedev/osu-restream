//  HitFactoryOsu.cs
//  Author: Dean Herbert <pe@ppy.sh>
//  Copyright (c) 2010 2010 Dean Herbert

using OpenTK;
using osum.GameplayElements.Beatmaps;
using System.Collections.Generic;

namespace osum.GameplayElements.HitObjects.Osu
{
    internal class HitFactoryOsu : HitFactory
    {
        public HitFactoryOsu(HitObjectManager hitObjectMananager) : base(hitObjectMananager)
        {
        }

        internal override HitCircle CreateHitCircle(Vector2 startPosition, int startTime, bool newCombo,
            HitObjectSoundType soundType, int comboOffset)
        {
            return new HitCircle(hitObjectManager, startPosition, startTime, newCombo, comboOffset, soundType);
        }

        internal override Slider CreateSlider(Vector2 startPosition, int startTime, bool newCombo,
            HitObjectSoundType soundType, CurveTypes curveType, int repeatCount, double sliderLength, List<Vector2> sliderPoints, List<HitObjectSoundType> soundTypes, int comboOffset, double velocity, double tickDistance, List<SampleSetInfo> sampleSets)
        {
            return new Slider(hitObjectManager, startPosition, startTime, newCombo, comboOffset, soundType, curveType, repeatCount, sliderLength, sliderPoints, soundTypes, velocity, tickDistance, sampleSets);
        }

        internal override Spinner CreateSpinner(int startTime, int endTime, HitObjectSoundType soundType)
        {
            return new Spinner(hitObjectManager, startTime, endTime, soundType);
        }

        internal override HoldCircle CreateHoldCircle(Vector2 pos, int time, bool newCombo, HitObjectSoundType soundType, int repeatCount, double length, List<HitObjectSoundType> sounds, int comboOffset, double velocity, double tickDistance, List<SampleSetInfo> sampleSets)
        {
            return new HoldCircle(hitObjectManager, pos, time, newCombo, comboOffset, soundType, length, repeatCount, sounds, velocity, tickDistance, sampleSets);
        }
    }
}