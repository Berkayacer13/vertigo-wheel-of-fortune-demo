using System.Collections.Generic;
using UnityEngine;

namespace Wof.Presentation
{
    /// <summary>
    /// Maps a string key (asset name) to a Sprite. This is how the UI-free Domain
    /// (which only carries an IconKey) gets resolved to an actual sprite at render time,
    /// ideally backed by a Sprite Atlas.
    /// </summary>
    [CreateAssetMenu(menuName = "Wof/Sprite Registry", fileName = "sprite_registry")]
    public sealed class SpriteRegistry : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public string key;
            public Sprite sprite;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        private Dictionary<string, Sprite> _lookup;

        public Sprite Resolve(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            if (_lookup == null)
            {
                _lookup = new Dictionary<string, Sprite>(entries.Count);
                foreach (var e in entries)
                    if (!string.IsNullOrEmpty(e.key) && e.sprite != null)
                        _lookup[e.key] = e.sprite;
            }

            return _lookup.TryGetValue(key, out var sprite) ? sprite : null;
        }

#if UNITY_EDITOR
        /// <summary>Editor helper: drop sprites in and auto-fill keys from their asset names.</summary>
        [ContextMenu("Fill Keys From Sprite Names")]
        private void FillKeysFromSpriteNames()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e.sprite != null) e.key = e.sprite.name;
                entries[i] = e;
            }
            _lookup = null;
        }
#endif
    }
}
