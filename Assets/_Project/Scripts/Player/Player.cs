using UnityEngine;

/// <summary>
/// Lớp điều khiển trung tâm của Người chơi, kế thừa từ Entity.
/// Lo nhiệm vụ thu thập Input bàn phím/chuột, quản lý trạng thái chung và điều phối các mảnh ghép phụ.
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerCombat))]
[RequireComponent(typeof(PlayerSwordTech))]
[RequireComponent(typeof(PlayerAnima))]
public class Player : Entity
{
    // Các tham chiếu đến mảnh ghép phụ gắn chung trên một Object (Đã được cam kết luôn tồn tại nhờ RequireComponent)
    public PlayerMovement Movement { get; private set; }
    public PlayerCombat Combat { get; private set; }
    public PlayerSwordTech SwordTech { get; private set; }
    public PlayerAnima Anima { get; private set; }

    [Header("Input Data")]
    public float horizontalInput { get; private set; }
    public bool jumpInput { get; private set; }
    public bool jumpUpInput { get; private set; }

    protected override void Awake()
    {
        base.Awake(); // Lấy Rigidbody, Animator, Health từ Entity

        // Tự động kết nối đến các mảnh ghép chức năng
        Movement = GetComponent<PlayerMovement>();
        Combat = GetComponent<PlayerCombat>();
        SwordTech = GetComponent<PlayerSwordTech>();
        Anima = GetComponent<PlayerAnima>();
    }

    protected virtual void Update()
    {
        // Nếu đang Dash, khóa toàn bộ Input di chuyển và không cho phép lật mặt
        if (Movement.isDashing)
        {
            ClearInput();
            return;
        }

        GatherInput();

        // Tận dụng hàm lật mặt ảo của Entity bằng cách truyền dữ liệu trục ngang vào
        // Chỉ cho phép lật mặt khi thực sự có di chuyển ngang
        if (Mathf.Abs(horizontalInput) > 0.01f)
        {
            ControlFlip(horizontalInput);
        }
    }

    private void GatherInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // Đọc phím nhảy
        if (Input.GetButtonDown("Jump")) jumpInput = true;
        if (Input.GetButtonUp("Jump")) jumpUpInput = true;
    }

    /// <summary>
    /// Hàm xóa dữ liệu đệm Jump Input sau khi đã thực hiện cú nhảy (gọi từ PlayerMovement) để tránh nhảy liên tục.
    /// </summary>
    public void UseJumpInput() => jumpInput = false;
    public void UseJumpUpInput() => jumpUpInput = false;

    /// <summary>
    /// Hàm xóa dữ liệu đệm Input để tránh lỗi kẹt phím khi bị khựng hoặc dash.
    /// </summary>
    public void ClearInput()
    {
        horizontalInput = 0f;
        jumpInput = false;
    }
}