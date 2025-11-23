using UnityEngine;
using Unity.Netcode;

public class StaminaController : NetworkBehaviour
{
    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaConsumptionRate = 20f; // Stamina consumed per second
    [SerializeField] private float staminaRegenerationRate = 15f; // Stamina regenerated per second
    [SerializeField] private float staminaExhaustionThreshold = 10f; // Stamina needed to recover from exhaustion

    private NetworkVariable<float> currentStamina = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> isExhausted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private bool isSprinting = false;

    public event System.Action<float, float> OnStaminaChanged;
    public event System.Action<bool> OnExhaustionStateChanged;

    public float CurrentStamina => currentStamina.Value;
    public float MaxStamina => maxStamina;
    public bool IsExhausted => isExhausted.Value;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        currentStamina.OnValueChanged += (prev, current) => OnStaminaChanged?.Invoke(current, maxStamina);
        isExhausted.OnValueChanged += (prev, current) => OnExhaustionStateChanged?.Invoke(current);

        if (IsOwner)
        {
            currentStamina.Value = maxStamina;
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        if (isSprinting)
        {
            ConsumeStamina();
        }
        else
        {
            RegenerateStamina();
        }
    }

    private void ConsumeStamina()
    {
        if (currentStamina.Value > 0)
        {
            currentStamina.Value -= staminaConsumptionRate * Time.deltaTime;
            if (currentStamina.Value <= 0)
            {
                currentStamina.Value = 0;
                isExhausted.Value = true;
            }
        }
    }

    private void RegenerateStamina()
    {
        if (currentStamina.Value < maxStamina)
        {
            currentStamina.Value += staminaRegenerationRate * Time.deltaTime;
            if (currentStamina.Value > maxStamina)
            {
                currentStamina.Value = maxStamina;
            }

            if (isExhausted.Value && currentStamina.Value >= staminaExhaustionThreshold)
            {
                isExhausted.Value = false;
            }
        }
    }

    public void SetSprinting(bool sprinting)
    {
        isSprinting = sprinting;
    }

    public bool CanSprint()
    {
        return !isExhausted.Value && currentStamina.Value > 0;
    }
}
