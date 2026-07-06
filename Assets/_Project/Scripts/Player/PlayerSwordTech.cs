using System.Collections.Generic;
using UnityEngine;

public class PlayerSwordTech : MonoBehaviour
{
    [Header("Sword Mode Management")]
    [SerializeField] private Sword.SwordMode preferredSwordMode = Sword.SwordMode.StickToEnemies;

    [Header("Ranged Attack (Sword Throw)")]
    [SerializeField] private float swordPickUpDistance = 1f;
    [SerializeField] private GameObject swordPrefab;
    private GameObject activeSword;
    private bool throwInput;

    [Header("Dash to Sword Settings")]
    [SerializeField] private float dashSpeed = 25f;
    private Vector2 dashTargetPosition;
    private float originalGravity;

    [Header("Dash Attack Settings")]
    [SerializeField] private int dashDamage = 2;
    [SerializeField] private float dashDamageRadius = 0.6f;
    [SerializeField] private LayerMask enemyLayer;
    private SpriteRenderer playerSprite;


    private Rigidbody2D rb;
    private PlayerMovement movement;
    private Health playerHealth;
    private List<Collider2D> enemiesHitDuringDash = new List<Collider2D>();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();
        playerHealth = GetComponent<Health>();
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


            if (Input.GetKeyDown(KeyCode.Q))
            {
                preferredSwordMode = (preferredSwordMode == Sword.SwordMode.PierceAll)
                    ? Sword.SwordMode.StickToEnemies
                    : Sword.SwordMode.PierceAll;
                Debug.Log("Đã đổi chế độ ném kiếm sang: " + preferredSwordMode);
            }

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

    // Hàm public giúp bên script Combat check xem kiếm có đang ở ngoài không
    public bool HasActiveSword() => activeSword != null;

    void RecallSword()
    {
        if (activeSword != null)
        {
            Sword swordScript = activeSword.GetComponent<Sword>();
            if (swordScript != null) swordScript.CallRecall();
        }
    }

    void HandleThrow()
    {
        if (!throwInput) return;
        throwInput = false;

        // Nhánh 1: Nếu kiếm đã ở ngoài -> Thực hiện đóng băng kiếm và chuẩn bị Dash
        if (activeSword != null)
        {
            InitiateDashToSword();
            return;
        }

        // Nhánh 2: Nếu chưa có kiếm -> Tiến hành phóng kiếm mới
        ThrowNewSword();
    }
    void InitiateDashToSword()
    {
        Sword swordScript = activeSword.GetComponent<Sword>();

        // Kiểm tra xem script Sword có hợp lệ và trạng thái kiếm có cho phép Dash không
        if (swordScript != null && swordScript.CanDashTo)
        {
            // 1. KHÓA CỨNG CÂY KIẾM LẠI NGAY LẬP TỨC, KHÔNG CHO BAY NỮA!
            swordScript.FreezeSword();

            // 2. GÁN VỊ TRÍ ĐÍCH CHÍNH LÀ VỊ TRÍ KHỰNG LẠI CỦA KIẾM
            dashTargetPosition = activeSword.transform.position;

            // 3. THIẾT LẬP TRẠNG THÁI DASH VÀ KHÓA VẬT LÝ PLAYER
            movement.isDashing = true;
            rb.gravityScale = 0f;
            enemiesHitDuringDash.Clear();

            // 4. BẬT BẤT TỬ VÀ ẨN HIỂN THỊ ĐỂ CHUẨN BỊ LÀM EFFECT "XOẸT"
            if (playerHealth != null) playerHealth.SetDashInvincibility(true);
            if (playerSprite != null) playerSprite.enabled = false;
        }
    }
    void ThrowNewSword()
    {
        // Tính toán hướng phóng dựa trên vị trí con chuột
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f;
        Vector2 throwDirection = (mouseWorldPosition - transform.position).normalized;

        // Sinh ra cây kiếm và gọi hàm kích hoạt bay
        activeSword = Instantiate(swordPrefab, transform.position, Quaternion.identity);
        Sword newSwordScript = activeSword.GetComponent<Sword>();
        if (newSwordScript != null)
        {
            newSwordScript.SetMode(preferredSwordMode);
            newSwordScript.Launch(throwDirection, transform);
        }
    }

    void ExecuteDash()
    {
        Vector2 currentPos = transform.position;
        Vector2 dashDirection = (dashTargetPosition - currentPos).normalized;
        rb.linearVelocity = dashDirection * dashSpeed;
    }

    void CheckDashArrival()
    {
        float distanceToTarget = Vector2.Distance(transform.position, dashTargetPosition);
        bool isDashingDown = rb.linearVelocity.y < -0.1f;
        bool canCheckObstacles = distanceToTarget < 1.5f;

        if (distanceToTarget < 0.4f ||
           (canCheckObstacles && isDashingDown && movement.IsGrounded()) ||
           (canCheckObstacles && movement.IsTouchingWall()))
        {
            if (distanceToTarget < 0.4f)
            {
                transform.position = dashTargetPosition;
            }

            // Gọi hàm dọn dẹp sạch sẽ
            ResetDashState();
        }
    }

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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Nếu đang dash và đâm trúng Quái Neo đang bị găm kiếm
        if (movement.isDashing && collision.CompareTag("Enemy"))
        {
            AnchorPointEnemy anchorEnemy = collision.GetComponent<AnchorPointEnemy>();
            if (anchorEnemy != null && anchorEnemy.IsSwordStuck)
            {
                float bounceForce = anchorEnemy.GetBounceForce();

                // 1. Tiêu diệt quái trước
                anchorEnemy.ExecuteAnchorKill();

                // 2. Dọn dẹp trạng thái Dash (Hiện lại hình, trả trọng lực...)
                ResetDashState();

                // 3. Mượn hệ thống Wall Jump để khóa di chuyển ngang tạm thời
                try
                {
                    typeof(PlayerMovement).GetField("isWallJumping", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(movement, true);
                    typeof(PlayerMovement).GetField("wallJumpCounter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(movement, 0.25f);
                }
                catch (System.Exception e) { Debug.LogError(e.Message); }

                // 4. Thực hiện cú nảy vút lên trời
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce);
                Debug.Log("Đâm trúng điểm neo thành công! Nhân vật đã hiện hình và nảy lên.");
            }
        }
    }
}