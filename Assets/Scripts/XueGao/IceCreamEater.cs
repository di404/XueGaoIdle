using System;
using System.Collections.Generic;
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
        [SerializeField] private float completeThreshold = 0.8f;

        private Texture2D runtimeTexture;
        private Color[] pixels;
        private bool[] ediblePixels;
        private bool[] eatenPixels;
        private readonly List<SamplePoint> samplePoints = new List<SamplePoint>();
        private readonly List<List<int>> sampleNeighbors = new List<List<int>>();
        private int ediblePixelCount;
        private int eatenPixelCount;
        private bool completed;
        private IceCreamDefinition currentDefinition;
        private IceCreamStickDefinition currentStickDefinition;

        public float Progress => ediblePixelCount <= 0 ? 0f : eatenPixelCount / (float)ediblePixelCount;
        public IceCreamDefinition CurrentDefinition => currentDefinition;
        public IceCreamStickDefinition CurrentStickDefinition => currentStickDefinition;

        private void Awake()
        {
            if (stickRenderer != null)
            {
                stickRenderer.gameObject.SetActive(false);
            }
        }

        public void Load(IceCreamDefinition definition, IceCreamStickDefinition stickDefinition = null)
        {
            currentDefinition = definition;
            currentStickDefinition = stickDefinition;
            completed = false;
            eatenPixelCount = 0;
            samplePoints.Clear();
            sampleNeighbors.Clear();

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

            float alphaThreshold = Mathf.Max(0f, definition.alphaThreshold);
            ediblePixels = new bool[pixels.Length];
            eatenPixels = new bool[pixels.Length];
            ediblePixelCount = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                ediblePixels[i] = pixels[i].a > alphaThreshold;
                if (ediblePixels[i])
                {
                    ediblePixelCount++;
                }
            }

            Sprite runtimeSprite = Sprite.Create(runtimeTexture, new Rect(0f, 0f, runtimeTexture.width, runtimeTexture.height), sourceSprite.pivot / sourceSprite.rect.size, sourceSprite.pixelsPerUnit);
            iceCreamRenderer.sprite = runtimeSprite;
            iceCreamRenderer.gameObject.SetActive(true);
            GenerateSamplePoints(sourceSprite, alphaThreshold);

            if (stickRenderer != null)
            {
                ApplyStickDefinition(stickDefinition);
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

            MarkSamplesEaten(local, radiusWorld);
            EvaluateDisconnectedPieces();
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

        public void RevealStick(IceCreamStickDefinition stickDefinition = null)
        {
            if (stickDefinition != null)
            {
                currentStickDefinition = stickDefinition;
            }

            if (iceCreamRenderer != null)
            {
                iceCreamRenderer.gameObject.SetActive(false);
            }

            if (stickRenderer != null)
            {
                ApplyStickDefinition(currentStickDefinition);
                stickRenderer.gameObject.SetActive(true);
            }
        }

        public void ShowStick()
        {
            RevealStick();
        }

        public void HideStick()
        {
            if (stickRenderer != null)
            {
                stickRenderer.gameObject.SetActive(false);
            }
        }

        private void ApplyStickDefinition(IceCreamStickDefinition stickDefinition)
        {
            if (stickRenderer == null)
            {
                return;
            }

            Sprite fallbackSprite = currentDefinition != null ? currentDefinition.stickSprite : null;
            stickRenderer.sprite = stickDefinition != null && stickDefinition.stickSprite != null ? stickDefinition.stickSprite : fallbackSprite;
            stickRenderer.color = stickDefinition != null ? stickDefinition.tint : Color.white;
        }

        private void GenerateSamplePoints(Sprite sourceSprite, float alphaThreshold)
        {
            if (runtimeTexture == null || sourceSprite == null || pixels == null)
            {
                return;
            }

            int targetCount = Mathf.Max(0, currentDefinition != null ? currentDefinition.sampleCount : 0);
            if (targetCount == 0)
            {
                return;
            }

            Rect bounds = FindEdiblePixelBounds(alphaThreshold);
            if (bounds.width <= 0f || bounds.height <= 0f)
            {
                return;
            }

            int gridSize = Mathf.CeilToInt(Mathf.Sqrt(targetCount));
            float stepX = bounds.width / gridSize;
            float stepY = bounds.height / gridSize;
            float pixelsPerUnit = sourceSprite.pixelsPerUnit;
            Vector2 pivot = sourceSprite.pivot;

            for (int y = 0; y < gridSize && samplePoints.Count < targetCount; y++)
            {
                for (int x = 0; x < gridSize && samplePoints.Count < targetCount; x++)
                {
                    int pixelX = Mathf.Clamp(Mathf.RoundToInt(bounds.xMin + (x + 0.5f) * stepX), 0, runtimeTexture.width - 1);
                    int pixelY = Mathf.Clamp(Mathf.RoundToInt(bounds.yMin + (y + 0.5f) * stepY), 0, runtimeTexture.height - 1);
                    int index = pixelY * runtimeTexture.width + pixelX;
                    if (pixels[index].a <= alphaThreshold)
                    {
                        continue;
                    }

                    Vector2 localPosition = new Vector2((pixelX - pivot.x) / pixelsPerUnit, (pixelY - pivot.y) / pixelsPerUnit);
                    samplePoints.Add(new SamplePoint(localPosition));
                }
            }

            BuildSampleNeighbors();
        }

        private Rect FindEdiblePixelBounds(float alphaThreshold)
        {
            int minX = runtimeTexture.width;
            int minY = runtimeTexture.height;
            int maxX = -1;
            int maxY = -1;

            for (int y = 0; y < runtimeTexture.height; y++)
            {
                for (int x = 0; x < runtimeTexture.width; x++)
                {
                    if (pixels[y * runtimeTexture.width + x].a <= alphaThreshold)
                    {
                        continue;
                    }

                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            }

            return maxX < minX || maxY < minY ? Rect.zero : Rect.MinMaxRect(minX, minY, maxX + 1, maxY + 1);
        }

        private void BuildSampleNeighbors()
        {
            sampleNeighbors.Clear();
            for (int i = 0; i < samplePoints.Count; i++)
            {
                sampleNeighbors.Add(new List<int>());
            }

            if (samplePoints.Count <= 1)
            {
                return;
            }

            float nearestDistance = float.MaxValue;
            for (int i = 0; i < samplePoints.Count; i++)
            {
                for (int j = i + 1; j < samplePoints.Count; j++)
                {
                    float distance = Vector2.Distance(samplePoints[i].LocalPosition, samplePoints[j].LocalPosition);
                    if (distance > 0f)
                    {
                        nearestDistance = Mathf.Min(nearestDistance, distance);
                    }
                }
            }

            float neighborDistance = nearestDistance * 1.55f;
            float neighborDistanceSquared = neighborDistance * neighborDistance;
            for (int i = 0; i < samplePoints.Count; i++)
            {
                for (int j = i + 1; j < samplePoints.Count; j++)
                {
                    if ((samplePoints[i].LocalPosition - samplePoints[j].LocalPosition).sqrMagnitude > neighborDistanceSquared)
                    {
                        continue;
                    }

                    sampleNeighbors[i].Add(j);
                    sampleNeighbors[j].Add(i);
                }
            }
        }

        private void MarkSamplesEaten(Vector3 biteLocalPosition, float radiusWorld)
        {
            float radiusLocal = radiusWorld / Mathf.Max(iceCreamRenderer.transform.lossyScale.x, 0.0001f);
            float radiusSquared = radiusLocal * radiusLocal;
            Vector2 bitePosition = new Vector2(biteLocalPosition.x, biteLocalPosition.y);

            for (int i = 0; i < samplePoints.Count; i++)
            {
                SamplePoint samplePoint = samplePoints[i];
                if (samplePoint.IsEaten || (samplePoint.LocalPosition - bitePosition).sqrMagnitude > radiusSquared)
                {
                    continue;
                }

                samplePoint.IsEaten = true;
                samplePoints[i] = samplePoint;
            }
        }

        private void EvaluateDisconnectedPieces()
        {
            // Reserved for future island detection and small-piece fade out.
            if (samplePoints.Count == 0 || sampleNeighbors.Count != samplePoints.Count)
            {
                return;
            }
        }

        private struct SamplePoint
        {
            public readonly Vector2 LocalPosition;
            public bool IsEaten;

            public SamplePoint(Vector2 localPosition)
            {
                LocalPosition = localPosition;
                IsEaten = false;
            }
        }
    }
}
