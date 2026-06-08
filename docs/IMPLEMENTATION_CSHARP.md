# Wheel of Fortune — C# Implementation Guide

> Companion to [ROADMAP.md](ROADMAP.md). This file turns every system in the roadmap into **concrete Unity 2021 LTS C#**: class layout, full code for the core, method signatures for the rest, and the reasoning behind each choice.
>
> Read order: **§1 assemblies → §2 enums → §3 data (SO) → §4 domain → §5 events → §6 services → §7 state machine → §8 views → §9 editor → §10 tests**. That's also dependency order (each layer only references the ones above it).

---

## 0. Mental model (how the pieces talk)

```
        Unity Editor                         Pure C# (no Unity UI)            Unity MonoBehaviours
   ┌────────────────────┐   reads    ┌──────────────────────────┐   events   ┌────────────────────┐
   │ ScriptableObjects  │──────────► │ Domain + Services + FSM   │──────────► │ Views (UI)         │
   │ (WheelConfig …)    │            │ (the rules, fully testable)│ ◄────────  │ (buttons → calls) │
   └────────────────────┘            └──────────────────────────┘  calls      └────────────────────┘
```

**Golden rule:** Domain & Services never call `UnityEngine.UI`. Views never decide rules — they raise input and render state. They meet only through **C# events** (logic→UI) and **method calls** (UI→logic). This is what makes it testable and SOLID.

---

## 1. Assembly Definitions (enforce the layering)

Create one `.asmdef` per layer. The compiler then *physically prevents* Domain from referencing UI — the architecture can't rot.

```
Scripts/Domain/Wof.Domain.asmdef            → references: (none)
Scripts/Data/Wof.Data.asmdef                → references: Wof.Domain
Scripts/Application/Wof.Application.asmdef   → references: Wof.Domain, Wof.Data
Scripts/Presentation/Wof.Presentation.asmdef→ references: Wof.Domain, Wof.Data, Wof.Application, DOTween
Scripts/Editor/Wof.Editor.asmdef            → references: all above (Editor-only: "Include Platforms = Editor")
Tests/Wof.Tests.asmdef                      → references: Wof.Domain, Wof.Data (+ "UnityEngine.TestRunner","UnityEditor.TestRunner")
```

Example `Wof.Domain.asmdef`:
```json
{
  "name": "Wof.Domain",
  "references": [],
  "autoReferenced": true,
  "noEngineReferences": false
}
```
> `Wof` = "Wheel of Fortune" root namespace. Every file starts with `namespace Wof.<Layer>`.

---

## 2. Enums & small value types (`Domain/Enums.cs`)

```csharp
namespace Wof.Domain
{
    public enum WheelTier   { Bronze, Silver, Golden }
    public enum ZoneType    { Normal, Safe, Super }
    public enum RewardKind  { Gold, Cash, WeaponSkin, Consumable, Chest, Points, SpecialSkin, Bomb }
    public enum RarityTier  { Tier1, Tier2, Tier3, Special }
    public enum WinVfx      { None, Star, GoldenShine }

    public enum GamePhase   { Boot, ZoneIntro, Idle, Spinning, Resolving, Reward, BombExploded, CashOut, GameOver }
}
```

`Reward` — a plain, immutable result the wallet stores (decoupled from the SO so logic is testable without assets):

```csharp
namespace Wof.Domain
{
    public readonly struct Reward
    {
        public readonly string  Id;
        public readonly RewardKind Kind;
        public readonly int     Amount;
        public readonly string  IconKey;   // sprite lookup key, not a Sprite (keeps Domain UI-free)

        public Reward(string id, RewardKind kind, int amount, string iconKey)
        {
            Id = id; Kind = kind; Amount = amount; IconKey = iconKey;
        }

        public Reward WithAmount(int amount) => new Reward(Id, Kind, amount, IconKey);
        public bool IsBomb => Kind == RewardKind.Bomb;
    }
}
```

---

## 3. Data layer — ScriptableObjects (`Data/`)

### 3.1 `RewardDefinition.cs` (R2 designer-editable reward)
```csharp
using UnityEngine;
using Wof.Domain;

namespace Wof.Data
{
    [CreateAssetMenu(menuName = "Wof/Reward Definition", fileName = "reward_")]
    public class RewardDefinition : ScriptableObject
    {
        [SerializeField] private string     id;
        [SerializeField] private RewardKind kind;
        [SerializeField] private string     displayName;
        [SerializeField] private Sprite     icon;          // ← demo_content render
        [SerializeField] private int        baseAmount = 1;
        [SerializeField] private RarityTier rarity = RarityTier.Tier1;
        [SerializeField] private WinVfx     winVfx = WinVfx.Star;

        public string     Id          => string.IsNullOrEmpty(id) ? name : id;
        public RewardKind Kind        => kind;
        public string     DisplayName => displayName;
        public Sprite     Icon        => icon;
        public int        BaseAmount  => baseAmount;
        public RarityTier Rarity      => rarity;
        public WinVfx     WinVfx      => winVfx;

        // Bridge SO → pure Domain struct. IconKey = asset name so the View can resolve the Sprite via an atlas/registry.
        public Reward ToReward(int amount) => new Reward(Id, Kind, amount, icon ? icon.name : Id);

#if UNITY_EDITOR
        private void OnValidate() => baseAmount = Mathf.Max(0, baseAmount);
#endif
    }
}
```

