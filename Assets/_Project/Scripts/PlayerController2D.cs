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

    public int FacingDirection { get { return facingDirection; } }
    public Vector2 AimDirection { get { return aimDirection; } }
    public bool IsGrounded { get { return isGrounded; } }
    public bool IsDashing { get { return isDashing; } }
    public float HorizontalInput { get { return horizontal; } }
    public Vector2 Velocity { get { return rb != null ? rb.linearVelocity : Vector2.zero; } }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravityScale = rb.gravityScale;
        originalScale = transform.localScale;
    }

    private void Update()
    {
        horizontal = Input.GetAxisRaw("Horizontal");
        UpdateAimDirection();

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

        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W)) && isGrounded && !isDashing)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

        if (dashCooldownTimer > 0)
            dashCooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.LeftShift) && dashCooldownTimer <= 0 && !isDashing)
            StartDash();

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0) EndDash();
        }
    }

    private void FixedUpdate()
    {
        if (isDashing)
        {
            rb.linearVelocity = new Vector2(facingDirection * dashSpeed, 0);
            return;
        }

        rb.linearVelocity = new Vector2(horizontal * moveSpeed, rb.linearVelocity.y);
    }

    private void UpdateAimDirection()
    {
        Vector2 rawAim = Vector2.zero;

        if (Input.GetKey(KeyCode.RightArrow)) rawAim.x += 1f;
        if (Input.GetKey(KeyCode.LeftArrow)) rawAim.x -= 1f;
        if (Input.GetKey(KeyCode.UpArrow)) rawAim.y += 1f;
        if (Input.GetKey(KeyCode.DownArrow)) rawAim.y -= 1f;

        if (rawAim.sqrMagnitude > 0.01f)
            aimDirection = rawAim.normalized;
        else
            aimDirection = new Vector2(facingDirection, 0f);
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

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null) Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
