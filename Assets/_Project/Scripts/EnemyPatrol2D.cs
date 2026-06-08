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
    public LayerMask enemyLayer;
    public float detectionRange = 18f;
    public float preferredShootDistance = 7f;
    public float closePressureDistance = 1.25f;
    public float rushStopDistance = 1.15f;
    public bool canJumpObstacles;
    public float jumpForce = 7.2f;
    public float jumpCooldown = 0.9f;
    public float stuckJumpInterval = 0.45f;
    public float obstacleJumpClearanceHeight = 1.05f;
    public float obstacleLandingProbeDistance = 0.95f;
    public bool canDropToReachPlayer = true;
    public float dropWhenPlayerBelowBy = 0.85f;
    public float dropHorizontalWindow = 14f;
    public bool useWaypointNavigation;
    public float repathInterval = 0.65f;
    public float waypointReachDistance = 0.55f;
    public float verticalRouteThreshold = 0.75f;
    public float separationRadius = 0.75f;
    public float separationStrength = 0.35f;
    public float minimumEnemyScaleX = 0.9f;
    public float minimumEnemyScaleY = 1.18f;
    public bool moveOnlyWhenPlayerDetected = true;
    public bool useRunNGunStationaryBehaviour = false;
    public bool onlyEngageOnSameLevel = true;
    public float sameLevelVerticalTolerance = 0.85f;
    public bool onlyEngageWhenPlayerInFront = true;
    public float minimumPlayerDistance = 3f;
    public float patrolRadius = 2f;
    public bool stopAtLedges = true;
    public float ledgeProbeForwardDistance = 0.55f;
    public float ledgeProbeDownDistance = 1.45f;

    public bool IsMoving { get; private set; }
    public Vector2 Velocity { get { return rb != null ? rb.linearVelocity : Vector2.zero; } }
    public bool IsUsingWaypointRoute { get { return false; } }
    public Vector2 CurrentWaypointTarget { get { return player != null ? (Vector2)player.position : (Vector2)transform.position; } }
    public int FacingDirection { get { return direction; } }

    private int direction = -1;
    private Damageable damageable;
    private EnemyShooter2D shooter;
    private Rigidbody2D rb;
    private Transform player;
    private float nextJumpTime;
    private Vector2 homePosition;

    private void Awake()
    {
        damageable = GetComponent<Damageable>();
        shooter = GetComponent<EnemyShooter2D>();
        rb = GetComponent<Rigidbody2D>();
        direction = startingDirection >= 0 ? 1 : -1;
        homePosition = transform.position;
        obstacleLayer = obstacleLayer.value == 0 ? groundLayer : obstacleLayer;
        enemyLayer = enemyLayer.value == 0 ? LayerMask.GetMask("Enemy") : enemyLayer;
        NormalizeScale();

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

        if (player == null)
        {
            StopHorizontalMovement();
            return;
        }

        Vector2 toPlayer = player.position - transform.position;
        float horizontalDistance = Mathf.Abs(toPlayer.x);
        float verticalDistance = Mathf.Abs(toPlayer.y);
        bool sameLevel = !onlyEngageOnSameLevel || verticalDistance <= sameLevelVerticalTolerance;
        bool playerInFront = !onlyEngageWhenPlayerInFront || Mathf.Sign(toPlayer.x) == direction;
        bool playerDetected = horizontalDistance <= detectionRange && sameLevel && playerInFront;

        if (!playerDetected)
        {
            PatrolNearHome();
            return;
        }

        int desiredDirection = toPlayer.x >= 0f ? 1 : -1;
        FaceDirection(desiredDirection);

        if (useRunNGunStationaryBehaviour)
        {
            StopHorizontalMovement();
            return;
        }

        bool grounded = IsGrounded();
        bool hasWallAhead = HasBlockingWallAhead();
        bool hasGroundAhead = HasGroundAhead();
        bool clearShot = shooter != null && shooter.HasClearLineOfSight(player);

        if (ShouldStopNearPlayer(horizontalDistance, verticalDistance))
        {
            StopHorizontalMovement();
            return;
        }

        if (ShouldStopToShoot(horizontalDistance, verticalDistance, clearShot))
        {
            StopHorizontalMovement();
            return;
        }

        if (grounded && hasWallAhead)
        {
            if (canJumpObstacles && CanJumpObstacleAhead())
                TryJumpObstacle();

            StopHorizontalMovement();
            return;
        }

        if (grounded && !hasGroundAhead && !ShouldStepOffLedgeTowardPlayer(toPlayer))
        {
            StopHorizontalMovement();
            return;
        }

        MoveHorizontal(GetSeparatedMove(direction));
    }

    private void PatrolNearHome()
    {
        if (useRunNGunStationaryBehaviour || moveOnlyWhenPlayerDetected && patrolRadius <= 0f)
        {
            StopHorizontalMovement();
            return;
        }

        bool grounded = IsGrounded();
        bool hasWallAhead = HasBlockingWallAhead();
        bool hasGroundAhead = HasGroundAhead();
        float minX = homePosition.x - Mathf.Max(0f, patrolRadius);
        float maxX = homePosition.x + Mathf.Max(0f, patrolRadius);

        if (transform.position.x <= minX)
            FaceDirection(1);
        else if (transform.position.x >= maxX)
            FaceDirection(-1);

        if (grounded && (hasWallAhead || !hasGroundAhead))
        {
            FaceDirection(-direction);
            StopHorizontalMovement();
            return;
        }

        MoveHorizontal(GetSeparatedMove(direction) * 0.65f);
    }

    private bool ShouldStopToShoot(float horizontalDistance, float verticalDistance, bool clearShot)
    {
        if (shooter == null || !clearShot)
            return false;

        if (onlyEngageOnSameLevel && verticalDistance > sameLevelVerticalTolerance)
            return false;

        return horizontalDistance <= Mathf.Min(preferredShootDistance, minimumPlayerDistance);
    }

    private bool ShouldStopNearPlayer(float horizontalDistance, float verticalDistance)
    {
        float stopDistance = Mathf.Max(rushStopDistance, minimumPlayerDistance);
        float verticalLimit = onlyEngageOnSameLevel ? sameLevelVerticalTolerance : 1.1f;
        return horizontalDistance <= stopDistance && verticalDistance <= verticalLimit;
    }

    private bool ShouldStepOffLedgeTowardPlayer(Vector2 toPlayer)
    {
        if (!canDropToReachPlayer)
            return false;

        if (toPlayer.y > -dropWhenPlayerBelowBy)
            return false;

        if (Mathf.Abs(toPlayer.x) > dropHorizontalWindow)
            return false;

        return Mathf.Sign(toPlayer.x) == direction;
    }

    private void MoveHorizontal(float moveDirection)
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

    private float GetSeparatedMove(int desiredDirection)
    {
        if (enemyLayer.value == 0 || separationRadius <= 0f)
            return desiredDirection;

        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, separationRadius, enemyLayer);
        float crowdBias = 0f;
        for (int i = 0; i < nearbyEnemies.Length; i++)
        {
            Collider2D nearby = nearbyEnemies[i];
            if (nearby == null || nearby.transform == transform || nearby.isTrigger)
                continue;

            float dx = transform.position.x - nearby.transform.position.x;
            if (Mathf.Abs(dx) < 0.03f)
                dx = desiredDirection > 0 ? -0.03f : 0.03f;

            crowdBias += Mathf.Sign(dx) * separationStrength;
        }

        return Mathf.Clamp(desiredDirection + crowdBias, -1f, 1f);
    }

    private bool HasGroundAhead()
    {
        if (groundCheck == null || groundLayer.value == 0)
            return true;

        if (!stopAtLedges)
            return Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer) != null;

        Vector2 ahead = (Vector2)transform.position + new Vector2(direction * ledgeProbeForwardDistance, 0.08f);
        RaycastHit2D hit = Physics2D.Raycast(ahead, Vector2.down, ledgeProbeDownDistance, groundLayer);
        return hit.collider != null && !hit.collider.isTrigger;
    }

    private bool HasBlockingWallAhead()
    {
        if (wallCheck == null || obstacleLayer.value == 0)
            return false;

        Collider2D[] hits = Physics2D.OverlapCircleAll(wallCheck.position, checkRadius, obstacleLayer);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || hit.transform == transform || hit.isTrigger)
                continue;

            if (hit.GetComponent<PlatformEffector2D>() != null)
                continue;

            return true;
        }

        return false;
    }

    private bool IsGrounded()
    {
        Vector2 origin = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position + Vector2.down * 0.58f;
        return Physics2D.OverlapCircle(origin, checkRadius, groundLayer) != null;
    }

    private bool CanJumpObstacleAhead()
    {
        if (wallCheck == null || obstacleLayer.value == 0 || groundLayer.value == 0)
            return false;

        Vector2 obstaclePoint = wallCheck.position;
        Vector2 clearPoint = obstaclePoint + Vector2.up * obstacleJumpClearanceHeight;
        if (Physics2D.OverlapCircle(clearPoint, checkRadius, obstacleLayer) != null)
            return false;

        Vector2 landingOrigin = (Vector2)transform.position + new Vector2(direction * obstacleLandingProbeDistance, obstacleJumpClearanceHeight + 0.35f);
        RaycastHit2D landingHit = Physics2D.Raycast(landingOrigin, Vector2.down, obstacleJumpClearanceHeight + 0.9f, groundLayer);
        if (landingHit.collider == null || landingHit.collider.isTrigger)
            return false;

        return true;
    }

    private bool TryJumpObstacle()
    {
        if (!canJumpObstacles || rb == null || Time.time < nextJumpTime || !IsGrounded())
            return false;

        rb.linearVelocity = new Vector2(direction * moveSpeed, jumpForce);
        nextJumpTime = Time.time + jumpCooldown;
        IsMoving = true;
        return true;
    }

    private void FaceDirection(int newDirection)
    {
        direction = newDirection >= 0 ? 1 : -1;
        float xScale = Mathf.Abs(transform.localScale.x) * direction;
        transform.localScale = new Vector3(xScale, transform.localScale.y, transform.localScale.z);
    }

    private void NormalizeScale()
    {
        Vector3 scale = transform.localScale;
        float sign = scale.x < 0f ? -1f : 1f;
        float x = minimumEnemyScaleX > 0f ? Mathf.Max(Mathf.Abs(scale.x), minimumEnemyScaleX) : Mathf.Abs(scale.x);
        float y = minimumEnemyScaleY > 0f ? Mathf.Max(Mathf.Abs(scale.y), minimumEnemyScaleY) : Mathf.Abs(scale.y);
        transform.localScale = new Vector3(x * sign, y, scale.z);
    }

    private void OnDrawGizmos()
    {
        if (!GameplayDebugOverlay2D.DrawEnemyAI)
            return;

        DrawDebugGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        DrawDebugGizmos();
    }

    private void DrawDebugGizmos()
    {
        Gizmos.color = Color.green;
        if (groundCheck != null)
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);

        Gizmos.color = Color.red;
        if (wallCheck != null)
            Gizmos.DrawWireSphere(wallCheck.position, checkRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * direction * 1.1f);

        Gizmos.color = Color.yellow;
        Vector3 left = new Vector3(homePosition.x - patrolRadius, transform.position.y, transform.position.z);
        Vector3 right = new Vector3(homePosition.x + patrolRadius, transform.position.y, transform.position.z);
        Gizmos.DrawLine(left, right);
    }
}
