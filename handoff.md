# XueGao Handoff

## Current State

This Unity project has been moved from a single runtime-generated GUI prototype to a formal prefab/component structure.

Implemented runtime modules:

- `Assets/Scripts/XueGao/GameManager.cs`
  - Coordinates game state, current ice cream, money, sticks, upgrades, shop selection, click handling, and the completion -> stick reveal -> lottery flow.
- `Assets/Scripts/XueGao/IceCreamEater.cs`
  - Uses 2D `SpriteRenderer` gameplay.
  - Creates a runtime texture from the selected ice cream sprite.
  - Erases pixels at bite positions.
  - Completes the ice cream at 80% eaten.
  - Shows/hides the stick reveal sprite.
- `Assets/Scripts/XueGao/MouthController.cs`
  - Tracks pointer world position.
  - Controls mouth/bite radius and upgrade level.
  - Moves the mouth preview sprite.
- `Assets/Scripts/XueGao/LotterySystem.cs`
  - Weighted snow-stick prize rolls.
  - Applies current ice cream multiplier and luck upgrade.
- `Assets/Scripts/XueGao/GameUI.cs`
  - UGUI-only UI refresh layer.
  - Handles stats, progress, prize message, history, upgrades, and shop buttons.
- `Assets/Scripts/XueGao/JuicyFeedbacks.cs`
  - Calls Feel `MMFeedbacks`.
  - Adds fallback scale/fade juice for bite, completion, and prize reveal.
- `Assets/Scripts/XueGao/RuntimeUIInputBinder.cs`
  - Creates runtime Input System UI actions so UGUI buttons work with the project’s new Input System setup.
- `Assets/Scripts/XueGao/IceCreamDefinition.cs`
  - ScriptableObject data for each ice cream.
- `Assets/Scripts/XueGao/PrizeResult.cs`
  - Immutable lottery result struct.

Old prototype compatibility:

- `Assets/Scripts/GameManger.cs` is intentionally left as an empty compatibility shell.
- It no longer auto-generates UI or gameplay.

Generated assets currently exist:

- `Assets/XueGao/Art/`
  - Placeholder sprites: `OldIce.png`, `CreamIce.png`, `ChocolateIce.png`, `LuxuryIce.png`, `Stick.png`, `MouthPreview.png`.
  - These are imported as Sprite and readable (`isReadable: 1`) so `IceCreamEater` can call `GetPixels()`.
- `Assets/XueGao/Data/`
  - `OldIce.asset`, `CreamIce.asset`, `ChocolateIce.asset`, `LuxuryIce.asset`.
- `Assets/XueGao/Prefabs/`
  - `GameRoot.prefab`
  - `GameUI.prefab`
  - `IceCreamPlayArea.prefab`

Scene:

- `Assets/Scenes/SampleScene.unity`
  - Contains `GameRoot`, `EventSystem`, and `Main Camera`.
  - `EventSystem` has `XueGao.RuntimeUIInputBinder`.

## Builder And Validation

Editor tooling:

- `Assets/Editor/XueGaoProjectBuilder.cs`
  - Menu: `XueGao > Build Formal Game`
  - Method: `XueGaoProjectBuilder.BuildFormalGame`
  - Generates placeholder sprites, data assets, prefabs, and rewrites `SampleScene`.
  - Menu: `XueGao > Validate Formal Game`
  - Method: `XueGaoProjectBuilder.ValidateFormalGame`
  - Verifies key prefabs, data assets, readable sprites, Feel `MMFeedbacks`, and scene objects.
- `Assets/Editor/XueGaoAutoBuildTrigger.cs`
  - Temporary utility for triggering the builder from the editor using `Assets/Editor/.xuegao_autobuild`.
  - This can remain, but it is no longer required now that assets are generated.

Useful commands:

```powershell
dotnet build Assembly-CSharp.csproj
dotnet build Assembly-CSharp-Editor.csproj
```

Unity batchmode build:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe' -batchmode -quit -projectPath 'C:\Users\fuyin\XueGao' -executeMethod XueGaoProjectBuilder.BuildFormalGame -logFile 'C:\Users\fuyin\XueGao\Logs\codex-build-formal.log'
```

Unity batchmode validation:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe' -batchmode -quit -projectPath 'C:\Users\fuyin\XueGao' -executeMethod XueGaoProjectBuilder.ValidateFormalGame -logFile 'C:\Users\fuyin\XueGao\Logs\codex-validate-formal.log'
```

