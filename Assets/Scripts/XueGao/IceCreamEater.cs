using System;
using UnityEngine;

namespace XueGao
{
    public class IceCreamEater : MonoBehaviour
    {
        public event Action<float> ProgressChanged;
        public event Action Completed;
        public event Action<Vector3> BiteApplied;

        [SerializeField] private SpriteRenderer iceCreamRenderer;
        [SerializeField] private SpriteRenderer stickRenderer;
        [SerializeField] private SpriteRenderer bitePreviewRenderer;
        [SerializeField] private float completeThreshold = 0.8f;

        private Texture2D runtimeTexture;
        private Color[] pixels;
        private bool[] ediblePixels;
        private bool[] eatenPixels;
        private int ediblePixelCount;
        private int eatenPixelCount;
        private bool completed;
        private IceCreamDefinition currentDefinition;

        public float Progress => ediblePixelCount <= 0 ? 0f : eatenPixelCount / (float)ediblePixelCount;
        public IceCreamDefinition CurrentDefinition => currentDefinition;

        private void Awake()
        {
            if (stickRenderer != null)
            {
                stickRenderer.gameObject.SetActive(false);
            }
        }

        public void Load(IceCreamDefinition definition)
        {
            currentDefinition = definition;
            completed = false;
            eatenPixelCount = 0;

            if (definition == null || definition.fullSprite == null)
            {
                return;
            }

            Sprite sourceSprite = definition.fullSprite;
            Texture2D sourceTexture = sourceSprite.texture;
            runtimeTexture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);
            runtimeTexture.filterMode = FilterMode.Point;
            pixels = sourceTexture.GetPixels();
            runtimeTexture.SetPixels(pixels);
            runtimeTexture.Apply();

            ediblePixels = new bool[pixels.Length];
            eatenPixels = new bool[pixels.Length];
            ediblePixelCount = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                ediblePixels[i] = pixels[i].a > 0.1f;
                if (ediblePixels[i])
                {
                    ediblePixelCount++;
                }
            }

            Sprite runtimeSprite = Sprite.Create(runtimeTexture, new Rect(0f, 0f, runtimeTexture.width, runtimeTexture.height), sourceSprite.pivot / sourceSprite.rect.size, sourceSprite.pixelsPerUnit);
            iceCreamRenderer.sprite = runtimeSprite;
            iceCreamRenderer.gameObject.SetActive(true);

            if (stickRenderer != null)
            {
                stickRenderer.sprite = definition.stickSprite;
                stickRenderer.gameObject.SetActive(false);
            }

            ProgressChanged?.Invoke(Progress);
        }

        public bool TryBite(Vector3 worldPosition, float radiusWorld)
        {
            if (completed || runtimeTexture == null || iceCreamRenderer.sprite == null)
            {
                return false;
            }

            Vector3 local = iceCreamRenderer.transform.InverseTransformPoint(worldPosition);
            float pixelsPerUnit = iceCreamRenderer.sprite.pixelsPerUnit;
            int centerX = Mathf.RoundToInt(local.x * pixelsPerUnit + runtimeTexture.width * 0.5f);
            int centerY = Mathf.RoundToInt(local.y * pixelsPerUnit + runtimeTexture.height * 0.5f);
            int radius = Mathf.CeilToInt(radiusWorld * pixelsPerUnit);
            int radiusSquared = radius * radius;
            int changed = 0;

            for (int y = Mathf.Max(0, centerY - radius); y <= Mathf.Min(runtimeTexture.height - 1, centerY + radius); y++)
            {
                for (int x = Mathf.Max(0, centerX - radius); x <= Mathf.Min(runtimeTexture.width - 1, centerX + radius); x++)
                {
                    int dx = x - centerX;
                    int dy = y - centerY;
                    if (dx * dx + dy * dy > radiusSquared)
                    {
                        continue;
                    }

                    int index = y * runtimeTexture.width + x;
                    if (!ediblePixels[index] || eatenPixels[index])
                    {
                        continue;
                    }

                    eatenPixels[index] = true;
                    pixels[index] = Color.clear;
                    eatenPixelCount++;
                    changed++;
                }
            }

            if (changed == 0)
            {
                return false;
            }

            runtimeTexture.SetPixels(pixels);
            runtimeTexture.Apply();
            BiteApplied?.Invoke(worldPosition);
            ProgressChanged?.Invoke(Progress);

            if (Progress >= completeThreshold)
            {
                completed = true;
                Completed?.Invoke();
            }

            return true;
        }

        public void ShowStick()
        {
            if (iceCreamRenderer != null)
            {
                iceCreamRenderer.gameObject.SetActive(false);
            }

            if (stickRenderer != null)
            {
                stickRenderer.gameObject.SetActive(true);
            }
        }

        public void HideStick()
        {
            if (stickRenderer != null)
            {
                stickRenderer.gameObject.SetActive(false);
            }
        }

        public void SetPreviewVisible(bool visible)
        {
            if (bitePreviewRenderer != null)
            {
                bitePreviewRenderer.gameObject.SetActive(visible);
            }
        }
    }
}
