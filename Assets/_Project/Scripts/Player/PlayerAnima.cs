using System;
using UnityEngine;

public class PlayerAnima : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxSoul = 99;
    [SerializeField] private int soulPerHit = 11; // Lượng linh lực nhận được khi chém trúng quái

    [Header("Current State")]
    [SerializeField] private int currentSoul = 0;

    // Sự kiện thông báo khi Linh lực thay đổi (dùng cho UI cập nhật sau này)
    // Parameter: (int currentSoul, int maxSoul)
    public event Action<int, int> OnSoulChanged;

    public int CurrentSoul => currentSoul;
    public int MaxSoul => maxSoul;

    private void Start()
    {
        // Khởi tạo linh lực ban đầu (bằng 0 hoặc max tùy bạn chọn)
        currentSoul = 0;
        OnSoulChanged?.Invoke(currentSoul, maxSoul);
    }

    /// <summary>
    /// Cộng thêm linh lực khi chém trúng kẻ địch.
    /// </summary>
    public void AddSoul(int amount)
    {
        if (amount <= 0) return;

        currentSoul = Mathf.Clamp(currentSoul + amount, 0, maxSoul);
        OnSoulChanged?.Invoke(currentSoul, maxSoul);
    }

    /// <summary>
    /// overload hỗ trợ cộng mặc định theo soulPerHit
    /// </summary>
    public void AddSoulFromHit()
    {
        AddSoul(soulPerHit);
    }

    /// <summary>
    /// Kiểm tra xem người chơi có đủ linh lực để dùng kỹ năng không.
    /// </summary>
    public bool HasEnoughSoul(int amount)
    {
        return currentSoul >= amount;
    }

    /// <summary>
    /// Khấu trừ linh lực khi dùng kỹ năng.
    /// </summary>
    /// <returns>True nếu trừ thành công, False nếu không đủ linh lực.</returns>
    public bool ConsumeSoul(int amount)
    {
        if (!HasEnoughSoul(amount))
        {
            return false;
        }

        currentSoul -= amount;
        OnSoulChanged?.Invoke(currentSoul, maxSoul);
        return true;
    }
}
