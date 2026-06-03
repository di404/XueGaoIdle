using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace XueGao
{
    public class GameManager : MonoBehaviour
    {
        private enum GameState
        {
            Table,
            FocusedEating,
            PrizeReveal
        }

        [SerializeField] private List<IceCreamDefinition> iceCreams = new List<IceCreamDefinition>();
        [SerializeField] private IceCreamEater eater;
        [SerializeField] private MouthController mouth;
        [SerializeField] private LotterySystem lottery;
        [SerializeField] private GameUI ui;
        [SerializeField] private JuicyFeedbacks feedbacks;
        [SerializeField] private IceCreamStickDefinition defaultStickDefinition;
        [SerializeField] private float stickRevealDelay = 0.45f;
        [SerializeField] private float prizeRevealDelay = 0.65f;
        [SerializeField] private int tableCapacity = 12;
        [SerializeField] private Vector2 tableCenter = new Vector2(0f, -0.65f);
        [SerializeField] private Vector2 tableSlotSpacing = new Vector2(1.35f, 1.1f);
        [SerializeField] private int tableColumns = 4;
        [SerializeField] private float tableItemMaxHeight = 0.82f;
        [SerializeField] private Vector3 focusedIceCreamPosition = new Vector3(0f, -0.28f, 0f);
        [SerializeField] private float focusedTableAlpha = 0.18f;

        private readonly List<TableIceCreamItem> tableItems = new List<TableIceCreamItem>();
        private readonly List<string> history = new List<string>();

        private GameState state = GameState.Table;
        private Transform tableRoot;
        private SpriteRenderer tableSurfaceRenderer;
        private Texture2D tableSurfaceTexture;
        private TableIceCreamItem activeItem;
        private int money;
        private int sticks;
        private int mouthLevel = 1;
        private int autoBiteLevel;
        private int luckLevel;
        private float autoBiteTimer;
        private bool resolvingCompletion;
        private bool initialized;

        private void Awake()
        {
            Initialize();
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            if (lottery == null)
            {
                lottery = GetComponent<LotterySystem>();
            }

            EnsureTableRoot();
            ui.BuildShop(iceCreams, BuyIceCream);
            ui.MouthUpgradeButton.onClick.AddListener(UpgradeMouth);
            ui.AutoBiteUpgradeButton.onClick.AddListener(UpgradeAutoBite);
            ui.LuckUpgradeButton.onClick.AddListener(UpgradeLuck);
            ui.PrizeContinueButton.onClick.AddListener(CompletePrizeReveal);

            eater.ProgressChanged += OnProgressChanged;
            eater.BiteApplied += OnBiteApplied;
            eater.Completed += OnIceCreamCompleted;
            mouth.SetEater(eater);
            mouth.SetLevel(mouthLevel);
            eater.Clear();
            eater.gameObject.SetActive(false);
            SetState(GameState.Table);
            RefreshUI();
            initialized = true;
        }

        private void OnDestroy()
        {
            if (tableSurfaceTexture != null)
            {
                Destroy(tableSurfaceTexture);
            }
        }

        private void Update()
        {
            if (PointerPressedThisFrame())
            {
                if (state == GameState.Table)
                {
                    TrySelectTableItem();
                }
                else if (state == GameState.FocusedEating)
                {
                    TryBiteFocusedIceCream();
                }
            }

            if (state == GameState.FocusedEating && !resolvingCompletion && autoBiteLevel > 0)
            {
                autoBiteTimer += Time.deltaTime;
                float interval = Mathf.Max(0.18f, 1f / autoBiteLevel);
                while (autoBiteTimer >= interval)
                {
                    autoBiteTimer -= interval;
                    Vector3 point = eater.transform.position + new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(-1.0f, 0.9f), 0f);
                    eater.TryBite(point, mouth.BiteRadiusWorld * 0.7f);
                }
            }
        }

        private bool PointerPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            if (UnityEngine.InputSystem.Touchscreen.current != null)
            {
                return UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            }
#endif
            return false;
        }

        private void BuyIceCream(int index)
        {
            if (state != GameState.Table || index < 0 || index >= iceCreams.Count)
            {
                return;
            }

            if (tableItems.Count >= tableCapacity)
            {
                ui.SetPrizeMessage("桌子放满了，先吃掉几根雪糕。");
                RefreshUI();
                return;
            }

            IceCreamDefinition definition = iceCreams[index];
            if (money < definition.price)
            {
                ui.SetPrizeMessage("钱不够，先吃几根雪糕碰碰运气。");
                RefreshUI();
                return;
            }

            money -= definition.price;
            AddTableItem(definition);
            ui.SetPrizeMessage($"买了一根{definition.displayName}。");
            RefreshUI();
        }

        private void AddTableItem(IceCreamDefinition definition)
        {
            GameObject itemObject = new GameObject("TableIceCream_" + definition.displayName);
            itemObject.transform.SetParent(tableRoot, false);
            itemObject.transform.position = GetSlotPosition(tableItems.Count);

            SpriteRenderer renderer = itemObject.AddComponent<SpriteRenderer>();
            renderer.sprite = definition.fullSprite;
            renderer.sortingOrder = 2 + tableItems.Count;
            renderer.color = Color.white;
            ScaleTableItem(itemObject.transform, definition.fullSprite);

            tableItems.Add(new TableIceCreamItem(definition, itemObject, renderer));
        }

        private void TrySelectTableItem()
        {
            if (IsPointerOverUI())
            {
                return;
            }

            Vector3 pointerWorld = GetPointerWorld();
            for (int i = tableItems.Count - 1; i >= 0; i--)
            {
                TableIceCreamItem item = tableItems[i];
                if (item.Contains(pointerWorld))
                {
                    EnterFocusedEating(item);
                    return;
                }
            }
        }

        private void EnterFocusedEating(TableIceCreamItem item)
        {
            activeItem = item;
            activeItem.SetVisible(false);
            SetTableAlpha(focusedTableAlpha);
            eater.transform.position = focusedIceCreamPosition;
            eater.gameObject.SetActive(true);
            eater.Load(activeItem.Definition, defaultStickDefinition);
            autoBiteTimer = 0f;
            ui.SetPrizeMessage("正在吃：" + activeItem.Definition.displayName);
            SetState(GameState.FocusedEating);
            RefreshUI();
        }

        private void TryBiteFocusedIceCream()
        {
            if (resolvingCompletion || IsPointerOverUI())
            {
                return;
            }

            if (mouth.RefreshCursorState() && eater.TryBite(mouth.GetPointerWorld(), mouth.BiteRadiusWorld))
            {
                mouth.PlayBiteAnimation();
            }
        }

        private void OnProgressChanged(float progress)
        {
            ui.SetProgress(progress);
        }

        private void OnBiteApplied(Vector3 position)
        {
            feedbacks.PlayBite();
        }

        private void OnIceCreamCompleted()
        {
            if (resolvingCompletion || state != GameState.FocusedEating)
            {
                return;
            }

            StartCoroutine(ResolveCompletedIceCream());
        }

        private IEnumerator ResolveCompletedIceCream()
        {
            resolvingCompletion = true;
            SetState(GameState.PrizeReveal);
            sticks++;
            RefreshUI();

            IceCreamDefinition definition = activeItem.Definition;
            PrizeResult result = lottery.Roll(definition.prizeMultiplier, luckLevel);
            eater.RevealStick(result.StickDefinition);
            feedbacks.PlayComplete();
            ui.SetPrizeMessage("雪糕吃完了，正在翻雪糕棍...");
            yield return new WaitForSeconds(stickRevealDelay);

            yield return new WaitForSeconds(prizeRevealDelay);

            money += result.FinalAmount;
            string message = result.IsWin ? $"中奖！{result.Label} x{definition.prizeMultiplier} = ￥{result.FinalAmount}" : "谢谢参与，下根再来";
            ui.SetPrizeMessage(message);
            ui.ShowPrizeModal(result.IsWin ? "中奖！" : "谢谢参与", message);
            history.Insert(0, message);
            while (history.Count > 5)
            {
                history.RemoveAt(history.Count - 1);
            }

            feedbacks.PlayPrize(result.IsWin);
            RefreshUI();
        }

        private void CompletePrizeReveal()
        {
            if (state != GameState.PrizeReveal || activeItem == null)
            {
                return;
            }

            RemoveTableItem(activeItem);
            activeItem = null;
            eater.HideStick();
            eater.Clear();
            eater.gameObject.SetActive(false);
            resolvingCompletion = false;
            SetTableAlpha(1f);
            RelayoutTableItems();
            ui.HidePrizeModal();
            SetState(GameState.Table);
            RefreshUI();
        }

        private void RemoveTableItem(TableIceCreamItem item)
        {
            tableItems.Remove(item);
            if (item.GameObject != null)
            {
                Destroy(item.GameObject);
            }
        }

        private void RelayoutTableItems()
        {
            for (int i = 0; i < tableItems.Count; i++)
            {
                tableItems[i].GameObject.transform.position = GetSlotPosition(i);
                tableItems[i].SetVisible(true);
                tableItems[i].SetAlpha(1f);
            }
        }

        private void UpgradeMouth()
        {
            if (state == GameState.PrizeReveal)
            {
                return;
            }

            int cost = GetMouthCost();
            if (money < cost)
            {
                return;
            }

            money -= cost;
            mouthLevel++;
            mouth.SetLevel(mouthLevel);
            RefreshUI();
        }

        private void UpgradeAutoBite()
        {
            if (state == GameState.PrizeReveal)
            {
                return;
            }

            int cost = GetAutoBiteCost();
            if (money < cost)
            {
                return;
            }

            money -= cost;
            autoBiteLevel++;
            RefreshUI();
        }

        private void UpgradeLuck()
        {
            if (state == GameState.PrizeReveal)
            {
                return;
            }

            int cost = GetLuckCost();
            if (money < cost)
            {
                return;
            }

            money -= cost;
            luckLevel++;
            RefreshUI();
        }

        private int GetMouthCost()
        {
            return Mathf.RoundToInt(20f * Mathf.Pow(1.8f, mouthLevel - 1));
        }

        private int GetAutoBiteCost()
        {
            return Mathf.RoundToInt(50f * Mathf.Pow(2f, autoBiteLevel));
        }

        private int GetLuckCost()
        {
            return Mathf.RoundToInt(80f * Mathf.Pow(1.9f, luckLevel));
        }

        private void RefreshUI()
        {
            IceCreamDefinition displayedDefinition = activeItem != null ? activeItem.Definition : (iceCreams.Count > 0 ? iceCreams[0] : null);
            string iceCreamName = displayedDefinition != null ? displayedDefinition.displayName : "暂无雪糕";
            int multiplier = displayedDefinition != null ? displayedDefinition.prizeMultiplier : 1;
            ui.SetStats(money, sticks, iceCreamName, multiplier, mouthLevel);
            ui.SetHistory(history);
            ui.SetUpgradeTexts(mouthLevel, GetMouthCost(), CanUseSideMenus() && money >= GetMouthCost(), autoBiteLevel, GetAutoBiteCost(), CanUseSideMenus() && money >= GetAutoBiteCost(), luckLevel, GetLuckCost(), CanUseSideMenus() && money >= GetLuckCost());
            ui.RefreshShop(iceCreams, money, tableItems.Count, tableCapacity, state == GameState.Table);
            ui.SetTableStatus(GetTableStatus());
        }

        private string GetTableStatus()
        {
            if (state == GameState.Table)
            {
                return tableItems.Count == 0 ? "从左侧买一根雪糕放到桌上" : $"桌上雪糕 {tableItems.Count}/{tableCapacity}，点击一根开始吃";
            }

            if (state == GameState.FocusedEating)
            {
                return "正在吃雪糕";
            }

            return "开奖中";
        }

        private bool CanUseSideMenus()
        {
            return state == GameState.Table || state == GameState.FocusedEating;
        }

        private void SetState(GameState newState)
        {
            state = newState;
            ui.SetMode(state == GameState.Table, state == GameState.FocusedEating, state == GameState.PrizeReveal);
        }

        private void EnsureTableRoot()
        {
            if (tableRoot != null)
            {
                return;
            }

            Transform existing = transform.Find("TableRoot");
            if (existing != null)
            {
                tableRoot = existing;
                tableSurfaceRenderer = tableRoot.GetComponentInChildren<SpriteRenderer>();
                return;
            }

            tableRoot = new GameObject("TableRoot").transform;
            tableRoot.SetParent(transform, false);
            CreateTableSurface();
        }

        private void CreateTableSurface()
        {
            GameObject surface = new GameObject("TableSurface");
            surface.transform.SetParent(tableRoot, false);
            surface.transform.position = new Vector3(tableCenter.x, tableCenter.y, 0f);
            surface.transform.localScale = new Vector3(6.2f, 3.4f, 1f);

            tableSurfaceTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tableSurfaceTexture.SetPixel(0, 0, new Color(0.98f, 0.82f, 0.55f, 1f));
            tableSurfaceTexture.Apply();

            Sprite surfaceSprite = Sprite.Create(tableSurfaceTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            tableSurfaceRenderer = surface.AddComponent<SpriteRenderer>();
            tableSurfaceRenderer.sprite = surfaceSprite;
            tableSurfaceRenderer.sortingOrder = -4;
        }

        private Vector3 GetSlotPosition(int index)
        {
            int columns = Mathf.Max(1, tableColumns);
            int row = index / columns;
            int column = index % columns;
            int visibleRows = Mathf.Max(1, Mathf.CeilToInt(tableCapacity / (float)columns));
            float startX = tableCenter.x - (columns - 1) * tableSlotSpacing.x * 0.5f;
            float startY = tableCenter.y + (visibleRows - 1) * tableSlotSpacing.y * 0.5f;
            return new Vector3(startX + column * tableSlotSpacing.x, startY - row * tableSlotSpacing.y, 0f);
        }

        private void ScaleTableItem(Transform itemTransform, Sprite sprite)
        {
            if (sprite == null)
            {
                itemTransform.localScale = Vector3.one;
                return;
            }

            float height = Mathf.Max(0.001f, sprite.bounds.size.y);
            float scale = tableItemMaxHeight / height;
            itemTransform.localScale = Vector3.one * scale;
        }

        private void SetTableAlpha(float alpha)
        {
            if (tableSurfaceRenderer != null)
            {
                tableSurfaceRenderer.color = WithAlpha(tableSurfaceRenderer.color, Mathf.Lerp(0.25f, 1f, alpha));
            }

            for (int i = 0; i < tableItems.Count; i++)
            {
                if (tableItems[i] != activeItem)
                {
                    tableItems[i].SetAlpha(alpha);
                }
            }
        }

        private Vector3 GetPointerWorld()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return Vector3.zero;
            }

            Vector2 screenPosition = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                screenPosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            }
            else if (UnityEngine.InputSystem.Touchscreen.current != null)
            {
                screenPosition = UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue();
            }
#endif
            Vector3 world = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -mainCamera.transform.position.z));
            world.z = 0f;
            return world;
        }

        private bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        private sealed class TableIceCreamItem
        {
            public readonly IceCreamDefinition Definition;
            public readonly GameObject GameObject;
            private readonly SpriteRenderer renderer;

            public TableIceCreamItem(IceCreamDefinition definition, GameObject gameObject, SpriteRenderer renderer)
            {
                Definition = definition;
                GameObject = gameObject;
                this.renderer = renderer;
            }

            public bool Contains(Vector3 worldPosition)
            {
                return renderer != null && renderer.bounds.Contains(worldPosition);
            }

            public void SetVisible(bool visible)
            {
                if (GameObject != null)
                {
                    GameObject.SetActive(visible);
                }
            }

            public void SetAlpha(float alpha)
            {
                if (renderer != null)
                {
                    Color color = renderer.color;
                    color.a = Mathf.Clamp01(alpha);
                    renderer.color = color;
                }
            }
        }
    }
}
