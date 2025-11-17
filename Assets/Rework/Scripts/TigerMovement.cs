using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(Animator))]
public class TigerMovement : MonoBehaviour
{
    private CharacterController controller;
    private Animator animator;

    [Header("Movement Settings")]
    public float walkSpeed = 2.0f;
    public float runSpeed = 6.0f;
    public float turnSpeed = 150.0f;
    public float gravity = -20.0f;
    public float groundCheckDistance = 0.2f;

    private float verticalSpeed = 0f;
    private Vector3 moveDirection;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        
        // Pastikan CharacterController menyentuh tanah
        controller.skinWidth = 0.08f;
        controller.minMoveDistance = 0f;
    }

    void Update()
    {
        // Ground check yang lebih akurat
        isGrounded = controller.isGrounded;
        if (!isGrounded)
        {
            // Raycast untuk deteksi tanah yang lebih baik
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance + 0.1f))
            {
                isGrounded = true;
            }
        }

        // Input
        float horizontal = Input.GetAxis("Horizontal"); // A/D atau Panah Kiri/Kanan
        float vertical = Input.GetAxis("Vertical"); // W/S atau Panah Atas/Bawah

        // Kecepatan saat ini (berjalan atau berlari)
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        
        // Hanya bergerak maju jika ada input vertikal
        if (vertical < 0.1f) currentSpeed = 0;

        // Menghitung arah gerakan
        moveDirection = transform.forward * vertical * currentSpeed;

        // Mengaplikasikan gravitasi
        if (isGrounded)
        {
            verticalSpeed = -2f; // Gaya ke bawah lebih kuat agar tetap menempel di tanah
        }
        else
        {
            verticalSpeed += gravity * Time.deltaTime;
        }
        moveDirection.y = verticalSpeed;

        // Menggerakkan CharacterController
        controller.Move(moveDirection * Time.deltaTime);

        // Memutar karakter
        float turnAmount = horizontal * turnSpeed * Time.deltaTime;
        transform.Rotate(0, turnAmount, 0);

        // Update Animator
        // Gunakan kecepatan absolut untuk blend tree (0 = diam, >0 = bergerak)
        float animationSpeed = (vertical > 0.1f) ? (isRunning ? 1.0f : 0.5f) : 0.0f;
        animator.SetFloat("Speed", animationSpeed);
        animator.SetFloat("Turn", horizontal);
    }
}
