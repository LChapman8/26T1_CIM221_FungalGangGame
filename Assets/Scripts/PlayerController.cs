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
    private float groundResetLockTimer = 0f;
    [SerializeField] private float groundResetLockDuration = 0.1f;

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
        if (groundResetLockTimer > 0f)
            groundResetLockTimer -= Time.deltaTime;

        bool groundedNow = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );

        if (groundedNow && !wasGrounded && groundResetLockTimer <= 0f && rb.linearVelocity.y <= 0.01f)
        {
            jumpsUsed = 0;
        }

        isGrounded = groundedNow;
        wasGrounded = groundedNow;

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

        if (Input.GetKeyDown(KeyCode.Space) && jumpsUsed < maxJumps)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                jumpForce * jumpMultiplier
            );

            jumpsUsed++;
            groundResetLockTimer = groundResetLockDuration;
            isGrounded = false;
        }

        anim.SetFloat("Speed", Mathf.Abs(moveInput));

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