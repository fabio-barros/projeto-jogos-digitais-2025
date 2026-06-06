using UnityEngine;

public class NeraEnemyVisual : MonoBehaviour
{
    public SpriteRenderer legsRenderer;
    public SpriteRenderer torsoRenderer;
    public Vector3 torsoOffset = Vector3.zero;
    public float frameRate = 10f;

    public Sprite[] legsIdle;
    public Sprite[] legsRun;
    public Sprite[] legsHurt;
    public Sprite[] legsDeath;

    public Sprite[] torsoIdle;
    public Sprite[] torsoRun;
    public Sprite[] torsoShoot;

    public Sprite[] wholeHurt;
    public Sprite[] wholeDeath;

    private EnemyPatrol2D patrol;
    private EnemyShooter2D shooter;
    private Damageable damageable;
    private int lastHealth;
    private float hurtTimer;
    private float frameTimer;
    private int frameIndex;
    private string currentState;

    private void Awake()
    {
        patrol = GetComponent<EnemyPatrol2D>();
        shooter = GetComponent<EnemyShooter2D>();
        damageable = GetComponent<Damageable>();

        if (damageable != null)
            lastHealth = damageable.CurrentHealth;
    }

    private void Update()
    {
        if (legsRenderer == null || torsoRenderer == null)
            return;

        if (damageable != null && damageable.CurrentHealth < lastHealth)
        {
            hurtTimer = 0.18f;
            lastHealth = damageable.CurrentHealth;
        }

        if (hurtTimer > 0f)
            hurtTimer -= Time.deltaTime;

        string state = GetState();
        if (state != currentState)
        {
            currentState = state;
            frameIndex = 0;
            frameTimer = 0f;
        }

        frameTimer += Time.deltaTime;
        if (frameTimer >= 1f / frameRate)
        {
            frameTimer = 0f;
            frameIndex++;
        }

        ApplySprites(state);
    }

    private string GetState()
    {
        if (damageable != null && damageable.IsDead) return "death";
        if (hurtTimer > 0f) return "hurt";
        if (shooter != null && shooter.IsShooting) return "shoot";
        if (patrol != null && patrol.IsMoving) return "run";
        return "idle";
    }

    private void ApplySprites(string state)
    {
        if (state == "death")
        {
            SetWholeBody(HasSprites(wholeDeath) ? wholeDeath : legsDeath);
            return;
        }

        if (state == "hurt")
        {
            SetWholeBody(HasSprites(wholeHurt) ? wholeHurt : legsHurt);
            return;
        }

        torsoRenderer.enabled = true;
        torsoRenderer.transform.localPosition = torsoOffset;

        SetSprite(legsRenderer, state == "run" ? legsRun : legsIdle);

        if (state == "shoot")
            SetSprite(torsoRenderer, torsoShoot);
        else if (state == "run")
            SetSprite(torsoRenderer, torsoRun);
        else
            SetSprite(torsoRenderer, torsoIdle);
    }

    private void SetWholeBody(Sprite[] sprites)
    {
        torsoRenderer.enabled = false;
        SetSprite(legsRenderer, sprites);
    }

    private void SetSprite(SpriteRenderer target, Sprite[] sprites)
    {
        if (target == null || sprites == null || sprites.Length == 0)
            return;

        target.sprite = sprites[frameIndex % sprites.Length];
    }

    private bool HasSprites(Sprite[] sprites)
    {
        return sprites != null && sprites.Length > 0;
    }
}
