using UnityEngine;
using Wof.Domain;

namespace Wof.Presentation
{
    /// <summary>
    /// Top progress strip: a sliding, screen-centred window of zone numbers (replaces the
    /// old "ZONE n" label). The active zone is highlighted and kept centred; safe (×5) and
    /// super (×30) zones are colour-coded (R5/R6). The window clamps so it never shows a
    /// zone below 1, so early zones read 1,2,3,… like the reference card game.
    /// </summary>
    public sealed class ZoneTrackView : UiView
    {
        [SerializeField] private ZoneCell[] cells;          // ui_zone_cell_*
        [SerializeField] private int safeInterval = 5;
        [SerializeField] private int superInterval = 30;

        private static readonly Color NormalColor = new Color(0.82f, 0.84f, 0.88f);
        private static readonly Color SafeColor   = new Color(0.45f, 0.92f, 0.36f);
        private static readonly Color SuperColor  = new Color(1f, 0.78f, 0.18f);

        public void SetZone(int zone)
        {
            if (cells == null || cells.Length == 0) return;
            int start = Mathf.Max(1, zone - cells.Length / 2);
            for (int i = 0; i < cells.Length; i++)
            {
                int n = start + i;
                cells[i].Set(n, ColorFor(n), n == zone);
            }
        }

        private Color ColorFor(int zone) => ZoneRules.Resolve(zone, safeInterval, superInterval) switch
        {
            ZoneType.Super => SuperColor,
            ZoneType.Safe => SafeColor,
            _ => NormalColor,
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
