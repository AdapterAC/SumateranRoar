using Unity.Netcode;
using UnityEngine;

public class EnableForOwner : NetworkBehaviour
{
    [SerializeField] private Behaviour[] enableOnlyForOwner;

    public override void OnNetworkSpawn()
    {
        SetOwnerEnabled(IsOwner);
    }

    private void SetOwnerEnabled(bool enabled)
    {
        if (enableOnlyForOwner == null) return;
        foreach (var b in enableOnlyForOwner)
        {
            if (b) b.enabled = enabled;
        }
    }
}