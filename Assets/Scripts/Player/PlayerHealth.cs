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
    
    // Property untuk akses public
    public int CurrentHealth => currentHealth.Value;
    public int MaxHealth => maxHealth;
    public float CurrentSpeedMultiplier { get; private set; } = 1f;

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

    // Public method untuk menerima damage
    public void TakeDamage(int damageAmount = 1)
    {
        if (!IsOwner) return;
        
        if (currentHealth.Value > 0)
        {
            TakeDamageServerRpc(damageAmount);
        }
    }

    // Server RPC untuk handle damage
    [ServerRpc]
    private void TakeDamageServerRpc(int damageAmount)
    {
        if (currentHealth.Value > 0)
        {
            currentHealth.Value = Mathf.Max(0, currentHealth.Value - damageAmount);
            Debug.Log($"Player took {damageAmount} damage. Current health: {currentHealth.Value}");
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
}
