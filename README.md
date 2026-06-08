# Wheel of Fortune — Vertigo Games Demo

A "wheel of fortune" gambling game built for the Vertigo Games Game Developer Demo.
The player advances zone by zone and spins a revolver-cylinder wheel: 7 reward slices
+ 1 bomb. Hitting the bomb wipes the run; otherwise rewards stack and grow each zone.
The player may walk away (cash out) when the wheel is idle on a **safe** (every 5th)
or **super** (every 30th) zone.

> Engine: **Unity 2021 LTS** · Platform: **Android (APK)** · UI: **TextMeshPro**, Canvas Scaler "Expand"
> Aspect targets: **20:9 / 16:9 / 4:3**

## Architecture

A strictly layered, SOLID design. The lower layers compile and unit-test with **no UI**.

```
Presentation (Views, MonoBehaviours, DOTween)   — renders state, raises input
        ▲ events / method calls
Application (Services, State machine, Events)    — pure C#, orchestration
        ▲
Domain (Wheel, Zone rules, Reward, Wallet)       — pure C#, fully unit-testable
        ▲
Data (ScriptableObjects)                         — designer-editable config
```

Each layer is its own assembly (`.asmdef`) so the compiler **physically prevents**
Domain from referencing UI — the architecture can't rot.

| Layer | Assembly | References |
|---|---|---|
| Domain | `Wof.Domain` | (none) |
| Data | `Wof.Data` | Domain |
| Application | `Wof.Application` | Domain, Data |
| Presentation | `Wof.Presentation` | Domain, Data, Application, DOTween |
| Editor | `Wof.Editor` | all (Editor-only) |
| Tests | `Wof.Tests` | Domain, Data |

See [docs/ROADMAP.md](docs/ROADMAP.md) (what/why + UI-asset mapping) and
[docs/IMPLEMENTATION_CSHARP.md](docs/IMPLEMENTATION_CSHARP.md) (the code-first guide).
The original brief lives in [docs/brief/](docs/brief/).

## Project layout

```
Assets/_Project/
  Art/            imported demo_content (UI assets), Sprite Atlas
  Scripts/
    Domain/       Wof.Domain — enums, ZoneRules, WheelModel, RewardWallet, ...
    Data/         Wof.Data — ScriptableObject configs
    Application/  Wof.Application — Events, Services, GameContext, States
    Presentation/ Wof.Presentation — Views, GameController, helpers
    Editor/       Wof.Editor — custom inspectors
  Prefabs/  Scenes/  Settings/
Tests/            Wof.Tests — edit-mode unit tests
```

## Getting started

1. Install **Unity 2021.3 LTS** via Unity Hub and open this folder as a project.
2. Import **DOTween** (Asset Store / "Tools ▸ Demigiant ▸ DOTween Utility Panel" ▸
   *Setup DOTween…* ▸ **Create ASMDEF**) so the `Wof.Presentation` assembly can
   reference `DOTween`. (DOTween is a "plus" in the brief.)
3. Open `Window ▸ TextMeshPro ▸ Import TMP Essential Resources`.
4. Run the edit-mode tests: `Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All`.

> The **Domain / Data / Application / Tests** layers compile and the tests pass
> without DOTween. DOTween is only needed by the **Presentation** layer.

## Status

Built commit-by-commit, bottom-up (Domain → Data → Application → Presentation).
See the git log for the feature-by-feature progression.
