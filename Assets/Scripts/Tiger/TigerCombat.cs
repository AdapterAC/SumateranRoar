using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Animator))]
public class TigerCombat : NetworkBehaviour
{
    private Animator animator;
    private NetworkVariable<int> networkBiteIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        networkBiteIndex.OnValueChanged += OnBiteIndexChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        networkBiteIndex.OnValueChanged -= OnBiteIndexChanged;
    }

    private void OnBiteIndexChanged(int previous, int current)
    {
        // Hanya client yang perlu update animator dari network variable
        if (!IsServer)
        {
            animator.SetInteger("BiteIndex", current);
        }
    }

    void Update()
    {
        // Hanya owner yang bisa menyerang
        if (!IsOwner) return;

        // Serangan Cakar (Klik Kiri)
        if (Input.GetMouseButtonDown(0))
        {
            TriggerAttackServerRpc(0); // 0 = Claw attack
        }

        // Serangan Gigitan (Tombol E)
        if (Input.GetKeyDown(KeyCode.E))
        {
            TriggerAttackServerRpc(1); // 1 = Bite attack
        }

        // Serangan Ultimate (Klik Kanan)
        if (Input.GetMouseButtonDown(1))
        {
            TriggerAttackServerRpc(2); // 2 = Ultimate attack
        }
    }

    [ServerRpc]
    private void TriggerAttackServerRpc(int attackType)
    {
        if (attackType == 1) // Bite attack
        {
            // Ganti index di server
            networkBiteIndex.Value = 1 - networkBiteIndex.Value;
            animator.SetInteger("BiteIndex", networkBiteIndex.Value);
        }
        
        // Broadcast ke semua client
        TriggerAttackClientRpc(attackType);
    }

    [ClientRpc]
    private void TriggerAttackClientRpc(int attackType)
    {
        if (attackType == 0)
        {
            // Claw attack
            animator.SetTrigger("AttackClaw");
        }
        else if (attackType == 1)
        {
            // Bite attack - index sudah di-set oleh server
            animator.SetTrigger("AttackBite");
        }
        else if (attackType == 2)
        {
            // Ultimate attack
            animator.SetTrigger("AttackUltimate");
        }
    }
}
