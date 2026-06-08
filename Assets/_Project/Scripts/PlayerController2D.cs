using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    public float moveSpeed = 3.5f;
    public float jumpForce = 14f;
    public float dashSpeed = 18f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.8f;
    public float crouchMoveMultiplier = 0.5f;
    public float swimSpeed = 3f;
    public Vector2 crouchColliderSize = new Vector2(0.55f, 0.48f);
    public Vector2 crouchColliderOffset = new Vector2(0f, -0.445f);
    public bool keepFeetPlantedWhenCrouching = true;
    public float minimumCharacterScale = 1.18f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.16f;
    public float groundedCastDistance = 0.08f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private float horizontal;
    private Vector2 aimDirection = Vector2.right;
    private bool isGrounded;
    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private float originalGravityScale;
    private Vector3 originalScale;
    private int facingDirection = 1;
    private PlayerHealth playerHealth;
    private PhysicsMaterial2D noFrictionMaterial;
    private BoxCollider2D bodyCollider;
    private Vector2 standingColliderSize;
    private Vector2 standingColliderOffset;
    private bool isSwimming;

    public int FacingDirection { get { return facingDirection; } }
    public Vector2 AimDirection { get { return aimDirection; } }
    public bool IsGrounded { get { return isGrounded; } }
    public bool IsDashing { get { return isDashing; } }
    public bool IsSwimming { get { return isSwimming; } }
    public bool IsCrouching { get { return !isSwimming && RemnantInput.CrouchHeld() && isGrounded; } }
    public float HorizontalInput { get { return horizontal; } }
    public Vector2 Velocity { get { return rb != null ? rb.linearVelocity : Vector2.zero; } }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerHealth = GetComponent<PlayerHealth>();
        bodyCollider = GetComponent<BoxCollider2D>();
        originalGravityScale = rb.gravityScale;
        originalScale = transform.localScale;
        ConfigureOneWayTilemaps();

        if (bodyCollider != null)
        {
            standingColliderSize = bodyCollider.size;
            standingColliderOffset = bodyCollider.offset;
        }

        if (minimumCharacterScale > 0f && Mathf.Abs(originalScale.x) < minimumCharacterScale)
        {
            float facingSign = originalScale.x < 0f ? -1f : 1f;
            originalScale = new Vector3(minimumCharacterScale * facingSign, minimumCharacterScale, originalScale.z);
            transform.localScale = originalScale;
        }

        noFrictionMaterial = new PhysicsMaterial2D("Player_NoFriction");
        noFrictionMaterial.friction = 0f;
        noFrictionMaterial.bounciness = 0f;

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].sharedMaterial = noFrictionMaterial;
    }

    private void Update()
    {
        if (playerHealth != null && !playerHealth.CanAct)
        {
            horizontal = 0f;
            return;
        }

        horizontal = RemnantInput.MoveHorizontal();
        aimDirection = RemnantInput.AimDirection(facingDirection, transform.position);

        if (isSwimming)
        {
            isGrounded = false;
            ApplyCrouchCollider();
            return;
        }

        if (!RemnantInput.ShootHeld())
        {
            if (horizontal > 0)
            {
                FaceDirection(1);
            }
            else if (horizontal < 0)
            {
                FaceDirection(-1);
            }
        }

        isGrounded = CheckGrounded();

        ApplyCrouchCollider();

        if (RemnantInput.JumpDown() && isGrounded && !isDashing)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

        if (dashCooldownTimer > 0)
            dashCooldownTimer -= Time.deltaTime;

        if (RemnantInput.DashDown() && dashCooldownTimer <= 0 && !isDashing)
            StartDash();

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0) EndDash();
        }
    }

    private void FixedUpdate()
    {
        if (playerHealth != null && !playerHealth.CanAct)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isDashing)
        {
            rb.linearVelocity = new Vector2(facingDirection * dashSpeed, 0);
            return;
        }

        if (isSwimming)
        {
            rb.linearVelocity = new Vector2(horizontal * swimSpeed, RemnantInput.MoveVertical() * swimSpeed);
            return;
        }

        float activeMoveSpeed = IsCrouching ? moveSpeed * crouchMoveMultiplier : moveSpeed;
        rb.linearVelocity = new Vector2(horizontal * activeMoveSpeed, rb.linearVelocity.y);
    }

    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        rb.gravityScale = 0f;
    }

    private void EndDash()
    {
        isDashing = false;
        rb.gravityScale = originalGravityScale;
    }

    private void ApplyCrouchCollider()
    {
        if (bodyCollider == null)
            return;

        if (IsCrouching)
        {
            bodyCollider.size = crouchColliderSize;
            bodyCollider.offset = GetCrouchColliderOffset();
        }
        else
        {
            bodyCollider.size = standingColliderSize;
            bodyCollider.offset = standingColliderOffset;
        }
    }

    private Vector2 GetCrouchColliderOffset()
    {
        if (!keepFeetPlantedWhenCrouching)
            return crouchColliderOffset;

        float standingBottom = standingColliderOffset.y - standingColliderSize.y * 0.5f;
        float crouchOffsetY = standingBottom + crouchColliderSize.y * 0.5f;
        return new Vector2(crouchColliderOffset.x, crouchOffsetY);
    }

    private bool CheckGrounded()
    {
        if (groundLayer.value == 0)
            return false;

        if (groundCheck != null && Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null)
            return true;

        if (bodyCollider == null)
            return false;

        Bounds bounds = bodyCollider.bounds;
        Vector2 origin = new Vector2(bounds.center.x, bounds.min.y + 0.03f);
        Vector2 size = new Vector2(Mathf.Max(bounds.size.x - 0.08f, 0.12f), 0.08f);
        RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down, groundedCastDistance, groundLayer);
        return hit.collider != null;
    }

    private void ConfigureOneWayTilemaps()
    {
        Tilemap[] tilemaps = FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < tilemaps.Length; i++)
        {
            Tilemap tilemap = tilemaps[i];
            if (tilemap == null || tilemap.name != "Tilemap One-Way Platforms")
                continue;

            GameObject obj = tilemap.gameObject;
            int groundLayerIndex = LayerMask.NameToLayer("Ground");
            if (groundLayerIndex >= 0)
                obj.layer = groundLayerIndex;

            Vector3 position = obj.transform.localPosition;
            obj.transform.localPosition = new Vector3(Mathf.Round(position.x), Mathf.Round(position.y), position.z);
            obj.transform.localScale = Vector3.one;

            Rigidbody2D platformBody = obj.GetComponent<Rigidbody2D>();
            if (platformBody == null)
                platformBody = obj.AddComponent<Rigidbody2D>();
            platformBody.bodyType = RigidbodyType2D.Static;
            platformBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            TilemapCollider2D tilemapCollider = obj.GetComponent<TilemapCollider2D>();
            if (tilemapCollider == null)
                tilemapCollider = obj.AddComponent<TilemapCollider2D>();
            tilemapCollider.usedByComposite = true;
            tilemapCollider.usedByEffector = true;
            tilemapCollider.extrusionFactor = 0.03f;

            CompositeCollider2D composite = obj.GetComponent<CompositeCollider2D>();
            if (composite == null)
                composite = obj.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Outlines;
            composite.edgeRadius = 0.02f;
            composite.usedByEffector = true;

            PlatformEffector2D effector = obj.GetComponent<PlatformEffector2D>();
            if (effector == null)
                effector = obj.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.useOneWayGrouping = true;
            effector.surfaceArc = 175f;
            effector.useSideFriction = false;
            effector.useSideBounce = false;

            OneWayTilemapSupport2D support = obj.GetComponent<OneWayTilemapSupport2D>();
            if (support == null)
            {
                support = obj.AddComponent<OneWayTilemapSupport2D>();
            }
            else
            {
                support.colliderHeight = 0.18f;
                support.surfaceArc = 175f;
                support.RebuildSupportColliders();
            }
        }
    }

    public void FaceAimDirection(float minimumHorizontalAim = 0.2f)
    {
        if (Mathf.Abs(aimDirection.x) < minimumHorizontalAim)
            return;

        FaceDirection(aimDirection.x > 0f ? 1 : -1);
    }

    public void FaceDirection(int direction)
    {
        if (direction == 0)
            return;

        facingDirection = direction > 0 ? 1 : -1;
        transform.localScale = new Vector3(Mathf.Abs(originalScale.x) * facingDirection, originalScale.y, originalScale.z);
    }

    public void SetSwimming(bool swimming)
    {
        if (isSwimming == swimming)
            return;

        isSwimming = swimming;
        isDashing = false;

        if (rb != null)
        {
            rb.gravityScale = swimming ? 0.25f : originalGravityScale;
            rb.linearVelocity = swimming ? rb.linearVelocity * 0.35f : rb.linearVelocity;
        }
    }

    private void OnPlayerRespawned()
    {
        isDashing = false;
        dashTimer = 0f;
        rb.gravityScale = originalGravityScale;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null) Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

        if (bodyCollider != null)
        {
            Bounds bounds = bodyCollider.bounds;
            Vector2 origin = new Vector2(bounds.center.x, bounds.min.y + 0.03f);
            Vector2 size = new Vector2(Mathf.Max(bounds.size.x - 0.08f, 0.12f), 0.08f);
            Gizmos.DrawWireCube(origin + Vector2.down * groundedCastDistance, size);
        }
    }
}
