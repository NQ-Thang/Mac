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
    private int normalPlayerLayer; // id của Layer "Player"
    private int invinciblePlayerLayer; // id của Layer "PlayerInvincible"

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

            normalPlayerLayer = LayerMask.NameToLayer("Player"); // id của Layer "Player"   
            invinciblePlayerLayer = LayerMask.NameToLayer("PlayerInvincible"); // id của Layer "PlayerInvincible"
        }
    }

    public void TakeDamage(float damageAmount, Vector2 attackerPosition)
    {
        if (isPlayer && isInvincible) return; // Nếu là player và đang trong trạng thái bất tử, không nhận sát thương

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
            playerMovement.ApplyKnockbackStun(knockbackDuration);
        }
        yield return new WaitForSeconds(knockbackDuration);
    }

    private IEnumerator BecomeInvincibleRoutine()
    {
        isInvincible = true;

        gameObject.layer = invinciblePlayerLayer; // chuyển layer sang "PlayerInvincible" để tránh va chạm với kẻ thù

        float timer = 0f;
        while (timer < invincibleDuration) // chạy trong khoảng thời gian bất tử cho đến khi hết invincibleDuration
        {
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = (Mathf.Approximately(color.a, 1f)) ? 0.3f : 1f; // Chuyển đổi giữa rõ nét (1.0) và bán trong suốt (0.3), a -> alpha
                spriteRenderer.color = color;
            }
            yield return new WaitForSeconds(flashInterval); // tạm dừng coroutine trong khoảng thời gian flashInterval trước khi tiếp tục 
            timer += flashInterval;
        }

        // sau khi kết thúc vòng lặp
        if (spriteRenderer != null)
        {
            Color finalColor = spriteRenderer.color;
            finalColor.a = 1f; // Đảm bảo trả lại độ rõ nét 100% khi hết bất tử
            spriteRenderer.color = finalColor;
        }
        gameObject.layer = normalPlayerLayer; // chuyển layer trở lại "Player" để nhận va chạm với kẻ thù
        isInvincible = false; 
    }

    public void SetDashInvincibility(bool isInvincible) // Hàm này được gọi từ PlayerDash.cs để đặt trạng thái bất tử khi dash
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