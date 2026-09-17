using TMPro;
using UnityEngine;

namespace MelenitasDev.SoundsGood.Demo
{
    public class SG_Speaker : MonoBehaviour, IShowcaseResettable
    {
        // ----- Serialized Fields
        [SerializeField] private MusicEmitter musicEmitter;
        [SerializeField] private TextMeshProUGUI[] occlusionStateLabel;
    
        // ----- Fields
        private bool usingOcclusion = false;
    
        // ----- Public Methods
        public void SwitchOcclusion ()
        {
            usingOcclusion = !usingOcclusion;
            
            if (musicEmitter != null)
            {
                musicEmitter.Music.SetOcclusion(usingOcclusion).Play();
            }
            
            UpdateOcclusionLabels();
        }

        public void ResetShowcaseState ()
        {
            usingOcclusion = false;

            if (musicEmitter != null)
            {
                musicEmitter.Music.SetOcclusion(false);
                musicEmitter.Stop();
            }

            UpdateOcclusionLabels();
        }

        private void UpdateOcclusionLabels ()
        {
            if (occlusionStateLabel == null)
            {
                return;
            }

            foreach (TextMeshProUGUI label in occlusionStateLabel)
            {
                if (label != null)
                {
                    label.text = $"Occlusion: {(usingOcclusion ? "On" : "Off")}";
                }
            }
        }
    }
}
