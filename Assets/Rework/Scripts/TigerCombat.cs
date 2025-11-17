using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Animator))]
public class TigerCombat : NetworkBehaviour
{
    private Animator animator;
    private NetworkVariable<int> networkBiteIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Combat Settings")]
    public float clawAttackRange = 2.0f;
    public int clawAttackDamage = 1; // Damage 1 karena player health hanya 3
    public float biteAttackRange = 1.5f;
    public int biteAttackDamage = 1; // Damage 1 karena player health hanya 3
    public LayerMask playerLayer; // Atur di Inspector, misal: layer "Player"

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
    }

    // --- Fungsi ini dipanggil oleh Animation Event ---
    // Pastikan untuk menambahkannya di frame serangan pada animasi F_Attack_Claws.anim
    public void DealClawDamage()
    {
        // Hanya server yang bisa memberikan damage
        if (!IsServer) return;

        Vector3 attackPosition = transform.position + transform.forward * (clawAttackRange * 0.5f);
        Collider[] hitPlayers = Physics.OverlapSphere(attackPosition, clawAttackRange, playerLayer);

        foreach (Collider player in hitPlayers)
        {
            Debug.Log("Server: Harimau menyerang dengan cakar: " + player.name);
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(clawAttackDamage);
            }
        }
    }

    // --- Fungsi ini dipanggil oleh Animation Event pada animasi Bite ---
    // Pastikan untuk menambahkannya di frame serangan pada animasi F_Attack_Bite_Left.anim dan F_Attack_Bite_Right.anim
    public void DealBiteDamage()
    {
        // Hanya server yang bisa memberikan damage
        if (!IsServer) return;
        
        Vector3 attackPosition = transform.position + transform.forward * (biteAttackRange * 0.5f);
        Collider[] hitPlayers = Physics.OverlapSphere(attackPosition, biteAttackRange, playerLayer);

        foreach (Collider player in hitPlayers)
        {
            Debug.Log("Server: Harimau menyerang dengan gigitan: " + player.name);
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(biteAttackDamage);
            }
        }
    }

    // Gizmo untuk visualisasi jangkauan serangan di Editor
    void OnDrawGizmosSelected()
    {
        // Claw attack range (merah)
        Gizmos.color = Color.red;
        Vector3 clawPosition = transform.position + transform.forward * (clawAttackRange * 0.5f);
        Gizmos.DrawWireSphere(clawPosition, clawAttackRange);
        
        // Bite attack range (kuning)
        Gizmos.color = Color.yellow;
        Vector3 bitePosition = transform.position + transform.forward * (biteAttackRange * 0.5f);
        Gizmos.DrawWireSphere(bitePosition, biteAttackRange);
    }
}
