using System.Collections.Generic;
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
    public float detectionRange = 26f;
    public float preferredShootDistance = 5.5f;
    public float closePressureDistance = 2.5f;
    public float rushStopDistance = 1.35f;
    public bool canJumpObstacles = true;
    public float jumpForce = 7.2f;
    public float jumpCooldown = 0.9f;
    public float stuckJumpInterval = 0.45f;
    public float obstacleJumpClearanceHeight = 1.05f;
    public float obstacleLandingProbeDistance = 0.95f;
    public bool canDropToReachPlayer = true;
    public float dropWhenPlayerBelowBy = 0.85f;
    public float dropHorizontalWindow = 14f;
    public bool useWaypointNavigation = true;
    public float repathInterval = 0.65f;
    public float waypointReachDistance = 0.55f;
    public float verticalRouteThreshold = 0.75f;
    public float separationRadius = 1.1f;
    public float separationStrength = 0.55f;

    public bool IsMoving { get; private set; }
    public Vector2 Velocity { get { return rb != null ? rb.linearVelocity : Vector2.zero; } }
    public bool IsUsingWaypointRoute { get; private set; }
    public Vector2 CurrentWaypointTarget { get; private set; }
    public int FacingDirection { get { return direction; } }

    private int direction = -1;
    private Damageable damageable;
    private EnemyShooter2D shooter;
    private Rigidbody2D rb;
    private Transform player;
    private EnemyWaypointGraph2D waypointGraph;
    private List<EnemyWaypointNode2D> waypointPath;
    private int waypointIndex;
    private float nextRepathTime;
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
        enemyLayer = enemyLayer.value == 0 ? LayerMask.GetMask("Enemy") : enemyLayer;
        lastPosition = transform.position;

        PlayerController2D foundPlayer = FindAnyObjectByType<PlayerController2D>();
        if (foundPlayer != null)
            player = foundPlayer.transform;

        waypointGraph = EnemyWaypointGraph2D.Active;
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
        bool hasWaypointTarget = TryGetWaypointTarget(hasWallAhead, hasGroundAhead, out Vector2 waypointTarget);
        IsUsingWaypointRoute = hasWaypointTarget;
        CurrentWaypointTarget = waypointTarget;

        if (hasWaypointTarget)
        {
            int routeDirection = waypointTarget.x >= transform.position.x ? 1 : -1;
            if (Mathf.Abs(waypointTarget.x - transform.position.x) > 0.15f && routeDirection != direction)
                FaceDirection(routeDirection);

            if (grounded && hasWallAhead && CanJumpObstacleAhead())
                TryJumpObstacle(false);
        }

        if (!hasGroundAhead && grounded && !CanUseWaypointDrop(waypointTarget, hasWaypointTarget) && !ShouldDropTowardLowerPlayer())
        {
            StopHorizontalMovement();
            return;
        }

        if (hasWallAhead && grounded && CanJumpObstacleAhead())
        {
            TryJumpObstacle(true);
        }

        TrackStuckState();
        if (stuckTimer >= stuckJumpInterval && grounded && hasWallAhead && CanJumpObstacleAhead())
        {
            TryJumpObstacle(true);
            stuckTimer = 0f;
        }

        if (shouldHoldForShot && !hasWallAhead && !hasWaypointTarget)
        {
            StopHorizontalMovement();
            return;
        }

        if (!hasWaypointTarget && IsCloseToPlayer())
        {
            StopHorizontalMovement();
            return;
        }

        MoveHorizontal(GetSeparatedMove(direction));
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

    private bool TryGetWaypointTarget(bool hasWallAhead, bool hasGroundAhead, out Vector2 waypointTarget)
    {
        waypointTarget = transform.position;

        if (!useWaypointNavigation || player == null)
            return false;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer > detectionRange)
            return false;

        float heightDifference = Mathf.Abs(player.position.y - transform.position.y);
        bool needsRoute = hasWallAhead || !hasGroundAhead || heightDifference > verticalRouteThreshold || !HasClearDirectHorizontalRoute();
        if (!needsRoute)
            return false;

        if (waypointGraph == null)
            waypointGraph = EnemyWaypointGraph2D.Active;

        if (waypointGraph == null)
            return false;

        if (waypointPath == null || waypointPath.Count == 0 || Time.time >= nextRepathTime)
        {
            waypointPath = waypointGraph.FindPath(transform.position, player.position);
            waypointIndex = 0;
            nextRepathTime = Time.time + repathInterval;
        }

        if (waypointPath == null || waypointPath.Count == 0)
            return false;

        while (waypointIndex < waypointPath.Count - 1 && Vector2.Distance(transform.position, waypointPath[waypointIndex].Position) <= waypointReachDistance)
            waypointIndex++;

        EnemyWaypointNode2D node = waypointPath[Mathf.Clamp(waypointIndex, 0, waypointPath.Count - 1)];
        if (node == null)
            return false;

        waypointTarget = node.Position;
        return true;
    }

    private bool CanUseWaypointDrop(Vector2 waypointTarget, bool hasWaypointTarget)
    {
        if (!hasWaypointTarget)
            return false;

        return waypointTarget.y < transform.position.y - 0.35f;
    }

    private bool ShouldDropTowardLowerPlayer()
    {
        if (!canDropToReachPlayer || player == null)
            return false;

        Vector2 toPlayer = player.position - transform.position;
        if (toPlayer.y > -dropWhenPlayerBelowBy)
            return false;

        if (Mathf.Abs(toPlayer.x) > dropHorizontalWindow)
            return false;

        return Mathf.Sign(toPlayer.x) == direction || Mathf.Abs(toPlayer.x) < 0.25f;
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

    private bool IsCloseToPlayer()
    {
        if (player == null)
            return false;

        float horizontalDistance = Mathf.Abs(player.position.x - transform.position.x);
        float verticalDistance = Mathf.Abs(player.position.y - transform.position.y);
        return horizontalDistance <= rushStopDistance && verticalDistance <= 1.1f;
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

    private void MoveHorizontal(float moveDirection)
    {
        IsMoving = true;

        if (rb != null)
            rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);
        else
            transform.Translate(Vector2.right * moveDirection * moveSpeed * Time.deltaTime);
    }

    private float GetSeparatedMove(int desiredDirection)
    {
        if (enemyLayer.value == 0)
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
                dx = Random.value > 0.5f ? 0.03f : -0.03f;

            crowdBias += Mathf.Sign(dx) * separationStrength;
        }

        return Mathf.Clamp(desiredDirection + crowdBias, -1f, 1f);
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

    private bool CanJumpObstacleAhead()
    {
        if (wallCheck == null || obstacleLayer.value == 0)
            return false;

        Vector2 obstaclePoint = wallCheck.position;
        Vector2 clearPoint = obstaclePoint + Vector2.up * obstacleJumpClearanceHeight;
        if (Physics2D.OverlapCircle(clearPoint, checkRadius, obstacleLayer) != null)
            return false;

        Vector2 landingOrigin = (Vector2)transform.position + new Vector2(direction * obstacleLandingProbeDistance, obstacleJumpClearanceHeight + 0.25f);
        RaycastHit2D landingHit = Physics2D.Raycast(landingOrigin, Vector2.down, obstacleJumpClearanceHeight + 0.75f, groundLayer);
        if (landingHit.collider == null || landingHit.collider.isTrigger)
            return false;

        Vector2 bodyClearPoint = landingHit.point + Vector2.up * 0.65f;
        return Physics2D.OverlapCircle(bodyClearPoint, checkRadius, obstacleLayer) == null;
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

    private bool HasClearDirectHorizontalRoute()
    {
        if (player == null || obstacleLayer.value == 0)
            return true;

        Vector2 origin = transform.position + Vector3.up * 0.05f;
        Vector2 target = new Vector2(player.position.x, transform.position.y + 0.05f);
        Vector2 toTarget = target - origin;
        float distance = Mathf.Abs(toTarget.x);
        if (distance <= 0.35f)
            return true;

        RaycastHit2D hit = Physics2D.Raycast(origin, new Vector2(Mathf.Sign(toTarget.x), 0f), distance, obstacleLayer);
        return hit.collider == null || hit.collider.GetComponent<PlatformEffector2D>() != null;
    }

    private void FaceDirection(int newDirection)
    {
        direction = newDirection >= 0 ? 1 : -1;
        float xScale = Mathf.Abs(transform.localScale.x) * direction;
        transform.localScale = new Vector3(xScale, transform.localScale.y, transform.localScale.z);
    }

    private void Flip()
    {
        FaceDirection(-direction);
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

        if (IsUsingWaypointRoute)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(CurrentWaypointTarget, 0.3f);
            Gizmos.DrawLine(transform.position, CurrentWaypointTarget);
        }
    }
}
