using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHoverVehicle2D : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform seatPoint;
    public Transform leftGun;
    public Transform rightGun;
    public TextMesh promptText;
    public float moveSpeed = 6f;
    public float verticalSpeed = 4.5f;
    public float fireCooldown = 0.12f;

    private Transform rider;
    private PlayerController2D riderController;
    private PlayerShooter2D riderShooter;
    private PlayerBombThrower2D riderBombs;
    private PlayerMeleeAttack2D riderMelee;
    private Rigidbody2D riderBody;
    private Collider2D[] riderColliders;
    private SpriteRenderer[] riderRenderers;
    private Rigidbody2D rb;
    private CameraFollow2D cameraFollow;
    private Transform originalCameraTarget;
    private Transform nearbyPlayer;
    private float fireTimer;
    private float interactLockTimer;
    private int facingDirection = 1;

    public bool HasRider { get { return rider != null; } }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (interactLockTimer > 0f)
            interactLockTimer -= Time.deltaTime;

        if (fireTimer > 0f)
            fireTimer -= Time.deltaTime;

        if (rider == null)
        {
            if (promptText != null)
                promptText.gameObject.SetActive(nearbyPlayer != null);

            if (nearbyPlayer != null && interactLockTimer <= 0f && RemnantInput.InteractDown())
                Mount(nearbyPlayer);

            return;
        }

        if (promptText != null)
            promptText.gameObject.SetActive(false);

        if (seatPoint != null)
            rider.position = seatPoint.position;

        if (interactLockTimer <= 0f && RemnantInput.InteractDown())
        {
            Dismount();
            return;
        }

        if (RemnantInput.ShootHeld() && fireTimer <= 0f)
            FireFrontGuns();
    }

    private void FixedUpdate()
    {
        if (rider == null)
        {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, 0.08f);
            return;
        }

        Vector2 input = new Vector2(RemnantInput.MoveHorizontal(), RemnantInput.MoveVertical());
        if (Mathf.Abs(input.x) > 0.05f)
        {
            facingDirection = input.x > 0f ? 1 : -1;
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * facingDirection, transform.localScale.y, transform.localScale.z);
        }

        rb.linearVelocity = new Vector2(input.x * moveSpeed, input.y * verticalSpeed);
    }

    private void Mount(Transform player)
    {
        rider = player;
        riderController = rider.GetComponent<PlayerController2D>();
        riderShooter = rider.GetComponent<PlayerShooter2D>();
        riderBombs = rider.GetComponent<PlayerBombThrower2D>();
        riderMelee = rider.GetComponent<PlayerMeleeAttack2D>();
        riderBody = rider.GetComponent<Rigidbody2D>();
        riderColliders = rider.GetComponentsInChildren<Collider2D>();
        riderRenderers = rider.GetComponentsInChildren<SpriteRenderer>();

        SetRiderActive(false);

        if (Camera.main != null)
        {
            cameraFollow = Camera.main.GetComponent<CameraFollow2D>();
            if (cameraFollow != null)
            {
                originalCameraTarget = cameraFollow.target;
                cameraFollow.target = transform;
            }
        }

        if (seatPoint != null)
            rider.position = seatPoint.position;

        interactLockTimer = 0.25f;
    }

    private void Dismount()
    {
        if (rider == null)
            return;

        Vector3 exitOffset = new Vector3(-facingDirection * 1.2f, -0.45f, 0f);
        rider.position = transform.position + exitOffset;
        SetRiderActive(true);

        if (cameraFollow != null)
            cameraFollow.target = originalCameraTarget;

        rider = null;
        riderController = null;
        riderShooter = null;
        riderBombs = null;
        riderMelee = null;
        riderBody = null;
        riderColliders = null;
        riderRenderers = null;
        interactLockTimer = 0.25f;
    }

    private void SetRiderActive(bool active)
    {
        if (riderController != null) riderController.enabled = active;
        if (riderShooter != null) riderShooter.enabled = active;
        if (riderBombs != null) riderBombs.enabled = active;
        if (riderMelee != null) riderMelee.enabled = active;

        if (riderBody != null)
        {
            riderBody.linearVelocity = Vector2.zero;
            riderBody.simulated = active;
        }

        for (int i = 0; riderColliders != null && i < riderColliders.Length; i++)
            riderColliders[i].enabled = active;

        for (int i = 0; riderRenderers != null && i < riderRenderers.Length; i++)
            riderRenderers[i].enabled = active;
    }

    private void FireFrontGuns()
    {
        FireFrom(leftGun);
        FireFrom(rightGun);
        fireTimer = fireCooldown;
    }

    private void FireFrom(Transform gun)
    {
        if (projectilePrefab == null || gun == null)
            return;

        GameObject projectileObject = Instantiate(projectilePrefab, gun.position, Quaternion.identity);
        Projectile2D projectile = projectileObject.GetComponent<Projectile2D>();
        if (projectile != null)
            projectile.SetDirection(new Vector2(facingDirection, 0f));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (rider != null)
            return;

        if (other.GetComponent<PlayerController2D>() != null)
            nearbyPlayer = other.transform;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (nearbyPlayer == other.transform)
            nearbyPlayer = null;
    }
}
