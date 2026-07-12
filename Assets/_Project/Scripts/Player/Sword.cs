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

        rb.linearVelocity = Vector2.zero; // đặt vận tốc về 0 để kiếm dừng lại
        rb.bodyType = RigidbodyType2D.Kinematic; // không còn chịu tác động vật lý nữa
        Debug.Log("Kiếm đạt tầm tối đa, khựng lại chờ " + freezeDuration + " giây...");

        yield return new WaitForSeconds(freezeDuration); // Hàm này sẽ dừng lại trong khoảng thời gian freezeDuration nhưng game vẫn hoạt động bình thường

        if(!isStuck)
        {
            isReturning = true;
        }
    }

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
        if(distanceToPlayer < 1.5f)
        {
            CanDashTo = false; // không cho dash tới kiếm khi nó đang gần người chơi
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
        transform.rotation = Quaternion.identity; // đặt lại góc quay của kiếm để tránh bị xoay khi bay

        playerTransform = player; // lưu lại transform của người chơi để kiếm có thể quay về
        CanDashTo = true;
        isStuck = false;
    }

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

    public void SetMode(SwordMode mode) => currentMode = mode;

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