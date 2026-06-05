using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace XueGao
{
    public class GameUI : MonoBehaviour
    {
        private const int HighestBasePrize = 500;

        [SerializeField] private Text moneyText;
        [SerializeField] private Text sticksText;
        [SerializeField] private Text currentIceCreamText;
        [SerializeField] private Text mouthLevelText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Text progressText;
        [SerializeField] private Text prizeText;
        [SerializeField] private Text historyText;
        [SerializeField] private Button mouthUpgradeButton;
        [SerializeField] private Text mouthUpgradeText;
        [SerializeField] private Button autoBiteUpgradeButton;
        [SerializeField] private Text autoBiteUpgradeText;
        [SerializeField] private Button luckUpgradeButton;
        [SerializeField] private Text luckUpgradeText;
        [SerializeField] private Transform shopRoot;
        [SerializeField] private ShopItemView shopItemPrefab;
        [SerializeField] private CanvasGroup storePanelGroup;
        [SerializeField] private CanvasGroup upgradePanelGroup;
        [SerializeField] private CanvasGroup topBarGroup;
        [SerializeField] private CanvasGroup focusOverlay;
        [SerializeField] private Text tableStatusText;
        [SerializeField] private CanvasGroup prizeModalGroup;
        [SerializeField] private Text prizeModalTitle;
        [SerializeField] private Text prizeModalBody;
        [SerializeField] private Button prizeContinueButton;

        private readonly List<ShopItemView> shopItems = new List<ShopItemView>();

        public Button MouthUpgradeButton => mouthUpgradeButton;
        public Button AutoBiteUpgradeButton => autoBiteUpgradeButton;
        public Button LuckUpgradeButton => luckUpgradeButton;
        public Button PrizeContinueButton
        {
            get
            {
                EnsureRuntimePanels();
                return prizeContinueButton;
            }
        }

        private void Awake()
        {
            EnsureRuntimePanels();
            SetPrizeModalVisible(false);
        }

        public void SetStats(int money, int sticks, string iceCreamName, int multiplier, int mouthLevel)
        {
            moneyText.text = "￥" + money;
            sticksText.text = "雪糕棍 " + sticks + " 根";
            currentIceCreamText.text = iceCreamName + "  x" + multiplier;
            mouthLevelText.text = "嘴巴 Lv." + mouthLevel;
        }

        public void SetProgress(float progress)
        {
            progressSlider.value = Mathf.Clamp01(progress / 0.8f);
            progressText.text = "已吃 " + Mathf.FloorToInt(progress * 100f) + "% / 80%";
        }

        public void SetPrizeMessage(string message)
        {
            prizeText.text = message;
        }

        public void SetHistory(IReadOnlyList<string> history)
        {
            historyText.text = history.Count == 0 ? "开奖记录：暂无" : "开奖记录：\n" + string.Join("\n", history);
        }

        public void SetTableStatus(string message)
        {
            EnsureRuntimePanels();
            tableStatusText.text = message;
        }

        public void SetUpgradeTexts(int mouthLevel, int mouthCost, bool canBuyMouth, int autoLevel, int autoCost, bool canBuyAuto, int luckLevel, int luckCost, bool canBuyLuck)
        {
            mouthUpgradeText.text = $"升级嘴巴 Lv.{mouthLevel}\n咬痕变大  ￥{mouthCost}";
            autoBiteUpgradeText.text = $"自动吃 Lv.{autoLevel}\n每秒自动咬  ￥{autoCost}";
            luckUpgradeText.text = $"幸运值 Lv.{luckLevel}\n更容易中奖  ￥{luckCost}";
            mouthUpgradeButton.interactable = canBuyMouth;
            autoBiteUpgradeButton.interactable = canBuyAuto;
            luckUpgradeButton.interactable = canBuyLuck;
        }

        public void BuildShop(IReadOnlyList<IceCreamDefinition> definitions, UnityAction<int, RectTransform> onClicked)
        {
            if (shopRoot == null || shopItemPrefab == null)
            {
                return;
            }

            for (int i = shopRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(shopRoot.GetChild(i).gameObject);
            }

            shopItems.Clear();

            for (int i = 0; i < definitions.Count; i++)
            {
                int index = i;
                IceCreamDefinition definition = definitions[i];
                int maxPrize = HighestBasePrize * Mathf.Max(1, definition.prizeMultiplier);
                ShopItemView item = Instantiate(shopItemPrefab, shopRoot);
                item.gameObject.SetActive(true);
                item.Bind(definition, maxPrize, true, () => onClicked(index, item.SourceRectTransform));
                shopItems.Add(item);
            }
        }

        public void RefreshShop(IReadOnlyList<IceCreamDefinition> definitions, int money, int tableCount, int tableCapacity, bool canBuy)
        {
            for (int i = 0; i < shopItems.Count && i < definitions.Count; i++)
            {
                IceCreamDefinition definition = definitions[i];
                shopItems[i].SetCanBuy(canBuy && tableCount < tableCapacity && money >= definition.price);
            }
        }

        public void SetMode(bool tableMode, bool focusedMode, bool prizeMode)
        {
            EnsureRuntimePanels();
            SetPanelState(storePanelGroup, tableMode, 1f);
            SetPanelState(upgradePanelGroup, tableMode, 1f);
            SetPanelState(topBarGroup, true, 1f);
            SetPanelState(focusOverlay, focusedMode || prizeMode, prizeMode ? 0.72f : 0.38f);

            if (progressSlider != null)
            {
                progressSlider.gameObject.SetActive(focusedMode || prizeMode);
            }

            if (progressText != null)
            {
                progressText.gameObject.SetActive(focusedMode || prizeMode);
            }

            if (!prizeMode)
            {
                SetPrizeModalVisible(false);
            }
        }

        public void ShowPrizeModal(string title, string body)
        {
            EnsureRuntimePanels();
            prizeModalTitle.text = title;
            prizeModalBody.text = body;
            SetPrizeModalVisible(true);
        }

        public void HidePrizeModal()
        {
            SetPrizeModalVisible(false);
        }

        private void EnsureRuntimePanels()
        {
            if (storePanelGroup == null)
            {
                storePanelGroup = FindChildCanvasGroup("StorePanel");
            }

            if (upgradePanelGroup == null)
            {
                upgradePanelGroup = FindChildCanvasGroup("UpgradePanel");
            }

            if (topBarGroup == null)
            {
                topBarGroup = FindChildCanvasGroup("TopBar");
            }

            if (focusOverlay == null)
            {
                focusOverlay = CreateOverlay("FocusOverlay", new Color(0f, 0f, 0f, 0.42f));
            }

            if (tableStatusText == null)
            {
                tableStatusText = CreateText("TableStatusText", transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 32f), new Vector2(420f, 34f), 16, TextAnchor.MiddleCenter);
                tableStatusText.color = new Color(0.12f, 0.16f, 0.18f, 1f);
            }

            if (prizeModalGroup == null)
            {
                CreatePrizeModal();
            }
        }

        private CanvasGroup FindChildCanvasGroup(string childName)
        {
            Transform child = transform.Find(childName);
            if (child == null)
            {
                return null;
            }

            CanvasGroup group = child.GetComponent<CanvasGroup>();
            return group != null ? group : child.gameObject.AddComponent<CanvasGroup>();
        }

        private CanvasGroup CreateOverlay(string objectName, Color color)
        {
            GameObject overlay = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            overlay.transform.SetParent(transform, false);
            overlay.transform.SetAsFirstSibling();

            RectTransform rect = overlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = overlay.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            CanvasGroup group = overlay.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return group;
        }

        private void CreatePrizeModal()
        {
            GameObject modal = new GameObject("PrizeModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            modal.transform.SetParent(transform, false);
            modal.transform.SetAsLastSibling();

            RectTransform modalRect = modal.GetComponent<RectTransform>();
            modalRect.anchorMin = new Vector2(0.5f, 0.5f);
            modalRect.anchorMax = new Vector2(0.5f, 0.5f);
            modalRect.anchoredPosition = Vector2.zero;
            modalRect.sizeDelta = new Vector2(360f, 210f);

            Image modalImage = modal.GetComponent<Image>();
            modalImage.color = new Color(1f, 0.96f, 0.82f, 0.98f);

            prizeModalGroup = modal.GetComponent<CanvasGroup>();
            prizeModalTitle = CreateText("Title", modal.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(320f, 42f), 24, TextAnchor.MiddleCenter);
            prizeModalBody = CreateText("Body", modal.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(320f, 72f), 18, TextAnchor.MiddleCenter);
            prizeModalTitle.color = new Color(0.12f, 0.1f, 0.08f, 1f);
            prizeModalBody.color = new Color(0.18f, 0.14f, 0.1f, 1f);

            prizeContinueButton = CreateButton("ContinueButton", modal.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(160f, 44f), "继续");
        }

        private Text CreateText(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private Button CreateButton(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, string label)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(1f, 0.78f, 0.32f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            Text text = CreateText("Label", buttonObject.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-16f, -8f), 18, TextAnchor.MiddleCenter);
            text.text = label;
            text.color = new Color(0.1f, 0.12f, 0.14f, 1f);
            return button;
        }

        private void SetPanelState(CanvasGroup group, bool active, float alpha)
        {
            if (group == null)
            {
                return;
            }

            group.alpha = active ? alpha : 0f;
            group.interactable = active && alpha >= 0.9f;
            group.blocksRaycasts = active && alpha >= 0.9f;
        }

        private void SetPrizeModalVisible(bool visible)
        {
            if (prizeModalGroup == null)
            {
                return;
            }

            prizeModalGroup.alpha = visible ? 1f : 0f;
            prizeModalGroup.interactable = visible;
            prizeModalGroup.blocksRaycasts = visible;
            prizeModalGroup.gameObject.SetActive(visible);
        }
    }
}
