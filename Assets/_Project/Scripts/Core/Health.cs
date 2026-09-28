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
    private SpriteRenderer[] allSpriteRenderers;
    private WaitForSeconds flashWait;
    private WaitForSeconds knockbackWait;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        allSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        flashWait = new WaitForSeconds(flashInterval);
        knockbackWait = new WaitForSeconds(knockbackDuration);

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

        IEnemy enemy = GetComponent<IEnemy>();
        if (enemy != null)
        {
            enemy.OnHit();
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

        if (isPlayer)
        {
            Player playerHub = GetComponent<Player>();
            if (playerHub != null) playerHub.Heal.InterruptHeal();
        }

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
        yield return knockbackWait;
    }

    private IEnumerator BecomeInvincibleRoutine()
    {
        isInvincible = true;

        gameObject.layer = invinciblePlayerLayer; // chuyển layer sang "PlayerInvincible" để tránh va chạm với kẻ thù

        float timer = 0f;
        while (timer < invincibleDuration) // chạy trong khoảng thời gian bất tử cho đến khi hết invincibleDuration
        {
            SetAlpha(0.3f);
            yield return flashWait;
            timer += flashInterval;

            if (timer < invincibleDuration)
            {
                SetAlpha(1f);
                yield return flashWait;
                timer += flashInterval;
            }
        }

        SetAlpha(1f);
        gameObject.layer = normalPlayerLayer; // chuyển layer trở lại "Player" để nhận va chạm với kẻ thù
        isInvincible = false; 
    }

    private void SetAlpha(float alpha)
    {
        if (allSpriteRenderers != null)
        {
            for (int i = 0; i < allSpriteRenderers.Length; i++)
            {
                if (allSpriteRenderers[i] != null)
                {
                    Color c = allSpriteRenderers[i].color;
                    c.a = alpha;
                    allSpriteRenderers[i].color = c;
                }
            }
        }
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

    /// <summary>
    /// Kiểm tra xem lượng máu hiện tại đã đầy hay chưa.
    /// </summary>
    public bool IsHealthFull() => currentHealth >= maxHealth; // tránh lỗi số thập phân, dùng >= thay vì ==

    /// <summary>
    /// Hàm công khai hỗ trợ hồi lại lượng máu cho thực thể, chặn không vượt quá maxHealth.
    /// </summary>
    public void Heal(float amount)
    {
        if (currentHealth <= 0) return; // Đã chết thì không hồi nữa

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"{gameObject.name} được hồi máu! Máu hiện tại: {currentHealth}");
    }
}