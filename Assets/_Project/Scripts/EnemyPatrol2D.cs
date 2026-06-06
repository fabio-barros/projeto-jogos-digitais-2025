using UnityEngine;

public class EnemyPatrol2D : MonoBehaviour
{
    public float moveSpeed = 2f;
    public int startingDirection = -1;
    public Transform groundCheck;
    public Transform wallCheck;
    public float checkRadius = 0.15f;
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;
    public float detectionRange = 11f;
    public float preferredShootDistance = 5.5f;
    public float closePressureDistance = 2.5f;
    public bool canJumpObstacles = true;
    public float jumpForce = 5.5f;
    public float jumpCooldown = 0.9f;
    public float stuckJumpInterval = 0.45f;

    public bool IsMoving { get; private set; }
    public Vector2 Velocity { get { return rb != null ? rb.linearVelocity : Vector2.zero; } }

    private int direction = -1;
    private Damageable damageable;
    private EnemyShooter2D shooter;
    private Rigidbody2D rb;
    private Transform player;
    private float nextJumpTime;
    private float stuckTimer;
    private Vector2 lastPosition;

    private void Awake()
    {
        damageable = GetComponent<Damageable>();
        shooter = GetComponent<EnemyShooter2D>();
        rb = GetComponent<Rigidbody2D>();
        direction = startingDirection >= 0 ? 1 : -1;
        obstacleLayer = obstacleLayer.value == 0 ? groundLayer : obstacleLayer;
        lastPosition = transform.position;

        PlayerController2D foundPlayer = FindAnyObjectByType<PlayerController2D>();
        if (foundPlayer != null)
            player = foundPlayer.transform;

        FaceDirection(direction);
    }

    private void Update()
    {
        if (damageable != null && damageable.IsDead)
        {
            StopHorizontalMovement();
            return;
        }

        int desiredDirection = GetDesiredDirection();
        if (desiredDirection != direction)
            FaceDirection(desiredDirection);

        bool hasWallAhead = HasBlockingWallAhead();
        bool hasGroundAhead = HasGroundAhead();
        bool grounded = IsGrounded();
        bool shouldHoldForShot = ShouldHoldForShot();

        if (!hasGroundAhead && grounded)
        {
            StopHorizontalMovement();
            return;
        }

        if (hasWallAhead && grounded)
        {
            TryJumpObstacle(true);
        }

        TrackStuckState();
        if (stuckTimer >= stuckJumpInterval && grounded)
        {
            TryJumpObstacle(true);
            stuckTimer = 0f;
        }

        if (shouldHoldForShot && !hasWallAhead)
        {
            StopHorizontalMovement();
            return;
        }

        MoveHorizontal(direction);
    }

    private int GetDesiredDirection()
    {
        if (player == null)
            return direction;

        float toPlayerX = player.position.x - transform.position.x;
        if (Mathf.Abs(toPlayerX) > detectionRange)
            return direction;

        if (Mathf.Abs(toPlayerX) < 0.15f)
            return direction;

        return toPlayerX >= 0f ? 1 : -1;
    }

    private bool ShouldHoldForShot()
    {
        if (player == null || shooter == null)
            return false;

        Vector2 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;
        if (distance > preferredShootDistance || distance < closePressureDistance)
            return false;

        return shooter.HasClearLineOfSight(player);
    }

    private void TrackStuckState()
    {
        Vector2 currentPosition = transform.position;
        float movedX = Mathf.Abs(currentPosition.x - lastPosition.x);
        bool tryingToMove = !IsMoving || Mathf.Abs(direction) > 0;
        bool blockedWhileTrying = tryingToMove && movedX < 0.005f && Mathf.Abs(Velocity.y) < 0.1f && HasBlockingWallAhead();
        stuckTimer = blockedWhileTrying ? stuckTimer + Time.deltaTime : 0f;
        lastPosition = currentPosition;
    }

    private void MoveHorizontal(int moveDirection)
    {
        IsMoving = true;

        if (rb != null)
            rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);
        else
            transform.Translate(Vector2.right * moveDirection * moveSpeed * Time.deltaTime);
    }

    private void StopHorizontalMovement()
    {
        IsMoving = false;

        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private bool HasGroundAhead()
    {
        if (groundCheck == null)
            return true;

        Collider2D hit = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        return hit != null;
    }

    private bool TryJumpObstacle(bool force)
    {
        if (!canJumpObstacles || rb == null || !IsGrounded())
            return false;

        if (!force && Time.time < nextJumpTime)
            return false;

        if (Time.time < nextJumpTime)
            return false;

        rb.linearVelocity = new Vector2(direction * moveSpeed, jumpForce);
        nextJumpTime = Time.time + jumpCooldown;
        IsMoving = true;
        return true;
    }

    private bool TryJumpObstacle()
    {
        return TryJumpObstacle(false);
    }

    private bool IsGrounded()
    {
        Vector2 origin = (Vector2)transform.position + Vector2.down * 0.62f;
        return Physics2D.OverlapCircle(origin, checkRadius, groundLayer) != null;
    }

    private bool HasBlockingWallAhead()
    {
        if (wallCheck == null)
            return false;

        Collider2D[] hits = Physics2D.OverlapCircleAll(wallCheck.position, checkRadius, obstacleLayer);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || hit.transform == transform || hit.isTrigger)
                continue;

            // One-way platforms are valid footing, but should not make side-moving enemies turn around.
            if (hit.GetComponent<PlatformEffector2D>() != null)
                continue;

            return true;
        }

        return false;
    }

    private void FaceDirection(int newDirection)
    {
        direction = newDirection >= 0 ? 1 : -1;
        float xScale = Mathf.Abs(transform.localScale.x) * (direction > 0 ? -1f : 1f);
        transform.localScale = new Vector3(xScale, transform.localScale.y, transform.localScale.z);
    }

    private void Flip()
    {
        FaceDirection(-direction);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null) Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        if (wallCheck != null) Gizmos.DrawWireSphere(wallCheck.position, checkRadius);
    }
}
