using UnityEngine;

public class POWRescue : MonoBehaviour
{
    public int scoreReward = 500;
    public int healReward = 1;
    public GameObject rescuePrompt;
    public GameObject rescuedVisual;

    private bool _playerInside;
    private bool _rescued;
    private PlayerHealth _playerHealth;

    private void Start()
    {
        if (rescuePrompt != null) rescuePrompt.SetActive(false);
        if (rescuedVisual != null) rescuedVisual.SetActive(false);
    }

    private void Update()
    {
        if (_playerInside && !_rescued && Input.GetKeyDown(KeyCode.E))
        {
            Rescue();
        }
    }

    private void Rescue()
    {
        _rescued = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(scoreReward);
        }

        if (_playerHealth != null)
        {
            _playerHealth.Heal(healReward);
        }

        if (rescuePrompt != null) rescuePrompt.SetActive(false);
        if (rescuedVisual != null) rescuedVisual.SetActive(true);

        Destroy(gameObject, 1.5f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            _playerInside = true;
            _playerHealth = playerHealth;

            if (rescuePrompt != null && !_rescued)
            {
                rescuePrompt.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            _playerInside = false;
            _playerHealth = null;

            if (rescuePrompt != null)
            {
                rescuePrompt.SetActive(false);
            }
        }
    }
}
