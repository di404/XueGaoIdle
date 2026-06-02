using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace XueGao
{
    public class MouthController : MonoBehaviour
    {
        [SerializeField] private Transform preview;
        [SerializeField] private int level = 1;
        [SerializeField] private float baseRadius = 0.28f;
        [SerializeField] private float radiusPerLevel = 0.07f;

        public int Level => level;
        public float BiteRadiusWorld => baseRadius + level * radiusPerLevel;

        private Camera mainCamera;

        private void Awake()
        {
            mainCamera = Camera.main;
            UpdatePreviewScale();
        }

        private void Update()
        {
            if (preview == null)
            {
                return;
            }

            Vector3 world = GetPointerWorld();
            world.z = preview.position.z;
            preview.position = world;
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

        private void UpdatePreviewScale()
        {
            if (preview == null)
            {
                return;
            }

            float diameter = BiteRadiusWorld * 2f;
            preview.localScale = new Vector3(diameter, diameter, 1f);
        }
    }
}
