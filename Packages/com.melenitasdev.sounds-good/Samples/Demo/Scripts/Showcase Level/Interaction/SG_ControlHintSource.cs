using System.Collections.Generic;
using UnityEngine;

namespace MelenitasDev.SoundsGood.Demo
{
    public class SG_ControlHintSource : MonoBehaviour
    {
        // ----- Serialized Fields
        [Header("Controls")]
        [SerializeField] private GameObject[] controlHintPrefabs;

        [Header("Activation")]
        [SerializeField] private bool showWhenLookedAt = true;
        [SerializeField] private bool showWhileInsideTrigger = false;

        // ----- Fields
        private readonly List<SG_PlayerControlHints> registeredPlayers = new List<SG_PlayerControlHints>();

        // ----- Properties
        public GameObject[] ControlHintPrefabs => controlHintPrefabs;
        public bool ShowWhenLookedAt => showWhenLookedAt;

        // ----- Unity Events
        void OnTriggerEnter (Collider other)
        {
            RegisterPlayer(other);
        }

        void OnTriggerStay (Collider other)
        {
            RegisterPlayer(other);
        }

        void OnTriggerExit (Collider other)
        {
            if (!showWhileInsideTrigger)
            {
                return;
            }

            SG_PlayerControlHints playerHints = FindPlayerHints(other);
            if (playerHints == null)
            {
                return;
            }

            registeredPlayers.Remove(playerHints);
            playerHints.RemoveTriggerSource(this);
        }

        void OnDisable ()
        {
            for (int i = registeredPlayers.Count - 1; i >= 0; i--)
            {
                SG_PlayerControlHints playerHints = registeredPlayers[i];
                if (playerHints != null)
                {
                    playerHints.RemoveTriggerSource(this);
                }
            }

            registeredPlayers.Clear();
        }

        private void RegisterPlayer (Collider other)
        {
            if (!showWhileInsideTrigger)
            {
                return;
            }

            SG_PlayerControlHints playerHints = FindPlayerHints(other);
            if (playerHints == null || registeredPlayers.Contains(playerHints))
            {
                return;
            }

            registeredPlayers.Add(playerHints);
            playerHints.AddTriggerSource(this);
        }

        private static SG_PlayerControlHints FindPlayerHints (Collider other)
        {
            if (other == null)
            {
                return null;
            }

            SG_PlayerControlHints playerHints = other.GetComponentInParent<SG_PlayerControlHints>();
            if (playerHints != null)
            {
                return playerHints;
            }

            Rigidbody attachedRigidbody = other.attachedRigidbody;
            if (attachedRigidbody != null)
            {
                playerHints = attachedRigidbody.GetComponentInParent<SG_PlayerControlHints>();
                if (playerHints != null)
                {
                    return playerHints;
                }

                playerHints = attachedRigidbody.GetComponentInChildren<SG_PlayerControlHints>();
                if (playerHints != null)
                {
                    return playerHints;
                }
            }

            return other.transform.root.GetComponentInChildren<SG_PlayerControlHints>();
        }
    }
}
