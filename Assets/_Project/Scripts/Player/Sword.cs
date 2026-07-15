using UnityEngine;
using System.Collections;

/// <summary>
/// Quản lý hành vi và trạng thái của Thanh Kiếm khi ném ra: 
/// Bay xa, Găm vào kẻ địch/tường, Xuyên qua tất cả (PierceAll), Khựng lại ở tầm tối đa và Quay trở về tay Người chơi.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Sword : MonoBehaviour
{
    [Header("Sword Mode Settings")]
    [SerializeField] private float flyingSpeed = 15f;
    [SerializeField] private float returnSpeed = 18f;
    [SerializeField] private float maxFlyDistance = 8f;
    [SerializeField] private float freezeDuration = 0.5f;

    [Header("Collision Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask enemyLayer; // Tối ưu hóa: Dùng Layer thay vì check Tag bằng chuỗi
    [SerializeField] private float swordDamage = 30f;

    private Rigidbody2D rb;
    private Vector3 startPosition;
    private Transform playerTransform;

    private bool isStopped = false;
    private bool isReturning = false;
    private bool isStuck = false;

    // Cache WaitForSeconds để tránh tạo rác bộ nhớ (GC Alloc)
    private WaitForSeconds freezeWait;
    private readonly WaitForSeconds delayStuckWait = new WaitForSeconds(0.01f);

    public bool CanDashTo { get; private set; } = true;
    public bool CanBePickedUp { get; private set; } = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        freezeWait = new WaitForSeconds(freezeDuration);
    }

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        if (isStuck) return;

        if (isReturning)
        {
            HandleReturnToPlayer();
            return;
        }

        if (isStopped) return;

        // Tối ưu hóa: Chỉ cập nhật khoảng cách nhặt nếu chưa được phép nhặt
        if (!CanBePickedUp && playerTransform != null)
        {
            if (Vector3.SqrMagnitude(playerTransform.position - transform.position) > 2.25f) // Dùng SqrMagnitude nhanh hơn Distance (tránh căn bậc hai)
            {
                CanBePickedUp = true;
            }
        }

        // Tối ưu so sánh khoảng cách bằng SqrMagnitude
        float currentSqrDistance = (transform.position - startPosition).sqrMagnitude;
        if (currentSqrDistance >= maxFlyDistance * maxFlyDistance)
        {
            StartCoroutine(StopAndReturnRoutine());
        }
    }

    /// <summary>
    /// Coroutine dừng vận tốc vật lý của kiếm khi đạt tầm bay tối đa, cho kiếm khựng lại chờ trong chốc lát rồi kích hoạt bay về.
    /// </summary>
    IEnumerator StopAndReturnRoutine()
    {
        isStopped = true;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        yield return freezeWait; // Sử dụng biến cache tránh tạo rác

        if (!isStuck)
        {
            isReturning = true;
        }
    }

    /// <summary>
    /// Điều khiển thanh kiếm bay liên tục hướng về vị trí người chơi và tự hủy khi lại rất gần.
    /// </summary>
    void HandleReturnToPlayer()
    {
        if (playerTransform == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 playerPos = playerTransform.position;
        Vector2 currentPos = transform.position;

        Vector2 returnDirection = (playerPos - currentPos).normalized;
        rb.linearVelocity = returnDirection * returnSpeed;

        float sqrDistanceToPlayer = (currentPos - playerPos).sqrMagnitude;

        if (sqrDistanceToPlayer < 2.25f) // 1.5f * 1.5f = 2.25f
        {
            CanDashTo = false;
        }
        if (sqrDistanceToPlayer < 0.25f) // 0.5f * 0.5f = 0.25f
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Phóng thanh kiếm bay theo một hướng xác định với vận tốc bay thiết lập ban đầu.
    /// </summary>
    public void Launch(Vector2 launchDirection, Transform player)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic; // Reset lại bodyType phòng trường hợp tái sử dụng từ Pool
        rb.linearVelocity = launchDirection * flyingSpeed;
        transform.rotation = Quaternion.identity;

        playerTransform = player;
        CanDashTo = true;
        isStuck = false;
        isStopped = false;
        isReturning = false;
        CanBePickedUp = false;
    }

    /// <summary>
    /// Xử lý va chạm 2D khi thanh kiếm đâm trúng Kẻ địch hoặc Mặt đất/Tường.
    /// </summary>
    public void OnTriggerEnter2D(Collider2D other)
    {
        if (isStuck) return;

        // Kiểm tra va chạm với Enemy bằng LayerMask (Nhanh và tối ưu hơn)
        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
        {
            Health enemyHealth = other.GetComponentInParent<Health>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(swordDamage, transform.position);
            }
        }

        // Kiểm tra va chạm với Ground
        if (((1 << other.gameObject.layer) & groundLayer) != 0)
        {
            if (isReturning) return;
            if (!isStuck) StartCoroutine(DelayStuckRoutine());
        }
    }

    void StuckInWall()
    {
        CanBePickedUp = true;
        isStuck = true;
        isReturning = false;
        StopAllCoroutines();

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    IEnumerator DelayStuckRoutine()
    {
        yield return delayStuckWait; // Sử dụng biến cache tránh tạo rác
        StuckInWall();
    }

    /// <summary>
    /// Kích hoạt trạng thái thu hồi.
    /// </summary>
    public void StartReturn()
    {
        isStuck = false;
        isStopped = true;
        StopAllCoroutines();

        rb.bodyType = RigidbodyType2D.Kinematic;
        isReturning = true;
    }

    /// <summary>
    /// Khóa cứng thanh kiếm đứng yên tại vị trí hiện tại để người chơi Lướt (Dash) tới.
    /// </summary>
    public void FreezeSword()
    {
        isReturning = false;
        isStopped = true;
        StopAllCoroutines();

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }
}