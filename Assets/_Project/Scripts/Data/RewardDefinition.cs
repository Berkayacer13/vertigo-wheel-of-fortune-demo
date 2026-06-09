using UnityEngine;
using Wof.Domain;

namespace Wof.Data
{
    /// <summary>
    /// Designer-editable definition of one reward type (R2). Bridges to the pure
    /// Domain <see cref="Reward"/> struct via <see cref="ToReward"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Wof/Reward Definition", fileName = "reward_")]
    public class RewardDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private RewardKind kind;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;            // from demo_content
        [SerializeField] private int baseAmount = 1;
        [SerializeField] private RarityTier rarity = RarityTier.Tier1;
        [SerializeField] private WinVfx winVfx = WinVfx.Star;

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public RewardKind Kind => kind;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public int BaseAmount => baseAmount;
        public RarityTier Rarity => rarity;
        public WinVfx WinVfx => winVfx;

        /// <summary>
        /// Bridge SO -> pure Domain struct. IconKey is the asset name so a View can
        /// resolve the Sprite via an atlas/registry without the Domain knowing about UI.
        /// </summary>
        public Reward ToReward(int amount) => new Reward(Id, Kind, amount, icon ? icon.name : Id);

#if UNITY_EDITOR
        private void OnValidate() => baseAmount = Mathf.Max(0, baseAmount);
#endif
    }
}