Important: batchmode cannot run while the same Unity project is already open in another Unity instance.

## Last Verified

The following passed before this handoff:

- `dotnet build Assembly-CSharp-Editor.csproj`
  - 0 warnings, 0 errors for the project/editor assembly after the local builder warning was fixed.
- `dotnet build Assembly-CSharp.csproj`
  - 0 warnings, 0 errors.
- `XueGaoProjectBuilder.ValidateFormalGame`
  - Log contains `XueGao validation passed.`

The validation log also contains Unity licensing startup errors such as failed handshake/access token messages, but Unity later resolves licensing and validation passes. These were not gameplay or asset validation failures.

## Known Issue In Progress

User reported: running the game shows only UI, no ice cream.

Root cause found:

- `GameUI.prefab` has a full-screen Screen Space Overlay UI `Background` Image.
- Screen Space Overlay renders over world-space 2D sprites, so it visually covers the `IceCreamPlayArea`.
- The ice cream exists, but the UI background is in front of it.

Fix already applied in source builder, but not yet regenerated into prefab/scene because the user interrupted right after the code edit:

- In `Assets/Editor/XueGaoProjectBuilder.cs`, `SaveUIPrefab()` now creates `Background` as transparent:

```csharp
Image background = CreatePanel("Background", canvasObject.transform, new Color(0.98f, 0.95f, 0.88f, 0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
background.raycastTarget = false;
```

Required next step:

1. Run `XueGaoProjectBuilder.BuildFormalGame` again so `GameUI.prefab`, `GameRoot.prefab`, and `SampleScene` are regenerated with transparent UI background.
2. Run `XueGaoProjectBuilder.ValidateFormalGame`.
3. Open Play Mode and confirm the ice cream is visible on the left side of the screen.

Recommended batchmode commands:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe' -batchmode -quit -projectPath 'C:\Users\fuyin\XueGao' -executeMethod XueGaoProjectBuilder.BuildFormalGame -logFile 'C:\Users\fuyin\XueGao\Logs\codex-build-after-ui-bg-fix.log'

& 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe' -batchmode -quit -projectPath 'C:\Users\fuyin\XueGao' -executeMethod XueGaoProjectBuilder.ValidateFormalGame -logFile 'C:\Users\fuyin\XueGao\Logs\codex-validate-after-ui-bg-fix.log'
```

If Unity is already open with this project, use the menu instead:

- `XueGao > Build Formal Game`
- `XueGao > Validate Formal Game`

## Implementation Notes

Gameplay flow:

1. Player clicks the 2D ice cream.
2. `GameManager` checks that the pointer is not over UGUI.
3. `IceCreamEater.TryBite()` erases a circular area from the runtime ice cream texture.
4. Progress updates through `GameUI`.
5. At 80% eaten, `IceCreamEater.Completed` fires.
6. `GameManager.ResolveCompletedIceCream()`:
   - increments stick count,
   - hides the ice cream,
   - shows the stick,
   - plays completion feedback,
   - waits,
   - rolls lottery,
   - displays win/loss,
   - plays prize feedback,
   - reloads the next ice cream.

Feel integration:

- `GameRoot.prefab` contains at least three `MMFeedbacks` components.
- `JuicyFeedbacks` references them and calls `PlayFeedbacks()`.
- Current Feel setup is minimal. It proves integration and hooks are present, but the individual `MMFeedbacks` components do not yet contain a rich list of configured feedback tracks. Add actual Feel feedback items in the inspector for stronger juice.

Prefab hierarchy:

- `GameRoot`
  - `IceCreamPlayArea`
    - `IceCreamSprite`
    - `StickReveal`
    - `MouthPreview`
  - `GameUI`
  - `FeelFeedbacks`

## Suggested Next Improvements

- After regenerating the UI background fix, run the game visually and confirm:
  - ice cream is visible,
  - clicking it erases bites,
  - 80% eaten triggers stick reveal,
  - prize text appears after the delay.
- Add actual Feel feedback tracks:
  - bite: small camera shake / scale punch / sound,
  - completion: larger pop, particles, stick flip,
  - prize win: coin sound, UI flash, scale bounce,
  - no prize: softer settle animation.
- Replace placeholder sprites with better generated or hand-drawn sprites.
- Consider a dedicated world-space play-area background instead of Canvas background.
- Add PlayMode tests for `LotterySystem` and `IceCreamEater` if test infrastructure is desired.
