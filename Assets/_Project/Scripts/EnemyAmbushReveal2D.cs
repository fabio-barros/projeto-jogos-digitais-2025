using System.Collections;
using UnityEngine;

public class EnemyAmbushReveal2D : MonoBehaviour
{
    public bool hiddenOnAwake = true;
    public bool disableGameplayWhileHidden = true;
    public float revealDelay;
    public int emergeDirection = -1;
    public float emergeDistance = 1.1f;
    public float emergeSpeed = 2.1f;
    public bool becomeStationaryAfterEmerge;

    private SpriteRenderer[] spriteRenderers;
    private Collider2D[] colliders;
    private Rigidbody2D rb;
    private EnemyPatrol2D patrol;
    private EnemyShooter2D shooter;
    private Animator animator;
    private float originalGravityScale;
    private RigidbodyType2D originalBodyType;
    private bool revealed;
    private bool revealing;

    private void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        colliders = GetComponentsInChildren<Collider2D>(true);
        rb = GetComponent<Rigidbody2D>();
        patrol = GetComponent<EnemyPatrol2D>();
        shooter = GetComponent<EnemyShooter2D>();
        animator = GetComponent<Animator>();

        if (rb != null)
        {
            originalGravityScale = rb.gravityScale;
            originalBodyType = rb.bodyType;
        }

        if (hiddenOnAwake)
            SetHidden(true);
    }

    public void Reveal()
    {
        if (revealed || revealing)
            return;

        StartCoroutine(RevealRoutine());
    }

    private IEnumerator RevealRoutine()
    {
        revealing = true;

        if (revealDelay > 0f)
            yield return new WaitForSeconds(revealDelay);

        revealed = true;
        SetHidden(false);
        FaceDirection(emergeDirection);

        if (patrol != null)
            patrol.enabled = false;

        if (shooter != null)
            shooter.enabled = false;

        float remainingDistance = Mathf.Max(0f, emergeDistance);
        int direction = emergeDirection >= 0 ? 1 : -1;

        while (remainingDistance > 0f)
        {
            float step = emergeSpeed * Time.deltaTime;
            remainingDistance -= step;

            if (rb != null && rb.simulated)
                rb.linearVelocity = new Vector2(direction * emergeSpeed, rb.linearVelocity.y);
            else
                transform.position += Vector3.right * direction * step;

            yield return null;
        }

        if (rb != null && rb.simulated)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (patrol != null)
        {
            patrol.useRunNGunStationaryBehaviour = becomeStationaryAfterEmerge;
            patrol.enabled = true;
        }

        if (shooter != null)
            shooter.enabled = true;

        revealing = false;
    }

    private void SetHidden(bool hidden)
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].enabled = !hidden;
        }

        if (disableGameplayWhileHidden)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null && !IsAmbushTriggerCollider(colliders[i]))
                    colliders[i].enabled = !hidden;
            }

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = hidden ? RigidbodyType2D.Kinematic : originalBodyType;
                rb.gravityScale = hidden ? 0f : originalGravityScale;
            }

            if (patrol != null)
                patrol.enabled = !hidden;

            if (shooter != null)
                shooter.enabled = !hidden;
        }

        if (animator != null)
            animator.enabled = !hidden;
    }

    private void FaceDirection(int direction)
    {
        if (direction == 0)
            return;

        Vector3 scale = transform.localScale;
        transform.localScale = new Vector3(Mathf.Abs(scale.x) * (direction > 0 ? 1f : -1f), scale.y, scale.z);
    }

    private bool IsAmbushTriggerCollider(Collider2D collider)
    {
        return collider.GetComponent<EnemyAmbushTrigger2D>() != null
            || collider.GetComponentInParent<EnemyAmbushTrigger2D>() != null;
    }
}
