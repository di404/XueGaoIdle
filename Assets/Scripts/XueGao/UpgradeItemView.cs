using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace XueGao
{
    public class UpgradeItemView : MonoBehaviour
    {
        [SerializeField] private UpgradeType upgradeType;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text nameText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text levelText;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Text priceText;

        private UpgradeDefinition definition;

        public UpgradeType UpgradeType => upgradeType;

        public void Bind(UpgradeDefinition newDefinition, UnityAction<UpgradeType> onClicked)
        {
            definition = newDefinition;
            ResolveReferences();

            if (upgradeButton != null)
            {
                upgradeButton.onClick.RemoveAllListeners();
                upgradeButton.onClick.AddListener(() => onClicked?.Invoke(upgradeType));
            }

            Refresh(0, false);
        }

        public void Refresh(int money, bool canUseMenu)
        {
            ResolveReferences();

            if (definition == null)
            {
                SetMissing();
                return;
            }

            if (iconImage != null)
            {
                iconImage.sprite = definition.Icon;
                iconImage.enabled = definition.Icon != null;
                iconImage.preserveAspect = true;
            }

            if (nameText != null)
            {
                nameText.text = definition.DisplayName;
            }

            if (descriptionText != null)
            {
                descriptionText.text = definition.Description;
            }

            if (levelText != null)
            {
                levelText.text = "Lv." + definition.DisplayLevel;
            }

            bool canBuy = canUseMenu && definition.CanPurchase(money);
            if (upgradeButton != null)
            {
                upgradeButton.interactable = canBuy;
            }

            if (priceText != null)
            {
                priceText.text = definition.IsMaxLevel ? "已满级" : "￥" + definition.CurrentCost;
            }
        }

        private void SetMissing()
        {
            if (nameText != null)
            {
                nameText.text = "未配置";
            }

            if (descriptionText != null)
            {
                descriptionText.text = string.Empty;
            }

            if (levelText != null)
            {
                levelText.text = "Lv.0";
            }

            if (upgradeButton != null)
            {
                upgradeButton.interactable = false;
            }

            if (priceText != null)
            {
                priceText.text = "-";
            }
        }

        private void ResolveReferences()
        {
            if (iconImage == null)
            {
                Transform icon = transform.Find("Icon");
                iconImage = icon != null ? icon.GetComponent<Image>() : null;
            }

            if (nameText == null)
            {
                Transform label = transform.Find("NameText");
                nameText = label != null ? label.GetComponent<Text>() : null;
            }

            if (descriptionText == null)
            {
                Transform description = transform.Find("DescriptionText");
                descriptionText = description != null ? description.GetComponent<Text>() : null;
            }

            if (levelText == null)
            {
                Transform level = transform.Find("LevelText");
                levelText = level != null ? level.GetComponent<Text>() : null;
            }

            if (upgradeButton == null)
            {
                Transform button = transform.Find("UpgradeButton");
                upgradeButton = button != null ? button.GetComponent<Button>() : GetComponentInChildren<Button>(true);
            }

            if (priceText == null && upgradeButton != null)
            {
                Transform price = upgradeButton.transform.Find("PriceText");
                priceText = price != null ? price.GetComponent<Text>() : upgradeButton.GetComponentInChildren<Text>(true);
            }
        }
    }
}
