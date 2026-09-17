using UnityEngine;

namespace MelenitasDev.SoundsGood.Demo
{
    public class SG_PhoneController : MonoBehaviour, IShowcaseResettable
    {
        [SerializeField] private SoundEmitter phoneSound;

        public void PlaySound()
        {
            phoneSound.Play();
        }

        public void ResetShowcaseState ()
        {
            if (phoneSound != null) phoneSound.Stop();
        }
    }
}
