using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float wallSlideSpeed = 2f;
    [SerializeField] private Vector2 wallJumpForce = new Vector2(10f, 14f);
    [SerializeField] private float wallJumpDuration = 0.15f;
    [SerializeField] private float wallCatchDelay = 0.1f;
    [SerializeField] private float fallMultiplier = 2.5f;

    [Header("Ground/Wall Check Settings")]
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private Transform groundCheckPosition;
    [SerializeField] private Transform wallCheckPosition;
    [SerializeField] private float wallCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundCheckLayer;
    [SerializeField] private LayerMask wallCheckLayer;

    private Rigidbody2D rb;
    private float horizontalInput;
    private bool jumpInput;
    private bool isWallSliding = false;
    private bool isWallJumping = false;
    private bool isWallFreezing = false;
    private float wallJumpDirection;
    private float wallJumpCounter;
    private float originalGravity;

    // Thuộc tính để các Script combat/dash truy cập công khai
    public bool isDashing { get; set; } = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravity = rb.gravityScale;
    }

    void Update()
    {
        if (isDashing) return;

        GatherInput();
        HandleWallSlideState();
    }

    void FixedUpdate()
    {
        if (isDashing) return;

        if (!isWallJumping)
        {
            MovePlayer();
        }

        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }

        HandleWallSlidePhysics();
        HandleJump();
    }

    void GatherInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        if (Input.GetButtonDown("Jump")) jumpInput = true;
    }

    void MovePlayer()
    {
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
        if (horizontalInput > 0) transform.localScale = new Vector3(1f, 1f, 1f);
        else if (horizontalInput < 0) transform.localScale = new Vector3(-1f, 1f, 1f);
    }

    void HandleJump()
    {
        if (jumpInput)
        {
            if (IsGrounded())
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
            else if (isWallSliding || (IsTouchingWall() && !IsGrounded()))
            {
                isWallJumping = true;
                wallJumpDirection = -transform.localScale.x;
                rb.linearVelocity = new Vector2(wallJumpDirection * wallJumpForce.x, wallJumpForce.y);
                wallJumpCounter = wallJumpDuration;
            }
        }

        jumpInput = false;

        if (isWallJumping)
        {
            wallJumpCounter -= Time.fixedDeltaTime;
            if (wallJumpCounter <= 0) isWallJumping = false;
        }
    }

    void HandleWallSlideState()
    {
        if (isWallJumping)
        {
            isWallSliding = false;
            isWallFreezing = false;
            return;
        }

        bool currentlyTouchingWall = IsTouchingWall() && !IsGrounded();

        if (currentlyTouchingWall)
        {
            float wallDirection = transform.localScale.x;

            if (rb.linearVelocity.y > 0.1f && (Mathf.Sign(rb.linearVelocity.x) == wallDirection || Mathf.Abs(rb.linearVelocity.x) < 0.1f))
            {
                isWallSliding = false;
                isWallFreezing = false;
            }
            else
            {
                isWallSliding = true;
                if (!isWallFreezing && rb.linearVelocity.y <= 0.1f)
                {
                    StartCoroutine(WallCatchRoutine());
                }
            }
        }
        else
        {
            isWallSliding = false;
            isWallFreezing = false;
            StopCoroutine(WallCatchRoutine());
        }
    }

    IEnumerator WallCatchRoutine()
    {
        isWallFreezing = true;
        isWallSliding = true;
        yield return new WaitForSeconds(wallCatchDelay);
        isWallFreezing = false;
    }

    void HandleWallSlidePhysics()
    {
        if (isWallSliding)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
        }
    }

    public bool IsGrounded() => Physics2D.OverlapCircle(groundCheckPosition.position, groundCheckRadius, groundCheckLayer);
    public bool IsTouchingWall() => Physics2D.OverlapCircle(wallCheckPosition.position, wallCheckRadius, wallCheckLayer);

    private void OnDrawGizmos()
    {
        if (groundCheckPosition != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheckPosition.position, groundCheckRadius);
        }
        if (wallCheckPosition != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(wallCheckPosition.position, wallCheckRadius);
        }
    }
}