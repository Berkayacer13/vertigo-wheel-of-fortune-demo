using System.Collections.Generic;
using Wof.Domain;

namespace Wof.Application
{
    /// <summary>
    /// What a cash-out actually paid, totalled by the one method that did the paying. The
    /// cash-out screen used to re-sort the banked rewards itself and had to mirror
    /// <see cref="EconomyService.Bank"/> by hand — one missed rule and the summary would
    /// promise loot the player does not keep.
    /// </summary>
    public readonly struct BankReceipt
    {
        public readonly uint Gold;
        public readonly uint Cash;

        /// <summary>Reward entries moved into the permanent inventory.</summary>
        public readonly int Items;

        /// <summary>Everything that left the run wallet, in wallet order.</summary>
        public readonly IReadOnlyList<Reward> Rewards;

        public BankReceipt(uint gold, uint cash, int items, IReadOnlyList<Reward> rewards)
        {
            Gold = gold;
            Cash = cash;
            Items = items;
            Rewards = rewards;
        }
    }
}
