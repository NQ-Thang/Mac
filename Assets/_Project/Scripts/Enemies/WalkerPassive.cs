using UnityEngine;

public class WalkerPassive : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 2f;
    protected bool movingRight = true;

    [Header("Checks")]
    public Transform groundCheck;
    public Transform wallCheck;
    public float checkDistance = 0.5f;
    public LayerMask obstacleLayer;

    [Header("Visual")]
    [SerializeField] protected Transform spriteTransform;

    protected Rigidbody2D rb;

    // Lưu vị trí ban đầu của GroundCheck và WallCheck
    private Vector3 groundCheckLocalPos;
    private Vector3 wallCheckLocalPos;

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (groundCheck != null)
            groundCheckLocalPos = groundCheck.localPosition;

        if (wallCheck != null)
            wallCheckLocalPos = wallCheck.localPosition;
    }

    protected virtual void FixedUpdate()
    {
        HandleMovement();
        HandleObstacles();
    }

    protected virtual void HandleMovement()
    {
        float moveDir = movingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(moveDir * walkSpeed, rb.linearVelocity.y);
    }

    protected virtual void HandleObstacles()
    {
        if (!groundCheck || !wallCheck) return;

        bool isGroundedAhead =
            Physics2D.Raycast(
                groundCheck.position,
                Vector2.down,
                checkDistance,
                obstacleLayer);

        Vector2 forward =
            movingRight ? Vector2.right : Vector2.left;

        bool isWallAhead =
            Physics2D.Raycast(
                wallCheck.position,
                forward,
                checkDistance,
                obstacleLayer);

        if (!isGroundedAhead || isWallAhead)
        {
            ChangeDirection();
        }
    }

    protected virtual void ChangeDirection()
    {
        movingRight = !movingRight;

        // Lật sprite
        if (spriteTransform != null)
        {
            Vector3 scale = spriteTransform.localScale;
            scale.x = Mathf.Abs(scale.x) * (movingRight ? 1 : -1);
            spriteTransform.localScale = scale;
        }

        // Di chuyển GroundCheck
        if (groundCheck != null)
        {
            Vector3 pos = groundCheckLocalPos;
            pos.x = Mathf.Abs(pos.x) * (movingRight ? 1 : -1);
            groundCheck.localPosition = pos;
        }

        // Di chuyển WallCheck
        if (wallCheck != null)
        {
            Vector3 pos = wallCheckLocalPos;
            pos.x = Mathf.Abs(pos.x) * (movingRight ? 1 : -1);
            wallCheck.localPosition = pos;
        }
    }

    private void OnDrawGizmos()
    {
        if (!groundCheck || !wallCheck) return;

        Gizmos.color = Color.cyan;

        Gizmos.DrawLine(
            groundCheck.position,
            groundCheck.position + Vector3.down * checkDistance);

        Vector3 forward = movingRight ? Vector3.right : Vector3.left;

        Gizmos.DrawLine(
            wallCheck.position,
            wallCheck.position + forward * checkDistance);
    }
}