using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace XueGao
{
    public class ShopItemView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text nameText;
        [SerializeField] private Text detailText;
        [SerializeField] private Image iconImage;

        public Button Button
        {
            get
            {
                ResolveReferences();
                return button;
            }
        }

        public RectTransform SourceRectTransform
        {
            get
            {
                ResolveReferences();
                return button != null ? button.transform as RectTransform : transform as RectTransform;
            }
        }

        public void Bind(IceCreamDefinition definition, int maxPrize, bool canBuy, UnityAction onClicked)
        {
            ResolveReferences();

            if (nameText != null)
            {
                nameText.text = definition.displayName;
            }

            if (detailText != null)
            {
                detailText.text = $"￥{definition.price}  最高￥{maxPrize}";
            }

            if (iconImage != null)
            {
                iconImage.sprite = definition.fullSprite;
                iconImage.enabled = definition.fullSprite != null;
                iconImage.preserveAspect = true;
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(onClicked);
                button.interactable = canBuy;
            }
        }

        public void SetCanBuy(bool canBuy)
        {
            ResolveReferences();

            if (button != null)
            {
                button.interactable = canBuy;
            }
        }

        private void ResolveReferences()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (nameText == null)
            {
                Transform nameTransform = transform.Find("NameText");
                nameText = nameTransform != null ? nameTransform.GetComponent<Text>() : null;
            }

            if (detailText == null)
            {
                Transform detailTransform = transform.Find("DetailText");
                detailText = detailTransform != null ? detailTransform.GetComponent<Text>() : null;
            }

            if (iconImage == null)
            {
                Transform iconTransform = transform.Find("Icon");
                iconImage = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
            }
        }
    }
}
