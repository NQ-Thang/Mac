using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý toàn bộ cơ chế kỹ năng Kiếm Thuật của Người chơi:
/// Ném kiếm, Đổi chế độ bay, Thu hồi kiếm từ xa, Lướt (Dash) tới kiếm,
/// Gây sát thương diện rộng khi lướt và Nảy lên khi đâm trúng kẻ địch/Anchor Point.
/// </summary>
public class PlayerSwordTech : MonoBehaviour
{
    [Header("Ranged Attack (Sword Throw)")]
    /// <summary>
    /// Khoảng cách tối thiểu để người chơi tự động nhặt lại kiếm khi đi lại gần.
    /// </summary>
    [SerializeField] private float swordPickUpDistance = 1f;

    /// <summary>
    /// Prefab của thanh kiếm sẽ được khởi tạo khi ném.
    /// </summary>
    [SerializeField] private GameObject swordPrefab;

    private GameObject activeSword;
    private bool throwInput;

    [Header("Dash to Sword Settings")]
    /// <summary>
    /// Tốc độ lướt của người chơi từ vị trí hiện tại đến vị trí thanh kiếm.
    /// </summary>
    [SerializeField] private float dashSpeed = 25f;

    private Vector2 dashTargetPosition;
    private float originalGravity;

    [Header("Dash Attack Settings")]
    /// <summary>
    /// Lượng sát thương gây ra cho kẻ địch nằm trên đường lướt.
    /// </summary>
    [SerializeField] private int dashDamage = 2;

    /// <summary>
    /// Bán kính quét tìm kẻ địch xung quanh người chơi để gây sát thương trong lúc lướt.
    /// </summary>
    [SerializeField] private float dashDamageRadius = 0.6f;

    /// <summary>
    /// Layer đại diện cho kẻ địch.
    /// </summary>
    [SerializeField] private LayerMask enemyLayer;

    [Header("Anima Cost Settings")]
    [SerializeField] private int dashAnimaCost = 0;  // Số Anima tiêu tốn khi Dash
    [SerializeField] private int throwAnimaCost = 0;   

