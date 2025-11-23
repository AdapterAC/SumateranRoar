using UnityEngine;
using Unity.Netcode;
using System.Collections;

// Script untuk mengatur sistem nyawa pemain
// Player memiliki 3 nyawa, dengan penalti speed berdasarkan nyawa yang tersisa
public class PlayerHealth : NetworkBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 3;
    private NetworkVariable<int> currentHealth = new NetworkVariable<int>(3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Speed Penalties")]
    [Tooltip("Speed multiplier ketika health = 2 (75% dari normal)")]
    [SerializeField] private float twoHealthSpeedMultiplier = 0.75f;
    
    [Tooltip("Speed multiplier ketika health = 1 (50% dari normal)")]
    [SerializeField] private float oneHealthSpeedMultiplier = 0.5f;

    [Header("Animation Settings")]
    [Tooltip("Durasi transisi smooth untuk Injured layer weight")]
    [SerializeField] private float injuredLayerTransitionDuration = 0.5f;

    [Header("Auto-Run After Hit Settings")]
    [Tooltip("Durasi lari otomatis setelah terkena hit (dalam detik)")]
    [SerializeField] private float autoRunDuration = 3f;
    
    [Tooltip("Kecepatan lari otomatis (multiplier dari sprint speed)")]
    [SerializeField] private float autoRunSpeedMultiplier = 1.2f;

    [Header("References")]
    private Animator animator;
    private MoveBehaviour moveBehaviour;
    private BasicBehaviour basicBehaviour;
    
    // Original speed values untuk restore
    private float originalWalkSpeed;
    private float originalRunSpeed;
    private float originalSprintSpeed;
    
    // Animator parameters
    private int injuredLayerIndex;
    private int dieHash;
    private int hitHash;
    
    // Coroutine tracking
    private Coroutine injuredLayerTransitionCoroutine;
    private Coroutine autoRunCoroutine;
    
    // Auto-run state tracking
    private bool isAutoRunning = false;
    private Vector3 attackerPosition;
    
    // Property untuk akses public
    public int CurrentHealth => currentHealth.Value;
    public int MaxHealth => maxHealth;
    public float CurrentSpeedMultiplier { get; private set; } = 1f;

    public event System.Action<int, int> OnHealthChangedEvent;

    private void Awake()
    {
        // Setup references
        animator = GetComponent<Animator>();
        moveBehaviour = GetComponent<MoveBehaviour>();
        basicBehaviour = GetComponent<BasicBehaviour>();
        
        // Setup animator hash dan layer index
        dieHash = Animator.StringToHash("Die");
        hitHash = Animator.StringToHash("Hit");
        injuredLayerIndex = animator.GetLayerIndex("Injured");
        
        if (injuredLayerIndex == -1)
        {
            Debug.LogWarning("Injured layer tidak ditemukan di Animator! Pastikan layer 'Injured' sudah dibuat.");
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsOwner)
        {
            // Simpan original speed values
            if (moveBehaviour != null)
            {
                originalWalkSpeed = moveBehaviour.walkSpeed;
                originalRunSpeed = moveBehaviour.runSpeed;
                originalSprintSpeed = moveBehaviour.sprintSpeed;
            }
            
            // Initialize health
            if (IsServer)
            {
                currentHealth.Value = maxHealth;
            }
            
            // Subscribe ke perubahan health
            currentHealth.OnValueChanged += OnHealthChanged;
            
            // Apply initial state
            UpdatePlayerState(currentHealth.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        if (IsOwner)
        {
            currentHealth.OnValueChanged -= OnHealthChanged;
        }
    }

    // Callback ketika health berubah
    private void OnHealthChanged(int previousValue, int newValue)
    {
        if (!IsOwner) return;

        OnHealthChangedEvent?.Invoke(newValue, maxHealth);
        
        Debug.Log($"Health changed from {previousValue} to {newValue}");
        
        // Trigger hit animation jika health berkurang (terkena damage)
        if (newValue < previousValue && newValue > 0)
        {
            TriggerHitAnimation();
        }
        
        UpdatePlayerState(newValue);
    }

    // Update state pemain berdasarkan health
    private void UpdatePlayerState(int health)
    {
        if (!IsOwner || moveBehaviour == null || animator == null) return;

        switch (health)
        {
            case 3:
                // Health penuh - normal state
                CurrentSpeedMultiplier = 1f;
                SetInjuredLayer(false);
                ApplySpeedMultiplier(1f);
                break;
                
            case 2:
                // Health 2 - speed berkurang 25% (75% dari normal)
                CurrentSpeedMultiplier = twoHealthSpeedMultiplier;
                SetInjuredLayer(false);
                ApplySpeedMultiplier(twoHealthSpeedMultiplier);
                Debug.Log("Health = 2: Speed reduced to 75%");
                break;
                
            case 1:
                // Health 1 - masuk state injured dengan speed 50%
                CurrentSpeedMultiplier = oneHealthSpeedMultiplier;
                SetInjuredLayer(true);
                ApplySpeedMultiplier(oneHealthSpeedMultiplier);
                Debug.Log("Health = 1: Entered Injured state, speed reduced to 50%");
                break;
                
            case 0:
                // Health 0 - trigger death animation
                CurrentSpeedMultiplier = 0f;
                TriggerDeath();
                Debug.Log("Health = 0: Player died");
                break;
                
            default:
                // Handle unexpected values
                if (health < 0)
                {
                    currentHealth.Value = 0;
                    UpdatePlayerState(0);
                }
                break;
        }
    }

    // Terapkan speed multiplier ke MoveBehaviour
    private void ApplySpeedMultiplier(float multiplier)
    {
        if (moveBehaviour == null) return;
        
        moveBehaviour.walkSpeed = originalWalkSpeed * multiplier;
        moveBehaviour.runSpeed = originalRunSpeed * multiplier;
        moveBehaviour.sprintSpeed = originalSprintSpeed * multiplier;
    }

    // Set Injured layer weight
    private void SetInjuredLayer(bool isInjured)
    {
        if (animator == null || injuredLayerIndex == -1) return;
        
        // Stop coroutine yang sedang berjalan jika ada
        if (injuredLayerTransitionCoroutine != null)
        {
            StopCoroutine(injuredLayerTransitionCoroutine);
        }
        
        // Mulai smooth transition
        float targetWeight = isInjured ? 1f : 0f;
        injuredLayerTransitionCoroutine = StartCoroutine(SmoothTransitionInjuredLayer(targetWeight));
    }

    // Coroutine untuk smooth transition layer weight
    private IEnumerator SmoothTransitionInjuredLayer(float targetWeight)
    {
        if (animator == null || injuredLayerIndex == -1) yield break;
        
        float startWeight = animator.GetLayerWeight(injuredLayerIndex);
        float elapsed = 0f;
        
        while (elapsed < injuredLayerTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / injuredLayerTransitionDuration;
            
            // Smooth lerp menggunakan SmoothStep untuk transisi yang lebih natural
            float smoothT = t * t * (3f - 2f * t);
            float currentWeight = Mathf.Lerp(startWeight, targetWeight, smoothT);
            
            animator.SetLayerWeight(injuredLayerIndex, currentWeight);
            
            yield return null;
        }
        
        // Pastikan weight final tepat
        animator.SetLayerWeight(injuredLayerIndex, targetWeight);
        injuredLayerTransitionCoroutine = null;
    }

    // Trigger hit animation saat terkena damage
    private void TriggerHitAnimation()
    {
        if (animator == null) return;
        
        animator.SetTrigger(hitHash);
        Debug.Log("Hit animation triggered!");
    }

    // Trigger death animation
    private void TriggerDeath()
    {
        if (animator == null) return;
        
        // Disable movement
        if (moveBehaviour != null)
        {
            moveBehaviour.enabled = false;
        }
        
        if (basicBehaviour != null)
        {
            basicBehaviour.enabled = false;
        }
        
        // Trigger death animation
        animator.SetTrigger(dieHash);
        
        Debug.Log("Death animation triggered");
        
        // Optional: Call game over atau respawn setelah delay
        Invoke(nameof(OnPlayerDeath), 3f); // Delay 3 detik untuk animasi
    }

    // Callback ketika player mati (bisa di-override atau ditambah event)
    private void OnPlayerDeath()
    {
        Debug.Log("Player death complete. Implement respawn or game over logic here.");
        // TODO: Implement respawn logic atau game over screen
    }

    // Public method untuk menerima damage dengan posisi attacker
    public void TakeDamage(int damageAmount = 1, Vector3 attackerPos = default)
    {
        if (!IsServer)
        {
            // Jika bukan server, minta server untuk mengurangi health
            TakeDamageServerRpc(damageAmount, attackerPos);
        }
        else
        {
            // Jika sudah di server, langsung kurangi health
            if (currentHealth.Value > 0)
            {
                currentHealth.Value = Mathf.Max(0, currentHealth.Value - damageAmount);
                Debug.Log($"Server: Player took {damageAmount} damage. Current health: {currentHealth.Value}");
                
                // Trigger auto-run on the owner's client
                if (attackerPos != default)
                {
                    TriggerAutoRunClientRpc(attackerPos);
                }
            }
        }
    }

    // Server RPC untuk handle damage
    [ServerRpc(RequireOwnership = false)] // Allow non-owners to call this
    private void TakeDamageServerRpc(int damageAmount, Vector3 attackerPos = default)
    {
        if (currentHealth.Value > 0)
        {
            currentHealth.Value = Mathf.Max(0, currentHealth.Value - damageAmount);
            Debug.Log($"Server RPC: Player took {damageAmount} damage. Current health: {currentHealth.Value}");
            
            // Trigger auto-run on the owner's client
            if (attackerPos != default)
            {
                TriggerAutoRunClientRpc(attackerPos);
            }
        }
    }

    // Public method untuk heal
    public void Heal(int healAmount = 1)
    {
        if (!IsOwner) return;
        
        if (currentHealth.Value < maxHealth && currentHealth.Value > 0)
        {
            HealServerRpc(healAmount);
        }
    }

    // Server RPC untuk handle healing
    [ServerRpc]
    private void HealServerRpc(int healAmount)
    {
        if (currentHealth.Value > 0 && currentHealth.Value < maxHealth)
        {
            int oldHealth = currentHealth.Value;
            currentHealth.Value = Mathf.Min(maxHealth, currentHealth.Value + healAmount);
            Debug.Log($"Player healed {currentHealth.Value - oldHealth} HP. Current health: {currentHealth.Value}");
        }
    }

    // Reset health ke maksimum (untuk respawn)
    public void ResetHealth()
    {
        if (!IsOwner) return;
        
        ResetHealthServerRpc();
    }

    [ServerRpc]
    private void ResetHealthServerRpc()
    {
        currentHealth.Value = maxHealth;
        
        if (IsOwner)
        {
            // Re-enable movement
            if (moveBehaviour != null)
            {
                moveBehaviour.enabled = true;
            }
            
            if (basicBehaviour != null)
            {
                basicBehaviour.enabled = true;
            }
        }
    }

    // Check apakah player masih hidup
    public bool IsAlive()
    {
        return currentHealth.Value > 0;
    }

    // Check apakah player dalam state injured
    public bool IsInjured()
    {
        return currentHealth.Value == 1;
    }

    // Debug info untuk testing
    /*
    private void OnGUI()
    {
        if (!IsOwner) return;
        
        GUI.color = Color.white;
        GUI.Label(new Rect(10, 10, 300, 30), $"Health: {currentHealth.Value}/{maxHealth}");
        GUI.Label(new Rect(10, 40, 300, 30), $"Speed Multiplier: {CurrentSpeedMultiplier * 100}%");
        GUI.Label(new Rect(10, 70, 300, 30), $"State: {GetCurrentStateString()}");
        
        // Debug buttons
        if (GUI.Button(new Rect(10, 100, 150, 30), "Take Damage"))
        {
            TakeDamage(1);
        }
        
        if (GUI.Button(new Rect(170, 100, 150, 30), "Heal"))
        {
            Heal(1);
        }
    }
    */

    private string GetCurrentStateString()
    {
        switch (currentHealth.Value)
        {
            case 3: return "Healthy";
            case 2: return "Damaged (75% speed)";
            case 1: return "Injured (50% speed)";
            case 0: return "Dead";
            default: return "Unknown";
        }
    }

    // ClientRpc untuk trigger auto-run pada owner
    [ClientRpc]
    private void TriggerAutoRunClientRpc(Vector3 attackerPos)
    {
        if (!IsOwner) return;
        
        // Simpan posisi attacker dan mulai auto-run
        attackerPosition = attackerPos;
        
        // Stop auto-run coroutine yang sedang berjalan jika ada
        if (autoRunCoroutine != null)
        {
            StopCoroutine(autoRunCoroutine);
        }
        
        // Mulai auto-run coroutine
        autoRunCoroutine = StartCoroutine(AutoRunFromAttacker());
    }

    // Coroutine untuk auto-run menjauhi attacker
    private IEnumerator AutoRunFromAttacker()
    {
        if (moveBehaviour == null || basicBehaviour == null) yield break;
        if (!IsAlive()) yield break;
        
        isAutoRunning = true;
        float elapsed = 0f;
        
        Rigidbody rb = basicBehaviour.GetRigidBody;
        if (rb == null) yield break;
        
        Debug.Log($"Adrenaline mode activated! Running away from attacker for {autoRunDuration} seconds.");
        
        // Set animator to sprint speed immediately
        int speedHash = Animator.StringToHash("Speed");
        
        while (elapsed < autoRunDuration)
        {
            // Hitung arah lari (menjauhi attacker)
            Vector3 directionAwayFromAttacker = (transform.position - attackerPosition).normalized;
            directionAwayFromAttacker.y = 0; // Keep movement on horizontal plane
            
            if (directionAwayFromAttacker != Vector3.zero)
            {
                // Rotate player to face away from attacker
                Quaternion targetRotation = Quaternion.LookRotation(directionAwayFromAttacker);
                Quaternion newRotation = Quaternion.Slerp(rb.rotation, targetRotation, basicBehaviour.turnSmoothing * 3f);
                rb.MoveRotation(newRotation);
                
                // Calculate movement speed based on current health state
                float baseSpeed = moveBehaviour.sprintSpeed * autoRunSpeedMultiplier;
                float adjustedSpeed = baseSpeed * CurrentSpeedMultiplier; // Apply health penalty
                
                // Move player away from attacker using AddForce for smoother movement
                Vector3 targetVelocity = directionAwayFromAttacker * adjustedSpeed;
                targetVelocity.y = rb.linearVelocity.y; // Preserve vertical velocity (gravity)
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 10f);
                
                // Set animator speed to sprint value (higher values = faster animation)
                // Use sprintSpeed value directly for proper running animation
                animator.SetFloat(speedHash, moveBehaviour.sprintSpeed);
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Reset animator speed gradually
        animator.SetFloat(speedHash, 0f);
        
        isAutoRunning = false;
        autoRunCoroutine = null;
        
        Debug.Log("Adrenaline mode ended. Player regains manual control.");
    }
    
    // Property untuk check apakah sedang auto-running
    public bool IsAutoRunning()
    {
        return isAutoRunning;
    }
}
