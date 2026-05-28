using UnityEngine;

public class HazardDamageZone : MonoBehaviour
{
    public int damage = 1;
    public float tickInterval = 0.35f;

    private float nextDamageTime;

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other, true);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other, false);
    }

    private void TryDamage(Collider2D other, bool immediate)
    {
        if (!immediate && Time.time < nextDamageTime)
            return;

        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health == null)
            return;

        health.TakeDamage(damage);
        nextDamageTime = Time.time + tickInterval;
    }
}