    private SpriteRenderer playerSprite;
    private Rigidbody2D rb;
    private PlayerMovement movement;
    private Health playerHealth;
    private PlayerAnima anima;
    private List<Collider2D> enemiesHitDuringDash = new List<Collider2D>();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();
        playerHealth = GetComponent<Health>();
        anima = GetComponent<PlayerAnima>();
        originalGravity = rb.gravityScale;
        playerSprite = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (movement.isDashing)
        {
            CheckDashArrival();
        }
        else
        {
            if (Input.GetMouseButtonDown(1)) throwInput = true;
            if (Input.GetKeyDown(KeyCode.R)) RecallSword();

            CheckAutoPickUpSword();
        }
    }

    void FixedUpdate()
    {
        if (movement.isDashing)
        {
            ExecuteDash();
            HandleDashDamage();
        }
        else
        {
            HandleThrow();
        }
    }

    /// <summary>
    /// Kiểm tra xem hiện tại có thanh kiếm nào đang active trên bản đồ hay không.
    /// </summary>
    /// <returns>True nếu kiếm đang ở ngoài, False nếu kiếm đang ở trong tay.</returns>
    public bool HasActiveSword() => activeSword != null;

    /// <summary>
    /// Ra lệnh thu hồi thanh kiếm đang ở ngoài bay trở về tay người chơi.
    /// </summary>
    void RecallSword()
    {
        if (activeSword != null)
        {
            Sword swordScript = activeSword.GetComponent<Sword>();
            if (swordScript != null) swordScript.StartReturn();
        }
    }

    /// <summary>
    /// Xử lý quyết định: Nếu đã có kiếm ngoài bản đồ thì Lướt (Dash) tới kiếm, nếu chưa có thì Ném kiếm mới.
    /// </summary>
    void HandleThrow()
    {
        if (!throwInput) return;
        throwInput = false;

        if (activeSword != null)
        {
            InitiateDashToSword();
            return;
        }

        ThrowNewSword();
    }

    /// <summary>
    /// Chuẩn bị các thiết lập ban đầu trước khi lướt: Khóa thanh kiếm đứng yên, ẩn hình ảnh người chơi, bật bất tử và tắt trọng lực.
    /// </summary>
    void InitiateDashToSword()
    {
        Sword swordScript = activeSword.GetComponent<Sword>();

        if (swordScript != null && swordScript.CanDashTo)
        {
            // KIỂM TRA & TIÊU HAO ANIMA KHI DASH
            if (anima != null)
            {
                if (!anima.HasEnoughAnima(dashAnimaCost))
                {
                    Debug.Log("Không đủ Anima để thực hiện Dash!");
                    return; // Không đủ Anima -> Hủy Dash
                }

                // Trừ Anima
                anima.ConsumeAnima(dashAnimaCost);
            }

            swordScript.FreezeSword();
            dashTargetPosition = activeSword.transform.position;

            movement.isDashing = true;
            rb.gravityScale = 0f;
            enemiesHitDuringDash.Clear();

            if (playerHealth != null) playerHealth.SetDashInvincibility(true);
            if (playerSprite != null) playerSprite.enabled = false;
        }
    }

    /// <summary>
    /// Khởi tạo Prefab thanh kiếm mới tại vị trí người chơi và ném theo hướng con trỏ chuột.
    /// </summary>
    void ThrowNewSword()
    {
        if (throwAnimaCost > 0 && anima != null)
        {
            if (!anima.HasEnoughAnima(throwAnimaCost))
            {
                Debug.Log("Không đủ Anima để ném kiếm!");
                return;
            }
            anima.ConsumeAnima(throwAnimaCost);
        }

        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f;
        Vector2 throwDirection = (mouseWorldPosition - transform.position).normalized;

        activeSword = Instantiate(swordPrefab, transform.position, Quaternion.identity);
        Sword newSwordScript = activeSword.GetComponent<Sword>();
        if (newSwordScript != null)
        {
            newSwordScript.Launch(throwDirection, transform);
        }
    }

    /// <summary>
    /// Gán vận tốc di chuyển cho Rigidbody2D đẩy nhân vật lướt thẳng tới vị trí thanh kiếm.
    /// </summary>
    void ExecuteDash()
    {
        Vector2 currentPos = transform.position;
        Vector2 dashDirection = (dashTargetPosition - currentPos).normalized;
        rb.linearVelocity = dashDirection * dashSpeed;
    }

    /// <summary>
    /// Kiểm tra xem cú lướt đã chạm tới mục tiêu chưa. Xử lý va chạm nảy trên quái thường/Quái Neo hoặc dừng lướt khi đụng đất/tường.
    /// </summary>
    void CheckDashArrival()
    {
        if (activeSword == null)
        {
            ResetDashState();
            return;
        }

        float distanceToTarget = Vector2.Distance(transform.position, dashTargetPosition);

        if (distanceToTarget < 0.5f)
        {
            transform.position = dashTargetPosition;

            Transform swordParent = activeSword.transform.parent;

            if (swordParent != null)
            {
                // Kiểm tra Quái Neo (dùng GetComponentInParent để tóm được cả hitbox con)
                AnchorPointEnemy anchorEnemy = swordParent.GetComponentInParent<AnchorPointEnemy>();
                if (anchorEnemy != null)
                {
                    float bounceForce = anchorEnemy.GetBounceForce();
                    anchorEnemy.ExecuteAnchorKill();
                    TriggerBounce(bounceForce);
                    Debug.Log("Kích hoạt giết Anchor thành công!");
                    return;
                }

                // Kiểm tra quái thường
                Health enemyHealth = swordParent.GetComponentInParent<Health>();
                if (enemyHealth != null)
                {
                    TriggerBounce(12f);
                    Debug.Log("Kích hoạt nảy trên quái thường!");
                    return;
                }
            }

            ResetDashState();
        }
        else if (distanceToTarget < 1.5f && (movement.IsGrounded() || movement.IsTouchingWall()))
        {
            ResetDashState(); // Thoát trạng thái Dash ngay lập tức khi va chạm
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Khi đang Dash, bỏ qua việc nhận sát thương trực tiếp từ các Trigger khác
        if (movement.isDashing) return;
    }

    /// <summary>
    /// Kích hoạt cú nảy người chơi lên không trung sau khi lướt trúng kẻ địch hoặc Quái Neo.
    /// </summary>
    /// <param name="bounceForce">Lực đẩy nảy lên theo chiều đứng.</param>
    private void TriggerBounce(float bounceForce)
    {
        StartCoroutine(PostDashInvincibilityRoutine(0.3f));

        ResetDashState();

        try
        {
            typeof(PlayerMovement).GetField("isWallJumping", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(movement, true);
            typeof(PlayerMovement).GetField("wallJumpCounter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(movement, 0.25f);
        }
        catch (System.Exception e) { Debug.LogError(e.Message); }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce);
    }

    /// <summary>
    /// Coroutine duy trì trạng thái bất tử thêm một khoảng thời gian ngắn sau khi kết thúc kỹ năng lướt.
    /// </summary>
    /// <param name="duration">Thời gian bất tử (tính bằng giây).</param>
    private IEnumerator PostDashInvincibilityRoutine(float duration)
    {
        if (playerHealth != null)
        {
            playerHealth.SetDashInvincibility(true);
            yield return new WaitForSeconds(duration);
            playerHealth.SetDashInvincibility(false);
        }
    }

    /// <summary>
    /// Quét các kẻ địch nằm trong bán kính lướt và gây sát thương (mỗi kẻ địch chỉ bị dính sát thương 1 lần trong suốt cú lướt).
    /// </summary>
    void HandleDashDamage()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, dashDamageRadius, enemyLayer);
        foreach (Collider2D enemy in hitEnemies)
        {
            if (!enemiesHitDuringDash.Contains(enemy))
            {
                Health enemyHealth = enemy.GetComponent<Health>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(dashDamage, transform.position);
                    enemiesHitDuringDash.Add(enemy);
                }
            }
        }
    }

    /// <summary>
    /// Tự động kiểm tra khoảng cách giữa người chơi và thanh kiếm, nếu lại gần sẽ tự động nhặt lại kiếm.
    /// </summary>
    private void CheckAutoPickUpSword()
    {
        if (activeSword != null && !movement.isDashing)
        {
            Sword swordScript = activeSword.GetComponent<Sword>();
            if (swordScript != null && swordScript.CanBePickedUp)
            {
                float distanceToSword = Vector2.Distance(transform.position, activeSword.transform.position);
                if (distanceToSword <= swordPickUpDistance)
                {
                    Debug.Log("Nhân vật đi đến gần và tự động nhặt lại kiếm!");
                    Destroy(activeSword);
                    activeSword = null;
                }
            }
        }
    }

    /// <summary>
    /// Đặt lại toàn bộ thông số về trạng thái bình thường sau khi kết thúc lướt (Hiện lại Sprite, bật lại trọng lực, tắt bất tử và xóa thanh kiếm).
    /// </summary>
    private void ResetDashState()
    {
        rb.gravityScale = originalGravity;
        rb.linearVelocity = Vector2.zero;

        if (playerSprite != null) playerSprite.enabled = true;
        if (playerHealth != null) playerHealth.SetDashInvincibility(false);

        enemiesHitDuringDash.Clear();

        if (activeSword != null)
        {
            Destroy(activeSword);
            activeSword = null;
        }

        movement.isDashing = false;
    }
}