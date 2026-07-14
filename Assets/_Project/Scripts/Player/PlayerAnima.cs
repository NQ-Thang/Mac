using System;
using UnityEngine;

public class PlayerAnima : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxAnima = 100;
    [SerializeField] private int animaPerHit = 20; // Lượng linh lực nhận được khi chém trúng quái

    [Header("Current State")]
    [SerializeField] private int currentAnima = 0;

    // Sự kiện thông báo khi Linh lực thay đổi (dùng cho UI cập nhật sau này)
    // Parameter: (int currentAnima, int maxAnima)
    public event Action<int, int> OnAnimaChanged;

    public int CurrentAnima => currentAnima;
    public int MaxAnima => maxAnima;

    private void Start()
    {
        // Khởi tạo linh lực ban đầu (bằng 0 hoặc max tùy bạn chọn)
        currentAnima = 0;
        OnAnimaChanged?.Invoke(currentAnima, maxAnima);
    }

    /// <summary>
    /// Cộng thêm linh lực khi chém trúng kẻ địch.
    /// </summary>
    public void AddAnima(int amount)
    {
        if (amount <= 0) return;

        currentAnima = Mathf.Clamp(currentAnima + amount, 0, maxAnima); // chặn anima luôn trên 0 và dưới maxAnima
        OnAnimaChanged?.Invoke(currentAnima, maxAnima);
    }

    /// <summary>
    /// overload hỗ trợ cộng mặc định theo animaPerHit
    /// </summary>
    public void AddAnimaFromHit()
    {
        AddAnima(animaPerHit);
    }

    /// <summary>
    /// Kiểm tra xem người chơi có đủ linh lực để dùng kỹ năng không.
    /// </summary>
    public bool HasEnoughAnima(int amount)
    {
        return currentAnima >= amount;
    }

    /// <summary>
    /// Khấu trừ linh lực khi dùng kỹ năng.
    /// </summary>
    /// <returns>True nếu trừ thành công, False nếu không đủ linh lực.</returns>
    public bool ConsumeAnima(int amount)
    {
        if (!HasEnoughAnima (amount))
        {
            return false;
        }

        currentAnima -= amount;
        OnAnimaChanged?.Invoke(currentAnima, maxAnima);
        return true;
    }
}
