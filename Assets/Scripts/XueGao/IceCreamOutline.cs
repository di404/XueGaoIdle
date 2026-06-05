using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

namespace XueGao
{
    public class IceCreamOutline : MonoBehaviour
    {
        [Header("Outline")]
        [SerializeField] private float outlineScale = 1.06f;
        [SerializeField] private Color outlineColor = new Color(0.18f, 0.12f, 0.22f, 0.9f);
        [SerializeField] private Color hoverOutlineColor = new Color(1f, 0.9f, 0.3f, 1f);
        [SerializeField] private float hoverDuration = 0.08f;

        private SpriteRenderer iceCreamRenderer;
        private SpriteRenderer stickRenderer;
        [SerializeField] private SpriteRenderer iceCreamOutlineRenderer;
        [SerializeField] private SpriteRenderer stickOutlineRenderer;
        private Transform visualRoot;
        private Coroutine hoverRoutine;
        private bool initialized;
        private bool isHovered;
        private float currentAlpha = 1f;

        public Transform VisualRoot => visualRoot != null ? visualRoot : transform;

        public void Initialize(SpriteRenderer iceCream, SpriteRenderer stick)
        {
            if (iceCream == null || stick == null)
            {
                return;
            }

            iceCreamRenderer = iceCream;
            stickRenderer = stick;
            if (!initialized)
            {
                CreateVisualRoot();
                CreateOutlineRenderers();
                initialized = true;
            }

            LoadIceCreamSprite(iceCream.sprite);
            LoadStickSprite(stick.sprite);
            SetSortingOrder(iceCream.sortingOrder - 2);
            ApplyOutlineColor(isHovered ? hoverOutlineColor : outlineColor);
        }

        public void SetVisible(bool visible)
        {
            if (iceCreamOutlineRenderer != null)
            {
                iceCreamOutlineRenderer.gameObject.SetActive(visible);
            }

            if (stickOutlineRenderer != null)
            {
                stickOutlineRenderer.gameObject.SetActive(visible);
            }

            if (!visible)
            {
                SetHovered(false, false);
            }
        }

        [Button("Set Hover Color")]
        public void SetHoverColor()
        {
            SetHovered(true);
        }


        [Button("Set Default Color")]
        public void SetDefaultColor()
        {
            SetHovered(false);
        }

        private void LoadIceCreamSprite(Sprite sprite)
        {
            if (iceCreamOutlineRenderer != null)
            {
                iceCreamOutlineRenderer.sprite = sprite;
            }
        }

        private void LoadStickSprite(Sprite sprite)
        {
            if (stickOutlineRenderer != null)
            {
                stickOutlineRenderer.sprite = sprite;
            }
        }

        private void SetSortingOrder(int sortingOrder)
        {
            if (iceCreamOutlineRenderer != null)
            {
                iceCreamOutlineRenderer.sortingOrder = sortingOrder;
            }

            if (stickOutlineRenderer != null)
            {
                stickOutlineRenderer.sortingOrder = sortingOrder;
            }
        }

        public void SetHovered(bool hovered, bool playFeedback = true)
        {
            if (isHovered == hovered)
            {
                return;
            }

            isHovered = hovered;
            Color targetColor = hovered ? hoverOutlineColor : outlineColor;
            if (!playFeedback || hoverDuration <= 0f || !isActiveAndEnabled)
            {
                StopHoverRoutine();
                ApplyOutlineColor(targetColor);
                return;
            }

            StopHoverRoutine();
            hoverRoutine = StartCoroutine(AnimateOutlineColor(ReadOutlineColor(), targetColor));
        }

        private void CreateOutlineRenderers()
        {
            if (iceCreamOutlineRenderer != null && stickOutlineRenderer != null)
            {
                return;
            }

            Transform outlineRoot = new GameObject("FeelOutline").transform;
            outlineRoot.SetParent(VisualRoot, false);
            outlineRoot.localScale = Vector3.one * outlineScale;
            outlineRoot.gameObject.layer = gameObject.layer;
            iceCreamOutlineRenderer = CreateOutlineRenderer("IceCreamOutline", outlineRoot, iceCreamRenderer);
            stickOutlineRenderer = CreateOutlineRenderer("StickOutline", outlineRoot, stickRenderer);
        }

        private void CreateVisualRoot()
        {
            if (visualRoot != null)
            {
                return;
            }

            visualRoot = new GameObject("FeelVisual").transform;
            visualRoot.SetParent(transform, false);
            visualRoot.gameObject.layer = gameObject.layer;
            iceCreamRenderer.transform.SetParent(visualRoot, false);
            stickRenderer.transform.SetParent(visualRoot, false);

            Transform outlineRoot = null;
            if (iceCreamOutlineRenderer != null)
            {
                outlineRoot = iceCreamOutlineRenderer.transform.parent;
            }
            else
            {
                outlineRoot = transform.Find("Outline");
            }

            if (outlineRoot != null && outlineRoot != visualRoot)
            {
                outlineRoot.SetParent(visualRoot, false);
            }
        }

        private SpriteRenderer CreateOutlineRenderer(string objectName, Transform parent, SpriteRenderer source)
        {
            GameObject outlineObject = new GameObject(objectName);
            outlineObject.layer = source.gameObject.layer;
            outlineObject.transform.SetParent(parent, false);
            outlineObject.transform.localPosition = source.transform.localPosition;
            outlineObject.transform.localRotation = source.transform.localRotation;
            outlineObject.transform.localScale = source.transform.localScale;

            SpriteRenderer outlineRenderer = outlineObject.AddComponent<SpriteRenderer>();
            outlineRenderer.color = outlineColor;
            return outlineRenderer;
        }

        private IEnumerator AnimateOutlineColor(Color from, Color to)
        {
            float elapsed = 0f;
            while (elapsed < hoverDuration)
            {
                elapsed += Time.deltaTime;
                ApplyOutlineColor(Color.Lerp(from, to, Mathf.Clamp01(elapsed / hoverDuration)));
                yield return null;
            }

            ApplyOutlineColor(to);
            hoverRoutine = null;
        }

        private void ApplyOutlineColor(Color color)
        {
            color.a *= currentAlpha;
            ApplyRendererColor(iceCreamOutlineRenderer, color);
            ApplyRendererColor(stickOutlineRenderer, color);
        }

        private Color ReadOutlineColor()
        {
            if (iceCreamOutlineRenderer != null)
            {
                Material material = iceCreamOutlineRenderer.material;
                if (material != null && material.HasProperty("_SolidColor"))
                {
                    return material.GetColor("_SolidColor");
                }

                return iceCreamOutlineRenderer.color;
            }

            return isHovered ? hoverOutlineColor : outlineColor;
        }

        private void StopHoverRoutine()
        {
            if (hoverRoutine == null)
            {
                return;
            }

            StopCoroutine(hoverRoutine);
            hoverRoutine = null;
        }

        private static void ApplyRendererColor(SpriteRenderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.color = color;
            Material material = renderer.material;
            if (material != null && material.HasProperty("_SolidColor"))
            {
                material.SetColor("_SolidColor", color);
            }
        }
    }
}
