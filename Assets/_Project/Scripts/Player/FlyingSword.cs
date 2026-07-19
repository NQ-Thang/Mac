using UnityEngine;

public class FlyingSword : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform playerTransform; // Kéo Player vào đây
    [SerializeField] private Vector3 offset = new Vector3(-1.2f, 1f, 0f); // Vị trí bay lơ lửng so với Player

    [Header("Movement Settings")]
    [SerializeField] private float followSpeed = 5f;       // Tốc độ đuổi theo
    [SerializeField] private float floatSpeed = 2f;        // Tốc độ nhấp nhô lơ lửng
    [SerializeField] private float floatAmount = 0.15f;    // Biên độ nhấp nhô (cao/thấp)
    [SerializeField] private float flipSpeed = 10f;        // [THÊM MỚI] Tốc độ lật kiếm

    private Vector3 velocity = Vector3.zero;
    private Vector3 currentOffset; // [THÊM MỚI] Dùng để lật offset sang bên kia Player

    void Start()
    {
        // Tự động tìm Player bằng Tag nếu chưa kéo thả (Để thuận tiện)
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }
        currentOffset = offset; // Khởi tạo offset ban đầu
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

        // --- PHẦN 1: XỬ LÝ LẬT KIẾM (FLIP) - [THÊM MỚI] ---
        HandleSwordFlip();

        // --- PHẦN 2: DI CHUYỂN BÁM THEO ---
        // 1. Tính toán vị trí đích (Dùng currentOffset để lật sang 2 bên)
        Vector3 targetPosition = playerTransform.position + currentOffset;

        // 2. Tạo hiệu ứng nhấp nhô lên xuống nhẹ nhàng
        float insideFloat = Mathf.Sin(Time.time * floatSpeed) * floatAmount;
        targetPosition.y += insideFloat;

        // 3. Đuổi theo Player mượt mà (Dùng SmoothDamp)
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, 1f / followSpeed);
    }

    /// <summary>
    /// Xử lý lật thanh kiếm và đổi vị trí offset dựa trên hướng di chuyển của Player.
    /// Dùng toán học để mượt hơn `localRotation = x`.
    /// </summary>
    void HandleSwordFlip()
    {
        Rigidbody2D playerRb = playerTransform.GetComponent<Rigidbody2D>();
        if (playerRb == null) return;

        float playerHorizontalVelocity = playerRb.linearVelocity.x;

        // Lấy scale hiện tại của con kiếm
        Vector3 swordScale = transform.localScale;

        if (playerHorizontalVelocity > 0.1f) // Player đi sang PHẢI
        {
            // Đảm bảo trục X của scale luôn DƯƠNG (không bị âm)
            swordScale.x = Mathf.Abs(swordScale.x);
            transform.localScale = swordScale;

            // Đẩy Offset ra sau lưng bên trái
            currentOffset = new Vector3(-Mathf.Abs(offset.x), offset.y, offset.z);
        }
        else if (playerHorizontalVelocity < -0.1f) // Player đi sang TRÁI
        {
            // Đảm bảo trục X của scale luôn ÂM để lật hình hình ảnh
            swordScale.x = -Mathf.Abs(swordScale.x);
            transform.localScale = swordScale;

            // Đẩy Offset ra sau lưng bên phải
            currentOffset = new Vector3(Mathf.Abs(offset.x), offset.y, offset.z);
        }
        // Bỏ cái đoạn return khi đứng yên đi để offset luôn được cập nhật chuẩn xác!
    }
}