using System.Collections;
using UnityEngine;

/// <summary>
/// Mảnh ghép quản lý di chuyển cơ bản của Người chơi: Chạy ngang, Nhảy (Variable Height + Apex Modifiers), Trượt tường, Bẩy tường.
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

    [Header("Apex Modifiers Settings")]
    [Tooltip("Ngưỡng vận tốc Y để nhận diện nhân vật đang ở đỉnh cú nhảy (vd: 1f - 2f)")]
    [SerializeField] private float apexThreshold = 1.5f;

    [Tooltip("Hệ số giảm trọng lực khi ở Apex để tạo cảm giác lơ lửng Hang Time (0.3f - 0.5f)")]
    [SerializeField] private float apexGravityMultiplier = 0.5f;

    [Tooltip("Hệ số tăng tốc độ di chuyển ngang khi ở Apex (1.2f - 1.5f)")]
    [SerializeField] private float apexBonusSpeedMultiplier = 1.2f;

    private float currentApexBonus = 1f;
    private bool isAtApex;

    private float jumpBufferCounter;
    private bool isJumping; // Dùng để xác định pha nhảy lên cho Variable Jump Cut

    private Player player;
    private bool isWallSliding = false;
    private bool isWallJumping = false;
    private bool isWallFreezing = false;
    private float wallJumpDirection;
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

        // 🟢 1. Xử lý Bộ đệm phím Nhảy (Jump Buffer) - Sử dụng jumpBufferTime
        if (player.jumpInput)
        {
            jumpBufferCounter = jumpBufferTime;
            player.UseJumpInput();
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // 🟢 2. Xử lý Thả phím nhảy sớm (Variable Jump Height) - Sử dụng jumpCutMultiplier & isJumping
        if (player.jumpUpInput)
        {
            if (player.rb.linearVelocity.y > 0f && isJumping)
            {
                player.rb.linearVelocity = new Vector2(player.rb.linearVelocity.x, player.rb.linearVelocity.y / jumpCutMultiplier);
                isJumping = false;
            }
            player.UseJumpUpInput();
        }

        // 🟢 3. Cập nhật trạng thái Đỉnh cú nhảy (Apex Modifiers)
        isAtApex = !player.IsGrounded() && Mathf.Abs(player.rb.linearVelocity.y) < apexThreshold;
        if (isAtApex)
        {
            currentApexBonus = apexBonusSpeedMultiplier;
        }
        else
        {
            currentApexBonus = 1f;
        }
    }

    void FixedUpdate()
    {
        if (isDashing) return;

        HandleWallSlideState();

        if (!isWallJumping)
        {
            MovePlayer();
        }

        // 🌟 XỬ LÝ TRỌNG LỰC LINH HOẠT (FALL MULTIPLIER & APEX HANG TIME)
        if (player.rb.linearVelocity.y < 0f && !isWallSliding)
        {
            // Trạng thái đang rơi bình thường -> Rơi nặng và chắc chân
            player.rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
            isJumping = false; // Đã bắt đầu rơi -> Tắt cờ nhảy
        }
        else if (isAtApex && !isWallSliding)
        {
            // Trạng thái ở Đỉnh cú nhảy (Apex) -> Giảm trọng lực để lơ lửng nhẹ (Hang Time)
            player.rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (apexGravityMultiplier - 1) * Time.fixedDeltaTime;
        }

        HandleWallSlidePhysics();
        HandleJump();
    }

    void MovePlayer()
    {
        float targetSpeed = player.horizontalInput * moveSpeed * currentApexBonus;
        player.rb.linearVelocity = new Vector2(targetSpeed, player.rb.linearVelocity.y);
    }

    void HandleJump()
    {
        if (jumpBufferCounter > 0f)
        {
            if (player.IsGrounded())
            {
                player.rb.linearVelocity = new Vector2(player.rb.linearVelocity.x, jumpForce);
                jumpBufferCounter = 0f;
                isJumping = true; // Đánh dấu bắt đầu nhảy
            }
            else if (isWallSliding || (player.IsTouchingWall() && !player.IsGrounded()))
            {
                isWallJumping = true;
                wallJumpDirection = player.facingDirection * -1f;
                player.rb.linearVelocity = new Vector2(wallJumpDirection * wallJumpForce.x, wallJumpForce.y);
                wallJumpCounter = wallJumpDuration;

                jumpBufferCounter = 0f;
                isWallSliding = false;
                isJumping = true; // Đánh dấu bắt đầu nhảy tường
            }
        }

        if (isWallJumping)
        {
            wallJumpCounter -= Time.fixedDeltaTime;
            if (wallJumpCounter <= 0) isWallJumping = false;
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
            bool pressingIntoWall = (player.horizontalInput > 0.1f && player.facingDirection == 1) ||
                                    (player.horizontalInput < -0.1f && player.facingDirection == -1);

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
}