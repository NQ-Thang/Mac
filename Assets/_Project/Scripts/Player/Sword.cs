using UnityEngine;
using System.Collections; 

public class Sword : MonoBehaviour
{
    public enum SwordMode { PierceAll, StickToEnemies }
    [Header("Sword Mode Settings")]
    [SerializeField] private SwordMode currentMode = SwordMode.StickToEnemies;

    [SerializeField] private float flyingSpeed = 15f;
    [SerializeField] private float returnSpeed = 18f;
    [SerializeField] private float maxFlyDistance = 8f;
    [SerializeField] private float freezeDuration = 0.5f;

    [Header("Collision Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float swordDamage = 30f;
    [Range(0f, 1f)]
    [SerializeField] private float enemyPenetrationDepth = 0.4f;

    private Rigidbody2D rb;
    private Vector3 startPosition;
    private Transform playerTransform;

    private bool isStopped = false;
    private bool isReturning = false;
    private bool isStuck = false;

    public bool CanDashTo { get; private set; } = true;
    public bool CanBePickedUp { get; private set; } = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
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

        // Nếu kiếm đang bay ra và khoảng cách với người chơi đã > 1.5m, cho phép nhặt
        if (playerTransform != null && Vector3.Distance(playerTransform.position, transform.position) > 1.5f)
        {
            CanBePickedUp = true;
        }

        float currentDistance = Vector3.Distance(startPosition, transform.position);

        if (currentDistance >= maxFlyDistance)
        {
            StartCoroutine(StopAndReturnRoutine());
        }
    }

    IEnumerator StopAndReturnRoutine()
    {
        isStopped = true;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        Debug.Log("Kiếm đạt tầm tối đa, khựng lại chờ " + freezeDuration + " giây...");

        yield return new WaitForSeconds(freezeDuration);

        isReturning = true;
        Debug.Log("Hết thời gian chờ, kiếm đang bay về!");

        if(!isStuck)
        {
            isReturning = true;
        }
    }

    void HandleReturnToPlayer()
    {
        if (playerTransform == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 returnDirection = (playerTransform.position - transform.position).normalized;
        rb.linearVelocity = returnDirection * returnSpeed;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if(distanceToPlayer < 1.5f)
        {
            CanDashTo = false;
        }
        if (distanceToPlayer < 0.5f)
        {
            Debug.Log("Kiếm đã quay về với chủ nhân!");
            Destroy(gameObject);
        }
    }

    public void Launch(Vector2 launchDirection, Transform player)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = launchDirection * flyingSpeed;
        transform.rotation = Quaternion.identity;

        playerTransform = player;
        CanDashTo = true;
        isStuck = false;
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (isStuck) return;

        // 1. Xử lý gây sát thương khi chạm Quái
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy") || other.CompareTag("Enemy"))
        {
            Health enemyHealth = other.GetComponentInParent<Health>(); // Lấy Health từ cha nếu là đa hitbox
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(swordDamage, transform.position);
            }

            // XỬ LÝ GĂM KIẾM THEO CHẾ ĐỘ
            HandleSwordSticking(other);
        }

        // 2. Xử lý găm vào tường/đất
        if (((1 << other.gameObject.layer) & groundLayer) != 0)
        {
            if (isReturning) return;
            if (!isStuck) StartCoroutine(DelayStuckRoutine());
        }
    }

    private void HandleSwordSticking(Collider2D other)
    {
        // TRƯỜNG HỢP 1: Chạm trúng con AnchorPointEnemy (Luôn luôn găm, giữ nguyên logic cũ)
        AnchorPointEnemy anchor = other.GetComponent<AnchorPointEnemy>();
        if (anchor != null) return; // Để con Anchor tự xử lý logic găm của nó như cũ

        // TRƯỜNG HỢP 2: Chế độ Xuyên thấu (PierceAll) -> Bỏ qua không găm vào quái thường
        if (currentMode == SwordMode.PierceAll) return;

        // TRƯỜNG HỢP 3: Chế độ Luôn găm (StickToEnemies)
        if (currentMode == SwordMode.StickToEnemies)
        {
            if (isReturning) return;

            // BỘ LỌC HITBOX CHO BOSS VÀ QUÁI TO:
            // Nếu collider có tag "SwordIgnore" (ví dụ gán cho Thân/Chân Boss) -> Cho kiếm xuyên qua
            if (other.CompareTag("SwordIgnore") || other.gameObject.name.Contains("Body") || other.gameObject.name.Contains("Legs"))
            {
                Debug.Log("Kiếm xuyên qua vùng không cho phép găm: " + other.gameObject.name);
                return;
            }

            // Nếu vượt qua bộ lọc (hoặc là quái nhỏ chỉ có 1 hitbox), tiến hành găm vào quái:
            StuckInEnemy(other);
        }
    }

    private void StuckInEnemy(Collider2D enemyCollider)
    {
        CanBePickedUp = true;
        isStuck = true;
        isReturning = false;
        StopAllCoroutines();

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Dịch chuyển kiếm hơi sâu vào hitbox một chút cho đẹp
        Vector3 hitPoint = transform.position;
        Vector3 enemyCenter = enemyCollider.transform.position;
        transform.position = Vector3.Lerp(hitPoint, enemyCenter, enemyPenetrationDepth);

        // Biến kiếm thành con của hitbox/quái để di chuyển theo quái
        transform.SetParent(enemyCollider.transform);
        Debug.Log("Kiếm đã găm vào quái: " + enemyCollider.name);
    }

    // --- HÀM PUBLIC ĐỂ PLAYER ĐỔI CHẾ ĐỘ TỪ XA ---
    public void SetMode(SwordMode mode) => currentMode = mode;


    void StuckInWall()
    {
        CanBePickedUp = true;
        isStuck = true;
        isReturning = false;
        StopAllCoroutines();
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    IEnumerator DelayStuckRoutine()
    {
        yield return new WaitForSeconds(0.01f);
        StuckInWall();
    }
    public void CallRecall()
    {
        isStuck = false;
        isStopped = true;
        StopAllCoroutines();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        isReturning = true;
    }

    public void FreezeSword()
    {
        Rigidbody2D swordRb = GetComponent<Rigidbody2D>();
        if (swordRb != null)
        {
            swordRb.linearVelocity = Vector2.zero; // Triệt tiêu vận tốc bay
            swordRb.bodyType = RigidbodyType2D.Kinematic; // Khóa cứng vật lý để không bị rơi rụng hay đẩy lệch
        }
    }
}