using UnityEngine;
using Unity.Netcode;
using FOV;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(FieldOfView))]
public class TigerCombat : NetworkBehaviour
{
    [Header("Combat Settings")]
    public AnimationClip attHitAnim;
    public AnimationClip attMissAnim;
    public CooldownUI cooldownUI;
    public float aimAssistRotationSpeed = 10f;
    public bool enableAimAssist = true;
    public bool instantAimRotation = true; // Opsi untuk instant rotation
    public bool debugMode = false; // Untuk melihat log input

    private Animator animator;
    private FieldOfView fieldOfView;
    private TigerMovement tigerMovement;
    private NetworkVariable<int> networkBiteIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    // Cooldown untuk mencegah spam dan memastikan animasi selesai
    private float lastAttackTime = 0f;
    public float attackCooldown = 0.1f; // Cooldown sangat kecil, hanya untuk mencegah double-click
    
    // Input buffering untuk mengatasi missed input
    private bool rightClickPressed = false;
    private bool leftClickPressed = false;
    private bool eKeyPressed = false;
    private bool isAttacking = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
        fieldOfView = GetComponent<FieldOfView>();
        tigerMovement = GetComponent<TigerMovement>();
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
        // Cek cooldown dan status attacking
        if (Time.time - lastAttackTime < attackCooldown || isAttacking)
        {
            if (debugMode) Debug.Log("Attack on cooldown or already attacking");
            return;
        }

        lastAttackTime = Time.time;
        isAttacking = true;

        if (debugMode)
        {
            string attackName = attackType == 0 ? "Claw" : (attackType == 1 ? "Bite" : "Ultimate");
            Debug.Log($"[TigerCombat] Performing {attackName} attack at {Time.time}");
        }

        // 1. Aim assist dulu
        AimAtNearestPlayerInFOV();

        // 2. Trigger animasi attack (Claw/Bite/Ultimate) seperti biasa
        if (attackType == 0)
        {
            animator.SetTrigger("AttackClaw");
        }
        else if (attackType == 1)
        {
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

        // 3. Sync ke network
        TriggerAttackServerRpc(attackType);

        // 4. Mulai coroutine untuk attack sequence (attack anim → cek hit → cooldown anim)
        StartCoroutine(AttackSequence(attackType));
    }

    private IEnumerator AttackSequence(int attackType)
    {
        // Tunggu attack animation selesai (estimasi durasi attack animation)
        // Untuk Claw/Bite biasanya ~0.5-1 detik, Ultimate mungkin lebih lama
        float attackAnimDuration = attackType == 2 ? 1.5f : 0.8f;
        yield return new WaitForSeconds(attackAnimDuration);

        // Setelah attack animation selesai, CEK apakah kena player
        bool playerHit = CheckHitPlayer();
        
        // Pilih animasi cooldown (hit atau miss)
        AnimationClip cooldownAnim = playerHit ? attHitAnim : attMissAnim;
        float cooldownDuration = cooldownAnim != null ? cooldownAnim.length : 1f;

        // Nonaktifkan gerakan selama cooldown animation
        if (tigerMovement != null)
        {
            tigerMovement.SetCanMove(false);
        }

        // Play animasi cooldown (hit/miss)
        if (cooldownAnim != null)
        {
            animator.Play(cooldownAnim.name, 0, 0f);
        }

        // Tampilkan UI Cooldown
        if (cooldownUI != null)
        {
            cooldownUI.StartCooldown(cooldownDuration);
        }

        // Sync cooldown animation ke network
        PlayCooldownAnimServerRpc(cooldownAnim != null ? cooldownAnim.name : "");

        // Tunggu cooldown animation selesai
        yield return new WaitForSeconds(cooldownDuration);

        // Setelah cooldown selesai, aktifkan kembali gerakan
        isAttacking = false;
        if (tigerMovement != null)
        {
            tigerMovement.SetCanMove(true);
        }
    }

    /// <summary>
    /// Cek apakah ada player di FOV saat ini (untuk menentukan hit/miss)
    /// </summary>
    private bool CheckHitPlayer()
    {
        if (fieldOfView == null) return false;

        List<Transform> playersInFOV = fieldOfView.Field<Transform>("Player");
        return playersInFOV.Count > 0;
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

    [ServerRpc]
    private void PlayCooldownAnimServerRpc(string animName)
    {
        // Broadcast cooldown animation ke semua client
        PlayCooldownAnimClientRpc(animName);
    }

    [ClientRpc]
    private void TriggerAttackClientRpc(int attackType)
    {
        // Skip jika ini adalah owner (sudah di-trigger lokal di PerformAttack)
        if (IsOwner) return;
        
        // Trigger attack animation
        if (attackType == 0)
        {
            animator.SetTrigger("AttackClaw");
        }
        else if (attackType == 1)
        {
            animator.SetInteger("BiteIndex", networkBiteIndex.Value);
            animator.SetTrigger("AttackBite");
        }
        else if (attackType == 2)
        {
            animator.SetTrigger("AttackUltimate");
        }
    }

    [ClientRpc]
    private void PlayCooldownAnimClientRpc(string animName)
    {
        // Skip jika ini adalah owner (sudah di-play lokal di AttackSequence)
        if (IsOwner) return;
        
        // Play cooldown animation (hit/miss)
        if (!string.IsNullOrEmpty(animName))
        {
            animator.Play(animName, 0, 0f);
        }
    }
}
