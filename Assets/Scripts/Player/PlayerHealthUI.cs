using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Script untuk menampilkan health bar UI
// Attach script ini ke Canvas atau UI GameObject
public class PlayerHealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    
    [Header("UI Elements")]
    [SerializeField] private Image[] healthIcons; // Array untuk 3 icon nyawa
    [SerializeField] private TextMeshProUGUI healthText; // Text untuk menampilkan health
    [SerializeField] private TextMeshProUGUI statusText; // Text untuk menampilkan status
    
    [Header("Heart Icons")]
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Sprite emptyHeartSprite;
    
    [Header("Colors")]
    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color damagedColor = Color.yellow;
    [SerializeField] private Color injuredColor = Color.red;
    [SerializeField] private Color deadColor = Color.gray;

    private void Start()
    {
        // Auto-find PlayerHealth if not assigned
        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            Debug.LogError("PlayerHealth tidak ditemukan! Assign PlayerHealth component di Inspector.");
            enabled = false;
            return;
        }

        UpdateUI();
    }

    private void Update()
    {
        if (playerHealth != null)
        {
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        int currentHealth = playerHealth.CurrentHealth;
        int maxHealth = playerHealth.MaxHealth;
        
        // Update health icons
        UpdateHealthIcons(currentHealth, maxHealth);
        
        // Update health text
        if (healthText != null)
        {
            healthText.text = $"{currentHealth} / {maxHealth}";
            healthText.color = GetHealthColor(currentHealth);
        }
        
        // Update status text
        if (statusText != null)
        {
            statusText.text = GetStatusText(currentHealth);
            statusText.color = GetHealthColor(currentHealth);
        }
    }

    private void UpdateHealthIcons(int currentHealth, int maxHealth)
    {
        if (healthIcons == null || healthIcons.Length == 0) return;

        for (int i = 0; i < healthIcons.Length; i++)
        {
            if (healthIcons[i] == null) continue;

            // Show icon if within max health range
            if (i < maxHealth)
            {
                healthIcons[i].gameObject.SetActive(true);
                
                // Set sprite based on current health
                if (fullHeartSprite != null && emptyHeartSprite != null)
                {
                    healthIcons[i].sprite = i < currentHealth ? fullHeartSprite : emptyHeartSprite;
                }
                
                // Set color
                healthIcons[i].color = i < currentHealth ? GetHealthColor(currentHealth) : Color.gray;
            }
            else
            {
                healthIcons[i].gameObject.SetActive(false);
            }
        }
    }

    private Color GetHealthColor(int health)
    {
        switch (health)
        {
            case 3: return healthyColor;
            case 2: return damagedColor;
            case 1: return injuredColor;
            case 0: return deadColor;
            default: return Color.white;
        }
    }

    private string GetStatusText(int health)
    {
        switch (health)
        {
            case 3: return "Healthy (100% Speed)";
            case 2: return "Damaged (75% Speed)";
            case 1: return "Injured (50% Speed)";
            case 0: return "Dead";
            default: return "";
        }
    }

    // Method untuk menampilkan damage feedback
    public void ShowDamageFeedback()
    {
        // TODO: Implement damage animation atau visual feedback
        // Misalnya: flash merah, shake, dll
    }
}
