using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XueGao
{
    public class GameUI : MonoBehaviour
    {
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
        [SerializeField] private Button shopButtonPrefab;

        private readonly List<Button> shopButtons = new List<Button>();
        private readonly List<Text> shopLabels = new List<Text>();

        public Button MouthUpgradeButton => mouthUpgradeButton;
        public Button AutoBiteUpgradeButton => autoBiteUpgradeButton;
        public Button LuckUpgradeButton => luckUpgradeButton;

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

        public void SetUpgradeTexts(int mouthLevel, int mouthCost, bool canBuyMouth, int autoLevel, int autoCost, bool canBuyAuto, int luckLevel, int luckCost, bool canBuyLuck)
        {
            mouthUpgradeText.text = $"升级嘴巴 Lv.{mouthLevel}\n咬痕变大  ￥{mouthCost}";
            autoBiteUpgradeText.text = $"自动吃 Lv.{autoLevel}\n每秒自动咬  ￥{autoCost}";
            luckUpgradeText.text = $"幸运值 Lv.{luckLevel}\n更容易中奖  ￥{luckCost}";
            mouthUpgradeButton.interactable = canBuyMouth;
            autoBiteUpgradeButton.interactable = canBuyAuto;
            luckUpgradeButton.interactable = canBuyLuck;
        }

        public void BuildShop(IReadOnlyList<IceCreamDefinition> definitions, UnityEngine.Events.UnityAction<int> onClicked)
        {
            for (int i = shopButtons.Count - 1; i >= 0; i--)
            {
                Destroy(shopButtons[i].gameObject);
            }

            shopButtons.Clear();
            shopLabels.Clear();

            for (int i = 0; i < definitions.Count; i++)
            {
                int index = i;
                Button button = Instantiate(shopButtonPrefab, shopRoot);
                button.gameObject.SetActive(true);
                button.onClick.AddListener(() => onClicked(index));
                shopButtons.Add(button);
                shopLabels.Add(button.GetComponentInChildren<Text>());
            }
        }

        public void RefreshShop(IReadOnlyList<IceCreamDefinition> definitions, IReadOnlyList<bool> unlocked, int currentIndex, int money)
        {
            for (int i = 0; i < shopButtons.Count; i++)
            {
                IceCreamDefinition definition = definitions[i];
                bool isUnlocked = unlocked[i];
                bool isCurrent = i == currentIndex;
                if (isCurrent)
                {
                    shopLabels[i].text = $"{definition.displayName}\n使用中  x{definition.prizeMultiplier}";
                    shopButtons[i].interactable = true;
                }
                else if (isUnlocked)
                {
                    shopLabels[i].text = $"{definition.displayName}\n已解锁，点击切换  x{definition.prizeMultiplier}";
                    shopButtons[i].interactable = true;
                }
                else
                {
                    shopLabels[i].text = $"{definition.displayName}\n购买 ￥{definition.price}  x{definition.prizeMultiplier}";
                    shopButtons[i].interactable = money >= definition.price;
                }
            }
        }
    }
}
