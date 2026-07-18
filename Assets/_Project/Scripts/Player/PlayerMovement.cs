using System.Collections;
using UnityEngine;

/// <summary>
/// Mảnh ghép quản lý di chuyển cơ bản của Người chơi: 
/// Chạy ngang, Nhảy (Variable Height + Apex Modifiers + Jump Buffer + Coyote Time + Ledge Corner Correction), Trượt tường, Bẩy tường.
/// </summary>
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

    [Header("Jump Assist & Variable Height")]
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private float jumpCutMultiplier = 2.5f;

    [Header("Coyote Time Settings")]
    [SerializeField] private float coyoteTime = 0.12f;
    private float coyoteTimeCounter;

    [Header("Apex Modifiers Settings")]
    [SerializeField] private float apexThreshold = 1.5f;
    [SerializeField] private float apexGravityMultiplier = 0.5f;
    [SerializeField] private float apexBonusSpeedMultiplier = 1.2f;

    [Header("Ledge Corner Correction Settings")]
    [SerializeField] private float cornerCorrectionDistance = 0.2f;
    [SerializeField] private Transform headCheckPosition;
    [SerializeField] private float headCheckDistance = 0.2f;

    private float currentApexBonus = 1f;
    private bool isAtApex;
    private float jumpBufferCounter;
    private bool isJumping;

    private Player player;
    private bool isWallSliding = false;
    private bool isWallJumping = false;
    private bool isWallFreezing = false;
    private float wallJumpCounter;

    private Coroutine wallCatchCoroutine;
    private WaitForSeconds wallCatchWait;

    public bool isDashing { get; set; } = false;

    void Awake()
    {
        player = GetComponent<Player>();
        wallCatchWait = new WaitForSeconds(wallCatchDelay);
    }

    void Update()
    {
        if (isDashing) return;

        // 1. Quản lý Coyote Time & Jump Buffer Timers
        coyoteTimeCounter = player.IsGrounded() ? coyoteTime : coyoteTimeCounter - Time.deltaTime;

        if (player.jumpInput)
        {
            jumpBufferCounter = jumpBufferTime;
            player.UseJumpInput();
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // 2. Thả phím nhảy sớm (Variable Jump Height)
        if (player.jumpUpInput)
        {
            if (player.rb.linearVelocity.y > 0f && isJumping)
            {
                player.rb.linearVelocity = new Vector2(player.rb.linearVelocity.x, player.rb.linearVelocity.y / jumpCutMultiplier);
                isJumping = false;
            }
            player.UseJumpUpInput();
        }

        // 3. Đánh giá trạng thái Đỉnh cú nhảy (Apex Modifiers)
        isAtApex = !player.IsGrounded() && Mathf.Abs(player.rb.linearVelocity.y) < apexThreshold;
        currentApexBonus = isAtApex ? apexBonusSpeedMultiplier : 1f;
    }

    void FixedUpdate()
    {
        if (isDashing) return;

        CheckCornerCorrection();
        HandleWallSlideState();

        if (!isWallJumping)
        {
            MovePlayer();
        }

        ApplyCustomGravity();
        HandleWallSlidePhysics();
        HandleJump();
    }

    void MovePlayer()
    {
        float targetSpeed = player.horizontalInput * moveSpeed * currentApexBonus;
        player.rb.linearVelocity = new Vector2(targetSpeed, player.rb.linearVelocity.y);
    }

    /// <summary>
    /// Áp dụng trọng lực linh hoạt: Rơi nặng hơn khi đi xuống, Lơ lửng hơn khi ở đỉnh Apex.
    /// </summary>
    void ApplyCustomGravity()
    {
        if (isWallSliding) return;

        if (player.rb.linearVelocity.y < 0f)
        {
            // Trạng thái đang rơi -> Rơi chắc chân
            player.rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
            isJumping = false;
        }
        else if (isAtApex)
        {
            // Trạng thái ở Đỉnh Apex -> Lơ lửng nhẹ (Hang Time)
            player.rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (apexGravityMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    void HandleJump()
    {
        if (jumpBufferCounter > 0f)
        {
            if (coyoteTimeCounter > 0f)
            {
                player.rb.linearVelocity = new Vector2(player.rb.linearVelocity.x, jumpForce);
                jumpBufferCounter = 0f;
                coyoteTimeCounter = 0f;
                isJumping = true;
            }
            else if (isWallSliding || (player.IsTouchingWall() && !player.IsGrounded()))
            {
                isWallJumping = true;
                float wallJumpDirection = player.facingDirection * -1f;
                player.rb.linearVelocity = new Vector2(wallJumpDirection * wallJumpForce.x, wallJumpForce.y);
                wallJumpCounter = wallJumpDuration;

                jumpBufferCounter = 0f;
                coyoteTimeCounter = 0f;
                isWallSliding = false;
                isJumping = true;
            }
        }

        if (isWallJumping)
        {
            wallJumpCounter -= Time.fixedDeltaTime;
            if (wallJumpCounter <= 0) isWallJumping = false;
        }
    }

    /// <summary>
    /// Tự động nhích nhẹ nhân vật sang bên khi bị vướng mép bục lúc nhảy lên.
    /// Tối ưu: Dùng rb.position thay cho transform.position để tránh giật hình vật lý.
    /// </summary>
    private void CheckCornerCorrection()
    {
        if (player.rb.linearVelocity.y <= 0f || headCheckPosition == null) return;

        Vector2 rayOrigin = headCheckPosition.position;

        bool leftHit = Physics2D.Raycast(rayOrigin - new Vector2(cornerCorrectionDistance, 0), Vector2.up, headCheckDistance, player.GetGroundLayer());
        bool rightHit = Physics2D.Raycast(rayOrigin + new Vector2(cornerCorrectionDistance, 0), Vector2.up, headCheckDistance, player.GetGroundLayer());

        if (leftHit && !rightHit)
        {
            player.rb.position += new Vector2(cornerCorrectionDistance, 0);
        }
        else if (rightHit && !leftHit)
        {
            player.rb.position -= new Vector2(cornerCorrectionDistance, 0);
        }
    }

    public void ForceWallJumpState(float counterDuration)
    {
        isWallJumping = true;
        wallJumpCounter = counterDuration;
    }

    void HandleWallSlideState()
    {
        if (isWallJumping)
        {
            isWallSliding = false;
            isWallFreezing = false;
            StopWallCatchCoroutine();
            return;
        }

        bool currentlyTouchingWall = player.IsTouchingWall() && !player.IsGrounded();

        if (currentlyTouchingWall)
        {
            // Rút gọn phép kiểm tra bấm phím hướng vào tường bằng toán học
            bool pressingIntoWall = Mathf.Abs(player.horizontalInput) > 0.1f && Mathf.Sign(player.horizontalInput) == player.facingDirection;

            if (player.rb.linearVelocity.y > 0.1f || !pressingIntoWall)
            {
                isWallSliding = false;
                isWallFreezing = false;
                StopWallCatchCoroutine();
            }
            else
            {
                isWallSliding = true;
                if (!isWallFreezing && player.rb.linearVelocity.y <= 0.1f && wallCatchCoroutine == null)
                {
                    wallCatchCoroutine = StartCoroutine(WallCatchRoutine());
                }
            }
        }
        else
        {
            isWallSliding = false;
            isWallFreezing = false;
            StopWallCatchCoroutine();
        }
    }

    IEnumerator WallCatchRoutine()
    {
        isWallFreezing = true;
        isWallSliding = true;
        yield return wallCatchWait;
        isWallFreezing = false;
    }

    void StopWallCatchCoroutine()
    {
        if (wallCatchCoroutine != null)
        {
            StopCoroutine(wallCatchCoroutine);
            wallCatchCoroutine = null;
        }
    }

    void HandleWallSlidePhysics()
    {
        if (isWallSliding)
        {
            player.rb.linearVelocity = new Vector2(player.rb.linearVelocity.x, -wallSlideSpeed);
        }
    }

    public void ApplyKnockbackStun(float duration)
    {
        isWallJumping = true;
        wallJumpCounter = duration;
    }

    private void OnDrawGizmos()
    {
        if (headCheckPosition != null)
        {
            Gizmos.color = Color.green;
            Vector3 origin = headCheckPosition.position;
            Gizmos.DrawLine(origin - new Vector3(cornerCorrectionDistance, 0, 0), origin - new Vector3(cornerCorrectionDistance, 0, 0) + Vector3.up * headCheckDistance);
            Gizmos.DrawLine(origin + new Vector3(cornerCorrectionDistance, 0, 0), origin + new Vector3(cornerCorrectionDistance, 0, 0) + Vector3.up * headCheckDistance);
        }
    }
}