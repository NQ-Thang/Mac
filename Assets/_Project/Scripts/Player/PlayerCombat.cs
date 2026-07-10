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
        if (movement.isDashing) return; // nếu đang dash thì không thể chém

        // Chỉ cho chém thường nếu không có PlayerSwordTech hoặc kiếm không active, không đang bay
        if (Input.GetMouseButtonDown(0) && (swordTech == null || !swordTech.HasActiveSword()))
        {
            attackInput = true;
        }

        RotateAttackPointTowardsMouse();
    }

    void FixedUpdate()
    {
        if (movement.isDashing) return;
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
            attackInput = false; // reset đòn đánh để không chém liên tục
        }
    }

    void RotateAttackPointTowardsMouse()
    {
        if (attackPoint == null) return; // Nếu attackPoint chưa được gán, không làm gì cả
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition); // lấy vị trí chuột ở màn hình
        mouseWorldPosition.z = 0f; // bỏ trục z
        Vector3 direction = (mouseWorldPosition - transform.position).normalized; // tính hướng chuột
        attackPoint.position = transform.position + direction * attackOffsetDistance; // đặt vị trí attackPoint cách nhân vật theo hướng chuột
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