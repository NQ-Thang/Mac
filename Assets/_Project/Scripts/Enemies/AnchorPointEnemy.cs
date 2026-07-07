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

    public bool IsSwordStuck => isSwordStuck;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // CHỈ XỬ LÝ GĂM KIẾM KHI KIẾM BAY TRÚNG
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

                // Găm cây kiếm làm con trực tiếp của Quái Neo
                collision.transform.SetParent(transform);

                Debug.Log("Kiếm đã găm vào điểm Neo!");
            }
        }
    }

    public void ExecuteAnchorKill()
    {
        if (stuckSword != null)
        {
            stuckSword.transform.SetParent(null); // Giải phóng kiếm trước khi chết để tránh lỗi tham chiếu
        }
        Destroy(gameObject);
    }

    public float GetBounceForce() => bounceForceY;
}