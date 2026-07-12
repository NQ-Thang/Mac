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

    void GatherInput() // đọc input từ bàn phím
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        if (Input.GetButtonDown("Jump")) jumpInput = true;
    }

    public void ApplyKnockbackStun(float duration)
    {
        isWallJumping = true;
        wallJumpCounter = duration;
    }
    void MovePlayer() // di chuyển nhân vật dựa trên input
    {
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
        if (horizontalInput > 0) transform.localScale = new Vector3(1f, 1f, 1f); // lật nhân vật sang phải
        else if (horizontalInput < 0) transform.localScale = new Vector3(-1f, 1f, 1f); // lật nhân vật sang trái
    }

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

    IEnumerator WallCatchRoutine() // tạo khoảng dừng ngắn trước khi bắt đầu trượt xuống
    {
        isWallFreezing = true;
        isWallSliding = true;
        yield return new WaitForSeconds(wallCatchDelay);
        isWallFreezing = false;
    }

    void HandleWallSlidePhysics() // tạo hiệu ứng trượt xuống tường, giảm tốc độ rơi khi đang wall slide
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