### 3.2 `WheelConfig.cs` (the changeable wheel — R2)
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using Wof.Domain;

namespace Wof.Data
{
    [CreateAssetMenu(menuName = "Wof/Wheel Config", fileName = "wheel_")]
    public class WheelConfig : ScriptableObject
    {
        [Serializable]
        public class SliceEntry
        {
            public RewardDefinition reward;            // null allowed only when isBomb == true
            [Min(0f)] public float weight = 1f;        // relative landing probability
            public bool isBomb;
        }

        [SerializeField] private WheelTier tier = WheelTier.Bronze;
        [SerializeField, Min(2)] private int sliceCount = 8;     // matches 8-chamber revolver art
        [SerializeField] private List<SliceEntry> slices = new List<SliceEntry>();

        public WheelTier Tier        => tier;
        public int       SliceCount  => sliceCount;
        public IReadOnlyList<SliceEntry> Slices => slices;

#if UNITY_EDITOR
        // R2 + brief: validate the designer's data the moment they edit it.
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
```

### 3.3 `ZoneTuning.cs`
```csharp
using UnityEngine;

namespace Wof.Data
{
    [CreateAssetMenu(menuName = "Wof/Zone Tuning", fileName = "zone_tuning")]
    public class ZoneTuning : ScriptableObject
    {
        [Min(1)] public int safeInterval  = 5;
        [Min(1)] public int superInterval = 30;

        [Tooltip("Reward multiplier as a function of zone (R4: 'better every zone').")]
        public AnimationCurve rewardGrowth =
            AnimationCurve.Linear(1, 1f, 100, 10f);

        public WheelConfig normalWheel;
        public WheelConfig safeWheel;
        public WheelConfig superWheel;
    }
}
```

### 3.4 `GameSettings.cs`
```csharp
using DG.Tweening;
using UnityEngine;

namespace Wof.Data
{
    [CreateAssetMenu(menuName = "Wof/Game Settings", fileName = "game_settings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Spin")]
        public float spinDuration   = 3.5f;
        public int   fullRotations  = 5;            // whole turns before landing
        public Ease  spinEase       = Ease.OutCubic;

        [Header("Economy")]
        public int startingGold     = 100;
        public int reviveGoldCost   = 25;           // matches the brief screenshot
        public int startingCash     = 0;
    }
}
```

---

## 4. Domain layer — pure rules (`Domain/`, no UnityEngine.UI)

### 4.1 `ZoneRules.cs` — the most-tested file (R5/R6, and the ×30-before-×5 trap)
```csharp
namespace Wof.Domain
{
    public static class ZoneRules
    {
        public static ZoneType Resolve(int zone, int safeInterval = 5, int superInterval = 30)
        {
            if (zone <= 0) return ZoneType.Normal;
            if (zone % superInterval == 0) return ZoneType.Super; // check Super FIRST (30 is also %5==0)
            if (zone % safeInterval  == 0) return ZoneType.Safe;
            return ZoneType.Normal;
        }

        public static bool HasBomb(int zone, int safe = 5, int super = 30)
            => Resolve(zone, safe, super) == ZoneType.Normal;

        public static bool CanLeave(int zone, int safe = 5, int super = 30)
            => Resolve(zone, safe, super) != ZoneType.Normal;   // leavable only on Safe/Super (R7)

        public static WheelTier TierFor(ZoneType type) => type switch
        {
            ZoneType.Super => WheelTier.Golden,
            ZoneType.Safe  => WheelTier.Silver,
            _              => WheelTier.Bronze,
        };
    }
}
```

### 4.2 `ISliceSelector.cs` + `WeightedSliceSelector.cs` (Strategy + testable RNG)
```csharp
using System;
using System.Collections.Generic;

namespace Wof.Domain
{
    public interface ISliceSelector
    {
        int PickLandingIndex(IReadOnlyList<float> weights);
    }

    public sealed class WeightedSliceSelector : ISliceSelector
    {
        private readonly Random _rng;
        public WeightedSliceSelector(int? seed = null)
            => _rng = seed.HasValue ? new Random(seed.Value) : new Random();

