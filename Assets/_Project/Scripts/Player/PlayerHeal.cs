using UnityEngine;

/// <summary>
/// Mảnh ghép quản lý cơ chế tích tụ linh lực (Focus Anima) để hồi máu khi nhấn giữ phím.
/// Người chơi bắt buộc phải đứng yên, nếu thả phím hoặc bị trúng đòn sẽ bị ngắt tiến trình.
/// </summary>
[RequireComponent(typeof(Player))]
public class PlayerHeal : MonoBehaviour
{
    [Header("Heal Values")]
    [SerializeField] private float healAmount = 20f;
    [SerializeField] private int animaCost = 33;            // Tiêu tốn ~1/3 bình mana mặc định (100)
    [SerializeField] private float timeToHeal = 2f;         // Cần giữ chặt phím trong 2 giây

    [Header("Input Settings")]
    [SerializeField] private KeyCode healKey = KeyCode.S;   // Giữ phím S để hồi máu giống yêu cầu

    private Player player;
    private float holdTimer;
    private bool isHealing;

    void Start()
    {
        // Tự động kết nối đến Hub trung tâm
        player = GetComponent<Player>();
    }

    void Update()
    {
        // Không cho phép vận công hồi máu nếu đang lướt (Dash)
        if (player.Movement.isDashing)
        {
            InterruptHeal();
            return;
        }

        HandleHealFocus();
    }

    /// <summary>
    /// Xử lý logic nhấn giữ phím và tích lũy thời gian vận công.
    /// </summary>
    private void HandleHealFocus()
    {
        // 1. Nếu nhấn giữ phím S VÀ đang đứng yên dưới đất
        if (Input.GetKey(healKey) && player.IsGrounded() && Mathf.Abs(player.rb.linearVelocity.x) < 0.1f)
        {
            // Kiểm tra điều kiện: Máu chưa đầy và đủ Anima mới cho phép tụ lực
            if (player.health.IsHealthFull())
            {
                Debug.Log("Máu đã đầy, không cần hồi!");
                InterruptHeal();
                return;
            }

            if (!player.Anima.HasEnoughAnima(animaCost))
            {
                Debug.Log("Không đủ Anima để hồi máu!");
                InterruptHeal();
                return;
            }

            // Bắt đầu tiến trình tụ lực hồi máu
            if (!isHealing)
            {
                isHealing = true;
                Debug.Log("Bắt đầu vận công tụ lực hồi máu... (Hãy giữ chặt phím S)");
                // Bạn có thể kích hoạt Animation vận công (Focus/Cast) tại đây:
                // player.anim.SetBool("isHealing", true);
            }

            // Khóa không cho nhân vật di chuyển ngang lúc đang vận công bằng cách triệt tiêu vận tốc X
            player.rb.linearVelocity = new Vector2(0f, player.rb.linearVelocity.y);

            // Tích lũy thời gian giữ phím
            holdTimer += Time.deltaTime;

            // Đạt mốc 2 giây -> Hồi máu thành công!
            if (holdTimer >= timeToHeal)
            {
                ExecuteHeal();
            }
        }
        else
        {
            // Nếu người chơi thả phím S hoặc cố tình di chuyển/rơi tự do -> Ngắt tiến trình tụ lực
            if (isHealing)
            {
                InterruptHeal();
            }
        }
    }

    /// <summary>
    /// Thực hiện cấu trừ tài nguyên và bơm máu khi đại công cáo thành.
    /// </summary>
    private void ExecuteHeal()
    {
        player.Anima.ConsumeAnima(animaCost);
        player.health.Heal(healAmount);

        Debug.Log($"✨ Hồi máu thành công! +{healAmount} HP | Tốn {animaCost} Anima.");

        // Reset lại bộ đếm để nếu người chơi vẫn giữ chặt phím S thì sẽ tự động tích lũy lượt hồi máu tiếp theo
        holdTimer = 0f;

        // Nếu sau khi hồi mà máu đầy luôn thì tự động dừng trạng thái
        if (player.health.IsHealthFull())
        {
            InterruptHeal();
        }
    }

    /// <summary>
    /// Hàm ngắt tiến trình hồi máu khi người chơi thả phím hoặc bị quái đánh trúng.
    /// </summary>
    public void InterruptHeal()
    {
        if (!isHealing) return;

        isHealing = false;
        holdTimer = 0f;
        Debug.Log("❌ Tiến trình hồi máu bị gián đoạn hoặc kết thúc.");

        // Tắt Animation vận công tại đây:
        // player.anim.SetBool("isHealing", false);
    }
}