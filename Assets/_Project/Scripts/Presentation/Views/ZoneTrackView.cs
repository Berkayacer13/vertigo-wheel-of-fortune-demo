using UnityEngine;
using Wof.Domain;

namespace Wof.Presentation
{
    /// <summary>
    /// Top progress strip: a sliding, screen-centred window of zone numbers. The active zone
    /// is highlighted and kept centred, safe (×5) and super (×30) zones wear a coloured chip,
    /// and zones already behind the player are dimmed. The window clamps so it never shows a
    /// zone below 1, so early zones read 1,2,3,… like the reference card game.
    /// </summary>
    public sealed class ZoneTrackView : UiView
    {
        [SerializeField] private ZoneCell[] cells;          // ui_zone_cell_*
        [SerializeField] private int safeInterval = 5;
        [SerializeField] private int superInterval = 30;

        // The row is sized off the screen rather than pinned to constants: it is the player's
        // only read on how far they have come, and at a fixed 528 units it was a caption on a
        // 1080-wide phone. The pitch cap stops it ballooning on a 4:3 canvas, which is 1440
        // units wide for the same 7 chips.
        private const float WidthShare = 0.80f;
        private const float MaxPitch = 132f;
        private const float CellShare = 0.84f;   // chip size as a fraction of the pitch
        private const float FontShare = 0.44f;
        private const float BarPadding = 22f;

        private float _laidOutWidth;

        public void SetZone(int zone)
        {
            if (cells == null || cells.Length == 0) return;

            int start = Mathf.Max(1, zone - cells.Length / 2);
            for (int i = 0; i < cells.Length; i++)
            {
                int n = start + i;
                var type = ZoneRules.Resolve(n, safeInterval, superInterval);
                Color accent = ColorFor(type);
                // spent zones fade back so the row reads as progress, not just a number line
                if (n < zone) accent = UiPalette.Dim(accent, 0.5f);
                cells[i].Set(n, accent, type != ZoneType.Normal, n == zone);
            }
        }

        /// <summary>
        /// Re-space the row for the width actually available. Same reasoning as the wheel:
        /// the canvas is 1080 units wide in portrait on a tall phone but 1440 on 4:3, so a
        /// row laid out in constants is centred on one and stranded on the other.
        /// </summary>
        private void Update()
        {
            if (cells == null || cells.Length == 0) return;

            var parent = transform.parent as RectTransform;
            if (parent == null) return;

            float width = parent.rect.width;
            if (width <= 0f || Mathf.Abs(width - _laidOutWidth) < 1f) return;
            _laidOutWidth = width;

            float pitch = Mathf.Min(width * WidthShare / cells.Length, MaxPitch);
            float size = pitch * CellShare;
            float font = size * FontShare;

            ((RectTransform)transform).sizeDelta = new Vector2(pitch * cells.Length, size + BarPadding);

            float startX = -pitch * (cells.Length - 1) * 0.5f;
            for (int i = 0; i < cells.Length; i++)
                cells[i].SetGeometry(new Vector2(startX + i * pitch, 0f), size, font);
        }

        private static Color ColorFor(ZoneType type) => type switch
        {
            ZoneType.Super => UiPalette.Gold,
            ZoneType.Safe => UiPalette.SafeGreen,
            _ => UiPalette.ZoneNeutral,
        };

#if UNITY_EDITOR
        protected override void AutoWire()
        {
            if (cells == null || cells.Length == 0)
                cells = GetComponentsInChildren<ZoneCell>(true);
        }
#endif
    }
}
