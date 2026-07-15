using System.Collections;
using UnityEngine;

/// <summary>
/// Mảnh ghép quản lý di chuyển cơ bản của Người chơi: Chạy ngang, Nhảy, Trượt tường, và Bẩy tường.
/// Lấy toàn bộ dữ liệu vật lý và Input thông qua component trung tâm Player.
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

    private Player player; // Tham chiếu đến lớp trung tâm để lấy dữ liệu chung
    private bool isWallSliding = false;
    private bool isWallJumping = false;
    private bool isWallFreezing = false;
    private float wallJumpDirection;
    private float wallJumpCounter;

    private Coroutine wallCatchCoroutine;
    private WaitForSeconds wallCatchWait;

    /// <summary>
    /// Đánh dấu trạng thái nhân vật đang Lướt (Dash). Khi bằng true, mọi di chuyển bằng bàn phím sẽ tạm dừng.
    /// </summary>
    public bool isDashing { get; set; } = false;

    void Awake()
    {
        player = GetComponent<Player>();
        wallCatchWait = new WaitForSeconds(wallCatchDelay);
    }

    void FixedUpdate()
    {
        if (isDashing) return;

        // Cập nhật trạng thái trượt tường dựa trên dữ liệu va chạm từ lớp cha Entity thông qua component Player
        HandleWallSlideState();

        if (!isWallJumping)
        {
            MovePlayer();
        }

        // Áp dụng gia tốc rơi nặng và chắc chắn hơn
        if (player.rb.linearVelocity.y < 0)
        {
            player.rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }

        HandleWallSlidePhysics();
        HandleJump();
    }

    void MovePlayer()
    {
        player.rb.linearVelocity = new Vector2(player.horizontalInput * moveSpeed, player.rb.linearVelocity.y);
    }

    void HandleJump()
    {
        if (player.jumpInput)
        {
            if (player.IsGrounded())
            {
                player.rb.linearVelocity = new Vector2(player.rb.linearVelocity.x, jumpForce);
                player.UseJumpInput(); // Xóa bộ đệm input sau khi dùng
            }
            else if (isWallSliding || (player.IsTouchingWall() && !player.IsGrounded()))
            {
                isWallJumping = true;
                // Xác định hướng nhảy dựa trên hướng nhìn của Entity gốc
                wallJumpDirection = player.facingDirection * -1f;
                player.rb.linearVelocity = new Vector2(wallJumpDirection * wallJumpForce.x, wallJumpForce.y);
                wallJumpCounter = wallJumpDuration;
                player.UseJumpInput();
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
            if (player.rb.linearVelocity.y > 0.1f && (Mathf.Sign(player.rb.linearVelocity.x) == player.facingDirection || Mathf.Abs(player.rb.linearVelocity.x) < 0.1f))
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