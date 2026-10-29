using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SignalLost.EditorTools
{
    public static class SignalLostInputActionsBuilder
    {
        public const string AssetPath = "Assets/Resources/Input/SignalLostControls.inputactions";

        [MenuItem("Signal Lost/Rebuild Input Actions")]
        public static void Build()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string absolutePath = Path.Combine(projectRoot, AssetPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));

            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "SignalLostControls";

            BuildGameplayMap(asset);
            BuildUiMap(asset);

            File.WriteAllText(absolutePath, asset.ToJson());
            Object.DestroyImmediate(asset);
            AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.Refresh();
            Debug.Log($"[SignalLostInput] Rebuilt {AssetPath}");
        }

        private static void BuildGameplayMap(InputActionAsset asset)
        {
            InputActionMap gameplay = asset.AddActionMap("Gameplay");

            InputAction move = gameplay.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");

            InputAction look = gameplay.AddAction("Look", InputActionType.Value, expectedControlLayout: "Vector2");
            look.AddBinding("<Mouse>/delta");
            look.AddBinding("<Gamepad>/rightStick").WithProcessor("scaleVector2(x=8,y=8)");

            InputAction jump = gameplay.AddAction("Jump", InputActionType.Button);
            jump.AddBinding("<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");

            InputAction sprint = gameplay.AddAction("Sprint", InputActionType.Button);
            sprint.AddBinding("<Keyboard>/leftShift");
            sprint.AddBinding("<Gamepad>/leftStickPress");

            InputAction interact = gameplay.AddAction("Interact", InputActionType.Button);
            interact.AddBinding("<Keyboard>/e");
            interact.AddBinding("<Gamepad>/buttonWest");

            InputAction fire = gameplay.AddAction("Fire", InputActionType.Button);
            fire.AddBinding("<Mouse>/leftButton");
            fire.AddBinding("<Gamepad>/rightTrigger");

            InputAction aim = gameplay.AddAction("Aim", InputActionType.Button);
            aim.AddBinding("<Mouse>/rightButton");
            aim.AddBinding("<Gamepad>/leftTrigger");

            InputAction reload = gameplay.AddAction("Reload", InputActionType.Button);
            reload.AddBinding("<Keyboard>/r");
            reload.AddBinding("<Gamepad>/buttonNorth");

            InputAction heal = gameplay.AddAction("Heal", InputActionType.Button);
            heal.AddBinding("<Keyboard>/h");
            heal.AddBinding("<Gamepad>/dpadUp");

            InputAction pause = gameplay.AddAction("Pause", InputActionType.Button);
            pause.AddBinding("<Keyboard>/escape");
            pause.AddBinding("<Gamepad>/start");
        }

        private static void BuildUiMap(InputActionAsset asset)
        {
            InputActionMap ui = asset.AddActionMap("UI");

            InputAction navigate = ui.AddAction("Navigate", InputActionType.Value, expectedControlLayout: "Vector2");
            navigate.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            navigate.AddBinding("<Gamepad>/leftStick");
            navigate.AddBinding("<Gamepad>/dpad");

            InputAction submit = ui.AddAction("Submit", InputActionType.Button);
            submit.AddBinding("<Keyboard>/enter");
            submit.AddBinding("<Gamepad>/buttonSouth");

            InputAction cancel = ui.AddAction("Cancel", InputActionType.Button);
            cancel.AddBinding("<Keyboard>/escape");
            cancel.AddBinding("<Gamepad>/buttonEast");

            InputAction point = ui.AddAction("Point", InputActionType.Value, expectedControlLayout: "Vector2");
            point.AddBinding("<Mouse>/position");

            InputAction click = ui.AddAction("Click", InputActionType.Button);
            click.AddBinding("<Mouse>/leftButton");

            InputAction rightClick = ui.AddAction("RightClick", InputActionType.Button);
            rightClick.AddBinding("<Mouse>/rightButton");

            InputAction middleClick = ui.AddAction("MiddleClick", InputActionType.Button);
            middleClick.AddBinding("<Mouse>/middleButton");

            InputAction scrollWheel = ui.AddAction("ScrollWheel", InputActionType.PassThrough, expectedControlLayout: "Vector2");
            scrollWheel.AddBinding("<Mouse>/scroll");

            InputAction trackedPosition = ui.AddAction("TrackedDevicePosition", InputActionType.PassThrough, expectedControlLayout: "Vector3");
            trackedPosition.AddBinding("<TrackedDevice>/devicePosition");

            InputAction trackedOrientation = ui.AddAction("TrackedDeviceOrientation", InputActionType.PassThrough, expectedControlLayout: "Quaternion");
            trackedOrientation.AddBinding("<TrackedDevice>/deviceRotation");
        }
    }
}
