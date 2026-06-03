using System;
using System.Collections.Generic;
using UnityEngine;

namespace XueGao
{
    public class IceCreamTable : MonoBehaviour
    {
        public event Action<IceCream> IceCreamClicked;

        [SerializeField] private IceCreamFactory iceCreamFactory;
        [SerializeField] private int tableCapacity = 12;
        [SerializeField] private Vector2 tableCenter = new Vector2(0f, -0.65f);
        [SerializeField] private Vector2 tableSlotSpacing = new Vector2(1.35f, 1.1f);
        [SerializeField] private int tableColumns = 4;
        [SerializeField] private float tableItemMaxHeight = 0.82f;

        private readonly List<IceCream> iceCreams = new List<IceCream>();
        private Transform tableRoot;
        private SpriteRenderer tableSurfaceRenderer;
        private Texture2D tableSurfaceTexture;

        public int Count => iceCreams.Count;
        public int Capacity => tableCapacity;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (iceCreamFactory == null)
            {
                iceCreamFactory = GetComponent<IceCreamFactory>();
            }

            EnsureTableRoot();
        }

        public bool TryAdd(IceCreamDefinition definition, out IceCream iceCream, out string failureMessage)
        {
            Initialize();
            iceCream = null;
            failureMessage = null;

            if (Count >= tableCapacity)
            {
                failureMessage = "桌子放满了，先吃掉几根雪糕。";
                return false;
            }

            if (iceCreamFactory == null)
            {
                failureMessage = "缺少雪糕工厂，无法生成雪糕。";
                return false;
            }

            iceCream = iceCreamFactory.Create(definition, tableRoot, GetSlotPosition(iceCreams.Count), 2 + iceCreams.Count);
            if (iceCream == null)
            {
                failureMessage = "雪糕模板未配置，无法生成雪糕。";
                return false;
            }

            ScaleTableItem(iceCream.transform, definition != null ? definition.fullSprite : null);
            iceCream.Clicked += OnIceCreamClicked;
            iceCream.HoverChanged += OnIceCreamHoverChanged;
            iceCream.InteractionEnabled = true;
            iceCream.DragEnabled = true;
            iceCream.RefreshCollider();
            iceCreams.Add(iceCream);
            return true;
        }

        public void Remove(IceCream iceCream)
        {
            if (iceCream == null)
            {
                return;
            }

            iceCream.Clicked -= OnIceCreamClicked;
            iceCream.HoverChanged -= OnIceCreamHoverChanged;
            iceCreams.Remove(iceCream);
            Destroy(iceCream.gameObject);
        }

        public void Relayout()
        {
            for (int i = 0; i < iceCreams.Count; i++)
            {
                IceCream iceCream = iceCreams[i];
                if (iceCream == null)
                {
                    continue;
                }

                iceCream.transform.SetParent(tableRoot, false);
                iceCream.transform.position = GetSlotPosition(i);
                iceCream.transform.localRotation = Quaternion.identity;
                ScaleTableItem(iceCream.transform, iceCream.CurrentDefinition != null ? iceCream.CurrentDefinition.fullSprite : null);
                iceCream.gameObject.SetActive(true);
                iceCream.SetAlpha(1f);
                iceCream.SetSortingOrder(2 + i);
                iceCream.InteractionEnabled = true;
                iceCream.DragEnabled = true;
                iceCream.RefreshCollider();
            }
        }

        public void SetTableAlpha(float alpha, IceCream excluded = null)
        {
            if (tableSurfaceRenderer != null)
            {
                tableSurfaceRenderer.color = WithAlpha(tableSurfaceRenderer.color, Mathf.Lerp(0.25f, 1f, alpha));
            }

            for (int i = 0; i < iceCreams.Count; i++)
            {
                IceCream iceCream = iceCreams[i];
                if (iceCream != null && iceCream != excluded)
                {
                    iceCream.SetAlpha(alpha);
                }
            }
        }

        public void SetInteractionEnabled(bool interactionEnabled, bool dragEnabled)
        {
            for (int i = 0; i < iceCreams.Count; i++)
            {
                IceCream iceCream = iceCreams[i];
                if (iceCream == null)
                {
                    continue;
                }

                iceCream.InteractionEnabled = interactionEnabled;
                iceCream.DragEnabled = dragEnabled;
            }
        }

        public string GetStatus(bool isTableMode)
        {
            if (!isTableMode)
            {
                return "正在吃雪糕";
            }

            return Count == 0 ? "从左侧买一根雪糕放到桌上" : $"桌上雪糕 {Count}/{tableCapacity}，点击一根开始吃";
        }

        private void OnDestroy()
        {
            for (int i = 0; i < iceCreams.Count; i++)
            {
                if (iceCreams[i] != null)
                {
                    iceCreams[i].Clicked -= OnIceCreamClicked;
                    iceCreams[i].HoverChanged -= OnIceCreamHoverChanged;
                }
            }

            if (tableSurfaceTexture != null)
            {
                Destroy(tableSurfaceTexture);
            }
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
            if (itemTransform == null)
            {
                return;
            }

            if (sprite == null)
            {
                itemTransform.localScale = Vector3.one;
                return;
            }

            float height = Mathf.Max(0.001f, sprite.bounds.size.y);
            float scale = tableItemMaxHeight / height;
            itemTransform.localScale = Vector3.one * scale;
        }

        private void OnIceCreamClicked(IceCream iceCream)
        {
            IceCreamClicked?.Invoke(iceCream);
        }

        private void OnIceCreamHoverChanged(IceCream iceCream, bool isHovered)
        {
            // Hook kept here so table-level hover behavior can evolve without involving GameManager.
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }
    }
}
