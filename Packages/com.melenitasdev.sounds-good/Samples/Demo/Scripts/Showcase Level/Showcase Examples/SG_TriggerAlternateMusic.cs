using UnityEngine;

namespace MelenitasDev.SoundsGood.Demo
{
    public class SG_TriggerAlternateMusic : MonoBehaviour, IShowcaseResettable
    {
        [SerializeField] private MusicEmitter music;

        private void OnTriggerEnter (Collider other)
        {
            PlayMusic();
        }

        private void OnTriggerStay (Collider other)
        {
            PlayMusic();
        }

        private void OnTriggerExit (Collider other)
        {
            if (music != null) music.Stop();
        }

        public void ResetShowcaseState ()
        {
            if (music != null) music.Stop();
        }

        private void PlayMusic ()
        {
            if (music != null && !music.Music.Playing)
            {
                music.Play();
            }
        }
    }
}
