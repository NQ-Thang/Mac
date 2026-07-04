using UnityEngine;

public class AnchorPointEnemy : MonoBehaviour
{
    [Header("Bounce Settings")]
    [SerializeField] private float bounceForceY = 12f;

    [Header("Visual Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float penetrationDepth = 0.5f;

    private bool isSwordStuck = false;
    private GameObject stuckSword;

    // Getter công khai để Player check xem quái đã bị găm kiếm chưa
    public bool IsSwordStuck => isSwordStuck;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. XỬ LÝ GĂM KIẾM
        if (collision.CompareTag("Sword") && !isSwordStuck)
        {
            Sword swordScript = collision.GetComponent<Sword>();
            if (swordScript != null)
            {
                isSwordStuck = true;
                stuckSword = collision.gameObject;

                Rigidbody2D swordRb = collision.GetComponent<Rigidbody2D>();
                if (swordRb != null)
                {
                    swordRb.linearVelocity = Vector2.zero;
                    swordRb.bodyType = RigidbodyType2D.Kinematic;
                }

                Vector3 hitPoint = collision.transform.position;
                Vector3 centerPoint = transform.position;
                collision.transform.position = Vector3.Lerp(hitPoint, centerPoint, penetrationDepth);
                collision.transform.SetParent(transform);

                Debug.Log("Kiếm đã găm sâu vào điểm Neo!");
            }
        }

        // LƯU Ý: Phần xử lý va chạm với Player được chuyển sang script của Player 
        // để đảm bảo dọn dẹp dữ liệu hiển thị (Sprite) đồng bộ nhất.
    }

    public void ExecuteAnchorKill()
    {
        if (stuckSword != null)
        {
            stuckSword.transform.SetParent(null);
        }
        Destroy(gameObject);
    }

    public float GetBounceForce() => bounceForceY;
}