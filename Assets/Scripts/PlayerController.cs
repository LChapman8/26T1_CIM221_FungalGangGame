using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
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

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;

    private float moveInput;
    private bool isGrounded;
    private bool wasGrounded;
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
        }
    }

    public bool IsControlLocked()
    {
        return controlLocked;
    }

    // Optional: visualize ground check
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}