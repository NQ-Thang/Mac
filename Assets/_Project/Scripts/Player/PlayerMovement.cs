using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý di chuyển cơ bản của Người chơi: Chạy ngang, Nhảy, Trượt tường (Wall Slide), 
/// Bẩy tường (Wall Jump), và cơ chế gia tốc rơi tự do.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    /// <summary>
    /// Tốc độ di chuyển ngang của nhân vật.
    /// </summary>
    [SerializeField] private float moveSpeed = 8f;

    /// <summary>
    /// Lực nhảy lên khi đứng dưới đất.
    /// </summary>
    [SerializeField] private float jumpForce = 12f;

    /// <summary>
    /// Tốc độ trượt xuống tối đa khi bám vào tường (Wall Slide).
    /// </summary>
    [SerializeField] private float wallSlideSpeed = 2f;

    /// <summary>
    /// Lực nhảy bẩy ra khỏi tường (X: Lực văng ngang, Y: Lực nảy lên).
    /// </summary>
    [SerializeField] private Vector2 wallJumpForce = new Vector2(10f, 14f);

    /// <summary>
    /// Thời gian tối đa giữ trạng thái Wall Jump (khóa di chuyển bàn phím để nhân vật văng tự nhiên).
    /// </summary>
    [SerializeField] private float wallJumpDuration = 0.15f;

    /// <summary>
    /// Thời gian khựng/dừng ngắn khi vừa bám vào tường trước khi bắt đầu trượt xuống.
    /// </summary>
    [SerializeField] private float wallCatchDelay = 0.1f;

    /// <summary>
    /// Hệ số tăng tốc độ rơi giúp đòn nhảy có cảm giác nặng và chắc chắn hơn khi rơi xuống.
    /// </summary>
    [SerializeField] private float fallMultiplier = 2.5f;

    [Header("Ground/Wall Check Settings")]
    /// <summary>
    /// Bán kính hình tròn kiểm tra va chạm mặt đất.
    /// </summary>
    [SerializeField] private float groundCheckRadius = 0.2f;

    /// <summary>
    /// Vị trí đặt điểm kiểm tra chạm đất (dưới chân nhân vật).
    /// </summary>
    [SerializeField] private Transform groundCheckPosition;

    /// <summary>
    /// Vị trí đặt điểm kiểm tra chạm tường (phía trước mặt nhân vật).
    /// </summary>
    [SerializeField] private Transform wallCheckPosition;

    /// <summary>
    /// Bán kính hình tròn kiểm tra va chạm tường.
    /// </summary>
    [SerializeField] private float wallCheckRadius = 0.2f;

    /// <summary>
    /// Layer đại diện cho mặt đất.
    /// </summary>
    [SerializeField] private LayerMask groundCheckLayer;

    /// <summary>
    /// Layer đại diện cho tường/vật cản có thể bám.
    /// </summary>
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
    /// <summary>
    /// Đánh dấu trạng thái nhân vật đang Lướt (Dash). Khi bằng true, mọi di chuyển bằng bàn phím sẽ tạm dừng.
    /// </summary>
    public bool isDashing { get; set; } = false; //cách viết ngắn gọn thay cho việc khai báo một biến và hai hàm get/set

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        //originalGravity = rb.gravityScale;
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

        if (!isWallJumping) // nếu không đang wall jump thì mới di chuyển nhân vật, nếu đang wall jump thì giữ nguyên vận tốc wall jump
        {
            MovePlayer();
        }

        if (rb.linearVelocity.y < 0) // nếu đang rơi thì tăng tốc độ rơi
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }

        HandleWallSlidePhysics();
        HandleJump();
    }

    /// <summary>
    /// Đọc tín hiệu điều khiển từ bàn phím (A/D, Mũi tên, Phím Jump).
    /// </summary>
    void GatherInput() // đọc input từ bàn phím
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        if (Input.GetButtonDown("Jump")) jumpInput = true;
    }

    /// <summary>
    /// Tạm thời khóa phím di chuyển của người chơi để thực hiện hiệu ứng choáng / đẩy lùi (Knockback Stun).
    /// </summary>
    /// <param name="duration">Thời gian bị khóa điều khiển (tính bằng giây).</param>
    public void ApplyKnockbackStun(float duration)
    {
        isWallJumping = true;
        wallJumpCounter = duration;
    }

    /// <summary>
    /// Áp dụng vận tốc di chuyển ngang dựa vào phím nhấn và lật Sprite nhân vật theo hướng đi.
    /// </summary>
    void MovePlayer() // di chuyển nhân vật dựa trên input
    {
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
        if (horizontalInput > 0) transform.localScale = new Vector3(1f, 1f, 1f); // lật nhân vật sang phải
        else if (horizontalInput < 0) transform.localScale = new Vector3(-1f, 1f, 1f); // lật nhân vật sang trái
    }

    /// <summary>
    /// Xử lý logic Nhảy thường khi dưới đất và Nhảy bẩy tường (Wall Jump) khi đang bám tường.
    /// </summary>
    void HandleJump() // xử lý nhảy và wall jump
    {
        if (jumpInput)
        {
            if (IsGrounded())
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
            else if (isWallSliding || (IsTouchingWall() && !IsGrounded())) // wall jump
            {
                isWallJumping = true;
                wallJumpDirection = -transform.localScale.x; // xác định hướng nhảy, ngược với hướng đang chạm tường
                rb.linearVelocity = new Vector2(wallJumpDirection * wallJumpForce.x, wallJumpForce.y);
                wallJumpCounter = wallJumpDuration;
                // wallJumpDuration: luôn giữ nguyên, đây là thời gian quy định cho một lần Wall Jump.
                // wallJumpCounter: mỗi khi bắt đầu Wall Jump sẽ được gán bằng wallJumpDuration, rồi giảm dần về 0.
            }
        }

        jumpInput = false; // reset jump input sau khi xử lý xong tránh nhảy liên tục

        if (isWallJumping)
        {
            wallJumpCounter -= Time.fixedDeltaTime; // Time.fixedDeltaTime = 0.02 giây
            if (wallJumpCounter <= 0) isWallJumping = false;
        }
    }

    /// <summary>
    /// Kiểm tra điều kiện để kích hoạt hoặc hủy trạng thái bám tường (Wall Slide) và khựng tường (Wall Freeze).
    /// </summary>
    void HandleWallSlideState() // xác định trạng thái Wall Slide và Wall Freeze
    {
        if (isWallJumping) // nếu đang wall jump thì không xử lí gì
        {
            isWallSliding = false;
            isWallFreezing = false;
            return;
        }

        bool currentlyTouchingWall = IsTouchingWall() && !IsGrounded(); // kiêm tra xem nhân vật có đang chạm tường và không chạm đất hay không

        if (currentlyTouchingWall)
        {
            float wallDirection = transform.localScale.x; // hướng của tường là hướng mà nhân vật đang đối diện, dựa vào hướng nhân vật đang nhìn

            // nếu đang nhảy và hướng về phía tường và không di chuyển ngang thì không wall slide
            if (rb.linearVelocity.y > 0.1f && (Mathf.Sign(rb.linearVelocity.x) == wallDirection || Mathf.Abs(rb.linearVelocity.x) < 0.1f))
            {
                isWallSliding = false;
                isWallFreezing = false;
            }
            else // nếu đang rơi hoặc di chuyển ngang thì wall slide
            {
                isWallSliding = true;
                if (!isWallFreezing && rb.linearVelocity.y <= 0.1f)
                {
                    StartCoroutine(WallCatchRoutine());
                }
            }
        }
        else // nhân vật đã rời khỏi tường
        {
            isWallSliding = false;
            isWallFreezing = false;
            StopCoroutine(WallCatchRoutine()); // tạo khoảng dừng ngắn trước khi bắt đầu trượt xuống
        }
    }

    /// <summary>
    /// Coroutine tạo khoảng khựng ngắn (Wall Freeze) ngay khi nhân vật vừa bám vào tường trước khi trượt xuống.
    /// </summary>
    IEnumerator WallCatchRoutine() // tạo khoảng dừng ngắn trước khi bắt đầu trượt xuống
    {
        isWallFreezing = true;
        isWallSliding = true;
        yield return new WaitForSeconds(wallCatchDelay);
        isWallFreezing = false;
    }

    /// <summary>
    /// Hãm tốc độ rơi theo chiều đứng khi nhân vật đang ở trạng thái trượt tường (Wall Slide).
    /// </summary>
    void HandleWallSlidePhysics() // tạo hiệu ứng trượt xuống tường, giảm tốc độ rơi khi đang wall slide
    {
        if (isWallSliding)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
        }
    }

    /// <summary>
    /// Kiểm tra xem nhân vật có đang đứng trên mặt đất hay không.
    /// </summary>
    /// <returns>True nếu điểm groundCheck chạm vào Layer mặt đất.</returns>
    public bool IsGrounded() => Physics2D.OverlapCircle(groundCheckPosition.position, groundCheckRadius, groundCheckLayer);

    /// <summary>
    /// Kiểm tra xem nhân vật có đang áp sát vào tường hay không.
    /// </summary>
    /// <returns>True nếu điểm wallCheck chạm vào Layer tường.</returns>
    public bool IsTouchingWall() => Physics2D.OverlapCircle(wallCheckPosition.position, wallCheckRadius, wallCheckLayer);

    /// <summary>
    /// Vẽ các hình tròn Gizmos trợ giúp quan sát phạm vi kiểm tra Đất (đỏ) và Tường (vàng) trong cửa sổ Scene.
    /// </summary>
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