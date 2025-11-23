using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class StaminaUI : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image staminaBarImage;
    [SerializeField] private GameObject staminaBarContainer;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.5f;

    private StaminaController staminaController;
    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;

    void Awake()
    {
        staminaController = GetComponentInParent<StaminaController>();
        canvasGroup = staminaBarContainer.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = staminaBarContainer.AddComponent<CanvasGroup>();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (staminaController == null)
        {
            Debug.LogError("StaminaController not found on parent object.");
            enabled = false;
            return;
        }

        if (IsOwner)
        {
            staminaController.OnStaminaChanged += UpdateStaminaBar;
            staminaController.OnExhaustionStateChanged += HandleExhaustion;
            // We need a way to know if the player is sprinting to show/hide the bar
            // This will be handled by the player/tiger movement scripts
        }
        else
        {
            staminaBarContainer.SetActive(false);
        }
        
        canvasGroup.alpha = 0; // Start hidden
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && staminaController != null)
        {
            staminaController.OnStaminaChanged -= UpdateStaminaBar;
            staminaController.OnExhaustionStateChanged -= HandleExhaustion;
        }
        base.OnNetworkDespawn();
    }

    private void UpdateStaminaBar(float currentStamina, float maxStamina)
    {
        if (staminaBarImage != null)
        {
            staminaBarImage.fillAmount = currentStamina / maxStamina;
        }
    }

    private void HandleExhaustion(bool isExhausted)
    {
        // Maybe add a visual effect when exhausted, like a red flash
    }

    public void SetVisible(bool visible)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeCanvasGroup(visible ? 1f : 0f));
    }

    private System.Collections.IEnumerator FadeCanvasGroup(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float time = 0;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }
}