        public int PickLandingIndex(IReadOnlyList<float> weights)
        {
            float total = 0f;
            for (int i = 0; i < weights.Count; i++) total += Math.Max(0f, weights[i]);
            if (total <= 0f) return _rng.Next(weights.Count);   // all-zero guard → uniform

            double roll = _rng.NextDouble() * total;
            for (int i = 0; i < weights.Count; i++)
            {
                roll -= Math.Max(0f, weights[i]);
                if (roll <= 0d) return i;
            }
            return weights.Count - 1;
        }
    }
}
```
> Injecting a `seed` makes the distribution **unit-testable** (run 100k picks, assert frequencies match weights). The View never touches RNG — it only animates to the index the Domain already chose.

### 4.3 `WheelModel.cs` — the runtime wheel built from config
```csharp
using System.Collections.Generic;

namespace Wof.Domain
{
    public sealed class WheelModel
    {
        public WheelTier Tier { get; }
        public IReadOnlyList<WheelSlice> Slices { get; }
        public int SliceCount => Slices.Count;

        public WheelModel(WheelTier tier, IReadOnlyList<WheelSlice> slices)
        { Tier = tier; Slices = slices; }

        public float SliceAngle => 360f / SliceCount;
        public WheelSlice SliceAt(int index) => Slices[index];

        public IReadOnlyList<float> Weights()
        {
            var w = new float[SliceCount];
            for (int i = 0; i < SliceCount; i++) w[i] = Slices[i].Weight;
            return w;
        }
    }

    public sealed class WheelSlice
    {
        public Reward Reward { get; }
        public float  Weight { get; }
        public bool   IsBomb => Reward.IsBomb;
        public WheelSlice(Reward reward, float weight) { Reward = reward; Weight = weight; }
    }
}
```

### 4.4 `RewardScaler.cs` (R4 "better every zone")
```csharp
namespace Wof.Domain
{
    public sealed class RewardScaler
    {
        private readonly System.Func<int, float> _multiplier; // injected from ZoneTuning.rewardGrowth.Evaluate

        public RewardScaler(System.Func<int, float> multiplier) => _multiplier = multiplier;

        public Reward Scale(Reward baseReward, int zone)
        {
            if (baseReward.IsBomb) return baseReward;
            int scaled = UnityMathRound(baseReward.Amount * _multiplier(zone));
            return baseReward.WithAmount(scaled);
        }

        private static int UnityMathRound(float v) => (int)System.Math.Round(v, System.MidpointRounding.AwayFromZero);
    }
}
```

### 4.5 `RewardWallet.cs` (R4/R8/R10 accumulate · wipe · bank)
```csharp
using System.Collections.Generic;

namespace Wof.Domain
{
    public sealed class RewardWallet
    {
        private readonly List<Reward> _runRewards = new List<Reward>();   // this run; lost on bomb
        public IReadOnlyList<Reward> RunRewards => _runRewards;
        public bool HasRewards => _runRewards.Count > 0;

        public void Add(Reward reward) => _runRewards.Add(reward);        // (R4)

        public void DetonateBomb() => _runRewards.Clear();               // (R8)

        // (R10) caller persists the returned list into permanent inventory, then we clear the run.
        public IReadOnlyList<Reward> CashOut()
        {
            var banked = new List<Reward>(_runRewards);
            _runRewards.Clear();
            return banked;
        }
    }
}
```

---

## 5. Events (`Application/GameEvents.cs`) — the one-way logic→UI channel

```csharp
using System;
using Wof.Domain;

namespace Wof.Application
{
    // Lightweight typed event hub. Views subscribe in OnEnable, unsubscribe in OnDisable.
    public sealed class GameEvents
    {
        public event Action<int, ZoneType>     ZoneChanged;     // zone, type
        public event Action<WheelModel>        WheelBuilt;
        public event Action                    SpinStarted;
        public event Action<int>               SpinLandedOnIndex;
        public event Action<Reward>            RewardWon;
        public event Action                    BombExploded;
        public event Action<int>               WalletChanged;   // run reward count
        public event Action<int, int>          CurrencyChanged; // gold, cash
        public event Action<GamePhase>         PhaseChanged;

        public void RaiseZoneChanged(int z, ZoneType t) => ZoneChanged?.Invoke(z, t);
        public void RaiseWheelBuilt(WheelModel w)       => WheelBuilt?.Invoke(w);
        public void RaiseSpinStarted()                  => SpinStarted?.Invoke();
        public void RaiseSpinLanded(int i)              => SpinLandedOnIndex?.Invoke(i);
        public void RaiseRewardWon(Reward r)            => RewardWon?.Invoke(r);
        public void RaiseBombExploded()                 => BombExploded?.Invoke();
        public void RaiseWalletChanged(int n)           => WalletChanged?.Invoke(n);
        public void RaiseCurrencyChanged(int g, int c)  => CurrencyChanged?.Invoke(g, c);
        public void RaisePhaseChanged(GamePhase p)      => PhaseChanged?.Invoke(p);
    }
}
```

---

## 6. Services (`Application/Services/`)

### 6.1 `WheelBuilder.cs` (Factory: SO config → `WheelModel`)
```csharp
using System.Collections.Generic;
using Wof.Data;
using Wof.Domain;

namespace Wof.Application
{
    public sealed class WheelBuilder
    {
        private readonly ZoneTuning _tuning;
        public WheelBuilder(ZoneTuning tuning) => _tuning = tuning;

