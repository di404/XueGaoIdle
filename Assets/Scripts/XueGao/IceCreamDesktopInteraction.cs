using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.EventSystems;

namespace XueGao
{
    public class IceCreamDesktopInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public event Action<IceCream> Clicked;
        public event Action<IceCream, bool> HoverChanged;
        public event Action<IceCream> DragStarted;
        public event Action<IceCream, Vector3> Dragged;
        public event Action<IceCream> DragEnded;

        [SerializeField] private IceCream iceCream;
        [SerializeField] private IceCreamOutline outline;

        [Header("Click")]
        [SerializeField] private float clickScale = 1.12f;
        [SerializeField] private float clickDuration = 0.16f;

        [Header("Drag")]
        [SerializeField] private float dragMaxTiltAngle = 12f;
        [SerializeField] private float dragTiltSensitivity = 80f;
        [SerializeField] private float dragTiltSmoothSpeed = 18f;
        [SerializeField] private float dragReturnSmoothSpeed = 14f;

        private MMF_Player clickFeedbacks;
        private Coroutine clickRoutine;
        private Coroutine suppressClickRoutine;
        private bool interactionEnabled = true;
        private bool dragEnabled = true;
        private bool isDragging;
        private bool isHovered;
        private bool suppressClick;
        private Vector3 dragOffset;
        private Vector3 lastDragPosition;
        private float targetTilt;
        private float currentTilt;

        public bool InteractionEnabled
        {
            get => interactionEnabled;
            set
            {
                if (interactionEnabled == value)
                {
                    return;
                }

                interactionEnabled = value;
                if (!interactionEnabled)
                {
                    ResetInteractionState();
                }
            }
        }

        public bool DragEnabled
        {
            get => dragEnabled;
            set
            {
                dragEnabled = value;
                if (!dragEnabled && isDragging)
                {
                    EndDrag();
                }
            }
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            UpdateDragTilt();
        }

        private void OnDisable()
        {
            ResetInteractionState();
        }

        public void Bind(IceCream targetIceCream, IceCreamOutline targetOutline)
        {
            iceCream = targetIceCream;
            outline = targetOutline;
            EnsureClickFeedbacks();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!interactionEnabled)
            {
                return;
            }

            SetHovered(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!interactionEnabled)
            {
                return;
            }

            SetHovered(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (suppressClick || eventData != null && eventData.dragging)
            {
                suppressClick = false;
                return;
            }

            if (!interactionEnabled || isDragging || clickRoutine != null)
            {
                return;
            }

            clickRoutine = StartCoroutine(PlayClickThenNotify());
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!interactionEnabled || !dragEnabled || !TryGetEventWorldPosition(eventData, out Vector3 worldPosition))
            {
                return;
            }

