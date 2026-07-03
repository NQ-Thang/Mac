using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float attackOffsetDistance = 0.5f;
    [SerializeField] private float attackDamage = 25f;

    private PlayerMovement movement;
    private PlayerSwordTech swordTech; // Cần check xem kiếm có đang ở ngoài không
    private bool attackInput;

    void Start()
    {
        movement = GetComponent<PlayerMovement>();
        swordTech = GetComponent<PlayerSwordTech>();
    }

    void Update()
    {
        if (movement.IsDashing) return;

        // Chỉ cho chém thường nếu chuột trái xuống VÀ kiếm KHÔNG ở ngoài vách/quái
        if (Input.GetMouseButtonDown(0) && (swordTech == null || !swordTech.HasActiveSword()))
        {
            attackInput = true;
        }

        RotateAttackPointTowardsMouse();
    }

    void FixedUpdate()
    {
        if (movement.IsDashing) return;
        HandleAttack();
    }

    void HandleAttack()
    {
        if (attackInput)
        {
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);
            foreach (Collider2D enemy in hitEnemies)
            {
                Health enemyHealth = enemy.GetComponent<Health>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(attackDamage, transform.position);
                }
            }
            attackInput = false;
        }
    }

    void RotateAttackPointTowardsMouse()
    {
        if (attackPoint == null) return;
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
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