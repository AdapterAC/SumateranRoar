using UnityEngine;
using Unity.Netcode;

// Script untuk memberikan damage ke player
// Attach script ini ke objek yang bisa melukai player (misal: Tiger, trap, dll)
public class DamageDealer : NetworkBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private int damageAmount = 1;
    
    [Header("Damage Cooldown")]
    [Tooltip("Cooldown dalam detik sebelum bisa memberikan damage lagi ke player yang sama")]
    [SerializeField] private float damageCooldown = 1f;
    
    [Header("Trigger Settings")]
    [SerializeField] private bool damageOnTriggerEnter = true;
    [SerializeField] private bool damageOnTriggerStay = false;
    [SerializeField] private float damageInterval = 1f; // Interval untuk trigger stay
    
    [Header("Tags")]
    [SerializeField] private string playerTag = "Player";
    
    // Track last damage time per player
    private System.Collections.Generic.Dictionary<GameObject, float> lastDamageTime = new System.Collections.Generic.Dictionary<GameObject, float>();

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (!damageOnTriggerEnter) return;

        if (other.CompareTag(playerTag))
        {
            DealDamageToPlayer(other.gameObject);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsServer) return;
        if (!damageOnTriggerStay) return;

        if (other.CompareTag(playerTag))
        {
            // Check if enough time has passed since last damage
            if (!lastDamageTime.ContainsKey(other.gameObject) ||
                Time.time - lastDamageTime[other.gameObject] >= damageInterval)
            {
                DealDamageToPlayer(other.gameObject);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        if (collision.gameObject.CompareTag(playerTag))
        {
            DealDamageToPlayer(collision.gameObject);
        }
    }

    private void DealDamageToPlayer(GameObject playerObject)
    {
        if (!IsServer) return; // Ensure damage is only processed on server
        // Check cooldown
        if (lastDamageTime.ContainsKey(playerObject))
        {
            if (Time.time - lastDamageTime[playerObject] < damageCooldown)
            {
                return; // Still in cooldown
            }
        }

        // Get PlayerHealth component
        PlayerHealth playerHealth = playerObject.GetComponent<PlayerHealth>();
        
        if (playerHealth != null && playerHealth.IsAlive())
        {
            // Deal damage and pass attacker position
            playerHealth.TakeDamage(damageAmount, transform.position);
            
            // Update last damage time
            lastDamageTime[playerObject] = Time.time;
            
            Debug.Log($"{gameObject.name} dealt {damageAmount} damage to {playerObject.name}");
        }
    }

    // Public method untuk memberikan damage secara manual
    public void DealDamage(GameObject playerObject, int customDamageAmount = -1)
    {
        int damage = customDamageAmount > 0 ? customDamageAmount : damageAmount;
        
        PlayerHealth playerHealth = playerObject.GetComponent<PlayerHealth>();
        
        if (playerHealth != null && playerHealth.IsAlive())
        {
            playerHealth.TakeDamage(damage, transform.position);
            Debug.Log($"{gameObject.name} manually dealt {damage} damage to {playerObject.name}");
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize trigger area if has collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            
            if (col is BoxCollider boxCol)
            {
                Gizmos.DrawCube(boxCol.center, boxCol.size);
            }
            else if (col is SphereCollider sphereCol)
            {
                Gizmos.DrawSphere(sphereCol.center, sphereCol.radius);
            }
        }
    }
}