        public WheelModel BuildForZone(int zone)
        {
            var type = ZoneRules.Resolve(zone, _tuning.safeInterval, _tuning.superInterval);
            WheelConfig cfg = type switch
            {
                ZoneType.Super => _tuning.superWheel,
                ZoneType.Safe  => _tuning.safeWheel,
                _              => _tuning.normalWheel,
            };

            var slices = new List<WheelSlice>(cfg.SliceCount);
            foreach (var e in cfg.Slices)
            {
                Reward r = e.isBomb
                    ? new Reward("bomb", RewardKind.Bomb, 0, "ui_card_icon_death")
                    : e.reward.ToReward(e.reward.BaseAmount);
                slices.Add(new WheelSlice(r, e.weight));
            }
            return new WheelModel(ZoneRules.TierFor(type), slices);
        }
    }
}
```

### 6.2 `EconomyService.cs` (currency + wallet façade, fires events)
```csharp
using Wof.Domain;

namespace Wof.Application
{
    public sealed class EconomyService
    {
        private readonly GameEvents _events;
        private readonly RewardWallet _wallet = new RewardWallet();

        public int Gold { get; private set; }
        public int Cash { get; private set; }
        public RewardWallet Wallet => _wallet;

        public EconomyService(GameEvents events, int startGold, int startCash)
        { _events = events; Gold = startGold; Cash = startCash; }

        public void AddRunReward(Reward r)
        {
            _wallet.Add(r);
            _events.RaiseWalletChanged(_wallet.RunRewards.Count);
        }

        public void Wipe()
        {
            _wallet.DetonateBomb();
            _events.RaiseWalletChanged(0);
        }

        public void Bank()  // R10
        {
            foreach (var r in _wallet.CashOut())
                if (r.Kind == RewardKind.Gold) AddGold(r.Amount);
                else if (r.Kind == RewardKind.Cash) AddCash(r.Amount);
            _events.RaiseWalletChanged(0);
        }

        public bool TrySpendGold(int cost)   // R9
        {
            if (Gold < cost) return false;
            Gold -= cost; _events.RaiseCurrencyChanged(Gold, Cash); return true;
        }

        public void AddGold(int n) { Gold += n; _events.RaiseCurrencyChanged(Gold, Cash); }
        public void AddCash(int n) { Cash += n; _events.RaiseCurrencyChanged(Gold, Cash); }
    }
}
```

### 6.3 `GameContext.cs` (lite DI container — wires everything once)
```csharp
using Wof.Data;
using Wof.Domain;

namespace Wof.Application
{
    public sealed class GameContext
    {
        public GameEvents     Events  { get; }
        public ZoneTuning     Tuning  { get; }
        public GameSettings   Settings{ get; }
        public WheelBuilder   Builder { get; }
        public EconomyService Economy { get; }
        public ISliceSelector Selector{ get; }
        public RewardScaler   Scaler  { get; }

        public int       Zone { get; private set; } = 1;
        public WheelModel CurrentWheel { get; private set; }

        public GameContext(ZoneTuning tuning, GameSettings settings, ISliceSelector selector = null)
        {
            Tuning   = tuning;
            Settings = settings;
            Events   = new GameEvents();
            Builder  = new WheelBuilder(tuning);
            Economy  = new EconomyService(Events, settings.startingGold, settings.startingCash);
            Selector = selector ?? new WeightedSliceSelector();
            Scaler   = new RewardScaler(z => tuning.rewardGrowth.Evaluate(z));
        }

        public void SetZone(int zone)
        {
            Zone = zone;
            CurrentWheel = Builder.BuildForZone(zone);
            var type = ZoneRules.Resolve(zone, Tuning.safeInterval, Tuning.superInterval);
            Events.RaiseZoneChanged(zone, type);
            Events.RaiseWheelBuilt(CurrentWheel);
        }

        public void NextZone() => SetZone(Zone + 1);
        public void ResetRun() { Economy.Wipe(); SetZone(1); }
    }
}
```

---

## 7. State machine (`Application/States/`)

### 7.1 Contract + driver
```csharp
namespace Wof.Application
{
    public interface IGameState
    {
        void Enter();
        void Tick(float dt) {}   // C# 8 default impl → states override only if needed
        void Exit();
    }

    public sealed class GameStateMachine
    {
        public IGameState Current { get; private set; }

