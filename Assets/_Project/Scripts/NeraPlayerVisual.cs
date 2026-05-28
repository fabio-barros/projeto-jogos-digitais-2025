using UnityEngine;

public class NeraPlayerVisual : MonoBehaviour
{
    public SpriteRenderer legsRenderer;
    public SpriteRenderer torsoRenderer;
    public Vector3 torsoOffset = Vector3.zero;
    public Vector3 runShootTorsoOffset = Vector3.zero;
    public float frameRate = 10f;
    public float crouchMoveFrameRate = 6f;
    public float meleeFrameRate = 18f;

    public Sprite[] legsIdle;
    public Sprite[] legsRun;
    public Sprite[] legsJump;
    public Sprite[] legsHurt;
    public Sprite[] legsDeath;

    public Sprite[] torsoIdle;
    public Sprite[] torsoRun;
    public Sprite[] torsoLookUp;
    public Sprite[] torsoShoot;
    public Sprite[] torsoShootUp;

    public Sprite[] wholeIdleAlt;
    public Sprite[] wholeCrouchIdle;
    public Sprite[] wholeCrouchMove;
    public Sprite[] wholeCrouchShoot;
    public Sprite[] wholeHurt;
    public Sprite[] wholeDeath;
    public Sprite[] wholeMelee;

    private PlayerController2D controller;
    private PlayerShooter2D shooter;
    private PlayerHealth health;
    private float frameTimer;
    private int frameIndex;
    private float meleeTimer;
    private string currentState;

    private void Awake()
    {
        controller = GetComponent<PlayerController2D>();
        shooter = GetComponent<PlayerShooter2D>();
        health = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (legsRenderer == null || torsoRenderer == null || controller == null)
            return;

        if (meleeTimer > 0f)
            meleeTimer -= Time.deltaTime;

        if (RemnantInput.MeleeDown() && health != null && health.CanAct)
            meleeTimer = GetDuration(wholeMelee);

        string state = GetState();
        if (state != currentState)
        {
            currentState = state;
            frameIndex = 0;
            frameTimer = 0f;
        }

        frameTimer += Time.deltaTime;
        float activeFrameRate = GetFrameRateForState(state);
        if (frameTimer >= 1f / activeFrameRate)
        {
            frameTimer = 0f;
            frameIndex++;
        }

        ApplySprites(state);
    }

    private string GetState()
    {
        if (meleeTimer > 0f) return "melee";
        if (health != null && health.IsDead) return "death";
        if (health != null && health.WasRecentlyHurt) return "hurt";
        if (RemnantInput.CrouchHeld() && controller.IsGrounded)
        {
            if (shooter != null && shooter.IsShooting) return "crouchShoot";
            if (Mathf.Abs(controller.HorizontalInput) > 0.05f) return "crouchMove";
            return "crouchIdle";
        }

        if (!controller.IsGrounded) return "jump";
        if (Mathf.Abs(controller.HorizontalInput) > 0.05f) return "run";
        return "idle";
    }

    private void ApplySprites(string state)
    {
        bool shooting = shooter != null && shooter.IsShooting;
        Vector2 visualAim = shooting && shooter != null ? shooter.CurrentShootDirection : controller.AimDirection;
        bool shootingUp = shooting && visualAim.y > 0.55f;
        bool lookingUp = visualAim.y > 0.55f;

        if (state == "melee")
        {
            SetWholeBody(wholeMelee);
            return;
        }

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

        if (state == "crouchShoot")
        {
            SetWholeBody(wholeCrouchShoot);
            return;
        }

        if (state == "crouchMove")
        {
            SetWholeBody(wholeCrouchMove);
            return;
        }

        if (state == "crouchIdle")
        {
            SetWholeBody(wholeCrouchIdle);
            return;
        }

        torsoRenderer.enabled = true;
        torsoRenderer.transform.localPosition = state == "run" && shooting ? runShootTorsoOffset : torsoOffset;

        if (state == "run")
            SetSprite(legsRenderer, legsRun);
        else if (state == "jump")
            SetSprite(legsRenderer, legsJump);
        else
            SetSprite(legsRenderer, legsIdle);

        if (shooting && shootingUp)
            SetSprite(torsoRenderer, torsoShootUp);
        else if (shooting)
            SetSprite(torsoRenderer, torsoShoot);
        else if (lookingUp)
            SetSprite(torsoRenderer, torsoLookUp);
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
        if (sprites == null || sprites.Length == 0)
            return;

        target.sprite = sprites[frameIndex % sprites.Length];
    }

    private bool HasSprites(Sprite[] sprites)
    {
        return sprites != null && sprites.Length > 0;
    }

    private float GetDuration(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0)
            return 0.35f;

        return Mathf.Max(0.2f, sprites.Length / meleeFrameRate);
    }

    private float GetFrameRateForState(string state)
    {
        if (state == "melee")
            return meleeFrameRate;

        if (state == "crouchMove")
            return crouchMoveFrameRate;

        return frameRate;
    }
}