            CancelPendingClick();
            isDragging = true;
            suppressClick = true;
            dragOffset = transform.position - worldPosition;
            lastDragPosition = transform.position;
            DragStarted?.Invoke(ResolveIceCream());
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || !TryGetEventWorldPosition(eventData, out Vector3 worldPosition))
            {
                return;
            }

            Vector3 targetPosition = worldPosition + dragOffset;
            targetPosition.z = transform.position.z;

            float deltaX = targetPosition.x - lastDragPosition.x;
            targetTilt = Mathf.Clamp(deltaX * dragTiltSensitivity, -dragMaxTiltAngle, dragMaxTiltAngle);

            transform.position = targetPosition;
            Dragged?.Invoke(ResolveIceCream(), targetPosition);
            lastDragPosition = transform.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging)
            {
                return;
            }

            EndDrag();
        }

        private IEnumerator PlayClickThenNotify()
        {
            EnsureClickFeedbacks();
            clickFeedbacks?.StopFeedbacks();
            clickFeedbacks?.PlayFeedbacks();

            float delay = Mathf.Max(0f, clickDuration);
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            clickRoutine = null;
            if (interactionEnabled && !isDragging)
            {
                Clicked?.Invoke(ResolveIceCream());
            }
        }

        private void EndDrag()
        {
            isDragging = false;
            targetTilt = 0f;
            ScheduleSuppressClickReset();
            DragEnded?.Invoke(ResolveIceCream());
        }

        private void ResetInteractionState()
        {
            CancelPendingClick();
            CancelSuppressClickReset();
            isDragging = false;
            suppressClick = false;
            SetHovered(false);
            targetTilt = 0f;
            currentTilt = 0f;
            ApplyTilt(0f);
        }

        private void CancelPendingClick()
        {
            if (clickRoutine != null)
            {
                StopCoroutine(clickRoutine);
                clickRoutine = null;
            }
        }

        private void ScheduleSuppressClickReset()
        {
            CancelSuppressClickReset();
            suppressClickRoutine = StartCoroutine(ClearSuppressClickAfterFrame());
        }

        private IEnumerator ClearSuppressClickAfterFrame()
        {
            yield return null;
            suppressClick = false;
            suppressClickRoutine = null;
        }

        private void CancelSuppressClickReset()
        {
            if (suppressClickRoutine == null)
            {
                return;
            }

            StopCoroutine(suppressClickRoutine);
            suppressClickRoutine = null;
        }

        private void SetHovered(bool hovered)
        {
            if (isHovered == hovered)
            {
                return;
            }

            isHovered = hovered;
            ResolveOutline()?.SetHovered(hovered);
            HoverChanged?.Invoke(ResolveIceCream(), hovered);
        }

        private void UpdateDragTilt()
        {
            Transform visualRoot = GetVisualRoot();
            if (visualRoot == null)
            {
                return;
            }

            float speed = isDragging ? dragTiltSmoothSpeed : dragReturnSmoothSpeed;
            currentTilt = Mathf.Lerp(currentTilt, targetTilt, 1f - Mathf.Exp(-speed * Time.deltaTime));
            ApplyTilt(currentTilt);
        }

        private void ApplyTilt(float tilt)
        {
            Transform visualRoot = GetVisualRoot();
            if (visualRoot != null)
            {
                visualRoot.localRotation = Quaternion.Euler(0f, 0f, tilt);
            }
        }

        private void EnsureClickFeedbacks()
        {
            if (clickFeedbacks != null)
            {
                return;
            }

            Transform visualRoot = GetVisualRoot();
            if (visualRoot == null)
            {
                return;
            }

            GameObject playerObject = new GameObject("DesktopClickFeedbacks");
            playerObject.layer = gameObject.layer;
            playerObject.transform.SetParent(transform, false);
            clickFeedbacks = playerObject.AddComponent<MMF_Player>();
            clickFeedbacks.FeedbacksList = new List<MMF_Feedback>();

            AnimationCurve punchCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.35f, 1f),
                new Keyframe(1f, 0f));
            MMTweenType punchTween = new MMTweenType(punchCurve);
            clickFeedbacks.FeedbacksList.Add(new MMF_Scale
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
                AllowAdditivePlays = false,
                DetermineScaleOnPlay = true
            });

            clickFeedbacks.PreInitialization();
            clickFeedbacks.Initialization();
        }

        private Transform GetVisualRoot()
        {
            ResolveReferences();
            if (outline != null && outline.VisualRoot != null)
            {
                return outline.VisualRoot;
            }

            return transform;
        }

        private IceCream ResolveIceCream()
        {
            if (iceCream == null)
            {
                iceCream = GetComponent<IceCream>();
            }

            return iceCream;
        }

        private IceCreamOutline ResolveOutline()
        {
            if (outline == null)
            {
                outline = ResolveIceCream() != null ? iceCream.Outline : GetComponentInChildren<IceCreamOutline>(true);
            }

            return outline;
        }

        private void ResolveReferences()
        {
            ResolveIceCream();
            ResolveOutline();
        }

        private static bool TryGetEventWorldPosition(PointerEventData eventData, out Vector3 worldPosition)
        {
            Camera eventCamera = eventData != null ? eventData.pressEventCamera : null;
            if (eventCamera == null)
            {
                eventCamera = Camera.main;
            }

            if (eventCamera == null || eventData == null)
            {
                worldPosition = Vector3.zero;
                return false;
            }

            Vector3 screenPosition = new Vector3(eventData.position.x, eventData.position.y, -eventCamera.transform.position.z);
            worldPosition = eventCamera.ScreenToWorldPoint(screenPosition);
            worldPosition.z = 0f;
            return true;
        }
    }
}
