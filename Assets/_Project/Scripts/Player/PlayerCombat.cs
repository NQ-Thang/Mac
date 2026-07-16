using UnityEngine;

/// <summary>
/// Mảnh ghép quản lý cơ chế chém thường cận chiến của Người chơi.
/// Tự động xoay vị trí chém theo hướng con trỏ chuột và gây sát thương diện rộng.
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float attackOffsetDistance = 0.5f;
    [SerializeField] private float attackDamage = 25f;

    private Player player;
    private Camera mainCamera;

    //Cache sẵn mảng va chạm và bộ lọc vật lý để triệt tiêu việc sinh rác bộ nhớ (GC)
    private readonly Collider2D[] hitEnemies = new Collider2D[10];
    private ContactFilter2D enemyContactFilter;

    void Start()
    {
        player = GetComponent<Player>();
        mainCamera = Camera.main;

        enemyContactFilter = new ContactFilter2D();
        enemyContactFilter.SetLayerMask(enemyLayer);
        enemyContactFilter.useLayerMask = true;
    }

    void Update()
    {
        if (player.Movement.isDashing) return;

        RotateAttackPointTowardsMouse();

        // Tối ưu hóa: Loại bỏ hoàn toàn so sánh null (vì Player Hub cam kết SwordTech luôn tồn tại)
        if (Input.GetMouseButtonDown(0) && !player.SwordTech.HasActiveSword())
        {
            PerformMeleeAttack();
        }
    }

    void PerformMeleeAttack()
    {
        if (attackPoint == null) return;

        //Sử dụng trực tiếp bộ lọc enemyContactFilter đã được cache sẵn từ Start
        int numColliders = Physics2D.OverlapCircle(attackPoint.position, attackRange, enemyContactFilter, hitEnemies);
        bool hitAnything = false;

        for (int i = 0; i < numColliders; i++)
        {
            Collider2D enemy = hitEnemies[i];
            if (enemy == null) continue;

            Health enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(attackDamage, transform.position);
                hitAnything = true;
            }
        }

        //  Gọi trực tiếp Anima từ Hub Player mà không cần check Null
        if (hitAnything)
        {
            player.Anima.AddAnimaFromHit();
        }
    }

    void RotateAttackPointTowardsMouse()
    {
        if (attackPoint == null || mainCamera == null) return;

        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f;

        Vector3 direction = (mouseWorldPosition - transform.position).normalized;
        attackPoint.position = transform.position + direction * attackOffsetDistance;
    }

    private void OnDrawGizmos()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}