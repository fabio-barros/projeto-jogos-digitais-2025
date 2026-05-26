using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    public float moveSpeed = 7f;
    public float jumpForce = 14f;
    public float dashSpeed = 18f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.8f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.16f;
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

    public int FacingDirection { get { return facingDirection; } }
    public Vector2 AimDirection { get { return aimDirection; } }
    public bool IsGrounded { get { return isGrounded; } }
    public bool IsDashing { get { return isDashing; } }
    public float HorizontalInput { get { return horizontal; } }
    public Vector2 Velocity { get { return rb != null ? rb.linearVelocity : Vector2.zero; } }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerHealth = GetComponent<PlayerHealth>();
        originalGravityScale = rb.gravityScale;
        originalScale = transform.localScale;
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

        if (horizontal > 0)
        {
            facingDirection = 1;
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
        else if (horizontal < 0)
        {
            facingDirection = -1;
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }

        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

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

        rb.linearVelocity = new Vector2(horizontal * moveSpeed, rb.linearVelocity.y);
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

    private void OnPlayerRespawned()
    {
        isDashing = false;
        dashTimer = 0f;
        rb.gravityScale = originalGravityScale;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null) Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
