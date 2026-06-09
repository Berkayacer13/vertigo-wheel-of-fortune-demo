using System;
using System.Collections.Generic;
using UnityEngine;
using Wof.Domain;

namespace Wof.Data
{
    /// <summary>
    /// The changeable wheel (R2): a tier plus the per-slice content. Edited through a
    /// custom inspector (see Wof.Editor.WheelConfigEditor). OnValidate warns the
    /// designer the moment the data violates a rule.
    /// </summary>
    [CreateAssetMenu(menuName = "Wof/Wheel Config", fileName = "wheel_")]
    public class WheelConfig : ScriptableObject
    {
        [Serializable]
        public class SliceEntry
        {
            public RewardDefinition reward;      // null allowed only when isBomb == true
            [Min(0f)] public float weight = 1f;  // relative landing probability
            public bool isBomb;
        }

        [SerializeField] private WheelTier tier = WheelTier.Bronze;
        [SerializeField, Min(2)] private int sliceCount = 8;   // matches 8-chamber revolver art
        [SerializeField] private List<SliceEntry> slices = new List<SliceEntry>();

        public WheelTier Tier => tier;
        public int SliceCount => sliceCount;
        public IReadOnlyList<SliceEntry> Slices => slices;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (slices.Count != sliceCount)
                Debug.LogWarning($"[{name}] slice list ({slices.Count}) != sliceCount ({sliceCount}).", this);

            int bombs = slices.FindAll(s => s != null && s.isBomb).Count;
            bool wantsBomb = tier == WheelTier.Bronze;   // only normal wheels carry a bomb
            if (wantsBomb && bombs != 1)
                Debug.LogWarning($"[{name}] Normal wheel must have exactly 1 bomb (found {bombs}).", this);
            if (!wantsBomb && bombs != 0)
                Debug.LogWarning($"[{name}] Safe/Super wheel must have 0 bombs (found {bombs}).", this);
        }
#endif
    }
}
