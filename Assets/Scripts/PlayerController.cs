using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class PlayerController : MonoBehaviour
{
    [Header("Base Movement")]
    public float moveSpeed = 5f;

    [Header("Jump")]
    public float jumpForce = 10f;
    public int maxJumps = 2;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Audio")]
    public AudioClip jumpSound;

    [Header("Movement Audio")]
    public AudioClip moveSound;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private AudioSource audioSource;

    private float moveInput;
    private bool isGrounded;
    private bool wasGrounded;
    private bool isMoving;
    private int jumpsUsed;

    private float movementMultiplier = 1f;
    private float jumpMultiplier = 1f;

    private bool controlLocked = false;
    private Vector3 lockedWorldPosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // Check if grounded
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );

        // Reset jumps only when landing
        if (isGrounded && !wasGrounded)
        {
            jumpsUsed = 0;
        }

        wasGrounded = isGrounded;

        if (controlLocked)
        {
            moveInput = 0f;
            rb.linearVelocity = Vector2.zero;
            transform.position = new Vector3(
                lockedWorldPosition.x,
                lockedWorldPosition.y,
                transform.position.z
            );

            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            anim.SetFloat("Speed", 0f);
            return;
        }

        moveInput = Input.GetAxisRaw("Horizontal");

        // Jump with spacebar
        if (Input.GetKeyDown(KeyCode.Space) && jumpsUsed < maxJumps)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                jumpForce * jumpMultiplier
            );

            jumpsUsed++;

            // Play jump sound
            if (jumpSound != null)
            {
                audioSource.PlayOneShot(jumpSound);
            }
        }

        // Check if player is moving on ground
        isMoving = Mathf.Abs(moveInput) > 0.1f && isGrounded;

        // Play / stop movement sound
        if (moveSound != null)
        {
            if (isMoving)
            {
                if (!audioSource.isPlaying)
                {
                    audioSource.clip = moveSound;
                    audioSource.loop = true;
                    audioSource.Play();
                }
            }
            else
            {
                if (audioSource.isPlaying && audioSource.clip == moveSound)
                {
                    audioSource.Stop();
                }
            }
        }

        // Animation
        anim.SetFloat("Speed", Mathf.Abs(moveInput));

        // Flip sprite
        if (moveInput != 0)
        {
            sr.flipX = moveInput < 0;
        }
    }

    void FixedUpdate()
    {
        if (controlLocked)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = new Vector2(
            moveInput * moveSpeed * movementMultiplier,
            rb.linearVelocity.y
        );
    }

    public void SetMovementMultiplier(float multiplier)
    {
        movementMultiplier = Mathf.Max(0f, multiplier);
    }

    public void SetJumpMultiplier(float multiplier)
    {
        jumpMultiplier = Mathf.Max(0f, multiplier);
    }

    public void SetControlLock(bool locked, Vector3 worldPosition = default)
    {
        controlLocked = locked;

        if (locked)
        {
            lockedWorldPosition = worldPosition;
            rb.linearVelocity = Vector2.zero;

            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }

    public bool IsControlLocked()
    {
        return controlLocked;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}