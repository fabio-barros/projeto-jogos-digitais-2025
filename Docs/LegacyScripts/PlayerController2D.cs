using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 7f;
    public float jumpForce = 14f;

    [Header("Dash")]
    public float dashSpeed = 18f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.8f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    private Rigidbody2D _rb;
    private float _horizontal;
    private bool _isGrounded;
    private bool _isDashing;
    private float _dashTimer;
    private float _dashCooldownTimer;
    private int _facingDirection = 1;

    public int FacingDirection => _facingDirection;
    public bool IsDashing => _isDashing;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        _horizontal = Input.GetAxisRaw("Horizontal");

        if (_horizontal > 0)
        {
            _facingDirection = 1;
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (_horizontal < 0)
        {
            _facingDirection = -1;
            transform.localScale = new Vector3(-1, 1, 1);
        }

        _isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (Input.GetKeyDown(KeyCode.Space) && _isGrounded && !_isDashing)
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
        }

        if (_dashCooldownTimer > 0)
        {
            _dashCooldownTimer -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && _dashCooldownTimer <= 0 && !_isDashing)
        {
            StartDash();
        }

        if (_isDashing)
        {
            _dashTimer -= Time.deltaTime;

            if (_dashTimer <= 0)
            {
                EndDash();
            }
        }
    }

    private void FixedUpdate()
    {
        if (_isDashing)
        {
            _rb.linearVelocity = new Vector2(_facingDirection * dashSpeed, 0);
            return;
        }

        _rb.linearVelocity = new Vector2(_horizontal * moveSpeed, _rb.linearVelocity.y);
    }

    private void StartDash()
    {
        _isDashing = true;
        _dashTimer = dashDuration;
        _dashCooldownTimer = dashCooldown;
        _rb.gravityScale = 0f;
    }

    private void EndDash()
    {
        _isDashing = false;
        _rb.gravityScale = 3f;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
