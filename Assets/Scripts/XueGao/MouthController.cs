using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace XueGao
{
    public class MouthController : MonoBehaviour
    {
        [SerializeField] private Transform preview;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Animator biteAnimator;
        [SerializeField] private IceCreamEater eater;
        [SerializeField] private int level = 1;
        [SerializeField] private float baseRadius = 0.28f;
        [SerializeField] private float radiusPerLevel = 0.07f;
        [SerializeField] private bool debugDrawBiteRadius = true;
        [SerializeField] private bool showDebugPreviewInGame;
        [SerializeField] private Color debugBiteRadiusColor = new Color(0f, 1f, 0.85f, 0.85f);

        public int Level => level;
        public float BiteRadiusWorld => baseRadius + level * radiusPerLevel;
        public bool IsOverBiteTarget { get; private set; }

        private Camera mainCamera;
        private Vector3 pointerWorld;
        private Vector3 visualBaseScale = Vector3.one;
        private float visualBaseRadius = 1f;

        private void Awake()
        {
            mainCamera = Camera.main;
            ResolveReferences();
            visualBaseRadius = Mathf.Max(0.0001f, BiteRadiusWorld);
            if (visualRoot != null)
            {
                visualBaseScale = visualRoot.localScale;
            }

            UpdatePreviewScale();
            SetMouthVisible(false);
        }

        private void OnDisable()
        {
            Cursor.visible = true;
        }

        private void OnDestroy()
        {
            Cursor.visible = true;
        }

        private void Update()
        {
            RefreshCursorState();
        }

        public bool RefreshCursorState()
        {
            pointerWorld = GetPointerWorld();

            if (preview != null)
            {
                preview.position = WithCurrentZ(preview, pointerWorld);
            }

            if (visualRoot != null)
            {
                visualRoot.position = WithCurrentZ(visualRoot, pointerWorld);
            }

            bool canShowMouth = eater != null && !IsPointerOverUI() && eater.CanBite(pointerWorld, BiteRadiusWorld);
            SetMouthVisible(canShowMouth);
            return canShowMouth;
        }

        public void SetLevel(int newLevel)
        {
            level = Mathf.Max(1, newLevel);
            UpdatePreviewScale();
        }

        public void AddLevel()
        {
            SetLevel(level + 1);
        }

        public void SetEater(IceCreamEater newEater)
        {
            eater = newEater;
        }

        public void PlayBiteAnimation()
        {
            if (biteAnimator == null)
            {
                return;
            }

            biteAnimator.Play(0, 0, 0f);
            biteAnimator.Update(0f);
        }

        public Vector3 GetPointerWorld()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            Vector2 screenPosition = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                screenPosition = Mouse.current.position.ReadValue();
            }
            else if (Touchscreen.current != null)
            {
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            }
#endif
            Vector3 world = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -mainCamera.transform.position.z));
            world.z = 0f;
            return world;
        }

        private void ResolveReferences()
        {
            if (eater == null)
            {
                eater = GetComponent<IceCreamEater>();
            }

            if (visualRoot == null)
            {
                Transform foundVisual = transform.Find("MouthVisual");
                if (foundVisual != null)
                {
                    visualRoot = foundVisual;
                }
            }

            if (visualRoot == null)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    if (child != preview)
                    {
                        visualRoot = child;
                        break;
                    }
                }
            }

            if (biteAnimator == null && visualRoot != null)
            {
                biteAnimator = visualRoot.GetComponentInChildren<Animator>(true);
            }
        }

        private void UpdatePreviewScale()
        {
            float diameter = BiteRadiusWorld * 2f;
            if (preview != null)
            {
                preview.localScale = new Vector3(diameter, diameter, 1f);
            }

            if (visualRoot != null)
            {
                float ratio = BiteRadiusWorld / Mathf.Max(0.0001f, visualBaseRadius);
                visualRoot.localScale = visualBaseScale * ratio;
            }
        }

        private void SetMouthVisible(bool visible)
        {
            IsOverBiteTarget = visible;

            if (visualRoot != null && visualRoot.gameObject.activeSelf != visible)
            {
                visualRoot.gameObject.SetActive(visible);
            }

            if (preview != null)
            {
                bool showPreview = debugDrawBiteRadius && showDebugPreviewInGame && visible;
                if (preview.gameObject.activeSelf != showPreview)
                {
                    preview.gameObject.SetActive(showPreview);
                }
            }

            Cursor.visible = !visible;
        }

        private bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private static Vector3 WithCurrentZ(Transform target, Vector3 world)
        {
            world.z = target.position.z;
            return world;
        }

        private void OnDrawGizmos()
        {
            if (!debugDrawBiteRadius)
            {
                return;
            }

            Vector3 center = Application.isPlaying ? pointerWorld : (preview != null ? preview.position : transform.position);
            DrawDebugCircle(center, BiteRadiusWorld, debugBiteRadiusColor);
        }

        private static void DrawDebugCircle(Vector3 center, float radius, Color color)
        {
            const int segments = 48;
            Gizmos.color = color;
            Vector3 previous = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}
