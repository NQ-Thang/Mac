using UnityEngine;
using System.Collections;

/// <summary>
/// Quản lý hành vi và trạng thái của Thanh Kiếm khi ném ra: 
/// Bay xa, Găm vào kẻ địch/tường, Xuyên qua tất cả (PierceAll), Khựng lại ở tầm tối đa và Quay trở về tay Người chơi.
/// </summary>
public class Sword : MonoBehaviour
{
    /// <summary>
    /// Định nghĩa các chế độ ném kiếm.
    /// </summary>
    public enum SwordMode
    {
        /// <summary>
        /// Chế độ xuyên qua tất cả kẻ địch mà không bị găm lại.
        /// </summary>
        PierceAll,

        /// <summary>
        /// Chế độ cắm/găm kiếm trực tiếp vào thân kẻ địch khi trúng đòn.
        /// </summary>
        StickToEnemies
    }

    [Header("Sword Mode Settings")]
    /// <summary>
    /// Chế độ hiện tại của thanh kiếm.
    /// </summary>
    [SerializeField] private SwordMode currentMode = SwordMode.StickToEnemies;

    /// <summary>
    /// Tốc độ bay của thanh kiếm khi vừa được ném ra.
    /// </summary>
    [SerializeField] private float flyingSpeed = 15f;

    /// <summary>
    /// Tốc độ bay khi thu hồi thanh kiếm trở về tay người chơi.
    /// </summary>
    [SerializeField] private float returnSpeed = 18f;

    /// <summary>
    /// Khoảng cách bay tối đa tính từ điểm ném trước khi kiếm tự động khựng lại và quay về.
    /// </summary>
    [SerializeField] private float maxFlyDistance = 8f;

    /// <summary>
    /// Khoảng thời gian kiếm khựng đứng yên trên không khi đạt tầm ném tối đa trước khi bắt đầu bay về.
    /// </summary>
    [SerializeField] private float freezeDuration = 0.5f;

    [Header("Collision Settings")]
    /// <summary>
    /// Layer đại diện cho mặt đất/tường chướng ngại vật.
    /// </summary>
    [SerializeField] private LayerMask groundLayer;

    /// <summary>
    /// Sát thương gây ra cho kẻ địch khi kiếm va chạm.
    /// </summary>
    [SerializeField] private float swordDamage = 30f;

    /// <summary>
    /// Tỉ lệ độ sâu găm vào thân quái (từ 0 đến 1) dùng cho phép toán dịch chuyển Lerp.
    /// </summary>
    [Range(0f, 1f)]
    [SerializeField] private float enemyPenetrationDepth = 0.4f;

    private Rigidbody2D rb;
    private Vector3 startPosition;
    private Transform playerTransform;

    private bool isStopped = false;
    private bool isReturning = false;
    private bool isStuck = false;

    /// <summary>
    /// Cho biết người chơi có thể Lướt (Dash) tới vị trí thanh kiếm này được hay không.
    /// </summary>
    public bool CanDashTo { get; private set; } = true;

    /// <summary>
    /// Cho biết người chơi có thể đi lại gần để tự động nhặt lại thanh kiếm này hay không.
    /// </summary>
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

    /// <summary>
    /// Coroutine dừng vận tốc vật lý của kiếm khi đạt tầm bay tối đa, cho kiếm khựng lại chờ trong chốc lát rồi kích hoạt bay về.
    /// </summary>
    IEnumerator StopAndReturnRoutine()
    {
        isStopped = true;

        rb.linearVelocity = Vector2.zero; // đặt vận tốc về 0 để kiếm dừng lại
        rb.bodyType = RigidbodyType2D.Kinematic; // không còn chịu tác động vật lý nữa
        Debug.Log("Kiếm đạt tầm tối đa, khựng lại chờ " + freezeDuration + " giây...");

        yield return new WaitForSeconds(freezeDuration); // Hàm này sẽ dừng lại trong khoảng thời gian freezeDuration nhưng game vẫn hoạt động bình thường

        if (!isStuck)
        {
            isReturning = true;
        }
    }

    /// <summary>
    /// Điều khiển thanh kiếm bay liên tục hướng về vị trí người chơi và tự hủy (thu hồi thành công) khi lại rất gần.
    /// </summary>
    void HandleReturnToPlayer()
    {
        if (playerTransform == null) // nếu người chơi chết thì xóa bỏ luôn kiếm
        {
            Destroy(gameObject);
            return;
        }

        Vector2 returnDirection = (playerTransform.position - transform.position).normalized;
        rb.linearVelocity = returnDirection * returnSpeed;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer < 1.5f)
        {
            CanDashTo = false; // không cho dash tới kiếm khi nó đang gần người chơi
        }
        if (distanceToPlayer < 0.5f)
        {
            Debug.Log("Kiếm đã quay về với chủ nhân!");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Phóng thanh kiếm bay theo một hướng xác định với vận tốc bay thiết lập ban đầu.
    /// </summary>
    /// <param name="launchDirection">Hướng ném kiếm dạng Vector2 đã chuẩn hóa.</param>
    /// <param name="player">Transform của Người chơi ném kiếm.</param>
    public void Launch(Vector2 launchDirection, Transform player)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = launchDirection * flyingSpeed;
        transform.rotation = Quaternion.identity; // đặt lại góc quay của kiếm để tránh bị xoay khi bay

        playerTransform = player; // lưu lại transform của người chơi để kiếm có thể quay về
        CanDashTo = true;
        isStuck = false;
    }

