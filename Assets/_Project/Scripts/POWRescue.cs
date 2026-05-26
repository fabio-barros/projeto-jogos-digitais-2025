using UnityEngine;

public class POWRescue : MonoBehaviour
{
    public int scoreReward = 500;
    public int healReward = 1;
    public GameObject rescuePrompt;
    public GameObject rescuedVisual;

    private bool playerInside;
    private bool rescued;
    private PlayerHealth playerHealth;

    private void Start()
    {
        if (rescuePrompt != null) rescuePrompt.SetActive(false);
        if (rescuedVisual != null) rescuedVisual.SetActive(false);
    }

    private void Update()
    {
        if (playerInside && !rescued && Input.GetKeyDown(KeyCode.E))
            Rescue();
    }

    private void Rescue()
    {
        rescued = true;

        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(scoreReward);

        if (playerHealth != null)
            playerHealth.Heal(healReward);

        if (rescuePrompt != null) rescuePrompt.SetActive(false);
        if (rescuedVisual != null) rescuedVisual.SetActive(true);

        Destroy(gameObject, 1.5f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerHealth health = collision.GetComponent<PlayerHealth>();
        if (health != null)
        {
            playerInside = true;
            playerHealth = health;
            if (rescuePrompt != null && !rescued) rescuePrompt.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        PlayerHealth health = collision.GetComponent<PlayerHealth>();
        if (health != null)
        {
            playerInside = false;
            playerHealth = null;
            if (rescuePrompt != null) rescuePrompt.SetActive(false);
        }
    }
}
