using UnityEngine;

[RequireComponent(typeof(Animator))]
public class TigerCombat : MonoBehaviour
{
    private Animator animator;
    private int biteAttackIndex = 0; // Untuk mengganti animasi gigitan

    [Header("Combat Settings")]
    public float clawAttackRange = 2.0f;
    public int clawAttackDamage = 10;
    public LayerMask playerLayer; // Atur di Inspector, misal: layer "Player"

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Serangan Cakar (Klik Kiri)
        if (Input.GetMouseButtonDown(0))
        {
            animator.SetTrigger("AttackClaw");
        }

        // Serangan Gigitan (Tombol E)
        if (Input.GetKeyDown(KeyCode.E))
        {
            // Tentukan animasi gigitan mana yang akan diputar
            animator.SetInteger("BiteIndex", biteAttackIndex);
            animator.SetTrigger("AttackBite");

            // Ganti indeks untuk serangan berikutnya
            biteAttackIndex = 1 - biteAttackIndex; // Alternatif antara 0 dan 1
        }
    }

    // --- Fungsi ini dipanggil oleh Animation Event ---
    // Pastikan untuk menambahkannya di frame serangan pada animasi F_Attack_Claws.anim
    public void DealClawDamage()
    {
        Collider[] hitPlayers = Physics.OverlapSphere(transform.position + transform.forward, clawAttackRange, playerLayer);

        foreach (Collider player in hitPlayers)
        {
            Debug.Log("Harimau menyerang " + player.name);
            // Asumsi player memiliki skrip PlayerHealth dengan metode TakeDamage
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(clawAttackDamage);
            }
        }
    }

    // Gizmo untuk visualisasi jangkauan serangan di Editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward, clawAttackRange);
    }
}
