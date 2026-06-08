# Vertigo Games — Wheel of Fortune Demo · Development Roadmap

> Source brief: `Vertigo Games Game Developer Demo.pdf`
> Engine: **Unity 2021 LTS** · Platform: **Android (APK release)** · UI: **TextMeshPro + Canvas Scaler "Expand"**
> Aspect targets: **20:9, 16:9, 4:3** · Deadline: **7 days**
>
> 📄 **Companion:** [IMPLEMENTATION_CSHARP.md](IMPLEMENTATION_CSHARP.md) — the concrete Unity C# for every system below (full code for the core, signatures for the rest, plus unit tests).

---

## 0. TL;DR

Reskin Critical Strike's **"Card Game"** as a **Wheel of Fortune**. The player advances zone by zone. At every zone they **spin a revolver-cylinder wheel** with 8 chambers — 7 reward slices + **1 bomb**. Hitting the bomb **wipes everything**; otherwise rewards stack and grow each zone. The player may **walk away (cash out)** when the wheel is idle and the zone is **safe** or **super**. Every **5th zone = safe silver spin (no bomb)**, every **30th zone = super golden spin (no bomb, special rewards)**.

The whole document below maps **every game function** and **every provided art asset** to a concrete system, screen, and naming convention.

---

## 1. Game Rules → Functions (traceability matrix)

Every line in the brief becomes a testable function. This is the contract.

| # | Rule (from brief) | Owning system | Key function(s) |
|---|---|---|---|
| R1 | Player spins a wheel instead of selecting cards | `WheelController` | `Spin()`, `EvaluateResult()` |
| R2 | Slice content of each wheel is editor-changeable | `WheelConfig` (ScriptableObject) + custom editor | `WheelConfig.Slices[]`, `OnValidate()` |
| R3 | Each zone wheel has multiple rewards + 1 bomb | `ZoneService` + `WheelBuilder` | `BuildWheelForZone(int zone)` |
| R4 | Bomb takes all rewards collected so far; others are reward cards that improve every zone | `RewardWallet` + `RewardScaler` | `DetonateBomb()`, `ScaleRewardsForZone(int)` |
| R5 | Every 5th zone = safe silver spin, no bomb | `ZoneRules` | `IsSafeZone(zone)` → `zone % 5 == 0` |
| R6 | Every 30th zone = super golden spin, special rewards, no bomb | `ZoneRules` | `IsSuperZone(zone)` → `zone % 30 == 0` |
| R7 | Player may leave only when wheel idle AND (safe zone OR super zone) | `GameStateMachine` + `LeaveButton` | `CanLeave()` |
| R8 | Hitting bomb → lose all rewards, restart possible | `BombExplodedState` | `OnBomb()`, `RestartRun()` |
| R9 | (Bonus) Continue with currency / revive | `ReviveService` | `ReviveWithGold()`, `ReviveWithAd()` |
| R10 | Walk away → collect everything banked so far | `CashOutState` | `CashOut()` |

**Zone-type resolution order (important):** check **super (×30) before safe (×5)** because 30 is divisible by 5. Zone 30, 60, 90… are super (golden), not safe.

```
ResolveZoneType(zone):
    if zone % 30 == 0 -> Super   (golden, no bomb, special rewards, leavable)
    elif zone % 5 == 0 -> Safe   (silver, no bomb, leavable)
    else               -> Normal (bronze, 1 bomb, NOT leavable until cleared)
```

---

## 2. Core Gameplay Loop (state machine)

The single source of truth for screen flow and what the player can do. Implemented as an explicit **State pattern** (`IGameState` with `Enter/Tick/Exit`), driven by `GameStateMachine`.

```
        ┌────────────┐
        │   Boot     │  load SOs, build atlas, init wallet
        └─────┬──────┘
              ▼
        ┌────────────┐
        │ ZoneIntro  │  resolve zone type, build wheel, animate zone strip
        └─────┬──────┘
              ▼
        ┌────────────┐   Spin pressed
        │   Idle     ├───────────────► Spinning
        │ (Ready)    │                     │
        │  Leave?────┼──► CashOut          │ wheel decelerates to slice
        └────────────┘  (safe/super only)  ▼
                                      ┌──────────┐
                                      │ Resolving│ read landed slice
                                      └────┬─────┘
                         reward │                 │ bomb
                                ▼                 ▼
                          ┌──────────┐      ┌──────────────┐
                          │  Reward  │      │ BombExploded │
                          │ (collect)│      │  (Revive?)   │
                          └────┬─────┘      └──┬────────┬──┘
                               ▼  next zone    │revive  │give up
                          ZoneIntro            ▼        ▼
                                            Idle     GameOver → Restart
```