    /// <summary>
    /// Xử lý va chạm 2D khi thanh kiếm đâm trúng Kẻ địch hoặc Mặt đất/Tường.
    /// </summary>
    /// <param name="other">Collider2D của vật thể va chạm.</param>
    public void OnTriggerEnter2D(Collider2D other) // other là collider của vật thể mà kiếm va chạm vào
    {
        if (isStuck) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy") || other.CompareTag("Enemy")) // kiểm tra tag và layer xem có phải là enemy không
        {
            Health enemyHealth = other.GetComponentInParent<Health>(); // lấy script Health
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(swordDamage, transform.position);
            }

            HandleSwordSticking(other);
        }

        if (((1 << other.gameObject.layer) & groundLayer) != 0)
        {
            if (isReturning) return;
            if (!isStuck) StartCoroutine(DelayStuckRoutine());
        }
    }

    /// <summary>
    /// Kiểm tra chế độ ném và các điều kiện vùng cấm găm để quyết định xem kiếm có được phép cắm vào kẻ địch hay không.
    /// </summary>
    /// <param name="other">Collider2D của kẻ địch bị đâm trúng.</param>
    private void HandleSwordSticking(Collider2D other)
    {
        AnchorPointEnemy anchor = other.GetComponent<AnchorPointEnemy>();
        if (anchor != null) return; // nếu va chạm vào AnchorPointEnemy thì thoát hàm luôn

        if (currentMode == SwordMode.PierceAll) return; // nếu kiếm đang ở chế độ PierceAll thì không găm vào quái, thoát hàm luôn

        if (currentMode == SwordMode.StickToEnemies)
        {
            if (isReturning) return;

            // các collider có tag "SwordIgnore" hoặc tên chứa "Body" hoặc "Legs" sẽ không cho phép kiếm găm vào
            if (other.CompareTag("SwordIgnore") || other.gameObject.name.Contains("Body") || other.gameObject.name.Contains("Legs"))
            {
                Debug.Log("Kiếm xuyên qua vùng không cho phép găm: " + other.gameObject.name);
                return;
            }

            StuckInEnemy(other);
        }
    }

    /// <summary>
    /// Thực hiện gắn thanh kiếm thành con (Child) của kẻ địch và dịch chuyển tâm kiếm hơi sâu vào thân quái.
    /// </summary>
    /// <param name="enemyCollider">Collider2D của kẻ địch.</param>
    private void StuckInEnemy(Collider2D enemyCollider)
    {
        CanBePickedUp = true;
        isStuck = true;
        isReturning = false;
        StopAllCoroutines(); // dừng toàn bộ Coroutine đang chạy

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        Vector3 hitPoint = transform.position; // lưu vị trí va chạm hiện tại của kiếm
        Vector3 enemyCenter = enemyCollider.transform.position; // lấy vị trí trung tâm của quái
        transform.position = Vector3.Lerp(hitPoint, enemyCenter, enemyPenetrationDepth); //Vector3.Lerp(a, b, t) sẽ lấy một điểm nằm giữa a và b

        transform.SetParent(enemyCollider.transform); // gắn kiếm vào quái 
        Debug.Log("Kiếm đã găm vào quái: " + enemyCollider.name);
    }

    /// <summary>
    /// Cài đặt chế độ bay cho thanh kiếm (StickToEnemies hoặc PierceAll).
    /// </summary>
    /// <param name="mode">Chế độ ném muốn thiết lập.</param>
    public void SetMode(SwordMode mode) => currentMode = mode;

    /// <summary>
    /// Khóa thanh kiếm cố định tại vị trí cắm vào tường hoặc mặt đất, triệt tiêu gia tốc vật lý.
    /// </summary>
    void StuckInWall()
    {
        CanBePickedUp = true;
        isStuck = true;
        isReturning = false;
        StopAllCoroutines();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f; // Triệt tiêu gia tốc xoay
            rb.bodyType = RigidbodyType2D.Kinematic; // Khóa vật lý không cho rơi/chìm
        }
    }

    /// <summary>
    /// Coroutine tạo độ trễ ngắn (0.01s) trước khi khóa thanh kiếm vào tường để tránh lỗi vật lý va chạm tức thì.
    /// </summary>
    IEnumerator DelayStuckRoutine()
    {
        yield return new WaitForSeconds(0.01f);
        StuckInWall();
    }

    /// <summary>
    /// Kích hoạt trạng thái thu hồi: Rút kiếm ra khỏi vị trí găm và bắt đầu cho kiếm bay về phía người chơi.
    /// </summary>
    public void StartReturn()
    {
        isStuck = false;
        isStopped = true;
        StopAllCoroutines();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        isReturning = true;
    }

    /// <summary>
    /// Khóa cứng thanh kiếm đứng yên tại vị trí hiện tại phục vụ mục đích làm điểm mốc để người chơi Lướt (Dash) tới.
    /// </summary>
    public void FreezeSword()
    {
        isReturning = false; // TẮT TRẠNG THÁI THU HỒI để kiếm không tự Destroy giữa chừng
        isStopped = true;
        StopAllCoroutines();

        Rigidbody2D swordRb = GetComponent<Rigidbody2D>();
        if (swordRb != null)
        {
            swordRb.linearVelocity = Vector2.zero; // Triệt tiêu vận tốc bay
            swordRb.bodyType = RigidbodyType2D.Kinematic; // Khóa cứng vật lý để không bị rơi rụng hay đẩy lệch
        }
    }
}