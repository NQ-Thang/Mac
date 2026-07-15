using UnityEngine;

/// <summary>
/// Mảnh ghép quản lý cơ chế chém thường cận chiến của Người chơi.
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
    private readonly Collider2D[] hitEnemies = new Collider2D[10];

    void Start()
    {
        player = GetComponent<Player>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        // Sử dụng trạng thái isDashing tập trung từ component Movement phụ thuộc
        if (player.Movement != null && player.Movement.isDashing) return;

        RotateAttackPointTowardsMouse();

        if (Input.GetMouseButtonDown(0) && (player.SwordTech == null || !player.SwordTech.HasActiveSword()))
        {
            PerformMeleeAttack();
        }
    }

    void PerformMeleeAttack()
    {
        if (attackPoint == null) return;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(enemyLayer);
        filter.useLayerMask = true;

        int numColliders = Physics2D.OverlapCircle(attackPoint.position, attackRange, filter, hitEnemies);
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

        if (hitAnything && player.Anima != null)
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