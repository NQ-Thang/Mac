using UnityEngine;
using System.Collections;

/// <summary>
/// Quái vật "Vô Tri": Kế thừa trực tiếp từ EnemyBase (Tầng 3).
/// Đi tuần tra bình thường, chỉ khi bị chém mới giật mình nhìn Player rồi bỏ chạy.
/// </summary>
public class VoTri : EnemyBase
{
    [Header("Walker Obstacle Checks")]
    [SerializeField] private Transform groundCheckAhead;
    [SerializeField] private Transform wallCheckAhead;
    [SerializeField] private float checkDistance = 0.5f;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Surprise & Question Mark")]
    [SerializeField] private GameObject questionMark;
    [SerializeField] private float surpriseTime = 0.4f;
    [SerializeField] private float questionTime = 1f;
    [SerializeField] private float runAwayDuration = 3f; // Thời gian bỏ chạy sau khi bị chém (hết thời gian sẽ tuần tra lại)

    private bool isRunningAway;
    private bool isKnockedBack; // Cờ chặn xung đột lực khi đang chịu Knockback

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        if (questionMark != null)
            questionMark.SetActive(false);
    }

    /// <summary>
    /// Ghi đè logic đánh giá trạng thái. Không tự chạy khi Player lại gần.
    /// </summary>
    protected override void EvaluateState()
    {
        if (player == null || currentState == EnemyState.Die || currentState == EnemyState.Surprised || isKnockedBack) return;

        if (isRunningAway)
        {
            currentState = EnemyState.Chase;
        }
        else
        {
            currentState = EnemyState.Patrol;
        }
    }

    protected override void PatrolUpdate()
    {
        if (isKnockedBack) return;

        HandleObstacles();
        rb.linearVelocity = new Vector2(facingDirection * walkSpeed, rb.linearVelocity.y);
    }

    protected override void ChaseUpdate()
    {
        if (player == null || isKnockedBack) return;

        // Xác định hướng chạy ngược với Player
        float runDir = transform.position.x > player.position.x ? 1f : -1f;

        HandleObstacles();

        rb.linearVelocity = new Vector2(runDir * chaseSpeed, rb.linearVelocity.y);
        ControlFlip(runDir);
    }

    protected virtual void HandleObstacles()
    {
        if (groundCheckAhead == null || wallCheckAhead == null) return;

        bool isGroundedAhead = Physics2D.Raycast(groundCheckAhead.position, Vector2.down, checkDistance, obstacleLayer);

        Vector2 forwardVector = facingRight ? Vector2.right : Vector2.left;
        bool isWallAhead = Physics2D.Raycast(wallCheckAhead.position, forwardVector, checkDistance, obstacleLayer);

        if (!isGroundedAhead || isWallAhead)
        {
            ChangeDirection();
        }
    }

    protected void ChangeDirection()
    {
        float targetDir = facingRight ? -1f : 1f;
        ControlFlip(targetDir);
    }

    /// <summary>
    /// Hàm nhận sự kiện khi bị chém. Giải quyết lỗi Knockback lớn và lỗi cắm mặt vào tường.
    /// </summary>
    public void OnHit()
    {
        if (currentState == EnemyState.Die) return;

        StopAllCoroutines();
        StartCoroutine(HitAndRunRoutine());
    }

    private IEnumerator HitAndRunRoutine()
    {
        // 1. Cho phép lực Knockback từ Player đẩy quái đi tự nhiên trong 0.15 giây đầu, không can thiệp vận tốc
        isKnockedBack = true;
        yield return new WaitForSeconds(0.15f);
        isKnockedBack = false;

        // 2. Bắt đầu trạng thái Giật mình khựng lại
        currentState = EnemyState.Surprised;
        rb.linearVelocity = Vector2.zero; // Triệt tiêu lực sau khi đã lùi xong
        isRunningAway = false;

        // Quay lại nhìn Player
        float dirToPlayer = player.position.x > transform.position.x ? 1f : -1f;
        ControlFlip(dirToPlayer);

        yield return new WaitForSeconds(surpriseTime);

        if (questionMark != null) questionMark.SetActive(true);
        yield return new WaitForSeconds(questionTime);
        if (questionMark != null) questionMark.SetActive(false);

        // 3. Quay đầu bỏ chạy
        ControlFlip(-dirToPlayer);
        isRunningAway = true;
        currentState = EnemyState.Chase;

        // Chạy trốn trong một khoảng thời gian quy định
        yield return new WaitForSeconds(runAwayDuration);

        // 4. HẾT THỜI GIAN CHẠY TRỐN -> QUAY TRỞ LẠI ĐI TUẦN TRA (PATROL)
        isRunningAway = false;
        currentState = EnemyState.Patrol;

        // 🔥 SỬA LỖI CẮM MẶT VÀO TƯỜNG: Ép quét địa hình ngay lập tức tại khung hình này. 
        // Nếu vừa hết thời gian chạy mà trước mặt là tường/vực, nó sẽ tự quay đầu ngay lập tức chứ không đâm vào.
        HandleObstacles();
    }
}