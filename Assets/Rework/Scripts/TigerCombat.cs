using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Animator))]
public class TigerCombat : NetworkBehaviour
{
    private Animator animator;
    private int biteAttackIndex = 0; // Untuk mengganti animasi gigitan

    [Header("Combat Settings")]
    public float clawAttackRange = 2.0f;
    public int clawAttackDamage = 1; // Damage 1 karena player health hanya 3
    public float biteAttackRange = 1.5f;
    public int biteAttackDamage = 1; // Damage 1 karena player health hanya 3
    public LayerMask playerLayer; // Atur di Inspector, misal: layer "Player"

    void Start()
    {
        animator = GetComponent<Animator>();
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
            // Bite attack - alternatif antara left dan right
            animator.SetInteger("BiteIndex", biteAttackIndex);
            animator.SetTrigger("AttackBite");

            // Hanya owner yang update index
            if (IsOwner)
            {
                biteAttackIndex = 1 - biteAttackIndex;
            }
        }
    }

    // --- Fungsi ini dipanggil oleh Animation Event ---
    // Pastikan untuk menambahkannya di frame serangan pada animasi F_Attack_Claws.anim
    public void DealClawDamage()
    {
        Vector3 attackPosition = transform.position + transform.forward * (clawAttackRange * 0.5f);
        Collider[] hitPlayers = Physics.OverlapSphere(attackPosition, clawAttackRange, playerLayer);

        foreach (Collider player in hitPlayers)
        {
            Debug.Log("Harimau menyerang dengan cakar: " + player.name);
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
        Vector3 attackPosition = transform.position + transform.forward * (biteAttackRange * 0.5f);
        Collider[] hitPlayers = Physics.OverlapSphere(attackPosition, biteAttackRange, playerLayer);

        foreach (Collider player in hitPlayers)
        {
            Debug.Log("Harimau menyerang dengan gigitan: " + player.name);
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
