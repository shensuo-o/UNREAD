using UnityEngine;

namespace MelenitasDev.SoundsGood.Demo
{
    public class SG_AmbienceActiver : MonoBehaviour, IShowcaseResettable
    {
        [SerializeField] private RandomAmbience ambience;

        private void Start()
        {
            ResetShowcaseState();
        }

        private void OnTriggerEnter (Collider other)
        {
            if (ambience != null) ambience.Play();
        }

        private void OnTriggerStay (Collider other)
        {
            if (ambience != null) ambience.Play();
        }

        private void OnTriggerExit (Collider other)
        {
            if (ambience != null) ambience.Stop();
        }

        public void ResetShowcaseState ()
        {
            if (ambience != null) ambience.Stop();
        }
    }
}
