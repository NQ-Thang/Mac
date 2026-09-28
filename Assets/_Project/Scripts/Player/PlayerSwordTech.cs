using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mảnh ghép quản lý toàn bộ kỹ năng phi kiếm và lướt tới vị trí kiếm của người chơi.
/// Lấy toàn bộ dữ liệu vật lý và Input thông qua component trung tâm Player.
/// </summary>
public class PlayerSwordTech : MonoBehaviour
{
    [Header("Ranged Attack (Sword Throw)")]
    [SerializeField] private float swordPickUpDistance = 1f;
    [SerializeField] private GameObject swordPrefab;

    private GameObject activeSword;
    private bool throwInput;

    [Header("Dash to Sword Settings")]
    [SerializeField] private float dashSpeed = 25f;
    [SerializeField] private float dashShrinkScale = 0.2f;

    private Vector2 dashTargetPosition;
    private float originalGravity;
    private Vector3 originalScale;

    [Header("Dash Attack Settings")]
    [SerializeField] private int dashDamage = 20;
    [SerializeField] private float dashDamageRadius = 0.6f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Anima Cost Settings")]
    [SerializeField] private int dashAnimaCost = 0;
    [SerializeField] private int throwAnimaCost = 0;

    private Player player;
    private Camera mainCamera;

    // Cache mảng va chạm và bộ lọc vật lý để triệt tiêu việc sinh rác GC khi lướt
    private readonly Collider2D[] hitEnemiesCache = new Collider2D[15];
    private readonly List<Collider2D> enemiesHitDuringDash = new List<Collider2D>();
    private ContactFilter2D dashDamageFilter;

    void Start()
    {
        player = GetComponent<Player>();
        originalGravity = player.rb.gravityScale;
        originalScale = transform.localScale;
        mainCamera = Camera.main;

        dashDamageFilter = new ContactFilter2D();
        dashDamageFilter.SetLayerMask(enemyLayer);
        dashDamageFilter.useLayerMask = true;
    }

    void Update()
    {
        if (player.Movement.isDashing)
        {
            if (activeSword == null)
            {
                ResetDashState();
            }
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
        if (player.Movement.isDashing)
        {
            ExecuteDash();
            HandleDashDamage();
        }
        else
        {
            HandleThrow();
        }
    }

    public bool HasActiveSword() => activeSword != null;

    void RecallSword()
    {
        if (activeSword != null)
        {
            Sword swordScript = activeSword.GetComponent<Sword>();
            if (swordScript != null) swordScript.StartReturn();
        }
    }

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

    void InitiateDashToSword()
    {
        if (activeSword == null) return;
        Sword swordScript = activeSword.GetComponent<Sword>();

        if (swordScript != null && swordScript.CanDashTo)
        {
            // Kiểm tra và tiêu hao nộ (Anima) - Đã bỏ check Null do Player cam kết Component này luôn tồn tại
            if (!player.Anima.HasEnoughAnima(dashAnimaCost))
            {
                Debug.Log("Không đủ Anima để thực hiện Dash!");
                return;
            }
            player.Anima.ConsumeAnima(dashAnimaCost);

            swordScript.FreezeSword();
            dashTargetPosition = activeSword.transform.position;

            player.Movement.isDashing = true;
            player.rb.gravityScale = 0f;

            // Xuyên qua toàn bộ vật thể và địa hình, không bị collider trên đường đi cản lại
            if (player.coreCollider != null)
            {
                player.coreCollider.enabled = false;
            }

            // Co lại thành một chùm sáng/điểm nhỏ trong thời gian dash
            transform.localScale = originalScale * dashShrinkScale;
            player.SetRenderersVisible(true);

            enemiesHitDuringDash.Clear();
            player.health.SetDashInvincibility(true);
        }
    }

    void ThrowNewSword()
    {
        if (throwAnimaCost > 0)
        {
            if (!player.Anima.HasEnoughAnima(throwAnimaCost))
            {
                Debug.Log("Không đủ Anima để ném kiếm!");
                return;
            }
            player.Anima.ConsumeAnima(throwAnimaCost);
        }

        // Tối ưu: Sử dụng camera đã được cache từ Start
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f;
        Vector2 throwDirection = (mouseWorldPosition - transform.position).normalized;

        activeSword = Instantiate(swordPrefab, transform.position, Quaternion.identity);
        Sword newSwordScript = activeSword.GetComponent<Sword>();
        if (newSwordScript != null)
        {
            newSwordScript.Launch(throwDirection, transform);
        }
    }

    void ExecuteDash()
    {
        if (activeSword == null)
        {
            ResetDashState();
            return;
        }

        Vector2 currentPos = transform.position;
        Vector2 toTarget = dashTargetPosition - currentPos;
        float distance = toTarget.magnitude;
        float step = dashSpeed * Time.fixedDeltaTime;

        if (distance <= step || distance < 0.1f)
        {
            player.rb.position = dashTargetPosition;
            transform.position = dashTargetPosition;
            ResetDashState();
        }
        else
        {
            Vector2 dashDirection = toTarget / distance;
            player.rb.linearVelocity = dashDirection * dashSpeed;
        }
    }

    void HandleDashDamage()
    {
        // TỐI ƯU HÓA TUYỆT ĐỐI: Sử dụng bộ lọc filter đã cache sẵn từ Start, loại bỏ "new ContactFilter2D"
        int numColliders = Physics2D.OverlapCircle(transform.position, dashDamageRadius, dashDamageFilter, hitEnemiesCache);

        for (int i = 0; i < numColliders; i++)
        {
            Collider2D enemy = hitEnemiesCache[i];
            if (enemy == null) continue;

            if (!enemiesHitDuringDash.Contains(enemy))
            {
                Health enemyHealth = enemy.GetComponentInParent<Health>();
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
        if (activeSword != null && !player.Movement.isDashing)
        {
            Sword swordScript = activeSword.GetComponent<Sword>();
            if (swordScript != null && swordScript.CanBePickedUp)
            {
                float sqrDistanceToSword = ((Vector2)transform.position - (Vector2)activeSword.transform.position).sqrMagnitude;
                if (sqrDistanceToSword <= swordPickUpDistance * swordPickUpDistance)
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
        player.rb.gravityScale = originalGravity;
        player.rb.linearVelocity = Vector2.zero;

        // Phục hồi kích thước và trạng thái bình thường của nhân vật
        transform.localScale = originalScale;
        player.SetRenderersVisible(true);

        if (player.coreCollider != null)
        {
            player.coreCollider.enabled = true;
        }

        player.health.SetDashInvincibility(false);
        enemiesHitDuringDash.Clear();

        if (activeSword != null)
        {
            Destroy(activeSword);
            activeSword = null;
        }

        player.Movement.isDashing = false;
    }

    void OnDisable()
    {
        if (player != null && player.Movement != null && player.Movement.isDashing)
        {
            ResetDashState();
        }
    }
}