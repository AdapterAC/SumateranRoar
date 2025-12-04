using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class IndicatorBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image[] lifeBarImages;             
    [SerializeField] private TextMeshProUGUI textActivated;   
    [SerializeField] private TextMeshProUGUI textTimer;
    [SerializeField] private TextMeshProUGUI textHumanLive;

    private GameStateManager gsm;

    // Cache PlayerHealth milik lokal
    private PlayerHealth localPlayerHealth;

    private void Start()
    {
        gsm = GameStateManager.Instance;

        if (gsm == null)
        {
            Debug.LogError("[IndicatorBar] GameStateManager tidak ditemukan di scene!");
        }

        // Coba dapatkan PlayerHealth milik lokal
        TryResolveLocalPlayerHealth();
    }

    private void Update()
    {
        // Jika belum ketemu saat Start, coba lagi (misalnya player spawn belakangan)
        if (localPlayerHealth == null)
        {
            TryResolveLocalPlayerHealth();
        }

        UpdateTimer();
        UpdateActivatedText();
        UpdateHumanLiveText();
        UpdateLifeBar();
    }

    private void TryResolveLocalPlayerHealth()
    {
        if (NetworkManager.Singleton == null) return;
        var localClient = NetworkManager.Singleton.LocalClient;
        if (localClient?.PlayerObject == null) return;

        var go = localClient.PlayerObject.gameObject;
        localPlayerHealth = go.GetComponent<PlayerHealth>();

        if (localPlayerHealth == null)
        {
            Debug.LogWarning("[IndicatorBar] PlayerHealth tidak ditemukan pada PlayerObject lokal.");
        }
    }

    private void UpdateTimer()
    {
        if (gsm == null) return;
        int timer = gsm.GetTimerCountDown();
        int minutes = timer / 60;
        int seconds = timer % 60;
        textTimer.text = $"{minutes:00}:{seconds:00}";
    }

    private void UpdateLifeBar()
    {
        if (lifeBarImages == null || lifeBarImages.Length == 0) return;

        int current = 0;
        int max = lifeBarImages.Length;

        // Ambil nilai dari PlayerHealth jika ada
        if (localPlayerHealth != null)
        {
            current = Mathf.Clamp(localPlayerHealth.CurrentHealth, 0, max);
        }

        // Nyalakan sejumlah ikon sesuai current health, matikan sisanya
        for (int i = 0; i < max; i++)
        {
            bool active = i < current;
            var img = lifeBarImages[i];
            if (img == null) continue;

            img.enabled = active;                  // matikan/nyalakan renderer
            img.color = active ? Color.white : new Color(1f, 1f, 1f, 0.15f); // sedikit transparan saat mati
        }
    }

    private void UpdateActivatedText()
    {
        if (gsm == null) return;

        int activated = gsm.GetTotalActivatedExitGates();
        Debug.Log($"[IndicatorBar] Total Activated Exit Gates: {activated}");

        // Jika jumlah gate berbeda, ganti 4 dengan jumlah aktual
        textActivated.text = $"{activated}/4";
    }

    private void UpdateHumanLiveText()
    {
        if (gsm == null) return;
        int live = gsm.GetLivingHumans();
        int total = gsm.GetTotalHumans();
        textHumanLive.text = $"Humans Live: {live}/{total}";
    }
}
