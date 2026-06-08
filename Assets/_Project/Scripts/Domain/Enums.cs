namespace Wof.Domain
{
    /// <summary>Visual/behavioural tier of a wheel. Drives art swap and bomb presence.</summary>
    public enum WheelTier { Bronze, Silver, Golden }

    /// <summary>Resolved type of a zone (R5/R6). Bronze=Normal, Silver=Safe, Golden=Super.</summary>
    public enum ZoneType { Normal, Safe, Super }

    /// <summary>What a slice pays out. <see cref="Bomb"/> is the special "wipe" slice.</summary>
    public enum RewardKind { Gold, Cash, WeaponSkin, Consumable, Chest, Points, SpecialSkin, Bomb }

    /// <summary>Rarity bucket used to gate which rewards appear in early vs. late zones.</summary>
    public enum RarityTier { Tier1, Tier2, Tier3, Special }

    /// <summary>Which win VFX a reward triggers on the popup.</summary>
    public enum WinVfx { None, Star, GoldenShine }

    /// <summary>The single source of truth for the screen flow (mirrors the state machine).</summary>
    public enum GamePhase { Boot, ZoneIntro, Idle, Spinning, Resolving, Reward, BombExploded, CashOut, GameOver }
}
