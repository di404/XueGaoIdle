using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using System.Collections.Generic;
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

        [Header("Click")]
        [SerializeField] private float clickScale = 1.12f;
        [SerializeField] private float clickDuration = 0.16f;

        private SpriteRenderer iceCreamRenderer;
        private SpriteRenderer stickRenderer;
        [SerializeField] private SpriteRenderer iceCreamOutlineRenderer;
        [SerializeField] private SpriteRenderer stickOutlineRenderer;
        private Transform visualRoot;
        private MMF_Player hoverEnterFeedbacks;
        private MMF_Player hoverExitFeedbacks;
        private MMF_Player clickFeedbacks;
        private bool initialized;
        private bool isHovered;
        private float currentAlpha = 1f;

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
                CreateFeelPlayers();
                initialized = true;
            }

            LoadIceCreamSprite(iceCream.sprite);
            LoadStickSprite(stick.sprite);
            SetSortingOrder(iceCream.sortingOrder - 2);
        }
        [Button("Set Hover Color")]
        public void SetHoverColor()
        {
            iceCreamOutlineRenderer.material.SetColor("_SolidColor", hoverOutlineColor);
            stickOutlineRenderer.material.SetColor("_SolidColor", hoverOutlineColor);
        }


        [Button("Set Hover Color")]

        public void SetDefaultColor()
        {
            iceCreamOutlineRenderer.material.SetColor("_SolidColor", outlineColor);
            stickOutlineRenderer.material.SetColor("_SolidColor", outlineColor);
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
            iceCreamOutlineRenderer.sortingOrder = sortingOrder;
            stickOutlineRenderer.sortingOrder = sortingOrder;
        }


        public void SetHovered(bool hovered, bool playFeedback = true)
        {
            if (!initialized || isHovered == hovered)
            {
                return;
            }

            isHovered = hovered;
            if (!playFeedback)
            {
                ApplyOutlineColor(hovered ? hoverOutlineColor : outlineColor);
                return;
            }

            MMF_Player feedbacksToStop = hovered ? hoverExitFeedbacks : hoverEnterFeedbacks;
            MMF_Player feedbacksToPlay = hovered ? hoverEnterFeedbacks : hoverExitFeedbacks;
            feedbacksToStop?.StopFeedbacks();
            feedbacksToPlay?.PlayFeedbacks();
        }

        private void CreateOutlineRenderers()
        {
            Transform outlineRoot = new GameObject("FeelOutline").transform;
            outlineRoot.SetParent(visualRoot, false);
            outlineRoot.localScale = Vector3.one * outlineScale;
            outlineRoot.gameObject.layer = gameObject.layer;

            iceCreamOutlineRenderer = CreateOutlineRenderer("IceCreamOutline", outlineRoot, iceCreamRenderer);
            stickOutlineRenderer = CreateOutlineRenderer("StickOutline", outlineRoot, stickRenderer);
        }

        private void CreateVisualRoot()
        {
            visualRoot = new GameObject("FeelVisual").transform;
            visualRoot.SetParent(transform, false);
            visualRoot.gameObject.layer = gameObject.layer;
            iceCreamRenderer.transform.SetParent(visualRoot, true);
            stickRenderer.transform.SetParent(visualRoot, true);
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

        private void CreateFeelPlayers()
        {
            hoverEnterFeedbacks = CreateSpriteColorPlayer("FeelHoverEnter", hoverOutlineColor);
            hoverExitFeedbacks = CreateSpriteColorPlayer("FeelHoverExit", outlineColor);
            clickFeedbacks = CreateClickScalePlayer();
        }

        private MMF_Player CreateSpriteColorPlayer(string playerName, Color destinationColor)
        {
            MMF_Player player = CreatePlayer(playerName);

            InitializePlayer(player);
            return player;
        }

        private MMF_Player CreateClickScalePlayer()
        {
            MMF_Player player = CreatePlayer("FeelClickScale");
            AnimationCurve punchCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.35f, 1f),
                new Keyframe(1f, 0f));
            MMTweenType punchTween = new MMTweenType(punchCurve);
            player.FeedbacksList.Add(new MMF_Scale
            {
                AnimateScaleTarget = visualRoot,
                Mode = MMF_Scale.Modes.Absolute,
                AnimateScaleDuration = clickDuration,
                RemapCurveZero = 1f,
                RemapCurveOne = clickScale,
                AnimateScaleTweenX = punchTween,
                AnimateScaleTweenY = punchTween,
                AnimateScaleTweenZ = punchTween,
                UniformScaling = true,
                AllowAdditivePlays = true,
                DetermineScaleOnPlay = true
            });

            InitializePlayer(player);
            return player;
        }

        private MMF_Player CreatePlayer(string playerName)
        {
            GameObject playerObject = new GameObject(playerName);
            playerObject.layer = gameObject.layer;
            playerObject.transform.SetParent(transform, false);
            MMF_Player player = playerObject.AddComponent<MMF_Player>();
            player.FeedbacksList = new List<MMF_Feedback>();
            return player;
        }

        private static void InitializePlayer(MMF_Player player)
        {
            player.PreInitialization();
            player.Initialization();
        }

        private void ApplyOutlineColor(Color color)
        {
            color.a *= currentAlpha;
        }
    }
}