        public void Change(IGameState next)
        {
            Current?.Exit();
            Current = next;
            Current?.Enter();
        }

        public void Tick(float dt) => Current?.Tick(dt);
    }
}
```

### 7.2 A representative state — `SpinningState` (the core moment)
```csharp
using Wof.Domain;

namespace Wof.Application
{
    public sealed class SpinningState : IGameState
    {
        private readonly GameContext _ctx;
        private readonly GameStateMachine _fsm;
        private readonly System.Func<int, System.Threading.Tasks.Task> _animateSpin; // View hook

        public SpinningState(GameContext ctx, GameStateMachine fsm,
                             System.Func<int, System.Threading.Tasks.Task> animateSpin)
        { _ctx = ctx; _fsm = fsm; _animateSpin = animateSpin; }

        public async void Enter()
        {
            _ctx.Events.RaisePhaseChanged(GamePhase.Spinning);

            // 1) Decide the result up-front (pure logic, testable).
            int index = _ctx.Selector.PickLandingIndex(_ctx.CurrentWheel.Weights());

            // 2) Let the View animate the cylinder to that index (DOTween) and await it.
            _ctx.Events.RaiseSpinStarted();
            await _animateSpin(index);
            _ctx.Events.RaiseSpinLanded(index);

            // 3) Hand off to resolution.
            _fsm.Change(new ResolvingState(_ctx, _fsm, index));
        }

        public void Exit() {}
    }
}
```

### 7.3 `ResolvingState` — reward vs bomb fork
```csharp
namespace Wof.Application
{
    public sealed class ResolvingState : IGameState
    {
        private readonly GameContext _ctx; private readonly GameStateMachine _fsm; private readonly int _index;
        public ResolvingState(GameContext ctx, GameStateMachine fsm, int index)
        { _ctx = ctx; _fsm = fsm; _index = index; }

        public void Enter()
        {
            _ctx.Events.RaisePhaseChanged(GamePhase.Resolving);
            var slice = _ctx.CurrentWheel.SliceAt(_index);

            if (slice.IsBomb)
            {
                _fsm.Change(new BombExplodedState(_ctx, _fsm));
            }
            else
            {
                var scaled = _ctx.Scaler.Scale(slice.Reward, _ctx.Zone);
                _ctx.Economy.AddRunReward(scaled);
                _ctx.Events.RaiseRewardWon(scaled);
                _fsm.Change(new RewardState(_ctx, _fsm));   // shows popup, then NextZone on Collect
            }
        }
        public void Exit() {}
    }
}
```

### 7.4 The rest — signatures (same shape, kept short)
```csharp
BootState        : load atlas/registry, Economy starting currency → fsm.Change(ZoneIntro(zone=1))
ZoneIntroState   : ctx.SetZone(zone); play zone-strip anim → fsm.Change(Idle)
IdleState        : enable SPIN; enable LEAVE iff ZoneRules.CanLeave(zone); waits for input
                   OnSpin()  → fsm.Change(Spinning)
                   OnLeave() → fsm.Change(CashOut)
RewardState      : wait for Collect tap → ctx.NextZone(); fsm.Change(ZoneIntro)
BombExplodedState: show revive screen
                   OnReviveGold() → if Economy.TrySpendGold(cost) fsm.Change(Idle)   // rewards kept (R9)
                   OnReviveAd()   → (ad ok) fsm.Change(Idle)
                   OnGiveUp()     → Economy.Wipe(); fsm.Change(GameOver)              // (R8)
CashOutState     : Economy.Bank(); show collected list → fsm.Change(GameOver/Menu)   // (R10)
GameOverState    : OnRestart() → ctx.ResetRun(); fsm.Change(ZoneIntro)
```

> Note the **input methods** (`OnSpin`, `OnLeave`, `OnReviveGold`…) live on the states. A thin `GameController` MonoBehaviour forwards button clicks to `fsm.Current as IXxxInput`. This keeps Unity out of the logic while still letting buttons drive it.

---

## 8. Presentation (`Presentation/Views/`) — MonoBehaviours

### 8.1 View base + OnValidate auto-wiring (brief: refs set from OnValidate, no editor OnClick)
```csharp
using UnityEngine;

namespace Wof.Presentation
{
    public abstract class UiView : MonoBehaviour
    {
        // Helper: find a child by exact name and grab a component — used inside OnValidate.
        protected T Bind<T>(ref T field, string childName) where T : Component
        {
            if (field == null)
            {
                var t = transform.FindDeep(childName);
                if (t != null) field = t.GetComponent<T>();
            }
            return field;
        }

#if UNITY_EDITOR
        protected virtual void OnValidate() => AutoWire();
        protected abstract void AutoWire();   // each view binds its own children here
#endif
    }
}
```

`TransformExtensions.FindDeep` (recursive find by name, `Utils/`):
```csharp
using UnityEngine;
namespace Wof.Presentation
{
    public static class TransformExtensions
    {
        public static Transform FindDeep(this Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = root.GetChild(i).FindDeep(name);
                if (r != null) return r;
            }
            return null;
        }
    }
}
```

### 8.2 `WheelView.cs` — auto-wire + DOTween spin (the centerpiece)
```csharp
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wof.Data;
using Wof.Domain;

