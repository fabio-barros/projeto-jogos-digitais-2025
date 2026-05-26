using UnityEngine;

public class POWRescue : MonoBehaviour
{
    public int scoreReward = 500;
    public int healReward = 1;
    public int bombReward = 1;
    public int ammoReward = 10;
    public float exitSpeed = 3f;
    public float exitDuration = 1.5f;
    public GameObject rescuePrompt;
    public GameObject rescuedVisual;

    private bool playerInside;
    private bool rescued;
    private float exitTimer;
    private PlayerHealth playerHealth;
    private PlayerBombThrower2D playerBombThrower;
    private PlayerShooter2D playerShooter;

    public bool PlayerInside { get { return playerInside; } }
    public bool Rescued { get { return rescued; } }

    private void Start()
    {
        if (rescuePrompt != null) rescuePrompt.SetActive(false);
        if (rescuedVisual != null) rescuedVisual.SetActive(false);
    }

    private void Update()
    {
        if (playerInside && !rescued && RemnantInput.InteractDown())
            Rescue();

        if (rescued)
        {
            exitTimer -= Time.deltaTime;
            transform.Translate(Vector2.right * exitSpeed * Time.deltaTime, Space.World);

            if (exitTimer <= 0f)
                Destroy(gameObject);
        }
    }

    private void Rescue()
    {
        rescued = true;

        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(scoreReward);

        if (playerHealth != null)
            playerHealth.Heal(healReward);

        if (playerBombThrower != null)
            playerBombThrower.RefillBombs(bombReward);

        if (playerShooter != null)
            playerShooter.RefillAmmo(ammoReward);

        if (rescuePrompt != null) rescuePrompt.SetActive(false);
        if (rescuedVisual != null) rescuedVisual.SetActive(true);

        exitTimer = exitDuration;

        Collider2D ownCollider = GetComponent<Collider2D>();
        if (ownCollider != null)
            ownCollider.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerHealth health = collision.GetComponent<PlayerHealth>();
        if (health != null)
        {
            playerInside = true;
            playerHealth = health;
            playerBombThrower = collision.GetComponent<PlayerBombThrower2D>();
            playerShooter = collision.GetComponent<PlayerShooter2D>();
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
            playerBombThrower = null;
            playerShooter = null;
            if (rescuePrompt != null) rescuePrompt.SetActive(false);
        }
    }
}
