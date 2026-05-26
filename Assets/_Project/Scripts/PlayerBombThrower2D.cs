using UnityEngine;

public class PlayerBombThrower2D : MonoBehaviour
{
    public GameObject bombPrefab;
    public Transform throwPoint;
    public int maxBombs = 3;
    public float throwForce = 9f;
    public float upwardBoost = 2.5f;
    public float throwCooldown = 0.5f;

    private PlayerController2D controller;
    private PlayerHealth playerHealth;
    private int currentBombs;
    private float cooldownTimer;
    private float throwFeedbackTimer;

    public int CurrentBombs { get { return currentBombs; } }
    public int MaxBombs { get { return maxBombs; } }
    public bool IsThrowing { get { return throwFeedbackTimer > 0f; } }

    private void Awake()
    {
        controller = GetComponent<PlayerController2D>();
        playerHealth = GetComponent<PlayerHealth>();
        currentBombs = maxBombs;
    }

    private void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
        if (throwFeedbackTimer > 0f) throwFeedbackTimer -= Time.deltaTime;

        if (playerHealth != null && !playerHealth.CanAct) return;

        if (RemnantInput.BombDown() && cooldownTimer <= 0f && currentBombs > 0)
            ThrowBomb();
    }

    public void RefillBombs(int amount)
    {
        currentBombs = Mathf.Min(currentBombs + amount, maxBombs);
    }

    private void ThrowBomb()
    {
        if (bombPrefab == null || throwPoint == null || controller == null) return;

        Vector2 aim = controller.AimDirection;
        Vector3 spawnPosition = throwPoint.position + new Vector3(aim.x * 0.35f, aim.y * 0.2f, 0f);
        GameObject bombObject = Instantiate(bombPrefab, spawnPosition, Quaternion.identity);

        Rigidbody2D rb = bombObject.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = aim * throwForce + Vector2.up * upwardBoost;

        currentBombs--;
        cooldownTimer = throwCooldown;
        throwFeedbackTimer = 0.2f;
    }
}
