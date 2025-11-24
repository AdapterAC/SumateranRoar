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
    public bool instantAimRotation = true; // Opsi untuk instant rotation
    public bool debugMode = false; // Untuk melihat log input

    private Animator animator;
    private FieldOfView fieldOfView;
    private NetworkVariable<int> networkBiteIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    // Cooldown untuk mencegah spam dan memastikan animasi selesai
    private float lastAttackTime = 0f;
    public float attackCooldown = 0.1f; // Cooldown sangat kecil, hanya untuk mencegah double-click
    
    // Input buffering untuk mengatasi missed input
    private bool rightClickPressed = false;
    private bool leftClickPressed = false;
    private bool eKeyPressed = false;

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

    // Update dipanggil DULU untuk capture input
    void Update()
    {
        // Hanya owner yang bisa menyerang
        if (!IsOwner) return;

        // Capture input di Update (lebih reliable)
        if (Input.GetMouseButtonDown(1))
        {
            rightClickPressed = true;
            if (debugMode) Debug.Log($"[Input] Right click detected at frame {Time.frameCount}");
        }
        if (Input.GetMouseButtonDown(0))
        {
            leftClickPressed = true;
            if (debugMode) Debug.Log($"[Input] Left click detected at frame {Time.frameCount}");
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            eKeyPressed = true;
            if (debugMode) Debug.Log($"[Input] E key detected at frame {Time.frameCount}");
        }
    }

    // LateUpdate untuk process attack setelah semua Update selesai
    void LateUpdate()
    {
        if (!IsOwner) return;

        // PRIORITAS: Process klik kanan dulu (ultimate)
        if (rightClickPressed)
        {
            rightClickPressed = false;
            PerformAttack(2); // Ultimate attack
            return; // Early return
        }

        // Klik kiri (claw)
        if (leftClickPressed)
        {
            leftClickPressed = false;
            PerformAttack(0); // Claw attack
            return;
        }

        // Tombol E (bite)
        if (eKeyPressed)
        {
            eKeyPressed = false;
            PerformAttack(1); // Bite attack
            return;
        }
    }

    /// <summary>
    /// Eksekusi serangan dengan instant feedback
    /// </summary>
    private void PerformAttack(int attackType)
    {
        // Cek cooldown
        if (Time.time - lastAttackTime < attackCooldown)
        {
            if (debugMode) Debug.Log("Attack on cooldown");
            return;
        }
        
        lastAttackTime = Time.time;
        
        if (debugMode)
        {
            string attackName = attackType == 0 ? "Claw" : (attackType == 1 ? "Bite" : "Ultimate");
            Debug.Log($"[TigerCombat] Performing {attackName} attack at {Time.time}");
        }
        
        // Aim assist
        AimAtNearestPlayerInFOV();
        
        // Trigger animasi lokal INSTANT
        if (attackType == 0)
        {
            animator.SetTrigger("AttackClaw");
        }
        else if (attackType == 1)
        {
            // Update bite index untuk owner
            if (IsServer)
            {
                networkBiteIndex.Value = 1 - networkBiteIndex.Value;
            }
            animator.SetInteger("BiteIndex", 1 - animator.GetInteger("BiteIndex"));
            animator.SetTrigger("AttackBite");
        }
        else if (attackType == 2)
        {
            animator.SetTrigger("AttackUltimate");
        }
        
        // Sync ke network (non-blocking)
        TriggerAttackServerRpc(attackType);
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
                
                if (instantAimRotation)
                {
                    // Instant rotation untuk response yang lebih cepat
                    transform.rotation = targetRotation;
                }
                else
                {
                    // Smooth rotation
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, aimAssistRotationSpeed * Time.deltaTime);
                }
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
        }
        
        // Broadcast ke semua client (kecuali owner yang sudah trigger lokal)
        TriggerAttackClientRpc(attackType);
    }

    [ClientRpc]
    private void TriggerAttackClientRpc(int attackType)
    {
        // Skip jika ini adalah owner (sudah di-trigger lokal di Update)
        if (IsOwner) return;
        
        if (attackType == 0)
        {
            // Claw attack
            animator.SetTrigger("AttackClaw");
        }
        else if (attackType == 1)
        {
            // Bite attack - update index dari network variable
            animator.SetInteger("BiteIndex", networkBiteIndex.Value);
            animator.SetTrigger("AttackBite");
        }
        else if (attackType == 2)
        {
            // Ultimate attack
            animator.SetTrigger("AttackUltimate");
        }
    }
}
