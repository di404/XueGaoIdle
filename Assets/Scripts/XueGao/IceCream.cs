using System;
using System.Collections.Generic;
using UnityEngine;

namespace XueGao
{
    public class IceCream : MonoBehaviour
    {
        public event Action<float> ProgressChanged;

        [SerializeField] private SpriteRenderer iceCreamRenderer;
        [SerializeField] private SpriteRenderer stickRenderer;
        [SerializeField] private bool debugDrawSamplePoints = true;
        [SerializeField] private float debugSamplePointSize = 0.035f;
        [SerializeField] private Color debugUneatenSampleColor = new Color(0.15f, 0.95f, 1f, 0.95f);
        [SerializeField] private Color debugEatenSampleColor = new Color(1f, 0.35f, 0.35f, 0.95f);
        [SerializeField] private IceCreamDetachedPiecePresenter detachedPiecePresenter;

        private Texture2D runtimeTexture;
        private Sprite runtimeSprite;
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
        private BoxCollider2D boxCollider;
        private IceCreamOutline outline;

        public float Progress => ediblePixelCount <= 0 ? 0f : eatenPixelCount / (float)ediblePixelCount;
        public IceCreamDefinition CurrentDefinition => currentDefinition;
        public IceCreamStickDefinition CurrentStickDefinition => currentStickDefinition;
        public IceCreamOutline Outline => ResolveOutline();
        public Transform VisualRoot => Outline != null && Outline.VisualRoot != null ? Outline.VisualRoot : transform;

        private void Awake()
        {
            ResolveRenderers();
            ResolveCollider();
        }

        public void LoadForEating(IceCreamStickDefinition stickDefinition = null)
        {
            ReleaseRuntimeSpriteAndTexture();
            ClearDetachedPieces();
            var definition = currentDefinition;
            currentStickDefinition = stickDefinition;
            completed = false;
            eatenPixelCount = 0;
            samplePoints.Clear();
            sampleNeighbors.Clear();

            if (definition == null || definition.fullSprite == null)
            {
                ediblePixelCount = 0;
                pixels = null;
                ediblePixels = null;
                eatenPixels = null;
                SetIceCreamSprite(null, false);
                return;
            }

            Sprite sourceSprite = definition.fullSprite;
            Texture2D sourceTexture = sourceSprite.texture;
            Rect textureRect = sourceSprite.textureRect;
            int rectWidth = Mathf.Max(1, Mathf.RoundToInt(textureRect.width));
            int rectHeight = Mathf.Max(1, Mathf.RoundToInt(textureRect.height));

            runtimeTexture = new Texture2D(rectWidth, rectHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };

            pixels = sourceTexture.GetPixels(Mathf.RoundToInt(textureRect.x), Mathf.RoundToInt(textureRect.y), rectWidth, rectHeight);
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

            runtimeSprite = Sprite.Create(runtimeTexture, new Rect(0f, 0f, runtimeTexture.width, runtimeTexture.height), sourceSprite.pivot / sourceSprite.rect.size, sourceSprite.pixelsPerUnit);
            SetIceCreamSprite(runtimeSprite, true);
            GenerateSamplePoints(sourceSprite, alphaThreshold);
            ApplyStickDefinition(stickDefinition);
            InitOutline();
        }

        public void LoadDefinitionForTable(IceCreamDefinition definition)
        {
            ReleaseRuntimeSpriteAndTexture();
            ClearDetachedPieces();
            currentDefinition = definition;
            currentStickDefinition = null;
            completed = false;
            ediblePixelCount = 0;
            eatenPixelCount = 0;
            pixels = null;
            ediblePixels = null;
            eatenPixels = null;
            samplePoints.Clear();
            sampleNeighbors.Clear();
            SetIceCreamSprite(definition != null ? definition.fullSprite : null, definition != null && definition.fullSprite != null);
            InitOutline();
            //HideStick();
        }

        public BiteResult TryBite(Vector3 worldPosition, float radiusWorld)
        {
            if (CountBiteablePixels(worldPosition, radiusWorld, out Vector3 local) == 0)
            {
                return BiteResult.NotApplied(Progress);
            }

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
                return BiteResult.NotApplied(Progress);
            }

