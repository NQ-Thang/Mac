using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("I-Frames & Flashing")]
    [SerializeField] private float invincibleDuration = 1.5f; // Thời gian bất tử
    [SerializeField] private float flashInterval = 0.15f;     // Tốc độ nhấp nháy
    private bool isInvincible = false;
    private SpriteRenderer spriteRenderer;
    private int normalPlayerLayer;
    private int invinciblePlayerLayer;

    [Header("KnockBack")]
    [SerializeField] private float knockbackForceX = 8f;
    [SerializeField] private float knockbackForceY = 5f;
    [SerializeField] private float knockbackDuration = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool isPlayer = false;

    private Rigidbody2D rb;
    private PlayerMovement playerMovement;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (isPlayer)
        {
            playerMovement = GetComponent<PlayerMovement>();

            // Lấy ID của các Layer (Hãy chắc chắn bạn đã tạo 2 Layer này trong Unity)
            normalPlayerLayer = LayerMask.NameToLayer("Player");
            invinciblePlayerLayer = LayerMask.NameToLayer("PlayerInvincible");
        }
    }

    public void TakeDamage(float damageAmount, Vector2 attackerPosition)
    {
        if (isPlayer && isInvincible) return;

        currentHealth -= damageAmount;
        Debug.Log(gameObject.name + " Mau con: " + currentHealth);

        VoTri voTri = GetComponent<VoTri>();

        if (voTri != null)
        {
            voTri.OnHit();
        }

        if (currentHealth > 0)
        {
            TriggerKnockBack(attackerPosition);

            if (isPlayer)
            {
                StartCoroutine(BecomeInvincibleRoutine());
            }
        }
        else
        {
            Die();
        }
    }


    private void TriggerKnockBack(Vector2 attackerPosition)
    {
        if (rb == null) return;

        float knockbackDirection = transform.position.x > attackerPosition.x ? 1f : -1f;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(knockbackDirection * knockbackForceX, knockbackForceY), ForceMode2D.Impulse);

        StartCoroutine(KnockbackRoutine());
    }

    private IEnumerator KnockbackRoutine()
    {
        if (isPlayer && playerMovement != null)
        {
            typeof(PlayerMovement).GetField("isWallJumping", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(playerMovement, true);
            typeof(PlayerMovement).GetField("wallJumpCounter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(playerMovement, knockbackDuration);
        }
        yield return new WaitForSeconds(knockbackDuration);
    }

    // COROUTINE XỬ LÝ BẤT TỬ & NHẤP NHÁY
    private IEnumerator BecomeInvincibleRoutine()
    {
        isInvincible = true;

        // Đổi sang Layer bất tử để đi xuyên qua quái (nhớ cài đặt Matrix Collision trong Project Settings)
        gameObject.layer = invinciblePlayerLayer;

        float timer = 0f;
        while (timer < invincibleDuration)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled; // Tắt/Bật hiển thị hình ảnh
            }
            yield return new WaitForSeconds(flashInterval);
            timer += flashInterval;
        }

        // Trả lại trạng thái bình thường khi hết thời gian
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        gameObject.layer = normalPlayerLayer;
        isInvincible = false;
    }

    public void SetDashInvincibility(bool isInvincible)
    {
        if (isPlayer)
        {
            gameObject.layer = isInvincible ? LayerMask.NameToLayer("PlayerInvincible") : LayerMask.NameToLayer("Player");
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " đã chết!");
        if (isPlayer)
        {
            currentHealth = maxHealth;
            // Nếu chết thì hồi sinh ngay lập tức phải reset lại layer và hiển thị cho chắc chắn
            if (spriteRenderer != null) spriteRenderer.enabled = true;
            gameObject.layer = normalPlayerLayer;
            isInvincible = false;

            Debug.Log("Player hồi sinh tạm thời để test!");
        }
        else
        {
            Destroy(gameObject);
        }
    }
}