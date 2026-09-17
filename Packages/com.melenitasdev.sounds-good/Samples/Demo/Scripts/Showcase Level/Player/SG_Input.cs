using System;
using UnityEngine;

namespace MelenitasDev.SoundsGood.Demo
{
    /// <summary>
    /// The demo's one place for reading input, so it runs on either input system.
    /// <para>
    /// UnityEngine.Input only exists while the old Input Manager is enabled. A project set to
    /// "Input System Package (New)" does not have it, and the demo scripts failed to compile —
    /// which took the whole sample assembly down with them.
    /// </para>
    /// The old path lives here, behind its own define. The new one cannot: using it needs a
    /// reference to the Input System package, and an assembly that references a package the
    /// project has not installed does not compile at all. So it lives in its own assembly next
    /// to this one, which Unity only builds when that package is there (see its .asmdef), and
    /// which installs itself into the hooks below on load.
    /// <para>
    /// Nothing here reads an action map. Keys are read directly, so the demo has no asset to
    /// configure and no chance of arriving with its bindings unset.
    /// </para>
    /// </summary>
    public static class SG_Input
    {
        // ----- Types
        /// <summary>What the Input System assembly fills in when that package is active.</summary>
        public sealed class Provider
        {
            public Func<KeyCode, bool> KeyHeld;
            public Func<KeyCode, bool> KeyPressedThisFrame;
            public Func<int, bool> MouseButtonHeld;
            public Func<int, bool> MouseButtonPressedThisFrame;
            public Func<Vector2> LookDelta;
            public Func<Vector2> MoveAxes;
            public Func<bool> JumpPressedThisFrame;
        }

        // ----- Fields
        private static Provider provider;

        // ----- Public Methods
        /// <summary>Called by the Input System assembly before the first scene loads.</summary>
        public static void Install (Provider newProvider) => provider = newProvider;

        public static bool GetKey (KeyCode key)
        {
            if (provider != null) return provider.KeyHeld(key);
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(key);
#else
            return false;
#endif
        }

        public static bool GetKeyDown (KeyCode key)
        {
            if (provider != null) return provider.KeyPressedThisFrame(key);
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(key);
#else
            return false;
#endif
        }

        public static bool GetMouseButton (int button)
        {
            if (provider != null) return provider.MouseButtonHeld(button);
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButton(button);
#else
            return false;
#endif
        }

        public static bool GetMouseButtonDown (int button)
        {
            if (provider != null) return provider.MouseButtonPressedThisFrame(button);
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(button);
#else
            return false;
#endif
        }

        /// <summary>Mouse movement since the last frame, in the old "Mouse X/Y" scale.</summary>
        public static Vector2 GetLookDelta ()
        {
            if (provider != null) return provider.LookDelta();
#if ENABLE_LEGACY_INPUT_MANAGER
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#else
            return Vector2.zero;
#endif
        }

        /// <summary>Movement on both axes, unsmoothed, in the -1..1 range.</summary>
        public static Vector2 GetMoveAxes ()
        {
            if (provider != null) return provider.MoveAxes();
#if ENABLE_LEGACY_INPUT_MANAGER
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#else
            return Vector2.zero;
#endif
        }

        public static bool GetJumpDown ()
        {
            if (provider != null) return provider.JumpPressedThisFrame();
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetButtonDown("Jump");
#else
            return false;
#endif
        }
    }
}
