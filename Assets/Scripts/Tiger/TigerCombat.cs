using UnityEngine;
using Unity.Netcode;
using FOV;
using System.Collections.Generic;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(FieldOfView))]
public class TigerCombat : NetworkBehaviour
{
    [Header("Combat Settings")]
    public float aimAssistRotationSpeed = 10f;
    public bool enableAimAssist = true;

    private Animator animator;
    private FieldOfView fieldOfView;
    private NetworkVariable<int> networkBiteIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Awake()
    {
        animator = GetComponent<Animator>();
        fieldOfView = GetComponent<FieldOfView>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        networkBiteIndex.OnValueChanged += OnBiteIndexChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        networkBiteIndex.OnValueChanged -= OnBiteIndexChanged;
    }

    private void OnBiteIndexChanged(int previous, int current)
    {
        // Hanya client yang perlu update animator dari network variable
        if (!IsServer)
        {
            animator.SetInteger("BiteIndex", current);
        }
    }

    void Update()
    {
        // Hanya owner yang bisa menyerang
        if (!IsOwner) return;

        // Serangan Cakar (Klik Kiri)
        if (Input.GetMouseButtonDown(0))
        {
            AimAtNearestPlayerInFOV();
            TriggerAttackServerRpc(0); // 0 = Claw attack
        }

        // Serangan Gigitan (Tombol E)
        if (Input.GetKeyDown(KeyCode.E))
        {
            AimAtNearestPlayerInFOV();
            TriggerAttackServerRpc(1); // 1 = Bite attack
        }

        // Serangan Ultimate (Klik Kanan)
        if (Input.GetMouseButtonDown(1))
        {
            AimAtNearestPlayerInFOV();
            TriggerAttackServerRpc(2); // 2 = Ultimate attack
        }
    }

    /// <summary>
    /// Mencari human terdekat dalam FOV dan memutar tiger ke arahnya (aim assist)
    /// </summary>
    private void AimAtNearestPlayerInFOV()
    {
        if (!enableAimAssist || fieldOfView == null) return;

        // Dapatkan semua Transform dengan tag "Player" dalam FOV
        List<Transform> playersInFOV = fieldOfView.Field<Transform>("Player");

        if (playersInFOV.Count == 0) return;

        // Cari player terdekat
        Transform nearestPlayer = null;
        float nearestDistance = float.MaxValue;

        foreach (Transform player in playersInFOV)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestPlayer = player;
            }
        }

        // Putar tiger menghadap ke player terdekat
        if (nearestPlayer != null)
        {
            Vector3 directionToPlayer = (nearestPlayer.position - transform.position).normalized;
            directionToPlayer.y = 0; // Hanya rotate di axis Y (horizontal)

            if (directionToPlayer.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, aimAssistRotationSpeed * Time.deltaTime);
                
                // Untuk instant rotation, gunakan ini:
                // transform.rotation = targetRotation;
            }
        }
    }

    [ServerRpc]
    private void TriggerAttackServerRpc(int attackType)
    {
        if (attackType == 1) // Bite attack
        {
            // Ganti index di server
            networkBiteIndex.Value = 1 - networkBiteIndex.Value;
            animator.SetInteger("BiteIndex", networkBiteIndex.Value);
        }
        
        // Broadcast ke semua client
        TriggerAttackClientRpc(attackType);
    }

    [ClientRpc]
    private void TriggerAttackClientRpc(int attackType)
    {
        if (attackType == 0)
        {
            // Claw attack
            animator.SetTrigger("AttackClaw");
        }
        else if (attackType == 1)
        {
            // Bite attack - index sudah di-set oleh server
            animator.SetTrigger("AttackBite");
        }
        else if (attackType == 2)
        {
            // Ultimate attack
            animator.SetTrigger("AttackUltimate");
        }
    }
}
