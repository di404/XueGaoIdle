using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace XueGao
{
    public class IceCreamEater : MonoBehaviour
    {
        public event Action<float> ProgressChanged;
        public event Action Completed;
        public event Action<Vector3> BiteApplied;

        [SerializeField] private float completeThreshold = 0.8f;
        [SerializeField] private int focusedSortingOrder = 20;
        [SerializeField] private MouthController mouth;

        private IceCream currentIceCream;
        private bool completed;
        private IceCreamDefinition currentDefinition;
        private IceCreamStickDefinition currentStickDefinition;

        public float Progress => currentIceCream != null ? currentIceCream.Progress : 0f;
        public IceCreamDefinition CurrentDefinition => currentDefinition;
        public IceCreamStickDefinition CurrentStickDefinition => currentStickDefinition;
        public bool InputEnabled { get; set; }

        private void Awake()
        {
            if (mouth == null)
            {
                mouth = GetComponent<MouthController>();
            }
        }

        private void Update()
        {
            if (!InputEnabled || completed || currentIceCream == null || !PointerPressedThisFrame())
            {
                return;
            }

            TryBiteFromPointer();
        }

        public void BeginEating(IceCream iceCream, IceCreamStickDefinition stickDefinition = null)
        {
            DetachCurrentIceCream();
            currentIceCream = iceCream;
            currentDefinition = iceCream != null ? iceCream.CurrentDefinition : null;
            currentStickDefinition = stickDefinition;
            completed = false;

            if (currentIceCream == null)
            {
                ProgressChanged?.Invoke(0f);
                return;
            }

            currentIceCream.ProgressChanged += OnIceCreamProgressChanged;
            currentIceCream.transform.SetParent(transform, false);
            currentIceCream.transform.localPosition = Vector3.zero;
            currentIceCream.transform.localRotation = Quaternion.identity;
            currentIceCream.transform.localScale = Vector3.one;
            currentIceCream.gameObject.SetActive(true);
            currentIceCream.SetAlpha(1f);
            currentIceCream.SetSortingOrder(focusedSortingOrder);
            currentIceCream.InteractionEnabled = false;
            currentIceCream.DragEnabled = false;
            currentIceCream.LoadForEating(stickDefinition);
            currentIceCream.SetOutlineVisible(false);
            ProgressChanged?.Invoke(Progress);
        }

        public void Clear()
        {
            EndEating();
        }

        public void EndEating()
        {
            if (currentIceCream != null)
            {
                currentIceCream.ProgressChanged -= OnIceCreamProgressChanged;
            }

            currentIceCream = null;
            currentDefinition = null;
            currentStickDefinition = null;
            completed = false;
            ProgressChanged?.Invoke(0f);
        }

        public void SetMouth(MouthController newMouth)
        {
            mouth = newMouth;
        }

        public bool TryBite(Vector3 worldPosition, float radiusWorld)
        {
            if (completed || currentIceCream == null)
            {
                return false;
            }

            IceCream.BiteResult result = currentIceCream.TryBite(worldPosition, radiusWorld);
            if (!result.WasApplied)
            {
                return false;
            }

            BiteApplied?.Invoke(worldPosition);
            OnIceCreamProgressChanged(result.Progress);
            return true;
        }

        public bool CanBite(Vector3 worldPosition, float radiusWorld)
        {
            return !completed && currentIceCream != null && currentIceCream.CanBite(worldPosition, radiusWorld);
        }

        public bool ContainsIceCreamSpritePoint(Vector3 worldPosition)
        {
            return currentIceCream != null && currentIceCream.ContainsSpritePoint(worldPosition);
        }

        public bool TryGetIceCreamSpriteWorldCorners(out Vector3 bottomLeft, out Vector3 bottomRight, out Vector3 topRight, out Vector3 topLeft)
        {
            if (currentIceCream != null)
            {
                return currentIceCream.TryGetSpriteWorldCorners(out bottomLeft, out bottomRight, out topRight, out topLeft);
            }

            bottomLeft = Vector3.zero;
            bottomRight = Vector3.zero;
            topRight = Vector3.zero;
            topLeft = Vector3.zero;
            return false;
        }

        public void RevealStick(IceCreamStickDefinition stickDefinition = null)
        {
            if (stickDefinition != null)
            {
                currentStickDefinition = stickDefinition;
            }

            currentIceCream?.RevealStick(currentStickDefinition);
        }

        public void ShowStick()
        {
            RevealStick();
        }

        public void HideStick()
        {
            currentIceCream?.HideStick();
        }

        private void TryBiteFromPointer()
        {
            if (mouth == null)
            {
                return;
            }

            if (mouth.RefreshCursorState() && TryBite(mouth.GetPointerWorld(), mouth.BiteRadiusWorld))
            {
                mouth.PlayBiteAnimation();
            }
        }

        private void OnDestroy()
        {
            DetachCurrentIceCream();
        }

        private void DetachCurrentIceCream()
        {
            if (currentIceCream != null)
            {
                currentIceCream.ProgressChanged -= OnIceCreamProgressChanged;
            }
        }

        private void OnIceCreamProgressChanged(float progress)
        {
            ProgressChanged?.Invoke(progress);

            if (!completed && progress >= completeThreshold)
            {
                completed = true;
                Completed?.Invoke();
            }
        }

        private static bool PointerPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            if (Touchscreen.current != null)
            {
                return Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            }
#endif
            return false;
        }
    }
}
