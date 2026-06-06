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

    public bool isBrute;

    private Animator animator;
    private EnemyPatrol2D patrol;
    private EnemyShooter2D shooter;
    private Damageable damageable;
    private int lastHealth;
    private float hurtTimer;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        patrol = GetComponent<EnemyPatrol2D>();
        shooter = GetComponent<EnemyShooter2D>();
        damageable = GetComponent<Damageable>();

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
        animator.SetBool(MovingHash, patrol != null && patrol.enabled && patrol.IsMoving && !dead);
        animator.SetBool(ShootingHash, shooter != null && shooter.IsShooting);
        animator.SetBool(HurtHash, hurtTimer > 0f && !dead);
        animator.SetBool(DeadHash, dead);
        animator.SetBool(BruteHash, isBrute);
    }

    private void OnDied()
    {
        animator.SetInteger(DeathVariantHash, Random.Range(0, 3));
    }
}
