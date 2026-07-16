using UnityEngine;

/// <summary>
/// Lớp nền móng cho tất cả quái vật trong game, kế thừa từ lớp gốc Entity.
/// Quản lý hệ thống AI States, phát hiện mục tiêu và ra lệnh hành vi.
/// </summary>
public class EnemyBase : Entity, IEnemy
{
    public enum EnemyState { Patrol, Chase, Attack, Surprised, Die }

    [Header("Base AI State")]
    public EnemyState currentState = EnemyState.Patrol;

    [Header("Base Movement")]
    public float walkSpeed = 2f;
    public float chaseSpeed = 3.5f;

    [Header("Base Detection")]
    public float detectionRange = 5f;
    public float attackRange = 1.5f;

    protected Transform player;

    /// <summary>
    /// Ghi đè Awake của Entity để lấy thêm dữ liệu đặc trưng của quái.
    /// </summary>
    protected override void Awake()
    {
        base.Awake(); // BẮT BUỘC: Để Entity lấy Rigidbody, Animator, Health...
    }

    protected virtual void Start()
    {
        if (rb != null) rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    /// <summary>
    /// Ghi đè Update để chạy luồng điều khiển AI state của quái thay vì đọc Input.
    /// </summary>
    protected virtual void Update()
    {
        if (currentState == EnemyState.Die || currentState == EnemyState.Surprised) return;

        EvaluateState();
        HandleStateBehavior();
    }

    /// <summary>
    /// Tính toán khoảng cách để chuyển đổi trạng thái AI.
    /// </summary>
    protected virtual void EvaluateState()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= attackRange) currentState = EnemyState.Attack;
        else if (distance <= detectionRange) currentState = EnemyState.Chase;
        else currentState = EnemyState.Patrol;
    }

    /// <summary>
    /// Điều hướng thực thi hành vi dựa trên State hiện tại.
    /// </summary>
    protected void HandleStateBehavior()
    {
        switch (currentState)
        {
            case EnemyState.Patrol: PatrolUpdate(); break;
            case EnemyState.Chase: ChaseUpdate(); break;
            case EnemyState.Attack: AttackUpdate(); break;
        }
    }

    // Các hàm ảo để các quái cụ thể (vô tri, hung dữ, quái bay) tự viết logic riêng
    protected virtual void PatrolUpdate() { }
    protected virtual void ChaseUpdate() { }
    protected virtual void AttackUpdate() { }

    /// <summary>
    /// Lệnh di chuyển hướng tới mục tiêu. 
    /// Hàm này sử dụng hàm ControlFlip có sẵn của lớp cha Entity.
    /// </summary>
    protected virtual void MoveTowards(Vector2 target, float speed)
    {
        Vector2 direction = (target - (Vector2)transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * speed, rb.linearVelocity.y);

        // Gọi hàm lật mặt dùng chung đã được tối ưu ở lớp cha Entity
        ControlFlip(direction.x);
    }

    /// <summary>
    /// Hàm phản hồi khi kẻ địch bị trúng đòn. 
    /// Viết dạng virtual để các lớp con (như VoTri) có thể override (ghi đè) và tùy biến cơ chế riêng.
    /// </summary>
    public virtual void OnHit()
    {
        Debug.Log(gameObject.name + " bị trúng đòn!");
        // Bạn có thể viết thêm logic chung cho mọi loại quái khi bị trúng đòn ở đây (ví dụ: tạo hạt máu, phát âm thanh...)
    }
}