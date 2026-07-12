using System.Collections;
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

    //[Header("Dash Safety")]
    //[SerializeField] private float maxDashDuration = 0.25f; // Thời gian Dash tối đa trước khi tự reset
    //private float currentDashTimer = 0f;

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

        if (activeSword != null)
        {
            InitiateDashToSword();
            return;
        }

        ThrowNewSword();
    }

    void InitiateDashToSword()
    {
        Sword swordScript = activeSword.GetComponent<Sword>();

        if (swordScript != null && swordScript.CanDashTo)
        {
            swordScript.FreezeSword();
            dashTargetPosition = activeSword.transform.position;

            movement.isDashing = true;
            rb.gravityScale = 0f;
            enemiesHitDuringDash.Clear();

            //currentDashTimer = 0f;

            if (playerHealth != null) playerHealth.SetDashInvincibility(true);
            if (playerSprite != null) playerSprite.enabled = false;
        }
    }

    void ThrowNewSword()
    {
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f;
        Vector2 throwDirection = (mouseWorldPosition - transform.position).normalized;

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
        if (activeSword == null)
        {
            ResetDashState();
            return;
        }

        //currentDashTimer += Time.deltaTime;

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

    private IEnumerator PostDashInvincibilityRoutine(float duration)
    {
        if (playerHealth != null)
        {
            playerHealth.SetDashInvincibility(true);
            yield return new WaitForSeconds(duration);
            playerHealth.SetDashInvincibility(false);
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
}