namespace Wof.Presentation
{
    public sealed class WheelView : UiView
    {
        // ---- auto-wired refs (set in OnValidate, never dragged by hand) ----
        [SerializeField] private RectTransform rotor;        // child "ui_image_spin_rotor" (animator lives here, NOT root)
        [SerializeField] private Image  spinBronze;          // ui_image_spin_bronze
        [SerializeField] private Image  spinSilver;          // ui_image_spin_silver
        [SerializeField] private Image  spinGolden;          // ui_image_spin_golden
        [SerializeField] private Image  indicator;           // ui_image_spin_indicator
        [SerializeField] private Button spinButton;          // ui_button_spin
        [SerializeField] private Button leaveButton;         // ui_button_leave
        [SerializeField] private TMP_Text titleValue;        // ui_text_wheel_title_value
        [SerializeField] private SliceView[] slices;         // ui_image_slice_{i}_*

        [SerializeField] private GameSettings settings;

        private System.Action _onSpin, _onLeave;

#if UNITY_EDITOR
        protected override void AutoWire()
        {
            Bind(ref rotor,      "ui_image_spin_rotor");
            Bind(ref spinBronze, "ui_image_spin_bronze");
            Bind(ref spinSilver, "ui_image_spin_silver");
            Bind(ref spinGolden, "ui_image_spin_golden");
            Bind(ref indicator,  "ui_image_spin_indicator");
            Bind(ref spinButton, "ui_button_spin");
            Bind(ref leaveButton,"ui_button_leave");
            Bind(ref titleValue, "ui_text_wheel_title_value");
            if (slices == null || slices.Length == 0)
                slices = GetComponentsInChildren<SliceView>(true);
        }
#endif

        // Buttons are subscribed in CODE (brief: no editor OnClick).
        private void OnEnable()
        {
            spinButton.onClick.AddListener(HandleSpin);
            leaveButton.onClick.AddListener(HandleLeave);
        }
        private void OnDisable()
        {
            spinButton.onClick.RemoveListener(HandleSpin);
            leaveButton.onClick.RemoveListener(HandleLeave);
        }
        public void BindInput(System.Action onSpin, System.Action onLeave) { _onSpin = onSpin; _onLeave = onLeave; }
        private void HandleSpin()  => _onSpin?.Invoke();
        private void HandleLeave() => _onLeave?.Invoke();

        // Called when a new wheel is built: swap art by tier, fill slices.
        public void Render(WheelModel wheel)
        {
            spinBronze.enabled = wheel.Tier == WheelTier.Bronze;
            spinSilver.enabled = wheel.Tier == WheelTier.Silver;
            spinGolden.enabled = wheel.Tier == WheelTier.Golden;
            for (int i = 0; i < slices.Length; i++)
                slices[i].Render(i < wheel.SliceCount ? wheel.SliceAt(i) : null);
        }

        public void SetLeaveEnabled(bool canLeave) => leaveButton.interactable = canLeave;
        public void SetSpinEnabled(bool canSpin)   => spinButton.interactable = canSpin;

        // ---- The spin animation. Returns a Task the state machine awaits. ----
        public Task SpinTo(int index, int sliceCount)
        {
            var tcs = new TaskCompletionSource<bool>();
            spinButton.interactable = false;

            float sliceAngle = 360f / sliceCount;
            // Children are laid out clockwise with slice 0 under the top indicator at Z=0.
            // Rotating the rotor +Z (CCW) by sliceAngle*index brings slice 'index' to the top.
            // Add whole turns for the spin feel. (If it visually lands on the wrong slice,
            //  negate 'land' — depends on your child layout direction.)
            float land  = sliceAngle * index;
            float jitter = Random.Range(-sliceAngle * 0.25f, sliceAngle * 0.25f); // not dead-center
            float targetZ = settings.fullRotations * 360f + land + jitter;

            rotor.localRotation = Quaternion.identity;
            rotor.DOLocalRotate(new Vector3(0, 0, targetZ), settings.spinDuration,
                                RotateMode.FastBeyond360)
                 .SetEase(settings.spinEase)
                 .OnComplete(() => tcs.TrySetResult(true));

            return tcs.Task;
        }
    }
}
```

### 8.3 `SliceView.cs` — one chamber
```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wof.Domain;

