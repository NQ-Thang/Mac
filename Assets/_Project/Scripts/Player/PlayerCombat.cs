using UnityEngine;

/// <summary>
/// Quản lý cơ chế chém thường cận chiến (Melee Attack) của Người chơi.
/// Tự động xoay vị trí chém theo hướng con trỏ chuột và gây sát thương diện rộng.
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    /// <summary>
    /// Vị trí tâm điểm để kiểm tra phạm vi chém trúng kẻ địch.
    /// </summary>
    [SerializeField] private Transform attackPoint;

    /// <summary>
    /// Bán kính diện tích đòn chém (độ rộng vùng chém).
    /// </summary>
    [SerializeField] private float attackRange = 0.5f;

    /// <summary>
    /// Layer đại diện cho kẻ địch để lọc đòn đánh chỉ trúng quái.
    /// </summary>
    [SerializeField] private LayerMask enemyLayer;

    /// <summary>
    /// Khoảng cách đẩy tâm chém (attackPoint) ra xa khỏi nhân vật theo hướng chuột.
    /// </summary>
    [SerializeField] private float attackOffsetDistance = 0.5f;

    /// <summary>
    /// Lượng sát thương gây ra cho kẻ địch trên mỗi cú chém.
    /// </summary>
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

    /// <summary>
    /// Thực hiện quét vùng chém (Physics2D.OverlapCircleAll) và gây sát thương cho tất cả kẻ địch trong tầm đánh.
    /// </summary>
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

    /// <summary>
    /// Tính toán vị trí con trỏ chuột trong không gian 2D và xoay/dịch chuyển điểm đánh (attackPoint) theo hướng đó.
    /// </summary>
    void RotateAttackPointTowardsMouse()
    {
        if (attackPoint == null) return; // Nếu attackPoint chưa được gán, không làm gì cả
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition); // lấy vị trí chuột ở màn hình
        mouseWorldPosition.z = 0f; // bỏ trục z
        Vector3 direction = (mouseWorldPosition - transform.position).normalized; // tính hướng chuột
        attackPoint.position = transform.position + direction * attackOffsetDistance; // đặt vị trí attackPoint cách nhân vật theo hướng chuột
    }

    /// <summary>
    /// Vẽ vòng tròn màu xanh nước biển biểu thị tầm chém trong cửa sổ Scene nhìn cho dễ chỉnh sửa.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}