**States to implement:** `BootState`, `ZoneIntroState`, `IdleState`, `SpinningState`, `ResolvingState`, `RewardState`, `BombExplodedState`, `CashOutState`, `GameOverState`. Each state owns exactly which buttons/inputs are interactable — never let the View decide game rules.

---

## 3. Architecture (SOLID / OOP / patterns)

> Brief explicitly grades on: *Reusable, maintainable, scalable, testable; SOLID + OOP; refactoring & design patterns.* Architecture is a first-class deliverable.

### 3.1 Layering
```
Presentation (Views, MonoBehaviours, DOTween)  ── only knows interfaces/events
        ▲ events / view-model
Application (Controllers, StateMachine, Services) ── pure C#, no UnityEngine.UI
        ▲
Domain (Wheel, Zone, Reward, Wallet rules)      ── pure C#, unit-testable
        ▲
Data (ScriptableObjects, save)                  ── config + persistence
```
Rule: **Domain/Application must compile and unit-test without any UI.** Views subscribe to events; they never contain game rules.

### 3.2 Patterns used (and where)
| Pattern | Where | Why |
|---|---|---|
| **State** | `GameStateMachine` | Clean per-screen behavior, no `if/else` spaghetti |
| **Strategy** | `IZoneRule` (Normal/Safe/Super), `ISliceSelector` | Swap zone behavior & landing logic |
| **ScriptableObject config** | `WheelConfig`, `RewardDefinition`, `ZoneTuning`, `GameSettings` | Designer-editable, no recompile (R2) |
| **Factory / Builder** | `WheelBuilder.BuildWheelForZone()` | Assemble runtime wheel from config + zone |
| **Observer / Event bus** | `GameEvents` (C# events / UnityEvents in code) | Decouple UI from logic |
| **Command** (optional) | spin / leave / revive actions | Testable input |
| **Object pool** | reward popups, star VFX | Mobile perf |
| **Service locator (lite) / DI** | `GameContext` | Wire services without singletons everywhere |

### 3.3 Folder structure
```
Assets/
  _Project/
    Art/                (imported demo_content + Sprite Atlas)
    Scripts/
      Domain/           Wheel, Slice, Zone, RewardWallet, RewardScaler, ZoneRules
      Application/      GameStateMachine, States/, Services/
      Data/             ScriptableObjects/ (WheelConfig, RewardDefinition…)
      Presentation/     Views/, Widgets/, Editor-bound MonoBehaviours
      Editor/           WheelConfigEditor, OnValidate helpers, build tools
      Utils/            DOTween extensions, SafeArea, ServiceLocator
    Prefabs/            UI prefabs (wheel, slice, popups, hud)
    Scenes/             Boot, Game
    Settings/           GameSettings.asset, ZoneTuning.asset, RewardTables/
  Plugins/DOTween/
```

---

## 4. Data Layer — ScriptableObjects (R2: editor-changeable)

### 4.1 `RewardDefinition` (one per reward type)
```
RewardType    : enum { Gold, Cash, WeaponSkin, Consumable, Chest, Skin, Points, Bomb }
displayName   : string
icon          : Sprite          // ← mapped to demo_content render
baseAmount    : int
rarityTier    : enum { Tier1, Tier2, Tier3, Special }
vfxOnWin      : enum { None, Star, GoldenShine }
```

### 4.2 `WheelConfig` (the changeable wheel — R2)
```
sliceCount    : int = 8                 // matches revolver-cylinder art (8 chambers)
slices[]      : SliceEntry { RewardDefinition reward; float weight; bool isBomb; }
wheelTier     : enum { Bronze, Silver, Golden }
```
A **custom inspector** (`WheelConfigEditor`) lets a designer add/remove/reorder slices, set weights, and flag the bomb slice — fully satisfying *"content of slices of each wheel should also be changeable from the editor."*

### 4.3 `ZoneTuning`
```
safeZoneInterval   : int = 5
superZoneInterval  : int = 30
rewardGrowthCurve  : AnimationCurve   // reward multiplier vs zone (R4 "better every zone")
normalWheel        : WheelConfig
safeWheel          : WheelConfig
superWheel         : WheelConfig
```

### 4.4 `GameSettings`
spin duration, spin easing, min/max full rotations, revive gold cost (e.g. **25**, per the example screenshot), starting currency.

---

## 5. Wheel System (the heart)

### 5.1 Domain functions
```
WheelModel.BuildForZone(zone, tuning)      // picks bronze/silver/golden config, injects/strips bomb
ISliceSelector.PickLandingIndex(slices[])  // weighted random; bomb excluded on safe/super
WheelModel.SliceAngle(index)               // 360 / sliceCount * index
```

### 5.2 Spin flow (Presentation)
1. `IdleState` → player taps **SPIN** → `WheelController.Spin()`.
2. Logic picks `landingIndex` **first** (deterministic result, animation just visualizes it — testable).
3. Compute target rotation = `(fullRotations*360) + offsetToAlign(landingIndex under indicator)`.
4. **DOTween** rotates the cylinder: `transform.DORotate(target, duration, RotateMode.FastBeyond360).SetEase(spinEase)` with an ease-out so it decelerates and "clicks" under the fixed top indicator.
5. On complete → `ResolvingState.Evaluate(landingIndex)`.

> The **indicator is fixed at top**; the **cylinder rotates** (matches the revolver art + `ui_spin_*_indicator` pointer). Alternative is rotating the indicator — pick cylinder rotation so the pointer art stays upright.

### 5.3 Result resolution
```
slice = wheel.slices[landingIndex]
if slice.isBomb -> raise OnBomb
else            -> wallet.Add(slice.reward.Scaled(zone)); raise OnReward(slice)
```

---

## 6. Zone System

```
ZoneService.Current : int
ZoneService.Advance()            // ++zone, rebuild wheel, fire OnZoneChanged
ZoneRules.IsSafe(zone)           // zone%5==0 && zone%30!=0
ZoneRules.IsSuper(zone)          // zone%30==0
ZoneRules.HasBomb(zone)          // !(IsSafe || IsSuper)
RewardScaler.Multiplier(zone)    // from rewardGrowthCurve (R4)
```
Zone type drives **three things at once**: which wheel art (bronze/silver/golden), whether the **LEAVE** button is enabled, and whether a bomb slice exists.

---

## 7. Reward & Economy System

```
RewardWallet.runRewards : List<RewardStack>   // banked this run, lost on bomb
RewardWallet.Add(reward)                       // accumulate (R4)
RewardWallet.DetonateBomb()                    // clear runRewards (R8)
RewardWallet.CashOut()                         // move runRewards → permanent inventory (R10)
Currency.gold / Currency.cash                  // persistent; gold spent on revive (R9)
```
- **Gold** (`UI_icon_gold`) = premium, used for **Revive** (R9 bonus).
- **Cash** (`UI_icon_cash`) = soft currency shown in HUD.
- Persistence via simple JSON save (`PlayerPrefs` or a `SaveService`) — bonus.

---

## 8. UI Screens — matched to provided assets

> Naming rules enforced everywhere: root-general→specific (`ui_image_spin_golden`), changeable elements end in **`_value`**, TextMeshPro only, **Sliced** sprites, **no** RaycastTarget/Maskable on non-interactive images, animators in **separate child transforms**, references auto-wired via **`OnValidate`**, **no editor OnClick** (subscribe in code), correct anchors/pivots, **never stretch** icons (preserve aspect).

### Screen A — In-Game HUD (top bar)
**Purpose:** show currencies, current zone, accumulated reward count.
| Element | Asset | Object name |
|---|---|---|
| Gold counter | `UI_icon_gold.png` | `ui_image_currency_gold` + `ui_text_currency_gold_value` |
| Cash counter | `UI_icon_cash.png` | `ui_image_currency_cash` + `ui_text_currency_cash_value` |
| Zone label | (text) | `ui_text_zone_value` |
| Buttons bg | `UI_button_grey_standard.png` (sliced) | `ui_button_settings` |

Anchors: gold/cash top-left & top-right; zone label top-center. Pivot per corner so all aspects hold.

### Screen B — Wheel Screen (main)
**Purpose:** the spin. Wheel art switches by zone type.
| Element | Asset | Object name |
|---|---|---|
| Normal wheel base | `ui_spin_bronze_base.png` | `ui_image_spin_bronze` |
| Safe wheel base | `ui_spin_silver_base.png` | `ui_image_spin_silver` |
| Super wheel base | `ui_spin_golden_base.png` | `ui_image_spin_golden` |
| Pointer (per tier) | `ui_spin_{bronze,silver,golden}_indicator.png` | `ui_image_spin_indicator` |
| Slice content (×8) | reward icons (see §10) | `ui_image_slice_{i}_icon`, `ui_text_slice_{i}_value` |
| SPIN button | `ui_spin_generic_button.png` | `ui_button_spin` |
| LEAVE / collect button | `UI_button_orange_standard.png` | `ui_button_leave` |
| Title ("GOLDEN SPIN") | (text) | `ui_text_wheel_title_value` |
| Subtitle ("Up To x10 Rewards") | (text) | `ui_text_wheel_subtitle_value` |

Layout (from the **GOLDEN SPIN** example screenshot): title top, wheel center with 8 chambers around a hub, indicator pinned at top of wheel, SPIN/center hub, subtitle bottom. **Wheel pivot = center**; rotate the `_rotor` child, **not** the root (animator-in-separate-transform rule). LEAVE button **disabled** (greyed) unless `ZoneRules.IsSafe || IsSuper`.

### Screen C — Zone Progress Strip
**Purpose:** show where the player is and what's next (safe/super markers).
| Element | Asset | Object name |
|---|---|---|
| Current zone panel | `ui_card_panel_zone_current.png` (blue) | `ui_image_zone_current` |
| Upcoming zone panel | `ui_card_panel_zone_coming.png` (white) | `ui_image_zone_coming` |
| Super zone panel | `ui_card_panel_zone_super.png` (green) | `ui_image_zone_super` |
| Generic bg / white | `ui_card_panel_zone_bg.png`, `_white.png` | `ui_image_zone_bg` |
| Zone card frame | `ui_card_frame_4px_zone.png`, `ui_card_zone_map_frame.png` | `ui_image_zone_frame` |
| Zone number | (text) | `ui_text_zone_index_value` |
Horizontal layout group; current zone highlighted blue, ×5 safe = silver tint, ×30 super = green. Frames are **sliced**.

### Screen D — Reward Popup (win)
**Purpose:** celebrate a reward, then "Collect".
| Element | Asset | Object name |
|---|---|---|
| Card frame | `ui_card_frame_gardient.png`, `ui_card_frame_12px_neutral.png` | `ui_image_reward_frame` |
| Reward icon | reward render (§10) | `ui_image_reward_icon` |
| Star burst VFX | `star_flash_alpha.png`, `star_glow_alpha.png` | child `_vfx_star` (separate transform) |
| Golden shine (super) | `ui_vfx_offer_shine.tga` | child `_vfx_shine` |
| Amount | (text) | `ui_text_reward_amount_value` |
| Collect button | `UI_button_orange_standard.png` | `ui_button_collect` |
VFX objects live under a dedicated `_vfx` child so the DOTween/animator never touches the root.

### Screen E — Bomb Exploded (loss / revive) — matches right screenshot
**Purpose:** "OH NO, A BOMB EXPLODED RIGHT IN YOUR HANDS! Revive yourself to keep your rewards."
| Element | Asset | Object name |
|---|---|---|
| Death card | `ui_card_icon_death.png` + `ui_card_frame_*` (red) | `ui_image_bomb_card` |
| Title | (text) | `ui_text_bomb_title_value` |
| Subtitle | (text) | `ui_text_bomb_subtitle_value` |
| GIVE UP button | `UI_button_grey_standard.png` | `ui_button_giveup` |
| REVIVE (gold) button | `UI_button_orange_standard.png` + `UI_icon_gold.png` | `ui_button_revive_gold` + `ui_text_revive_cost_value` (e.g. **25**) |
| REVIVE (ad) button | blue button + video icon | `ui_button_revive_ad` |
Buttons map exactly to the three in the brief screenshot: **GIVE UP / 25 REVIVE / REVIVE (ad)**. GIVE UP → `GameOver`; revive → back to `Idle`, rewards kept.

### Screen F — Cash Out / Collected
**Purpose:** when player LEAVES on a safe/super zone, show banked rewards then confirm.
| Element | Asset | Object name |
|---|---|---|
| Reward list items | chests + icons (§10) | `ui_image_loot_{i}` |
| Confirm button | `UI_button_orange_standard.png` | `ui_button_cashout_confirm` |
| Back button | `UI_button_grey_standard.png` | `ui_button_cashout_cancel` |

### Screen G — Game Over / Restart
Restart run (R8). Reuses grey/orange buttons. `ui_button_restart`, `ui_text_gameover_value`.

---

## 9. Reward catalog → asset mapping (slice & popup content)

These are the actual reward icons the wheel slices and popups display (`RewardDefinition.icon`).

| Reward category | Assets |
|---|---|
| **Currency** | `UI_icon_gold.png`, `UI_icon_cash.png` |
| **Chests** | `UI_icon_chest_{Bronze,silver,gold}_nolight.png`, `chest_{small,big,standart,super}` |
| **Weapon skins (tiered)** | `UI_Icon_Renders_tier1_shotgun`, `tier2_rifle`, `tier2_mle`, `tier3_{shotgun,smg,sniper}` |
| **"Points" tokens** | `UI_Icons_{Rifle,Pistol,SMG,Submachine,Shotgun,Sniper,Knife,Armor,Vest}_Points.png` |
| **Consumables** | `ui_icon_render_cons_grenade_{m26,m67}`, `healthshot_2_{neurostim,regenerator}`, `ui_icon_render_t_cons_molotov` |
| **Special / seasonal skins** | `ui_icon_aviator_glasses_easter`, `ui_icon_baseball_cap_easter`, `ui_icon_helmet_pumpkin`, `ui_icon_mle_bayonet_{easter_time,summer_vice}` |
| **Bomb slice** | `ui_card_icon_death.png` |

**Tier → zone mapping (R4 "better every zone"):** Tier1 weapons & small chests in early zones; Tier3 weapons, gold chests, and **special skins reserved for super (×30) golden spin** — matching the "special rewards" rule. `RewardScaler` multiplies currency amounts by `rewardGrowthCurve(zone)`.

---

## 10. Editor Tooling (R2)
- `WheelConfigEditor` : reorderable slice list, per-slice reward picker + weight slider + "is bomb" toggle, live total-weight readout, warning if a Normal wheel has ≠1 bomb or a Safe/Super wheel has any bomb.
- `OnValidate()` on every View MonoBehaviour: auto-find child buttons/images/texts by name and assign serialized fields → satisfies *"Button references automatically set from OnValidate."* No manual dragging, **no editor OnClick** — handlers are subscribed in `Awake/OnEnable` in code.
- Build menu item: one-click APK with versioned name.

---

## 11. UI Technical Compliance Checklist (from brief — must pass)
- [ ] Canvas Scaler = **Scale With Screen Size**, reference res set, **Match = Expand** behavior.
- [ ] **TextMeshPro** for all text; no legacy `Text`.
- [ ] Changeable elements named `*_value`.
- [ ] Names go **root-general → specific** (`ui_image_spin_golden`).
- [ ] **RaycastTarget OFF** + not Maskable on every decorative Image.
- [ ] Animators/DOTween targets in **separate child transforms**, never the root rect.
- [ ] Correct **anchors & pivots** for 20:9 / 16:9 / 4:3 (no drift).
- [ ] Button refs auto-set via **OnValidate**.
- [ ] **Sliced** sprites on all 9-slice buttons/panels/frames.
- [ ] **No editor OnClick / UnityEvent wiring** — code subscription only.
- [ ] **No stretched** icons (preserve aspect; use Layout/AspectRatioFitter).

---

## 12. Animation & VFX (DOTween — a "plus")
| Moment | Animation |
|---|---|
| Spin | `DORotate` ease-out, FastBeyond360, lands under indicator |
| Indicator tick | small per-chamber bounce/`DOPunchRotation` as it passes |
| Reward win | popup `DOScale` punch + `star_flash`/`star_glow` fade-in burst |
| Super reward | `ui_vfx_offer_shine.tga` sweeping shine loop |
| Bomb | screen shake + red flash + death card slam-in |
| Buttons | press `DOScale` 0.95, idle pulse on SPIN |
| Zone change | strip slides, current panel highlights |

---

## 13. Multi-Aspect Strategy (20:9 / 16:9 / 4:3)
- Canvas Scaler reference 1080×2340 (20:9 portrait) — adjust to chosen orientation; Match per-axis.
- **SafeArea** component on a root child for notches.
- Anchor HUD to corners, wheel to center (anchored-center, fixed size, no stretch), zone strip anchored top, popups center.
- **Test matrix:** Game-view presets for 2340×1080 (20:9), 1920×1080 (16:9), 1440×1080 (4:3) — verify no overlap/clipping. Deliverable screenshots come from exactly these three.

---

## 14. Audio (optional polish)
Spin loop tick, reward chime, bomb explosion, button click, cash-out fanfare — `AudioService` with pooled one-shots.

---

## 15. Testing
- **Edit-mode unit tests** (pure Domain): zone-type resolution (esp. zone 30 = super not safe), weighted selection distribution, bomb wipes wallet, cash-out moves rewards, reward scaling monotonic.
- **Play-mode**: spin lands on the pre-chosen slice; LEAVE only enabled on safe/super; revive restores Idle with rewards intact.

---

## 16. Build & Delivery (from brief)
- [ ] **Video** of gameplay.
- [ ] **Screenshots** at **20:9, 16:9, 4:3**.
- [ ] **GitHub** project, with working **APK attached as a Release**.
- [ ] README with build steps & architecture notes.

---

## 17. 7-Day Milestone Plan

| Day | Goal | Output |
|---|---|---|
| **1** | Project setup: Unity 2021 LTS, TMP, DOTween, Sprite Atlas from `demo_content`, folder structure, Boot/Game scenes, Canvas Scaler Expand. | Empty but compliant project skeleton |
| **2** | Domain layer + ScriptableObjects: `WheelConfig`, `RewardDefinition`, `ZoneTuning`, `ZoneRules`, `RewardWallet`, `RewardScaler` + unit tests. | Pure-C# game logic, green tests |
| **3** | State machine + services wiring (Boot→ZoneIntro→Idle→Spinning→Resolving). Wheel spin logic with deterministic landing. | Playable loop in editor (no art) |
| **4** | Wheel Screen UI (B) + HUD (A): bronze/silver/golden swap, indicator, 8 slices, SPIN, LEAVE gating. OnValidate auto-wiring. | Real wheel spins with art |
| **5** | Reward popup (D), Bomb-exploded/Revive (E), Cash-out (F), Game Over (G), zone strip (C). DOTween + star/shine VFX. | Full screen flow, matches screenshots |
| **6** | Editor tooling (`WheelConfigEditor`, build tool), reward catalog populated, currency/revive, persistence. Compliance checklist pass. | Feature-complete |
| **7** | Multi-aspect QA (20:9/16:9/4:3), polish, audio, record video + screenshots, build APK, push GitHub + Release. | Submission |

> Buffer: if blocked, the **continue/revive system (R9)** and **audio** are the first things to cut — they are explicitly "bonus" / polish.

---

## 18. Definition of Done
1. All 10 rules (R1–R10) implemented and demonstrable.
2. Every screen matches the provided art (golden/silver/bronze wheels, death-card revive screen, zone panels).
3. UI compliance checklist (§11) fully green.
4. Works on 20:9, 16:9, 4:3 with no layout breakage.
5. SOLID/OOP, ScriptableObject-driven, DOTween, Sprite Atlas (all "pluses" hit).
6. APK Release on GitHub + video + 3 screenshots delivered.
</content>
</invoke>
