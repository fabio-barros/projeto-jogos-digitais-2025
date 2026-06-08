using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EnemyAnimationDriver : MonoBehaviour
{
    private static readonly int MovingHash = Animator.StringToHash("Moving");
    private static readonly int ShootingHash = Animator.StringToHash("Shooting");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");
    private static readonly int DeadHash = Animator.StringToHash("Dead");
    private static readonly int BruteHash = Animator.StringToHash("Brute");
    private static readonly int DeathVariantHash = Animator.StringToHash("DeathVariant");
    private static readonly int RunNGunShootingHash = Animator.StringToHash("IsShooting");
    private static readonly int RunNGunShootingUpHash = Animator.StringToHash("IsShootingUp");
    private static readonly int RunNGunWaitingHash = Animator.StringToHash("IsWaiting");
    private static readonly int RunNGunWaitingUpHash = Animator.StringToHash("IsWaitingUp");
    private static readonly int RunNGunDeadHash = Animator.StringToHash("IsDead");

    public bool isBrute;

    private Animator animator;
    private EnemyPatrol2D patrol;
    private EnemyShooter2D shooter;
    private Damageable damageable;
    private int lastHealth;
    private float hurtTimer;
    private bool hasMovingParameter;
    private bool hasShootingParameter;
    private bool hasHurtParameter;
    private bool hasDeadParameter;
    private bool hasBruteParameter;
    private bool hasDeathVariantParameter;
    private bool hasRunNGunShootingParameter;
    private bool hasRunNGunShootingUpParameter;
    private bool hasRunNGunWaitingParameter;
    private bool hasRunNGunWaitingUpParameter;
    private bool hasRunNGunDeadParameter;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        patrol = GetComponent<EnemyPatrol2D>();
        shooter = GetComponent<EnemyShooter2D>();
        damageable = GetComponent<Damageable>();
        CacheAnimatorParameters();

        if (damageable != null)
        {
            lastHealth = damageable.CurrentHealth;
            damageable.Died += OnDied;
        }
    }

    private void OnDestroy()
    {
        if (damageable != null)
            damageable.Died -= OnDied;
    }

    private void Update()
    {
        if (damageable != null && damageable.CurrentHealth < lastHealth)
        {
            hurtTimer = 0.18f;
            lastHealth = damageable.CurrentHealth;
        }

        if (hurtTimer > 0f)
            hurtTimer -= Time.deltaTime;

        bool dead = damageable != null && damageable.IsDead;
        SetBoolIfPresent(hasMovingParameter, MovingHash, patrol != null && patrol.enabled && patrol.IsMoving && !dead);
        SetBoolIfPresent(hasShootingParameter, ShootingHash, shooter != null && shooter.IsShooting);
        SetBoolIfPresent(hasHurtParameter, HurtHash, hurtTimer > 0f && !dead);
        SetBoolIfPresent(hasDeadParameter, DeadHash, dead);
        SetBoolIfPresent(hasBruteParameter, BruteHash, isBrute);
        SetBoolIfPresent(hasRunNGunShootingParameter, RunNGunShootingHash, shooter != null && shooter.IsShooting);
        SetBoolIfPresent(hasRunNGunShootingUpParameter, RunNGunShootingUpHash, shooter != null && shooter.IsShootingUp);
        SetBoolIfPresent(hasRunNGunWaitingParameter, RunNGunWaitingHash, shooter != null && shooter.IsWaiting);
        SetBoolIfPresent(hasRunNGunWaitingUpParameter, RunNGunWaitingUpHash, shooter != null && shooter.IsWaitingUp);
        SetBoolIfPresent(hasRunNGunDeadParameter, RunNGunDeadHash, dead);
    }

    private void OnDied()
    {
        if (hasDeathVariantParameter)
            animator.SetInteger(DeathVariantHash, Random.Range(0, 3));
    }

    private void CacheAnimatorParameters()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == MovingHash) hasMovingParameter = true;
            if (parameter.nameHash == ShootingHash) hasShootingParameter = true;
            if (parameter.nameHash == HurtHash) hasHurtParameter = true;
            if (parameter.nameHash == DeadHash) hasDeadParameter = true;
            if (parameter.nameHash == BruteHash) hasBruteParameter = true;
            if (parameter.nameHash == DeathVariantHash) hasDeathVariantParameter = true;
            if (parameter.nameHash == RunNGunShootingHash) hasRunNGunShootingParameter = true;
            if (parameter.nameHash == RunNGunShootingUpHash) hasRunNGunShootingUpParameter = true;
            if (parameter.nameHash == RunNGunWaitingHash) hasRunNGunWaitingParameter = true;
            if (parameter.nameHash == RunNGunWaitingUpHash) hasRunNGunWaitingUpParameter = true;
            if (parameter.nameHash == RunNGunDeadHash) hasRunNGunDeadParameter = true;
        }
    }

    private void SetBoolIfPresent(bool hasParameter, int parameterHash, bool value)
    {
        if (hasParameter)
            animator.SetBool(parameterHash, value);
    }
}
