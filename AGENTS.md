# XueGao Agent 指南

## 项目概览

这是一个 Unity 2D 休闲游戏项目，Unity 版本为 `6000.4.9f1`。核心循环是：购买雪糕 → 雪糕落到桌面 → 点击某个雪糕进入聚焦吃雪糕界面 → 点击/触摸吃掉雪糕像素 → 达到 80% 完成阈值 → 显示雪糕棍并开奖 → 用奖金购买升级和更多雪糕。

入口场景是 `Assets/Scenes/Game.unity`。运行时代码都放在 `Assets/Scripts/XueGao/`，并使用 `namespace XueGao`。

## 目录结构

- `Assets/Scripts/XueGao/`：项目运行时脚本，一个文件一个主要类。
- `Assets/XueGao/Data/`：`IceCreamDefinition` 和 `IceCreamStickDefinition` 的 ScriptableObject 数据资产。
- `Assets/XueGao/Prefabs/`：游戏根节点、UI、吃雪糕区域、嘴巴光标、商店条目等 prefab。
- `Assets/XueGao/Art/`：美术 sprite 和动画资源。
- `Assets/Feel/`：第三方 More Mountains Feel / Nice Vibrations。除非用户明确要求，不要修改。
- `Assets/Scripts/GameManger.cs`：为了 GUID 兼容保留的 stub。真正的 `GameManager` 在 `Assets/Scripts/XueGao/GameManager.cs`。不要删除、重命名或修正这个拼写。

不要手动编辑或提交：`Library/`、`Temp/`、`Logs/`、`UserSettings/`、`.vs/`、`*.csproj`、`*.sln`、`*.slnx`。

## 核心类

| 类 | 职责 |
|---|---|
| `GameManager` | 游戏主状态机、桌面雪糕生成、聚焦吃雪糕流程、商店购买、升级、开奖结算、UI 刷新。保存金钱、雪糕棍、升级等级、桌面雪糕等运行时状态。 |
| `IceCreamEater` | 运行时 `Texture2D` 像素处理、咬痕删除、进度统计、断开碎片物理/淡出、雪糕棍显示/隐藏。文件较复杂，修改时要格外小心资源释放和协程状态。 |
| `MouthController` | 指针到世界坐标转换、嘴巴显示、咬痕半径按等级缩放、咬合动画触发。 |
| `LotterySystem` | 加权开奖。`Roll(int multiplier, int luckLevel)` 返回 `PrizeResult`，幸运值会降低未中奖权重。 |
| `GameUI` | UGUI `Text` / `Button` / `Slider` 绑定，商店列表动态创建，桌面/聚焦/开奖 UI 模式切换。 |
| `ShopItemView` | 商店条目 prefab 的视图绑定组件。负责显示雪糕名、价格、最高奖金、图标，并绑定购买按钮回调。 |
| `JuicyFeedbacks` | `MMF_Player` 集成，以及本地 coroutine 缩放反馈动画。 |
| `RuntimeUIInputBinder` | 使用 Input System 时，运行时创建 `InputSystemUIInputModule` action 绑定。 |
| `IceCreamDefinition` | 雪糕 SO：displayName、price、prizeMultiplier、fullSprite、sampleCount、alphaThreshold、stickSprite fallback。 |
| `IceCreamStickDefinition` | 雪糕棍 SO：displayName、stickSprite、tint。 |
| `PrizeResult` | readonly struct：Label、BaseAmount、FinalAmount（base × multiplier）、StickDefinition。 |

## UI 结构约定

- `Assets/XueGao/Prefabs/GameUI.prefab` 是主要 UI prefab。
- 左侧 `StorePanel` 只做商店，`shopRoot` 必须绑定到 `StorePanel/ShopRoot`。
- 右侧 `UpgradePanel` 只做升级，嘴巴、幸运值、自动吃按钮都应在右侧栏。
- 商店条目使用 `Assets/XueGao/Prefabs/ShopItem.prefab`，运行时由 `GameUI.BuildShop()` 动态实例化。
- 编辑器里可以在 `StorePanel/ShopRoot` 下保留 3 个 `ShopItem` 预览实例，方便手调样式；运行时初始化前会清理这些预览，再按数据动态生成真实列表。
- 不要在 `GameUI.BuildShop()` 里拼 UI 结构，也不要靠 `GetComponentInChildren<Text>()` 猜子节点；应通过 `ShopItemView` 的显式序列化引用绑定。
- 进入聚焦吃雪糕界面时，左右栏由 `GameUI.SetMode()` 统一隐藏或弱化，避免商店/升级语义反置。

## 代码约定

- 新脚本放到 `Assets/Scripts/XueGao/`，并使用 `namespace XueGao`。
- 新内容优先通过 `IceCreamDefinition` / `IceCreamStickDefinition` 资产扩展，不要硬编码数据。
- UI 使用 UGUI（`Text`、`Button`、`Slider`）。除非用户明确要求，不要切换到 TextMeshPro 或 UI Toolkit。
- 输入相关代码需要使用 `#if ENABLE_INPUT_SYSTEM` 防护。
- 修改序列化字段、prefab、scene 或 SO 时，保留已有 GUID 和 `.meta` 文件。
- 可以使用 Unity Editor / Unity MCP 修改 prefab 和场景，但修改后必须检查编译和 Console。

## 关键注意事项

- **IceCreamEater 内存管理**：每次加载雪糕都会创建运行时 `Texture2D`。`Load()` / `Clear()` 必须清理旧贴图、碎片和协程，避免泄漏。
- **完成结算防重入**：`GameManager` 使用完成结算保护逻辑，避免同一根雪糕重复开奖。
- **LotterySystem.Roll 语义**：返回 `(label, baseAmount, baseAmount * multiplier, stickDefinition)`，`FinalAmount` 已经包含倍率。
- **完成阈值**：`IceCreamEater.completeThreshold = 0.8f`。`GameUI` 的进度条也按 0.8 归一化。
- **GameManger.cs 拼写错误是故意保留**：stub 类名 `GameManger` 少了一个 `a`，这是为了兼容 Unity GUID。不要修。
- **Feel 插件不要动**：`JuicyFeedbacks` 依赖 `Assets/Feel/` 下的 `MoreMountains.Feedbacks`。不要修改第三方源码。

## 验证

- 使用 Unity MCP 检查 Editor 编译状态和 Console。若 Unity MCP 不可用，停止并询问用户。
- 涉及 gameplay、prefab、scene、UI 绑定的改动，需要做 Play Mode 验证。
- 当前项目没有测试框架，也没有现成测试。
- 只改文档时，用 `git status --short` 确认变更范围即可。
