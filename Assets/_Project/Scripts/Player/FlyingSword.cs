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

    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        // Tự động tìm đối tượng có Tag là "Player" trong Scene, không cần kéo thả nữa!
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindWithTag("Player").transform;
        }
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

        // 1. Tính toán vị trí đích (Vị trí Player + Khoảng cách bù Offset)
        Vector3 targetPosition = playerTransform.position + offset;

        // 2. Tạo hiệu ứng nhấp nhô lên xuống nhẹ nhàng cho giống "kiếm bay"
        float insideFloat = Mathf.Sin(Time.time * floatSpeed) * floatAmount;
        targetPosition.y += insideFloat;

        // 3. Đuổi theo Player mượt mà (Dùng SmoothDamp mượt hơn Lerp nhiều)
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, 1f / followSpeed);
    }
}