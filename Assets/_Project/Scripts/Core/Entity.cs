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
    [SerializeField] protected float groundCheckRadius = 0.2f;
    [SerializeField] protected LayerMask groundCheckLayer;

    [SerializeField] protected Transform wallCheckPosition;
    [SerializeField] protected float wallCheckRadius = 0.2f;
    [SerializeField] protected LayerMask wallCheckLayer;

    [Header("Direction Details")]
    public int facingDirection { get; protected set; } = 1; // 1: Phải, -1: Trái
    public bool facingRight { get; protected set; } = true;

    // Các biến hỗ trợ Frame Caching (Tránh tính toán vật lý trùng lặp)
    private bool isGroundedCached;
    private int lastGroundedFrame = -1;

    private bool isTouchingWallCached;
    private int lastWallFrame = -1;


    public LayerMask GetGroundLayer() => groundCheckLayer;
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        coreCollider = GetComponent<Collider2D>();

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
    }

    // Đã xóa bỏ các hàm Start(), Update(), FixedUpdate() rỗng để tiết kiệm CPU cho Unity

    /// <summary>
    /// Kiểm tra xem thực thể có chạm đất không.
    /// Đã tối ưu hóa: Chỉ tính toán vật lý tối đa 1 lần duy nhất mỗi khung hình.
    /// </summary>
    public bool IsGrounded()
    {
        if (groundCheckPosition == null) return false;

        // Nếu lượt gọi này nằm ở một Frame mới, ta mới quét vật lý lại
        if (Time.frameCount != lastGroundedFrame)
        {
            isGroundedCached = Physics2D.OverlapCircle(groundCheckPosition.position, groundCheckRadius, groundCheckLayer);
            lastGroundedFrame = Time.frameCount; // Time.frameCount là số Frame hiện tại thay đổi theo frame
        }
        return isGroundedCached;
    }

    /// <summary>
    /// Kiểm tra xem thực thể có chạm tường không.
    /// Đã tối ưu hóa: Chỉ tính toán vật lý tối đa 1 lần duy nhất mỗi khung hình.
    /// </summary>
    public bool IsTouchingWall()
    {
        if (wallCheckPosition == null) return false;

        if (Time.frameCount != lastWallFrame)
        {
            isTouchingWallCached = Physics2D.OverlapCircle(wallCheckPosition.position, wallCheckRadius, wallCheckLayer);
            lastWallFrame = Time.frameCount;
        }
        return isTouchingWallCached;
    }

    /// <summary>
    /// Hàm tự động lật mặt thực thể dựa trên hướng vận tốc X.
    /// </summary>
    protected virtual void ControlFlip(float velocityX)
    {
        if (velocityX > 0 && !facingRight) Flip();
        else if (velocityX < 0 && facingRight) Flip();
    }

    protected virtual void Flip()
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
            Gizmos.DrawWireSphere(groundCheckPosition.position, groundCheckRadius);
        }
        if (wallCheckPosition != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(wallCheckPosition.position, wallCheckRadius);
        }
    }
}