            MarkSamplesEaten(local, radiusWorld);
            EvaluateDisconnectedPieces();
            runtimeTexture.SetPixels(pixels);
            runtimeTexture.Apply();
            return BiteResult.Applied(Progress);
        }

        public bool CanBite(Vector3 worldPosition, float radiusWorld)
        {
            return CountBiteablePixels(worldPosition, radiusWorld, out _) > 0;
        }

        public bool ContainsSpritePoint(Vector3 worldPosition)
        {
            if (iceCreamRenderer == null || iceCreamRenderer.sprite == null || !iceCreamRenderer.gameObject.activeInHierarchy)
            {
                return false;
            }

            Vector3 localPosition = iceCreamRenderer.transform.InverseTransformPoint(worldPosition);
            return iceCreamRenderer.sprite.bounds.Contains(localPosition);
        }

        public bool TryGetSpriteWorldCorners(out Vector3 bottomLeft, out Vector3 bottomRight, out Vector3 topRight, out Vector3 topLeft)
        {
            bottomLeft = Vector3.zero;
            bottomRight = Vector3.zero;
            topRight = Vector3.zero;
            topLeft = Vector3.zero;

            if (iceCreamRenderer == null || iceCreamRenderer.sprite == null)
            {
                return false;
            }

            Bounds bounds = iceCreamRenderer.sprite.bounds;
            Transform target = iceCreamRenderer.transform;
            bottomLeft = target.TransformPoint(new Vector3(bounds.min.x, bounds.min.y, 0f));
            bottomRight = target.TransformPoint(new Vector3(bounds.max.x, bounds.min.y, 0f));
            topRight = target.TransformPoint(new Vector3(bounds.max.x, bounds.max.y, 0f));
            topLeft = target.TransformPoint(new Vector3(bounds.min.x, bounds.max.y, 0f));
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

        public void HideStick()
        {
            if (stickRenderer != null)
            {
                stickRenderer.gameObject.SetActive(false);
            }
        }
        public void SetOutlineVisible(bool visible)
        {
            ResolveOutline()?.SetVisible(visible);
        }

        public void SetOutlineHovered(bool isHovered)
        {
            ResolveOutline()?.SetHovered(isHovered);
        }

        public void SetAlpha(float alpha)
        {
            if (iceCreamRenderer != null)
            {
                iceCreamRenderer.color = WithAlpha(iceCreamRenderer.color, alpha);
            }

            if (stickRenderer != null)
            {
                stickRenderer.color = WithAlpha(stickRenderer.color, alpha);
            }
        }

        public void SetSortingOrder(int sortingOrder)
        {
            if (iceCreamRenderer != null)
            {
                iceCreamRenderer.sortingOrder = sortingOrder;
            }

            if (stickRenderer != null)
            {
                stickRenderer.sortingOrder = sortingOrder - 1;
            }
        }

        public Bounds GetVisualBounds()
        {
            return iceCreamRenderer != null ? iceCreamRenderer.bounds : new Bounds(transform.position, Vector3.zero);
        }

        public void RefreshCollider()
        {
            ResolveCollider();
            if (boxCollider == null)
            {
                return;
            }

            if (iceCreamRenderer == null || iceCreamRenderer.sprite == null)
            {
                boxCollider.enabled = false;
                return;
            }

            Bounds bounds = iceCreamRenderer.sprite.bounds;
            Vector3 bottomLeft = iceCreamRenderer.transform.TransformPoint(new Vector3(bounds.min.x, bounds.min.y, 0f));
            Vector3 topRight = iceCreamRenderer.transform.TransformPoint(new Vector3(bounds.max.x, bounds.max.y, 0f));
            Vector3 localBottomLeft = transform.InverseTransformPoint(bottomLeft);
            Vector3 localTopRight = transform.InverseTransformPoint(topRight);
            Vector2 min = Vector2.Min(localBottomLeft, localTopRight);
            Vector2 max = Vector2.Max(localBottomLeft, localTopRight);

            boxCollider.offset = (min + max) * 0.5f;
            boxCollider.size = max - min;
            boxCollider.isTrigger = true;
            boxCollider.enabled = true;
        }

        private int CountBiteablePixels(Vector3 worldPosition, float radiusWorld, out Vector3 localPosition)
        {
            localPosition = Vector3.zero;
            if (completed || runtimeTexture == null || iceCreamRenderer == null || iceCreamRenderer.sprite == null || radiusWorld <= 0f)
            {
                return 0;
            }

            localPosition = iceCreamRenderer.transform.InverseTransformPoint(worldPosition);
            float pixelsPerUnit = iceCreamRenderer.sprite.pixelsPerUnit;
            int centerX = Mathf.RoundToInt(localPosition.x * pixelsPerUnit + runtimeTexture.width * 0.5f);
            int centerY = Mathf.RoundToInt(localPosition.y * pixelsPerUnit + runtimeTexture.height * 0.5f);
            int radius = Mathf.CeilToInt(radiusWorld * pixelsPerUnit);
            int radiusSquared = radius * radius;
            int count = 0;

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

                    count++;
                }
            }