namespace Wof.Presentation
{
    public sealed class SliceView : MonoBehaviour
    {
        [SerializeField] private Image    iconValue;   // ui_image_slice_icon  (decorative → RaycastTarget OFF)
        [SerializeField] private TMP_Text amountValue; // ui_text_slice_amount_value
        [SerializeField] private SpriteRegistry sprites;

        public void Render(WheelSlice slice)
        {
            if (slice == null) { gameObject.SetActive(false); return; }
            gameObject.SetActive(true);
            iconValue.sprite   = sprites.Resolve(slice.IsBomb ? "ui_card_icon_death" : slice.Reward.IconKey);
            iconValue.preserveAspect = true;                       // brief: do not stretch
            amountValue.text   = slice.IsBomb ? "" : $"x{slice.Reward.Amount}";
        }
    }
}
```

### 8.4 `GameController.cs` — the only MonoBehaviour that owns the FSM
```csharp
using UnityEngine;
using Wof.Application;
using Wof.Data;
using Wof.Domain;

namespace Wof.Presentation
{
    public sealed class GameController : MonoBehaviour
    {
        [SerializeField] private ZoneTuning   tuning;
        [SerializeField] private GameSettings settings;
        [SerializeField] private WheelView    wheelView;
        [SerializeField] private HudView      hudView;
        [SerializeField] private RewardPopupView   rewardPopup;
        [SerializeField] private BombExplodedView  bombScreen;

        private GameContext _ctx;
        private GameStateMachine _fsm;

        private void Awake()
        {
            _ctx = new GameContext(tuning, settings);
            _fsm = new GameStateMachine();

            // Logic → UI subscriptions (event bus).
            _ctx.Events.WheelBuilt       += wheelView.Render;
            _ctx.Events.ZoneChanged      += OnZoneChanged;
            _ctx.Events.RewardWon        += rewardPopup.Show;
            _ctx.Events.BombExploded     += () => bombScreen.Show(_ctx.Settings.reviveGoldCost);
            _ctx.Events.CurrencyChanged  += hudView.SetCurrency;
            _ctx.Events.WalletChanged    += hudView.SetRunCount;

            // UI → logic input (forwarded to whatever state is active).
            wheelView.BindInput(onSpin: TrySpin, onLeave: TryLeave);
        }

        private void Start() => _fsm.Change(new BootState(_ctx, _fsm,
            animateSpin: i => wheelView.SpinTo(i, _ctx.CurrentWheel.SliceCount)));

        private void Update() => _fsm.Tick(Time.deltaTime);

        private void OnZoneChanged(int zone, ZoneType type)
        {
            wheelView.titleValueText = TitleFor(type);                 // (expose a setter)
            wheelView.SetLeaveEnabled(ZoneRules.CanLeave(zone, tuning.safeInterval, tuning.superInterval));
        }

        private void TrySpin()  { if (_fsm.Current is IdleState s) s.OnSpin(); }
        private void TryLeave() { if (_fsm.Current is IdleState s) s.OnLeave(); }

        private static string TitleFor(ZoneType t) => t switch {
            ZoneType.Super => "GOLDEN SPIN", ZoneType.Safe => "SILVER SPIN", _ => "SPIN" };
    }
}
```

### 8.5 `SafeArea.cs` + `SpriteRegistry.cs` (helpers)
```csharp
// SafeArea: re-anchor a RectTransform to Screen.safeArea each frame the area changes (notches).
// SpriteRegistry: ScriptableObject mapping string key → Sprite, so Domain can stay UI-free
//                 and Views resolve icons by name from the Sprite Atlas.
```

---

## 9. Editor tooling (`Editor/WheelConfigEditor.cs`) — R2 custom inspector

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Wof.Data;

namespace Wof.EditorTools
{
    [CustomEditor(typeof(WheelConfig))]
    public sealed class WheelConfigEditor : Editor
    {
        private ReorderableList _list;
        private SerializedProperty _slices;

        private void OnEnable()
        {
            _slices = serializedObject.FindProperty("slices");
            _list = new ReorderableList(serializedObject, _slices, true, true, true, true)
            {
                drawHeaderCallback = r => EditorGUI.LabelField(r, "Slices (reward · weight · bomb)"),
                drawElementCallback = (rect, i, _, __) =>
                {
                    var el = _slices.GetArrayElementAtIndex(i);
                    rect.height = EditorGUIUtility.singleLineHeight; rect.y += 2;
                    float w = rect.width;
                    EditorGUI.PropertyField(new Rect(rect.x,        rect.y, w*0.5f, rect.height),
                        el.FindPropertyRelative("reward"), GUIContent.none);
                    EditorGUI.PropertyField(new Rect(rect.x+w*0.52f, rect.y, w*0.28f, rect.height),
                        el.FindPropertyRelative("weight"), GUIContent.none);
                    EditorGUI.PropertyField(new Rect(rect.x+w*0.84f, rect.y, w*0.16f, rect.height),
                        el.FindPropertyRelative("isBomb"), GUIContent.none);
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "slices", "m_Script");
            _list.DoLayoutList();
            // live total-weight readout
            float total = 0; for (int i = 0; i < _slices.arraySize; i++)
                total += _slices.GetArrayElementAtIndex(i).FindPropertyRelative("weight").floatValue;
            EditorGUILayout.HelpBox($"Total weight: {total:0.##}", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
```

