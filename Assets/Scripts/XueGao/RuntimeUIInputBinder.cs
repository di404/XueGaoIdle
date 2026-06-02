using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace XueGao
{
    public class RuntimeUIInputBinder : MonoBehaviour
    {
#if ENABLE_INPUT_SYSTEM
        private InputActionAsset actionsAsset;

        private void Awake()
        {
            InputSystemUIInputModule module = GetComponent<InputSystemUIInputModule>();
            if (module == null || module.actionsAsset != null)
            {
                return;
            }

            actionsAsset = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionMap uiMap = new InputActionMap("UI");
            actionsAsset.AddActionMap(uiMap);

            InputAction point = uiMap.AddAction("Point", InputActionType.PassThrough, "<Pointer>/position");
            InputAction leftClick = uiMap.AddAction("LeftClick", InputActionType.PassThrough, "<Pointer>/press");
            InputAction scrollWheel = uiMap.AddAction("ScrollWheel", InputActionType.PassThrough, "<Pointer>/scroll");
            InputAction move = uiMap.AddAction("Move", InputActionType.PassThrough);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/s")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/a")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/d")
                .With("Right", "<Keyboard>/rightArrow");
            InputAction submit = uiMap.AddAction("Submit", InputActionType.Button, "<Keyboard>/enter");
            InputAction cancel = uiMap.AddAction("Cancel", InputActionType.Button, "<Keyboard>/escape");

            module.actionsAsset = actionsAsset;
            module.point = InputActionReference.Create(point);
            module.leftClick = InputActionReference.Create(leftClick);
            module.scrollWheel = InputActionReference.Create(scrollWheel);
            module.move = InputActionReference.Create(move);
            module.submit = InputActionReference.Create(submit);
            module.cancel = InputActionReference.Create(cancel);
            actionsAsset.Enable();
        }
#endif
    }
}
