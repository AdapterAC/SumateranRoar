using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController), typeof(Animator))]
public class TigerMovement : NetworkBehaviour
{
    private CharacterController controller;
    private Animator animator;
    
    [Header("Camera Reference")]
    public Transform cameraTransform; // Drag camera tiger ke sini

    [Header("Movement Settings")]
    public float walkSpeed = 2.0f;
    public float runSpeed = 6.0f;
    public float turnSpeed = 150.0f;
    public float mouseTurnSpeed = 2.0f; // Sensitivitas mouse untuk Turn parameter
    public float gravity = -20.0f;
    public float groundCheckDistance = 0.2f;

    private float verticalSpeed = 0f;
    private Vector3 moveDirection;
    private bool isGrounded;

    // Variabel untuk smoothing animasi
    public float animationSmoothTime = 0.2f;
    private float smoothedSpeed;
    private float smoothedTurn;
    
    // Variabel untuk akumulasi input mouse
    private float mouseInputAccumulator = 0f;

    // Network variables untuk sync animator parameters
    private NetworkVariable<float> networkSpeed = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<float> networkTurn = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // Variabel untuk terrain adaptation
    [Header("Terrain Adaptation")]
    public float groundRaycastDistance = 1.5f;
    public float rotationDamping = 5.0f;
    public LayerMask groundLayer;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        
        // Jika camera transform tidak di-assign, coba cari Camera.main sebagai fallback
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main?.transform;
            if (cameraTransform == null)
            {
                Debug.LogWarning("Camera Transform belum diassign! Drag camera object ke field Camera Transform di Inspector.");
            }
        }
        
        // Pastikan CharacterController menyentuh tanah
        controller.skinWidth = 0.08f;
        controller.minMoveDistance = 0f;

        if (groundLayer == 0)
        {
            groundLayer = LayerMask.GetMask("Default");
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Subscribe ke perubahan network variables untuk semua client
        networkSpeed.OnValueChanged += OnSpeedChanged;
        networkTurn.OnValueChanged += OnTurnChanged;

        // Jika bukan owner, set state awal dari network variables
        if (!IsOwner)
        {
            animator.SetFloat("Speed", networkSpeed.Value);
            animator.SetFloat("Turn", networkTurn.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        // Unsubscribe dari perubahan network variables
        networkSpeed.OnValueChanged -= OnSpeedChanged;
        networkTurn.OnValueChanged -= OnTurnChanged;
    }

    private void OnSpeedChanged(float previous, float current)
    {
        // Non-owner akan langsung set nilai, atau bisa juga ditambahkan smoothing di sini
        if (!IsOwner)
        {
            animator.SetFloat("Speed", current);
        }
    }

    private void OnTurnChanged(float previous, float current)
    {
        // Non-owner akan langsung set nilai, atau bisa juga ditambahkan smoothing di sini
        if (!IsOwner)
        {
            animator.SetFloat("Turn", current);
        }
    }

    void Update()
    {
        // Hanya owner yang bisa mengontrol
        if (!IsOwner) return;

        HandleMovement();
        HandleAnimation();
        HandleTerrainAdaptation();
    }

    void HandleMovement()
    {
        // Ground check
        isGrounded = controller.isGrounded;

        // Input
        float horizontal = Input.GetAxis("Horizontal"); // A/D atau Panah Kiri/Kanan
        float vertical = Input.GetAxis("Vertical"); // W/S atau Panah Atas/Bawah

        // Kecepatan saat ini (berjalan atau berlari)
        bool isRunning = Input.GetKey(KeyCode.LeftShift) && vertical > 0; // Hanya bisa lari maju
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        
        // Jika sedang bergerak, rotate harimau mengikuti arah kamera
        if (Mathf.Abs(vertical) > 0.1f && cameraTransform != null)
        {
            // Dapatkan arah kamera (hanya horizontal)
            Vector3 cameraForward = cameraTransform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();
            
            // Hitung rotasi target berdasarkan arah kamera
            Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
            
            // Rotate harimau secara smooth ke arah kamera
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime * 0.1f);
        }
        else
        {
            // Jika tidak bergerak, hanya rotate dengan tombol A/D
            float turnAmount = horizontal * turnSpeed * Time.deltaTime;
            transform.Rotate(0, turnAmount, 0);
        }
        
        // Menghitung arah gerakan berdasarkan forward harimau
        moveDirection = transform.forward * vertical * currentSpeed;

        // Mengaplikasikan gravitasi
        if (isGrounded && verticalSpeed < 0)
        {
            verticalSpeed = -2f;
        }
        else
        {
            verticalSpeed += gravity * Time.deltaTime;
        }
        moveDirection.y = verticalSpeed;

        // Menggerakkan CharacterController
        controller.Move(moveDirection * Time.deltaTime);
    }

    void HandleAnimation()
    {
        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        // 1. Tentukan target speed untuk animasi
        float targetAnimationSpeed = 0.0f;
        if (vertical > 0.1f)
        {
            targetAnimationSpeed = isRunning ? 1.0f : 0.5f;
        }
        else if (vertical < -0.1f)
        {
            targetAnimationSpeed = -0.5f; // Nilai untuk animasi berjalan mundur
        }
        
        // 2. Hitung Turn parameter berdasarkan perbedaan sudut antara harimau dan kamera
        float targetTurnAmount = 0f;
        
        if (Mathf.Abs(vertical) > 0.1f && cameraTransform != null)
        {
            // Dapatkan arah kamera dan harimau (hanya horizontal)
            Vector3 cameraForward = cameraTransform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();
            
            Vector3 tigerForward = transform.forward;
            tigerForward.y = 0;
            tigerForward.Normalize();
            
            // Hitung signed angle untuk menentukan arah belok
            float angleDifference = Vector3.SignedAngle(tigerForward, cameraForward, Vector3.up);
            
            // Konversi angle difference ke nilai Turn parameter (-1 sampai 1)
            // Normalisasi dari -90 sampai 90 derajat
            targetTurnAmount = Mathf.Clamp(angleDifference / 90f, -1f, 1f);
        }
        else
        {
            // Saat tidak bergerak, gunakan input keyboard untuk turn
            targetTurnAmount = horizontal;
        }

        // 3. Lakukan interpolasi (smoothing) dari nilai saat ini ke nilai target
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, targetAnimationSpeed, Time.deltaTime / animationSmoothTime);
        smoothedTurn = Mathf.Lerp(smoothedTurn, targetTurnAmount, Time.deltaTime / animationSmoothTime);

        // 4. Update network variables dengan nilai yang sudah di-smooth
        if (IsOwner)
        {
            networkSpeed.Value = smoothedSpeed;
            networkTurn.Value = smoothedTurn;
        }
        
        // 5. Update animator lokal untuk owner
        animator.SetFloat("Speed", smoothedSpeed);
        animator.SetFloat("Turn", smoothedTurn);
    }

    void HandleTerrainAdaptation()
    {
        RaycastHit hit;
        Vector3 raycastOrigin = transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(raycastOrigin, Vector3.down, out hit, groundRaycastDistance, groundLayer))
        {
            // Menyesuaikan posisi Y agar menempel di tanah
            Vector3 targetPosition = hit.point;
            if (controller.isGrounded)
            {
                transform.position = new Vector3(transform.position.x, targetPosition.y + controller.skinWidth, transform.position.z);
            }

            // Menyesuaikan rotasi dengan normal tanah
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationDamping);
        }
    }
}