---

## 10. Tests (`Tests/`) — edit-mode, pure Domain (brief: "easy testable")

```csharp
using NUnit.Framework;
using Wof.Domain;

public class ZoneRulesTests
{
    [TestCase(1,  ZoneType.Normal)]
    [TestCase(5,  ZoneType.Safe)]
    [TestCase(10, ZoneType.Safe)]
    [TestCase(30, ZoneType.Super)]   // the trap: ×30 is Super, NOT Safe
    [TestCase(60, ZoneType.Super)]
    [TestCase(7,  ZoneType.Normal)]
    public void Resolves_zone_type(int zone, ZoneType expected)
        => Assert.AreEqual(expected, ZoneRules.Resolve(zone));

    [Test] public void Normal_has_bomb()      => Assert.IsTrue(ZoneRules.HasBomb(3));
    [Test] public void Safe_has_no_bomb()     => Assert.IsFalse(ZoneRules.HasBomb(5));
    [Test] public void Super_has_no_bomb()    => Assert.IsFalse(ZoneRules.HasBomb(30));
    [Test] public void Cannot_leave_normal()  => Assert.IsFalse(ZoneRules.CanLeave(4));
    [Test] public void Can_leave_safe()       => Assert.IsTrue(ZoneRules.CanLeave(5));
}

public class WeightedSelectorTests
{
    [Test]
    public void Respects_weights_over_many_rolls()
    {
        var sel = new WeightedSliceSelector(seed: 42);
        var weights = new[] { 1f, 0f, 3f };   // index 1 should NEVER win
        int[] hits = new int[3];
        for (int i = 0; i < 100000; i++) hits[sel.PickLandingIndex(weights)]++;
        Assert.AreEqual(0, hits[1]);
        Assert.That(hits[2], Is.GreaterThan(hits[0]));   // ~3:1
    }
}

public class RewardWalletTests
{
    [Test] public void Bomb_wipes_all()
    {
        var w = new RewardWallet();
        w.Add(new Reward("g", RewardKind.Gold, 10, "g"));
        w.DetonateBomb();
        Assert.IsFalse(w.HasRewards);
    }

    [Test] public void CashOut_returns_then_clears()
    {
        var w = new RewardWallet();
        w.Add(new Reward("g", RewardKind.Gold, 10, "g"));
        var banked = w.CashOut();
        Assert.AreEqual(1, banked.Count);
        Assert.IsFalse(w.HasRewards);
    }
}
```

---

## 11. Build order checklist (what to type/create, in order)

1. **Assemblies** (§1) — create the 6 `.asmdef`s first so layering is enforced from line one.
2. **Enums + `Reward`** (§2).
3. **ScriptableObject classes** (§3), then create **assets**: `reward_*`, `wheel_bronze/silver/golden`, `zone_tuning`, `game_settings`. Fill `wheel_*` slices from the catalog in ROADMAP §9.
4. **Domain** (§4) + **tests** (§10) — get green before touching UI.
5. **Events + Services + GameContext** (§5–6).
6. **State machine + states** (§7).
7. **Views + GameController** (§8) — build the prefabs (ROADMAP §8 hierarchy), let `OnValidate` wire refs.
8. **Editor inspector** (§9).
9. Wire DOTween spin, verify it lands on the chosen index (flip `land` sign if needed).
10. Multi-aspect QA, APK, ship.

---

## 12. "Gotchas" that cost people points on this brief

- **`async void Enter()`** is fine for a state entry but never for general methods — use `async Task` elsewhere and `await`.
- The spin must **land on the pre-chosen index**, not pick on `OnComplete`. Decide first, animate second. Reviewers test this.
- **Unsubscribe** every event in `OnDisable` / dispose — leaks + double-fires are an easy ding on "maintainable."
- `preserveAspect = true` on every reward `Image` (brief: do not stretch).
- Turn **RaycastTarget OFF** on decorative images in code or a prefab pass — there are dozens; a `[ContextMenu]` utility that walks children and disables it on non-Button images saves time.
- Rotate the **`_rotor` child**, never the `WheelView` root (animator-in-separate-transform rule).
- Zone **30 is Super, not Safe** — covered by a test for a reason.
```
```
> This guide is intentionally code-first. Pair it with ROADMAP.md (the "what/why/UI-asset mapping") and you have the full picture: rules → architecture → C# → screens → build.
</content>
