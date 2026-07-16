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

    private Vector2 dashTargetPosition;
    private float originalGravity;

    [Header("Dash Attack Settings")]
    [SerializeField] private int dashDamage = 2;
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

    // Cache Coroutine và Wait để tối ưu bộ nhớ
    private Coroutine postDashInvincibilityCoroutine;
    private WaitForSeconds postDashInvincibleWait;

    void Start()
    {
        player = GetComponent<Player>();
        originalGravity = player.rb.gravityScale;
        mainCamera = Camera.main;

        dashDamageFilter = new ContactFilter2D();
        dashDamageFilter.SetLayerMask(enemyLayer);
        dashDamageFilter.useLayerMask = true;

        postDashInvincibleWait = new WaitForSeconds(0.3f);
    }

    void Update()
    {
        if (player.Movement.isDashing)
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
            enemiesHitDuringDash.Clear();

            player.health.SetDashInvincibility(true);
            player.spriteRenderer.enabled = false;
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
        Vector2 currentPos = transform.position;
        Vector2 dashDirection = (dashTargetPosition - currentPos).normalized;
        player.rb.linearVelocity = dashDirection * dashSpeed;
    }

    void CheckDashArrival()
    {
        if (activeSword == null)
        {
            ResetDashState();
            return;
        }

        // Tối ưu hóa tính toán khoảng cách 2D bằng sqrMagnitude của Vector2
        float sqrDistanceToTarget = ((Vector2)transform.position - dashTargetPosition).sqrMagnitude;

        if (sqrDistanceToTarget < 0.25f) // 0.5f * 0.5f = 0.25f
        {
            transform.position = dashTargetPosition;
            Transform swordParent = activeSword.transform.parent;

            if (swordParent != null)
            {
                AnchorPointEnemy anchorEnemy = swordParent.GetComponentInParent<AnchorPointEnemy>();
                if (anchorEnemy != null)
                {
                    float bounceForce = anchorEnemy.GetBounceForce();
                    anchorEnemy.ExecuteAnchorKill();
                    TriggerBounce(bounceForce);
                    return;
                }
            }

            ResetDashState();
        }
        else if (sqrDistanceToTarget < 2.25f && (player.IsGrounded() || player.IsTouchingWall())) // 1.5f * 1.5f = 2.25f
        {
            ResetDashState();
        }
    }

    private void TriggerBounce(float bounceForce)
    {
        if (postDashInvincibilityCoroutine != null) StopCoroutine(postDashInvincibilityCoroutine);
        postDashInvincibilityCoroutine = StartCoroutine(PostDashInvincibilityRoutine());

        ResetDashState();

        player.Movement.ForceWallJumpState(0.25f);
        player.rb.linearVelocity = new Vector2(player.rb.linearVelocity.x, bounceForce);
    }

    private IEnumerator PostDashInvincibilityRoutine()
    {
        player.health.SetDashInvincibility(true);
        yield return postDashInvincibleWait; // Sử dụng biến cache tránh sinh rác GC
        player.health.SetDashInvincibility(false);
        postDashInvincibilityCoroutine = null;
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

        player.spriteRenderer.enabled = true;
        player.health.SetDashInvincibility(false);

        enemiesHitDuringDash.Clear();

        if (activeSword != null)
        {
            Destroy(activeSword);
            activeSword = null;
        }

        player.Movement.isDashing = false;
    }
}