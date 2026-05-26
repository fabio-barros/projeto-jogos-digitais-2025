using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationDriver : MonoBehaviour
{
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int DashingHash = Animator.StringToHash("Dashing");
    private static readonly int ShootingHash = Animator.StringToHash("Shooting");
    private static readonly int ThrowingHash = Animator.StringToHash("Throwing");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");
    private static readonly int DeadHash = Animator.StringToHash("Dead");
    private static readonly int AimXHash = Animator.StringToHash("AimX");
    private static readonly int AimYHash = Animator.StringToHash("AimY");

    private Animator animator;
    private PlayerController2D controller;
    private PlayerShooter2D shooter;
    private PlayerBombThrower2D bombThrower;
    private PlayerHealth health;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<PlayerController2D>();
        shooter = GetComponent<PlayerShooter2D>();
        bombThrower = GetComponent<PlayerBombThrower2D>();
        health = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (controller != null)
        {
            animator.SetFloat(SpeedHash, Mathf.Abs(controller.HorizontalInput));
            animator.SetFloat(VerticalSpeedHash, controller.Velocity.y);
            animator.SetBool(GroundedHash, controller.IsGrounded);
            animator.SetBool(DashingHash, controller.IsDashing);
            animator.SetFloat(AimXHash, controller.AimDirection.x);
            animator.SetFloat(AimYHash, controller.AimDirection.y);
        }

        if (shooter != null)
            animator.SetBool(ShootingHash, shooter.IsShooting);

        if (bombThrower != null)
            animator.SetBool(ThrowingHash, bombThrower.IsThrowing);

        if (health != null)
        {
            animator.SetBool(HurtHash, health.WasRecentlyHurt && !health.IsDead);
            animator.SetBool(DeadHash, health.IsDead);
        }
    }
}
