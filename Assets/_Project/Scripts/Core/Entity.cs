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

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        coreCollider = GetComponent<Collider2D>();

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
    }

    protected virtual void Start() { }
    protected virtual void Update() { }
    protected virtual void FixedUpdate() { }

    public bool IsGrounded() => Physics2D.OverlapCircle(groundCheckPosition.position, groundCheckRadius, groundCheckLayer);
    public bool IsTouchingWall() => Physics2D.OverlapCircle(wallCheckPosition.position, wallCheckRadius, wallCheckLayer);

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
        transform.Rotate(0, 180, 0); // Xoay 180 độ trục Y chuẩn video, an toàn hơn âm scale
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