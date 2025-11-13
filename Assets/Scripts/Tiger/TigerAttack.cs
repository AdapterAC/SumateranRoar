using UnityEngine;
using Unity.Netcode;

// Script untuk Tiger Attack yang memberikan damage ke player
// Attach ke Tiger GameObject
public class TigerAttack : NetworkBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRadius = 2f;
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private float attackCooldown = 1.5f;
    
    [Header("Detection")]
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private LayerMask playerLayer;
    
    [Header("Animation")]
    private Animator animator;
    private int attackHash;
    
    // Internal variables
    private float lastAttackTime;
    private Transform targetPlayer;
    private bool isAttacking = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        attackHash = Animator.StringToHash("Attack");
    }

    private void Update()
    {
        if (!IsOwner) return;
        
        // Detect nearby player
        DetectPlayer();
        
        // Try to attack if player in range
        if (targetPlayer != null && CanAttack())
        {
            TryAttack();
        }
    }

    private void DetectPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange, playerLayer);
        
        if (hits.Length > 0)
        {
            // Find closest player
            float closestDistance = Mathf.Infinity;
            Transform closest = null;
            
            foreach (Collider hit in hits)
            {
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = hit.transform;
                }
            }
            
            targetPlayer = closest;
        }
        else
        {
            targetPlayer = null;
        }
    }

    private bool CanAttack()
    {
        if (isAttacking) return false;
        if (Time.time - lastAttackTime < attackCooldown) return false;
        
        if (targetPlayer != null)
        {
            float distance = Vector3.Distance(transform.position, targetPlayer.position);
            return distance <= attackRadius;
        }
        
        return false;
    }

    private void TryAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        
        // Trigger attack animation
        if (animator != null)
        {
            animator.SetTrigger(attackHash);
        }
        else
        {
            // If no animator, deal damage immediately
            DealDamageToTarget();
            isAttacking = false;
        }
    }

    // Called by animation event at the moment of impact
    public void OnAttackHit()
    {
        DealDamageToTarget();
    }

    // Called by animation event at the end of attack
    public void OnAttackComplete()
    {
        isAttacking = false;
    }

    private void DealDamageToTarget()
    {
        if (targetPlayer == null) return;
        
        // Check if still in range
        float distance = Vector3.Distance(transform.position, targetPlayer.position);
        if (distance > attackRadius) return;
        
        // Get PlayerHealth component
        PlayerHealth playerHealth = targetPlayer.GetComponent<PlayerHealth>();
        
        if (playerHealth != null && playerHealth.IsAlive())
        {
            // Deal damage
            playerHealth.TakeDamage(damageAmount);
            Debug.Log($"Tiger attacked {targetPlayer.name} for {damageAmount} damage");
        }
    }

    // Manual attack method (dapat dipanggil dari script lain)
    public void PerformAttack()
    {
        if (CanAttack())
        {
            TryAttack();
        }
    }

    // Visualize detection and attack range in editor
    private void OnDrawGizmosSelected()
    {
        // Detection range (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Attack range (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
        
        // Line to target
        if (targetPlayer != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetPlayer.position);
        }
    }

#if UNITY_EDITOR
    // Debug info in play mode
    private void OnGUI()
    {
        if (!IsOwner) return;
        
        GUIStyle style = new GUIStyle();
        style.fontSize = 12;
        style.normal.textColor = Color.white;
        
        string debugInfo = $"Tiger Attack Debug:\n";
        debugInfo += $"Target: {(targetPlayer != null ? targetPlayer.name : "None")}\n";
        debugInfo += $"Is Attacking: {isAttacking}\n";
        debugInfo += $"Can Attack: {CanAttack()}\n";
        debugInfo += $"Cooldown: {Mathf.Max(0, attackCooldown - (Time.time - lastAttackTime)):F1}s\n";
        
        if (targetPlayer != null)
        {
            float distance = Vector3.Distance(transform.position, targetPlayer.position);
            debugInfo += $"Distance: {distance:F1}m\n";
        }
        
        GUI.Label(new Rect(10, 150, 300, 150), debugInfo, style);
    }
#endif
}
