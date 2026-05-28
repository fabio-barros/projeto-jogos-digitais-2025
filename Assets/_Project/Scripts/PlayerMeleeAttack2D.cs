using UnityEngine;

public class PlayerMeleeAttack2D : MonoBehaviour
{
    public int damage = 1;
    public float range = 0.75f;
    public float verticalOffset = 0.05f;
    public float cooldown = 0.45f;
    public LayerMask enemyLayers;

    private PlayerController2D controller;
    private PlayerHealth health;
    private float cooldownTimer;

    private void Awake()
    {
        controller = GetComponent<PlayerController2D>();
        health = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (health != null && !health.CanAct)
            return;

        if (RemnantInput.MeleeDown() && cooldownTimer <= 0f)
            Attack();
    }

    private void Attack()
    {
        cooldownTimer = cooldown;

        int facing = controller != null ? controller.FacingDirection : 1;
        Vector2 center = (Vector2)transform.position + new Vector2(facing * range * 0.55f, verticalOffset);
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, range * 0.5f, enemyLayers);

        for (int i = 0; i < hits.Length; i++)
        {
            Damageable damageable = hits[i].GetComponent<Damageable>();
            if (damageable != null)
                damageable.TakeDamage(damage, new Vector2(facing, 0f));
        }
    }

    private void OnDrawGizmosSelected()
    {
        int facing = controller != null ? controller.FacingDirection : 1;
        Vector2 center = (Vector2)transform.position + new Vector2(facing * range * 0.55f, verticalOffset);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, range * 0.5f);
    }
}
