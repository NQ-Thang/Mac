using UnityEngine;

/// <summary>
/// Lớp cha gốc lớn nhất của mọi thực thể (Player, Enemy).
/// Quản lý dữ liệu vật lý nền tảng, hướng nhìn, va chạm mặt đất và tường.
/// </summary>
public class Entity : MonoBehaviour
{
    [Header("Core Components")]
    public Rigidbody2D rb { get; private set; }
    public Animator anim { get; private set; }
    public SpriteRenderer spriteRenderer { get; private set; }
    public Collider2D coreCollider { get; private set; }
    public Health health { get; private set; }

    [Header("Collision Check Settings")]
    [SerializeField] protected Transform groundCheckPosition;
    [SerializeField] protected Vector2 groundCheckSize = new Vector2(0.85f, 0.25f);
    [SerializeField] protected float groundCheckRadius = 0.2f;
    [SerializeField] protected LayerMask groundCheckLayer;

    [SerializeField] protected Transform wallCheckPosition;
    [SerializeField] protected Vector2 wallCheckSize = new Vector2(0.35f, 1.4f);
    [SerializeField] protected float wallCheckRadius = 0.2f;
    [SerializeField] protected LayerMask wallCheckLayer;
    [SerializeField] protected bool useBoxCheck = true;

    [Header("Direction Details")]
    public int facingDirection { get; protected set; } = 1; // 1: Phải, -1: Trái
    public bool facingRight { get; protected set; } = true;

    // Các biến hỗ trợ Frame Caching (Tránh tính toán vật lý trùng lặp)
    private bool isGroundedCached;
    private int lastGroundedFrame = -1;

    private bool isTouchingWallCached;
    private int lastWallFrame = -1;

    protected SpriteRenderer[] allSpriteRenderers;

    public LayerMask GetGroundLayer() => groundCheckLayer;
    public LayerMask GetWallLayer() => wallCheckLayer;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        coreCollider = GetComponent<Collider2D>();

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        allSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        anim = GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// Bật hoặc tắt hiển thị toàn bộ SpriteRenderer (bao gồm cả thân và đuôi).
    /// </summary>
    public virtual void SetRenderersVisible(bool isVisible)
    {
        if (allSpriteRenderers != null)
        {
            for (int i = 0; i < allSpriteRenderers.Length; i++)
            {
                if (allSpriteRenderers[i] != null)
                {
                    allSpriteRenderers[i].enabled = isVisible;
                }
            }
        }
    }

    /// <summary>
    /// Kiểm tra xem thực thể có chạm đất không.
    /// Sử dụng OverlapBox bao phủ toàn bộ chiều rộng chân để tránh bị hụt ground khi đứng sát mép.
    /// </summary>
    public bool IsGrounded()
    {
        if (groundCheckPosition == null) return false;

        if (Time.frameCount != lastGroundedFrame)
        {
            if (useBoxCheck && groundCheckSize.x > 0f && groundCheckSize.y > 0f)
            {
                isGroundedCached = Physics2D.OverlapBox(groundCheckPosition.position, groundCheckSize, 0f, groundCheckLayer);
            }
            else
            {
                isGroundedCached = Physics2D.OverlapCircle(groundCheckPosition.position, groundCheckRadius, groundCheckLayer);
            }
            lastGroundedFrame = Time.frameCount;
        }
        return isGroundedCached;
    }

    /// <summary>
    /// Kiểm tra xem thực thể có chạm tường không.
    /// Sử dụng OverlapBox theo chiều dọc thân để nhận diện tường ổn định ở mọi độ cao tiếp xúc.
    /// </summary>
    public bool IsTouchingWall()
    {
        if (wallCheckPosition == null) return false;

        if (Time.frameCount != lastWallFrame)
        {
            if (useBoxCheck && wallCheckSize.x > 0f && wallCheckSize.y > 0f)
            {
                isTouchingWallCached = Physics2D.OverlapBox(wallCheckPosition.position, wallCheckSize, 0f, wallCheckLayer);
            }
            else
            {
                isTouchingWallCached = Physics2D.OverlapCircle(wallCheckPosition.position, wallCheckRadius, wallCheckLayer);
            }
            lastWallFrame = Time.frameCount;
        }
        return isTouchingWallCached;
    }

    /// <summary>
    /// Hàm tự động lật mặt thực thể dựa trên hướng vận tốc X.
    /// </summary>
    public virtual void ControlFlip(float velocityX)
    {
        if (velocityX > 0 && !facingRight) Flip();
        else if (velocityX < 0 && facingRight) Flip();
    }

    public virtual void Flip()
    {
        facingRight = !facingRight;
        facingDirection *= -1;

        // Xoay 180 độ quanh trục Y.
        // LƯU Ý: Vì các điểm check (groundCheckPosition, wallCheckPosition) là con của Entity, 
        // việc xoay này sẽ tự động đưa điểm check tường sang hướng đối diện một cách hoàn hảo!
        transform.Rotate(0, 180, 0);
    }

    protected virtual void OnDrawGizmos()
    {
        if (groundCheckPosition != null)
        {
            Gizmos.color = Color.red;
            if (useBoxCheck && groundCheckSize.x > 0f && groundCheckSize.y > 0f)
            {
                Gizmos.DrawWireCube(groundCheckPosition.position, groundCheckSize);
            }
            else
            {
                Gizmos.DrawWireSphere(groundCheckPosition.position, groundCheckRadius);
            }
        }
        if (wallCheckPosition != null)
        {
            Gizmos.color = Color.yellow;
            if (useBoxCheck && wallCheckSize.x > 0f && wallCheckSize.y > 0f)
            {
                Gizmos.DrawWireCube(wallCheckPosition.position, wallCheckSize);
            }
            else
            {
                Gizmos.DrawWireSphere(wallCheckPosition.position, wallCheckRadius);
            }
        }
    }
}