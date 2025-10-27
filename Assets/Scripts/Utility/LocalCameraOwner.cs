using Unity.Netcode;
using UnityEngine;

namespace Utility
{
    public class LocalCameraOwner : NetworkBehaviour
    {
        [Header("All camera objects under this character (Camera, Cinemachine, etc)")]
        [SerializeField] private GameObject[] camerasToToggle;

        [Tooltip("Optional: AudioListeners to toggle with ownership")]
        [SerializeField] private AudioListener[] audioListeners;

        void Awake()
        {
            // Prevent multiple active cameras/listeners before ownership is known
            if (Application.isPlaying)
            {
                SetActiveForAll(false);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            SetActiveForAll(IsOwner);
        }

        public override void OnGainedOwnership()
        {
            SetActiveForAll(true);
        }

        public override void OnLostOwnership()
        {
            SetActiveForAll(false);
        }

        private void SetActiveForAll(bool active)
        {
            if (camerasToToggle != null)
            {
                foreach (var go in camerasToToggle)
                    if (go) go.SetActive(active);
            }

            if (audioListeners != null)
            {
                foreach (var al in audioListeners)
                    if (al) al.enabled = active;
            }
        }
    }
}