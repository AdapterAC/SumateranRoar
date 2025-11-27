using UnityEngine;
using UnityEngine.UI;

public class CooldownUI : MonoBehaviour
{
    [SerializeField] private Image imageCooldown;
    private float cooldownTime;
    private float cooldownTimer;

    void Start()
    {
        if (imageCooldown == null)
        {
            imageCooldown = GetComponent<Image>();
        }
        imageCooldown.fillAmount = 0.0f;
        gameObject.SetActive(false); // Mulai dalam keadaan tidak aktif
    }

    void Update()
    {
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
            imageCooldown.fillAmount = cooldownTimer / cooldownTime;
        }
        else
        {
            imageCooldown.fillAmount = 0.0f;
            gameObject.SetActive(false); // Nonaktifkan setelah cooldown selesai
        }
    }

    public void StartCooldown(float time)
    {
        gameObject.SetActive(true); // Aktifkan saat cooldown dimulai
        cooldownTime = time;
        cooldownTimer = time;
        imageCooldown.fillAmount = 1.0f;
    }
}
