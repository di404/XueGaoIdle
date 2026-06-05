using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XueGao
{
    public class IceCreamDetachedPiecePresenter : MonoBehaviour
    {
        [SerializeField] private float fadeDuration = 0.45f;
        [SerializeField] private float fadeStartAlpha = 1f;
        [SerializeField] private float fallDistance = 0.65f;
        [SerializeField] private float horizontalDrift = 0.12f;
        [SerializeField] private float tiltAngle = 14f;

        private readonly List<DetachedPiece> activePieces = new List<DetachedPiece>();

        public void Present(
            SpriteRenderer sourceRenderer,
            Color[] detachedPixels,
            int textureWidth,
            int textureHeight,
            FilterMode filterMode,
            Vector2 normalizedPivot,
            float pixelsPerUnit,
            int detachedPixelCount,
            Action<int> completed)
        {
            if (sourceRenderer == null || detachedPixels == null || detachedPixels.Length == 0 || textureWidth <= 0 || textureHeight <= 0)
            {
                return;
            }

            Texture2D detachedTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
            {
                filterMode = filterMode
            };
            detachedTexture.SetPixels(detachedPixels);
            detachedTexture.Apply();

            Sprite detachedSprite = Sprite.Create(detachedTexture, new Rect(0f, 0f, textureWidth, textureHeight), normalizedPivot, pixelsPerUnit);
            SpriteRenderer detachedRenderer = CreateDetachedPieceRenderer(sourceRenderer, detachedSprite);
            DetachedPiece piece = new DetachedPiece(detachedRenderer, detachedTexture, detachedSprite);
            activePieces.Add(piece);
            piece.Routine = StartCoroutine(FadeDetachedPiece(piece, detachedPixelCount, completed));
        }

        public void Clear()
        {
            for (int i = activePieces.Count - 1; i >= 0; i--)
            {
                DetachedPiece piece = activePieces[i];
                if (piece.Routine != null)
                {
                    StopCoroutine(piece.Routine);
                }

                DestroyPiece(piece);
            }

            activePieces.Clear();
        }

        private SpriteRenderer CreateDetachedPieceRenderer(SpriteRenderer sourceRenderer, Sprite detachedSprite)
        {
            GameObject detachedObject = new GameObject("DetachedIceCreamPiece");
            detachedObject.transform.SetParent(sourceRenderer.transform.parent, false);
            detachedObject.transform.position = sourceRenderer.transform.position;
            detachedObject.transform.rotation = sourceRenderer.transform.rotation;
            detachedObject.transform.localScale = sourceRenderer.transform.localScale;

            SpriteRenderer detachedRenderer = detachedObject.AddComponent<SpriteRenderer>();
            detachedRenderer.sprite = detachedSprite;
            detachedRenderer.color = WithAlpha(sourceRenderer.color, fadeStartAlpha);
            detachedRenderer.flipX = sourceRenderer.flipX;
            detachedRenderer.flipY = sourceRenderer.flipY;
            detachedRenderer.drawMode = sourceRenderer.drawMode;
            detachedRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
            detachedRenderer.sortingOrder = sourceRenderer.sortingOrder + 1;
            detachedRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
            return detachedRenderer;
        }

        private IEnumerator FadeDetachedPiece(DetachedPiece piece, int detachedPixelCount, Action<int> completed)
        {
            SpriteRenderer detachedRenderer = piece.Renderer;
            if (detachedRenderer == null)
            {
                activePieces.Remove(piece);
                DestroyPiece(piece);
                yield break;
            }

            float duration = Mathf.Max(0.01f, fadeDuration);
            Color startColor = detachedRenderer.color;
            Transform detachedTransform = detachedRenderer.transform;
            Vector3 startLocalPosition = detachedTransform.localPosition;
            Quaternion startLocalRotation = detachedTransform.localRotation;
            float driftDirection = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            Vector3 endLocalPosition = startLocalPosition + new Vector3(horizontalDrift * driftDirection, -fallDistance, 0f);
            Quaternion endLocalRotation = startLocalRotation * Quaternion.Euler(0f, 0f, tiltAngle * driftDirection);

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                if (detachedRenderer == null)
                {
                    activePieces.Remove(piece);
                    DestroyPiece(piece);
                    yield break;
                }

                float ratio = Mathf.Clamp01(t / duration);
                float easedRatio = EaseOutCubic(ratio);
                detachedTransform.localPosition = Vector3.LerpUnclamped(startLocalPosition, endLocalPosition, easedRatio);
                detachedTransform.localRotation = Quaternion.LerpUnclamped(startLocalRotation, endLocalRotation, easedRatio);
                detachedRenderer.color = WithAlpha(startColor, Mathf.Lerp(fadeStartAlpha, 0f, ratio));
                yield return null;
            }

            if (detachedRenderer != null)
            {
                detachedTransform.localPosition = endLocalPosition;
                detachedTransform.localRotation = endLocalRotation;
                detachedRenderer.color = WithAlpha(startColor, 0f);
            }

            completed?.Invoke(detachedPixelCount);
            activePieces.Remove(piece);
            DestroyPiece(piece);
        }

        private void DestroyPiece(DetachedPiece piece)
        {
            if (piece.Renderer != null)
            {
                Destroy(piece.Renderer.gameObject);
            }

            if (piece.Sprite != null)
            {
                Destroy(piece.Sprite);
            }

            if (piece.Texture != null)
            {
                Destroy(piece.Texture);
            }
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        private static float EaseOutCubic(float value)
        {
            value = 1f - Mathf.Clamp01(value);
            return 1f - value * value * value;
        }

        private sealed class DetachedPiece
        {
            public readonly SpriteRenderer Renderer;
            public readonly Texture2D Texture;
            public readonly Sprite Sprite;
            public Coroutine Routine;

            public DetachedPiece(SpriteRenderer renderer, Texture2D texture, Sprite sprite)
            {
                Renderer = renderer;
                Texture = texture;
                Sprite = sprite;
            }
        }
    }
}
