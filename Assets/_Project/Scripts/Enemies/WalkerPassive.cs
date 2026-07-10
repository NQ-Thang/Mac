using UnityEngine;

public class WalkerPassive : MonoBehaviour
{

    public float walkSpeed = 2f;
    protected bool movingRight = true;

    public Transform groundCheck;
    public Transform wallCheck;
    public float checkDistance = 0.5f;
    public LayerMask obstacleLayer;

    protected Rigidbody2D rb;
    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if(rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
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

        bool isGroundedAhead = Physics2D.Raycast(groundCheck.position, Vector2.down, checkDistance, obstacleLayer);
        Vector2 forwardDirection = movingRight ? Vector2.right : Vector2.left;
        bool isWallAhead = Physics2D.Raycast(wallCheck.position, forwardDirection, checkDistance, obstacleLayer);

        if (!isGroundedAhead || isWallAhead)
        {
            ChangeDirection();
        }
    }

    protected virtual void ChangeDirection()
    {
        movingRight = !movingRight;

        // Lật ngược Sprite của quái lại bằng cách đổi dấu trục X của LocalScale
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
    }

    private void OnDrawGizmos()
    {
        if (!groundCheck || !wallCheck)
            return;

        Gizmos.color = Color.cyan;

        // Tia check vực
        Gizmos.DrawLine(
            groundCheck.position,
            groundCheck.position + Vector3.down * checkDistance
        );

        // Tia check tường
        Vector3 forward = movingRight ? Vector3.right : Vector3.left;

        Gizmos.DrawLine(
            wallCheck.position,
            wallCheck.position + forward * checkDistance
        );
    }
}
