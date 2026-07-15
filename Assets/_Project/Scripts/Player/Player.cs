using UnityEngine;

/// <summary>
/// Lớp điều khiển trung tâm của Người chơi, kế thừa từ Entity.
/// Lo nhiệm vụ thu thập Input bàn phím/chuột và quản lý trạng thái chung.
/// </summary>
public class Player : Entity
{
    // Các tham chiếu đến mảnh ghép phụ gắn chung trên một Object
    public PlayerMovement Movement { get; private set; }
    public PlayerCombat Combat { get; private set; }
    public PlayerSwordTech SwordTech { get; private set; }
    public PlayerAnima Anima { get; private set; }

    [Header("Input Data")]
    public float horizontalInput { get; private set; }
    public bool jumpInput { get; private set; }

    protected override void Awake()
    {
        base.Awake(); // Lấy Rigidbody, Animator, Health từ Entity

        // Tự động kết nối đến các mảnh ghép chức năng
        Movement = GetComponent<PlayerMovement>();
        Combat = GetComponent<PlayerCombat>();
        SwordTech = GetComponent<PlayerSwordTech>();
        Anima = GetComponent<PlayerAnima>();
    }

    protected override void Update()
    {
        base.Update();

        // Nếu đang Dash hoặc đang chịu trạng thái đặc biệt từ SwordTech, khóa toàn bộ Input di chuyển
        if (Movement != null && Movement.isDashing)
        {
            ClearInput();
            return;
        }

        GatherInput();

        // Tận dụng hàm lật mặt ảo của Entity bằng cách truyền dữ liệu trục ngang vào
        ControlFlip(horizontalInput);
    }

    private void GatherInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // Đọc phím nhảy
        if (Input.GetButtonDown("Jump")) jumpInput = true;
    }

    /// <summary>
    /// Hàm xóa dữ liệu đệm Input để tránh lỗi kẹt phím khi bị khựng/dash.
    /// </summary>
    public void UseJumpInput() => jumpInput = false;

    private void ClearInput()
    {
        horizontalInput = 0f;
        jumpInput = false;
    }
}