using UnityEngine;

namespace MelenitasDev.SoundsGood.Demo
{
    public class SG_PlayerSoundsGoodTester : MonoBehaviour
    {
        [SerializeField] private CharacterController charController;
        [SerializeField] private GameObject pauseCanvas;
        [SerializeField] private SoundEmitter leftStepSound;
        [SerializeField] private SoundEmitter rightStepSound;
        [SerializeField] private SoundEmitter helloSound;
        [SerializeField] private float stepCooldown;
        [SerializeField, Range(0.1f, 1f)] private float walkStepCooldownMultiplier = 0.75f;
        [SerializeField, Range(0.1f, 1f)] private float sprintStepCooldownMultiplier = 0.5f;
        [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;

        private float stepTimer;
        private bool left;
        private bool paused = false;
    
        void Start ()
        {
            stepTimer = GetCurrentStepCooldown();
        }

        void Update ()
        {
            if (SG_Input.GetKeyDown(KeyCode.F))
            {
                helloSound.Play();
            }
        
            if (SG_Input.GetKeyDown(KeyCode.RightArrow))
            {
                float scale = Mathf.Min(3, Time.timeScale + 0.25f);
                Time.timeScale = scale;
            }
            else if (SG_Input.GetKeyDown(KeyCode.LeftArrow))
            {
                float scale = Mathf.Max(0.001f, Time.timeScale - 0.25f);
                Time.timeScale = scale;
            }
        
            if (SG_Input.GetKeyDown(KeyCode.P) && !paused)
            {
                pauseCanvas.SetActive(true);
                paused = true;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else if (SG_Input.GetKeyDown(KeyCode.P) && paused)
            {
                pauseCanvas.SetActive(false);
                paused = false;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }

            if (!charController.isGrounded || charController.velocity.sqrMagnitude <= 10) return;
        
            stepTimer -= Time.deltaTime;
        
            if (stepTimer > 0) return;

            if (left)
            {
                leftStepSound.Play();
                left = false;
            }
            else
            {
                rightStepSound.Play();
                left = true;
            }
        
            stepTimer = GetCurrentStepCooldown();
        }

        private float GetCurrentStepCooldown ()
        {
            float multiplier = SG_Input.GetKey(sprintKey)
                ? sprintStepCooldownMultiplier
                : walkStepCooldownMultiplier;

            return Mathf.Max(0.01f, stepCooldown * multiplier);
        }
    }
}