            return count;
        }

        private void OnDrawGizmosSelected()
        {
            if (!debugDrawSamplePoints || iceCreamRenderer == null || samplePoints.Count == 0)
            {
                return;
            }

            Transform target = iceCreamRenderer.transform;
            float size = Mathf.Max(0.001f, debugSamplePointSize);

            Gizmos.matrix = Matrix4x4.identity;
            for (int i = 0; i < samplePoints.Count; i++)
            {
                SamplePoint samplePoint = samplePoints[i];
                Gizmos.color = samplePoint.IsEaten ? debugEatenSampleColor : debugUneatenSampleColor;
                Gizmos.DrawSphere(target.TransformPoint(samplePoint.LocalPosition), size);
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
            BridgeInitialSampleIslands();
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

        private void BridgeInitialSampleIslands()
        {
            List<List<int>> islands = FindRemainingSampleIslands();
            while (islands.Count > 1)
            {
                int fromSample = -1;
                int toSample = -1;
                float bestDistanceSquared = float.MaxValue;

                List<int> connectedIsland = islands[0];
                for (int islandIndex = 1; islandIndex < islands.Count; islandIndex++)
                {
                    List<int> candidateIsland = islands[islandIndex];
                    for (int i = 0; i < connectedIsland.Count; i++)
                    {
                        int connectedSample = connectedIsland[i];
                        for (int j = 0; j < candidateIsland.Count; j++)
                        {
                            int candidateSample = candidateIsland[j];
                            float distanceSquared = (samplePoints[connectedSample].LocalPosition - samplePoints[candidateSample].LocalPosition).sqrMagnitude;
                            if (distanceSquared >= bestDistanceSquared)
                            {
                                continue;
                            }

                            bestDistanceSquared = distanceSquared;
                            fromSample = connectedSample;
                            toSample = candidateSample;
                        }
                    }
                }

                if (fromSample < 0 || toSample < 0)
                {
                    return;
                }

                AddSampleNeighbor(fromSample, toSample);
                islands = FindRemainingSampleIslands();
            }
        }

        private void AddSampleNeighbor(int a, int b)
        {
            if (!sampleNeighbors[a].Contains(b))
            {
                sampleNeighbors[a].Add(b);
            }

            if (!sampleNeighbors[b].Contains(a))
            {
                sampleNeighbors[b].Add(a);
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
                if (samplePoint.IsEaten || samplePoint.IsDetaching || (samplePoint.LocalPosition - bitePosition).sqrMagnitude > radiusSquared)
                {
                    continue;
                }

                samplePoint.IsEaten = true;
                samplePoints[i] = samplePoint;
            }
        }

        private void EvaluateDisconnectedPieces()
        {
            if (completed || samplePoints.Count == 0 || sampleNeighbors.Count != samplePoints.Count)
            {
                return;
            }

            List<List<int>> islands = FindRemainingSampleIslands();
            if (islands.Count <= 1)
            {
                return;
            }

            int keepIslandIndex = 0;
            int keepIslandSize = islands[0].Count;
            for (int i = 1; i < islands.Count; i++)
            {
                if (islands[i].Count > keepIslandSize)
                {
                    keepIslandIndex = i;
                    keepIslandSize = islands[i].Count;
                }
            }

            HashSet<int> samplesToDetach = new HashSet<int>();
            for (int i = 0; i < islands.Count; i++)
            {
                if (i == keepIslandIndex)
                {
                    continue;
                }

                for (int j = 0; j < islands[i].Count; j++)
                {
                    samplesToDetach.Add(islands[i][j]);
                }
            }

            DetachAndFadeSmallPiece(samplesToDetach);
        }

        private List<List<int>> FindRemainingSampleIslands()
        {
            List<List<int>> islands = new List<List<int>>();
            bool[] visited = new bool[samplePoints.Count];
            Queue<int> queue = new Queue<int>();

            for (int i = 0; i < samplePoints.Count; i++)
            {
                if (visited[i] || !IsRemainingSample(i))
                {
                    continue;
                }

                List<int> island = new List<int>();
                visited[i] = true;
                queue.Enqueue(i);

                while (queue.Count > 0)
                {
                    int current = queue.Dequeue();
                    island.Add(current);

                    List<int> neighbors = sampleNeighbors[current];
                    for (int j = 0; j < neighbors.Count; j++)
                    {
                        int neighbor = neighbors[j];
                        if (visited[neighbor] || !IsRemainingSample(neighbor))
                        {
                            continue;
                        }

                        visited[neighbor] = true;
                        queue.Enqueue(neighbor);
                    }
                }

                islands.Add(island);
            }

            return islands;
        }

        private bool IsRemainingSample(int index)
        {
            SamplePoint samplePoint = samplePoints[index];
            return !samplePoint.IsEaten && !samplePoint.IsDetaching;
        }

        private void DetachAndFadeSmallPiece(HashSet<int> sampleIndices)
        {
            if (sampleIndices == null || sampleIndices.Count == 0 || runtimeTexture == null || pixels == null)
            {
                return;
            }

            for (int i = 0; i < samplePoints.Count; i++)
            {
                if (!sampleIndices.Contains(i))
                {
                    continue;
                }

                SamplePoint samplePoint = samplePoints[i];
                samplePoint.IsDetaching = true;
                samplePoints[i] = samplePoint;
            }

            Color[] detachedPixels = new Color[pixels.Length];
            int detachedPixelCount = 0;
            for (int y = 0; y < runtimeTexture.height; y++)
            {
                for (int x = 0; x < runtimeTexture.width; x++)
                {
                    int pixelIndex = y * runtimeTexture.width + x;
                    if (!ediblePixels[pixelIndex] || eatenPixels[pixelIndex])
                    {
                        continue;
                    }

                    int nearestSampleIndex = FindNearestUneatenSampleIndex(x, y);
                    if (nearestSampleIndex < 0 || !sampleIndices.Contains(nearestSampleIndex))
                    {
                        continue;
                    }

                    detachedPixels[pixelIndex] = pixels[pixelIndex];
                    pixels[pixelIndex] = Color.clear;
                    eatenPixels[pixelIndex] = true;
                    detachedPixelCount++;
                }
            }

            if (detachedPixelCount == 0)
            {
                MarkDetachedSamplesEaten(sampleIndices);
                return;
            }

            Sprite sourceSprite = iceCreamRenderer != null ? iceCreamRenderer.sprite : null;
            if (sourceSprite == null)
            {
                MarkDetachedSamplesEaten(sampleIndices);
                return;
            }

            ResolveDetachedPiecePresenter()?.Present(
                iceCreamRenderer,
                detachedPixels,
                runtimeTexture.width,
                runtimeTexture.height,
                runtimeTexture.filterMode,
                sourceSprite.pivot / sourceSprite.rect.size,
                sourceSprite.pixelsPerUnit,
                detachedPixelCount,
                completedDetachedPixelCount => OnDetachedPieceCompleted(sampleIndices, completedDetachedPixelCount));
        }

        private int FindNearestUneatenSampleIndex(int pixelX, int pixelY)
        {
            if (iceCreamRenderer == null || iceCreamRenderer.sprite == null)
            {
                return -1;
            }

            Vector2 pivot = iceCreamRenderer.sprite.pivot;
            float pixelsPerUnit = iceCreamRenderer.sprite.pixelsPerUnit;
            Vector2 localPosition = new Vector2((pixelX - pivot.x) / pixelsPerUnit, (pixelY - pivot.y) / pixelsPerUnit);
            int nearestIndex = -1;
            float nearestDistanceSquared = float.MaxValue;

            for (int i = 0; i < samplePoints.Count; i++)
            {
                SamplePoint samplePoint = samplePoints[i];
                if (samplePoint.IsEaten)
                {
                    continue;
                }

                float distanceSquared = (samplePoint.LocalPosition - localPosition).sqrMagnitude;
                if (distanceSquared >= nearestDistanceSquared)
                {
                    continue;
                }

                nearestDistanceSquared = distanceSquared;
                nearestIndex = i;
            }

            return nearestIndex;
        }

        private void OnDetachedPieceCompleted(HashSet<int> sampleIndices, int detachedPixelCount)
        {
            MarkDetachedSamplesEaten(sampleIndices);
            eatenPixelCount += detachedPixelCount;
            ProgressChanged?.Invoke(Progress);
        }

        private void MarkDetachedSamplesEaten(HashSet<int> sampleIndices)
        {
            foreach (int sampleIndex in sampleIndices)
            {
                SamplePoint samplePoint = samplePoints[sampleIndex];
                samplePoint.IsDetaching = false;
                samplePoint.IsEaten = true;
                samplePoints[sampleIndex] = samplePoint;
            }
        }

        private void ClearDetachedPieces()
        {
            if (detachedPiecePresenter == null)
            {
                detachedPiecePresenter = GetComponent<IceCreamDetachedPiecePresenter>();
            }

            detachedPiecePresenter?.Clear();
        }

        private void ResolveRenderers()
        {
            if (iceCreamRenderer == null)
            {
                Transform found = transform.Find("IceCreamSprite");
                iceCreamRenderer = found != null ? found.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>(true);
            }

            if (stickRenderer == null)
            {
                Transform found = transform.Find("StickReveal");
                if (found != null)
                {
                    stickRenderer = found.GetComponent<SpriteRenderer>();
                }
            }
        }

        private void ResolveCollider()
        {
            if (boxCollider == null)
            {
                boxCollider = GetComponent<BoxCollider2D>();
            }

            if (boxCollider == null)
            {
                boxCollider = gameObject.AddComponent<BoxCollider2D>();
            }
        }

        private void InitOutline()
        {
            ResolveOutline()?.Initialize(iceCreamRenderer, stickRenderer);
        }

        private IceCreamOutline ResolveOutline()
        {
            if (outline == null)
            {
                outline = GetComponentInChildren<IceCreamOutline>(true);
            }

            return outline;
        }

        private IceCreamDetachedPiecePresenter ResolveDetachedPiecePresenter()
        {
            if (detachedPiecePresenter == null)
            {
                detachedPiecePresenter = GetComponent<IceCreamDetachedPiecePresenter>();
            }

            if (detachedPiecePresenter == null)
            {
                detachedPiecePresenter = gameObject.AddComponent<IceCreamDetachedPiecePresenter>();
            }

            return detachedPiecePresenter;
        }

        private void SetIceCreamSprite(Sprite sprite, bool visible)
        {
            if (iceCreamRenderer == null)
            {
                return;
            }

            iceCreamRenderer.sprite = sprite;
            iceCreamRenderer.color = Color.white;
            iceCreamRenderer.gameObject.SetActive(visible);
            RefreshCollider();
        }

        private void OnDestroy()
        {
            ReleaseRuntimeSpriteAndTexture();
            ClearDetachedPieces();
        }

        private void ReleaseRuntimeSpriteAndTexture()
        {
            if (runtimeSprite != null)
            {
                Destroy(runtimeSprite);
                runtimeSprite = null;
            }

            if (runtimeTexture != null)
            {
                Destroy(runtimeTexture);
                runtimeTexture = null;
            }
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        public readonly struct BiteResult
        {
            public readonly bool WasApplied;
            public readonly float Progress;

            private BiteResult(bool wasApplied, float progress)
            {
                WasApplied = wasApplied;
                Progress = progress;
            }

            public static BiteResult Applied(float progress)
            {
                return new BiteResult(true, progress);
            }

            public static BiteResult NotApplied(float progress)
            {
                return new BiteResult(false, progress);
            }
        }

        private struct SamplePoint
        {
            public readonly Vector2 LocalPosition;
            public bool IsEaten;
            public bool IsDetaching;

            public SamplePoint(Vector2 localPosition)
            {
                LocalPosition = localPosition;
                IsEaten = false;
                IsDetaching = false;
            }
        }
    }
}
