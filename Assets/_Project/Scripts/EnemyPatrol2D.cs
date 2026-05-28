using UnityEngine;

public class EnemyPatrol2D : MonoBehaviour
{
    public float moveSpeed = 2f;
    public Transform groundCheck;
    public Transform wallCheck;
    public float checkRadius = 0.15f;
    public LayerMask groundLayer;

    private int direction = -1;
    private Damageable damageable;

    private void Awake()
    {
        damageable = GetComponent<Damageable>();
    }

    private void Update()
    {
        if (damageable != null && damageable.IsDead)
            return;

        transform.Translate(Vector2.right * direction * moveSpeed * Time.deltaTime);

        if (groundCheck == null || wallCheck == null) return;

        bool hasGroundAhead = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        bool hasWallAhead = Physics2D.OverlapCircle(wallCheck.position, checkRadius, groundLayer);

        if (!hasGroundAhead || hasWallAhead) Flip();
    }

    private void Flip()
    {
        direction *= -1;
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null) Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        if (wallCheck != null) Gizmos.DrawWireSphere(wallCheck.position, checkRadius);
    }
}
