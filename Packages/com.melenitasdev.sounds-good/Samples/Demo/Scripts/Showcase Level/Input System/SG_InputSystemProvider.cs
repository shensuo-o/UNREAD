#if ENABLE_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace MelenitasDev.SoundsGood.Demo
{
    /// <summary>
    /// Reads the demo's input through the Input System package, and hands it to <see cref="SG_Input"/>.
    /// <para>
    /// This whole assembly is skipped unless ENABLE_INPUT_SYSTEM is defined (see the .asmdef), which
    /// is what keeps a project without the package from failing on a reference it cannot resolve.
    /// </para>
    /// Keyboard and Mouse are read straight, with no action map and no input asset: the demo is a
    /// sample, and a sample that arrives needing its bindings configured is a sample that looks broken.
    /// </summary>
    internal static class SG_InputSystemProvider
    {
        // ----- Constants
        // The old "Mouse X/Y" axes are pixels scaled by the Input Manager's 0.1 sensitivity. The new
        // delta is raw pixels, so it is scaled to match and the camera keeps the same feel.
        private const float LOOK_SCALE = 0.1f;

        // ----- Unity Events
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install ()
        {
            SG_Input.Install(new SG_Input.Provider
            {
                KeyHeld = key => Control(key)?.isPressed ?? false,
                KeyPressedThisFrame = key => Control(key)?.wasPressedThisFrame ?? false,
                MouseButtonHeld = button => MouseButton(button)?.isPressed ?? false,
                MouseButtonPressedThisFrame = button => MouseButton(button)?.wasPressedThisFrame ?? false,
                LookDelta = () => Mouse.current == null
                    ? Vector2.zero
                    : Mouse.current.delta.ReadValue() * LOOK_SCALE,
                MoveAxes = ReadMoveAxes,
                JumpPressedThisFrame = () => Keyboard.current?.spaceKey.wasPressedThisFrame ?? false,
            });
        }

        // ----- Private Methods
        private static Vector2 ReadMoveAxes ()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return Vector2.zero;

            float x = 0f;
            float y = 0f;

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;

            return new Vector2(x, y);
        }

        private static ButtonControl MouseButton (int button)
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return null;

            switch (button)
            {
                case 0: return mouse.leftButton;
                case 1: return mouse.rightButton;
                case 2: return mouse.middleButton;
                default: return null;
            }
        }

        /// <summary>
        /// The Input System control for a KeyCode, or null when there is no equivalent.
        /// <para>
        /// The demo keeps its KeyCode fields so the Inspector values already set in the scene keep
        /// working. Only the keys a sample can plausibly be bound to are mapped; anything else reads
        /// as not pressed rather than throwing.
        /// </para>
        /// </summary>
        private static ButtonControl Control (KeyCode key)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return null;

            // Key.A through Key.Z are in the same order as KeyCode.A through KeyCode.Z.
            if (key >= KeyCode.A && key <= KeyCode.Z)
            {
                return keyboard[Key.A + (key - KeyCode.A)];
            }

            switch (key)
            {
                // Spelled out rather than offset from Key.Digit0: the Key enum runs Digit1 to
                // Digit9 and puts Digit0 after them, so arithmetic here would be off by one.
                case KeyCode.Alpha0: return keyboard.digit0Key;
                case KeyCode.Alpha1: return keyboard.digit1Key;
                case KeyCode.Alpha2: return keyboard.digit2Key;
                case KeyCode.Alpha3: return keyboard.digit3Key;
                case KeyCode.Alpha4: return keyboard.digit4Key;
                case KeyCode.Alpha5: return keyboard.digit5Key;
                case KeyCode.Alpha6: return keyboard.digit6Key;
                case KeyCode.Alpha7: return keyboard.digit7Key;
                case KeyCode.Alpha8: return keyboard.digit8Key;
                case KeyCode.Alpha9: return keyboard.digit9Key;

                case KeyCode.Space: return keyboard.spaceKey;
                case KeyCode.Return: return keyboard.enterKey;
                case KeyCode.Escape: return keyboard.escapeKey;
                case KeyCode.Tab: return keyboard.tabKey;
                case KeyCode.Backspace: return keyboard.backspaceKey;
                case KeyCode.LeftShift: return keyboard.leftShiftKey;
                case KeyCode.RightShift: return keyboard.rightShiftKey;
                case KeyCode.LeftControl: return keyboard.leftCtrlKey;
                case KeyCode.RightControl: return keyboard.rightCtrlKey;
                case KeyCode.LeftAlt: return keyboard.leftAltKey;
                case KeyCode.RightAlt: return keyboard.rightAltKey;
                case KeyCode.UpArrow: return keyboard.upArrowKey;
                case KeyCode.DownArrow: return keyboard.downArrowKey;
                case KeyCode.LeftArrow: return keyboard.leftArrowKey;
                case KeyCode.RightArrow: return keyboard.rightArrowKey;
                default: return null;
            }
        }
    }
}
#endif
