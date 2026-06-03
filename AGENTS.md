# XueGao Agent Guide

## Project Overview

Unity 2D casual game (`6000.4.9f1`). Core loop: click/touch to eat ice cream pixels → reach 80% threshold → stick reveal → lottery → spend winnings on upgrades and new ice creams.

Entry scene: `Assets/Scenes/Game.unity`. All runtime code lives in `Assets/Scripts/XueGao` under `namespace XueGao`.

## Directory Layout

- `Assets/Scripts/XueGao/` — project runtime scripts (10 files, one class each)
- `Assets/XueGao/Data/` — `IceCreamDefinition` and `IceCreamStickDefinition` ScriptableObject assets
- `Assets/XueGao/Prefabs/` — game root, UI, ice cream area, mouth cursor prefabs
- `Assets/XueGao/Art/` — sprites and animations
- `Assets/Feel/` — third-party More Mountains Feel / Nice Vibrations. **Do not modify** unless explicitly asked.
- `Assets/Scripts/GameManger.cs` — **stub kept for GUID compatibility**. The real `GameManager` is `Assets/Scripts/XueGao/GameManager.cs`. Do not delete or rename the stub.

Never hand-edit or commit: `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `.vs/`, `*.csproj`, `*.sln`, `*.slnx`.

## Core Classes

| Class | Responsibility |
|---|---|
| `GameManager` | Game loop, shop, upgrades, ice cream loading, lottery settlement, UI refresh. Holds all state: money, sticks, upgrade levels, unlock list. |
| `IceCreamEater` | Runtime `Texture2D` pixel manipulation, bite removal, progress tracking, disconnected-piece physics/despawn, stick reveal/hide. ~800 lines — the most complex file. |
| `MouthController` | Pointer-to-world conversion, mouth visibility, bite radius (scales with level), bite animation trigger. |
| `LotterySystem` | Weighted prize roll. `Roll(int multiplier, int luckLevel)` → `PrizeResult`. Luck reduces no-prize weight. |
| `GameUI` | UGUI `Text`/`Button`/`Slider` wiring. Shop button list built dynamically. |
| `JuicyFeedbacks` | `MMF_Player` integration (Feel plugin) + local coroutine punch-scale animations. |
| `RuntimeUIInputBinder` | Creates `InputSystemUIInputModule` action bindings at runtime when Input System is active. |
| `IceCreamDefinition` | SO: displayName, price, prizeMultiplier, fullSprite, sampleCount, alphaThreshold, stickSprite fallback. |
| `IceCreamStickDefinition` | SO: displayName, stickSprite, tint. |
| `PrizeResult` | readonly struct: Label, BaseAmount, FinalAmount (= base × multiplier), StickDefinition. |

## Conventions

- New scripts → `Assets/Scripts/XueGao/` with `namespace XueGao`.
- Extend content via `IceCreamDefinition` / `IceCreamStickDefinition` assets, not hardcoded data.
- UI uses UGUI (`Text`, `Button`, `Slider`). Do not switch to TextMeshPro or UI Toolkit unless asked.
- Input code uses `#if ENABLE_INPUT_SYSTEM` guards.
- When editing serialized fields, prefabs, scenes, or SOs: preserve existing GUIDs and `.meta` files.

## Critical Gotchas

- **IceCreamEater memory**: Creates runtime `Texture2D` per ice cream load. `Load()` must clean up previous texture and detached pieces. Watch for coroutine leaks and double-completion (`resolvingCompletion` flag in GameManager guards this).
- **LotterySystem.Roll semantics**: Returns `(label, baseAmount, baseAmount * multiplier, stickDefinition)`. The `FinalAmount` already has the multiplier applied.
- **Completion threshold**: `completeThreshold = 0.8f` on `IceCreamEater`. The progress slider in GameUI also normalizes against 0.8.
- **GameManger.cs typo is intentional**: The stub class name `GameManger` (missing 'a') is locked by existing Unity GUIDs. Do not fix the typo.
- **Feel plugin**: `JuicyFeedbacks` depends on `MoreMountains.Feedbacks` from `Assets/Feel/`. Never modify Feel source.

## Verification

- Use Unity MCP to check Editor compilation status and Console for errors. If MCP is unavailable, stop and ask.
- Play Mode test for gameplay changes.
- No test framework is set up yet; there are no existing tests in the project.
- For doc-only changes, `git status --short` suffices.
