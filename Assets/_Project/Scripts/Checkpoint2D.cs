using UnityEngine;

public class Checkpoint2D : MonoBehaviour
{
    public Transform respawnPoint;
    public GameObject activeVisual;
    public int scoreReward = 100;

    private bool activated;

    private void Awake()
    {
        if (activeVisual != null)
            activeVisual.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health == null)
            return;

        health.SetRespawnPoint(respawnPoint != null ? respawnPoint.position : transform.position);

        if (activated)
            return;

        activated = true;

        if (activeVisual != null)
            activeVisual.SetActive(true);

        if (GameManager.Instance != null && scoreReward > 0)
            GameManager.Instance.AddScore(scoreReward);
    }
}
