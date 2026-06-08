using UnityEngine;
using UnityEngine.UI;

public class SimpleHUD : MonoBehaviour
{
    public PlayerHealth playerHealth;
    public PlayerShooter2D playerShooter;
    public PlayerBombThrower2D playerBombThrower;
    public Text healthText;
    public Text livesText;
    public Text ammoText;
    public Text bombText;
    public Text scoreText;
    public Text gameOverText;
    public Image healthFill;
    public Image livesFill;
    public Image ammoFill;
    public Image bombFill;
    public Sprite lifeHeartSprite;
    public Image[] healthHeartImages;
    public Image[] lifeHeartImages;

    private Font hudFont;
    private static Sprite fallbackHeartSprite;
    private GameObject startMenuPanel;
    private GameObject finishMenuPanel;
    private Text finishStatsText;

    private void Awake()
    {
        EnsureCanvasVisible();
        AutoBindPlayerReferences();
        hudFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureImprovedHUD();
        EnsureGameOverPanel();
        EnsureGameplayMenus();
    }

    private void OnEnable()
    {
        EnsureCanvasVisible();
    }

    private void EnsureCanvasVisible()
    {
        transform.localScale = Vector3.one;

        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.anchoredPosition = Vector2.zero;
        }

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();

        canvas.enabled = true;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.targetDisplay = 0;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
    }

    private void AutoBindPlayerReferences()
    {
        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<PlayerHealth>();

        if (playerShooter == null)
            playerShooter = FindAnyObjectByType<PlayerShooter2D>();

        if (playerBombThrower == null)
            playerBombThrower = FindAnyObjectByType<PlayerBombThrower2D>();
    }

    private void Update()
    {
        if (playerHealth != null && healthText != null)
        {
            healthText.gameObject.SetActive(false);
            SetFill(healthFill, playerHealth.CurrentHealth, playerHealth.MaxHealth);
            SetFill(livesFill, playerHealth.CurrentLives, playerHealth.MaxLives);
            UpdateHealthHearts();
            UpdateLifeHearts();
        }

        if (playerHealth != null && livesText != null)
            livesText.gameObject.SetActive(false);

        if (GameManager.Instance != null && scoreText != null)
            scoreText.text = "Score: " + GameManager.Instance.Score;

        if (playerBombThrower != null && bombText != null)
        {
            bombText.text = playerBombThrower.IsReloading
                ? "Bombs: Reload"
                : "Bombs: " + playerBombThrower.CurrentBombs + "/" + playerBombThrower.MaxBombs;
            SetFill(bombFill, playerBombThrower.CurrentBombs, playerBombThrower.MaxBombs);
        }

        if (playerShooter != null && ammoText != null)
        {
            ammoText.text = playerShooter.IsReloading
                ? "Ammo: Reload"
                : "Ammo: " + playerShooter.CurrentAmmo + "/" + playerShooter.MaxAmmo;
            SetFill(ammoFill, playerShooter.CurrentAmmo, playerShooter.MaxAmmo);
        }

        UpdateGameplayMenus();
    }

    private void SetFill(Image image, int current, int max)
    {
        if (image == null)
            return;

        image.fillAmount = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
    }

    private void EnsureLifeHearts()
    {
        if (livesText != null)
            livesText.gameObject.SetActive(false);

        int maxLives = playerHealth != null ? Mathf.Max(playerHealth.MaxLives, 1) : 3;
        if (lifeHeartImages != null && lifeHeartImages.Length == maxLives)
            return;

        Transform existing = transform.Find("LifeHearts_Runtime");
        if (existing != null)
            Destroy(existing.gameObject);

        GameObject container = new GameObject("LifeHearts_Runtime");
        container.transform.SetParent(transform, false);

        RectTransform rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -78f);
        rect.sizeDelta = new Vector2(maxLives * 30f, 28f);

        Sprite sprite = GetLifeHeartSprite();
        lifeHeartImages = new Image[maxLives];
        for (int i = 0; i < maxLives; i++)
        {
            GameObject heart = new GameObject("LifeHeart_" + (i + 1));
            heart.transform.SetParent(container.transform, false);

            RectTransform heartRect = heart.AddComponent<RectTransform>();
            heartRect.anchorMin = new Vector2(0f, 0.5f);
            heartRect.anchorMax = new Vector2(0f, 0.5f);
            heartRect.pivot = new Vector2(0f, 0.5f);
            heartRect.anchoredPosition = new Vector2(i * 30f, 0f);
            heartRect.sizeDelta = new Vector2(24f, 24f);

            Image image = heart.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            lifeHeartImages[i] = image;
        }

        UpdateLifeHearts();
    }

    private void EnsureHealthHearts()
    {
        int maxHealth = playerHealth != null ? Mathf.Max(playerHealth.MaxHealth, 1) : 3;
        if (healthHeartImages != null && healthHeartImages.Length == maxHealth)
            return;

        Transform existing = transform.Find("HealthHearts_Runtime");
        if (existing != null)
            Destroy(existing.gameObject);

        GameObject container = new GameObject("HealthHearts_Runtime");
        container.transform.SetParent(transform, false);

        RectTransform rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -42f);
        rect.sizeDelta = new Vector2(maxHealth * 30f, 28f);

        Sprite sprite = GetLifeHeartSprite();
        healthHeartImages = new Image[maxHealth];
        for (int i = 0; i < maxHealth; i++)
        {
            GameObject heart = new GameObject("HealthHeart_" + (i + 1));
            heart.transform.SetParent(container.transform, false);

            RectTransform heartRect = heart.AddComponent<RectTransform>();
            heartRect.anchorMin = new Vector2(0f, 0.5f);
            heartRect.anchorMax = new Vector2(0f, 0.5f);
            heartRect.pivot = new Vector2(0f, 0.5f);
            heartRect.anchoredPosition = new Vector2(i * 30f, 0f);
            heartRect.sizeDelta = new Vector2(24f, 24f);

            Image image = heart.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            healthHeartImages[i] = image;
        }

        UpdateHealthHearts();
    }

    private void UpdateHealthHearts()
    {
        if (healthHeartImages == null || healthHeartImages.Length == 0)
            EnsureHealthHearts();

        if (healthHeartImages == null || playerHealth == null)
            return;

        for (int i = 0; i < healthHeartImages.Length; i++)
        {
            if (healthHeartImages[i] == null)
                continue;

            healthHeartImages[i].color = i < playerHealth.CurrentHealth
                ? new Color(1f, 0.1f, 0.18f, 1f)
                : new Color(0.22f, 0.05f, 0.06f, 0.45f);
        }
    }

    private void UpdateLifeHearts()
    {
        if (lifeHeartImages == null || lifeHeartImages.Length == 0)
            EnsureLifeHearts();

        if (lifeHeartImages == null || playerHealth == null)
            return;

        for (int i = 0; i < lifeHeartImages.Length; i++)
        {
            if (lifeHeartImages[i] == null)
                continue;

            lifeHeartImages[i].color = i < playerHealth.CurrentLives
                ? Color.white
                : new Color(0.25f, 0.25f, 0.25f, 0.45f);
        }
    }

    private Sprite GetLifeHeartSprite()
    {
        if (lifeHeartSprite != null)
            return lifeHeartSprite;

        lifeHeartSprite = Resources.Load<Sprite>("Sprite Sheets/heart pixel art 32x32");
        if (lifeHeartSprite != null)
            return lifeHeartSprite;

        if (fallbackHeartSprite == null)
            fallbackHeartSprite = CreateFallbackHeartSprite();

        return fallbackHeartSprite;
    }

    private static Sprite CreateFallbackHeartSprite()
    {
        Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color red = new Color(1f, 0.1f, 0.18f, 1f);
        Color dark = new Color(0.35f, 0f, 0.05f, 1f);

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                bool leftLobe = (x - 5) * (x - 5) + (y - 10) * (y - 10) <= 16;
                bool rightLobe = (x - 10) * (x - 10) + (y - 10) * (y - 10) <= 16;
                bool point = y <= 10 && Mathf.Abs(x - 7.5f) <= y * 0.72f;
                bool filled = leftLobe || rightLobe || point;
                bool edge = filled && (x < 2 || x > 13 || y < 2 || y > 13 ||
                    !((x - 5) * (x - 5) + (y - 10) * (y - 10) <= 20 ||
                      (x - 10) * (x - 10) + (y - 10) * (y - 10) <= 20 ||
                      (y <= 11 && Mathf.Abs(x - 7.5f) <= y * 0.78f)));

                texture.SetPixel(x, y, filled ? (edge ? dark : red) : clear);
            }
        }

        texture.filterMode = FilterMode.Point;
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 16f);
    }

    private void EnsureImprovedHUD()
    {
        if (transform.Find("StatusPanel") == null && transform.Find("StatusPanel_Runtime") == null)
        {
            GameObject statusPanel = CreatePanel("StatusPanel_Runtime", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -12f), new Vector2(430f, 116f), new Color(0.02f, 0.025f, 0.035f, 0.74f));
            statusPanel.transform.SetAsFirstSibling();
            AddPanelAccent(statusPanel.transform, new Color(0.05f, 1f, 0.95f, 0.85f), true);
        }

        if (transform.Find("ControlsPanel") == null && transform.Find("ControlsPanel_Runtime") == null)
        {
            GameObject controlsPanel = CreatePanel("ControlsPanel_Runtime", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -12f), new Vector2(370f, 174f), new Color(0.02f, 0.025f, 0.035f, 0.82f));
            AddPanelAccent(controlsPanel.transform, new Color(1f, 0.86f, 0.12f, 0.9f), false);

            Text existingControls = FindText("ControlsText");
            if (existingControls != null)
                existingControls.gameObject.SetActive(false);

            CreateText("ControlsTitle_Runtime", controlsPanel.transform, "CONTROLS", new Vector2(0f, -10f), TextAnchor.UpperCenter, 19);
            CreateText("ControlsMove_Runtime", controlsPanel.transform, "MOVE A/D/LS   SWIM W/S", new Vector2(16f, -42f), TextAnchor.UpperLeft, 15);
            CreateText("ControlsAim_Runtime", controlsPanel.transform, "AIM Mouse / Arrows / RS", new Vector2(16f, -66f), TextAnchor.UpperLeft, 15);
            CreateText("ControlsShoot_Runtime", controlsPanel.transform, "SHOOT Click/F/X   MELEE C/LT", new Vector2(16f, -90f), TextAnchor.UpperLeft, 15);
            CreateText("ControlsTools_Runtime", controlsPanel.transform, "BOMB R/B   RELOAD T/Y", new Vector2(16f, -114f), TextAnchor.UpperLeft, 15);
            CreateText("ControlsAction_Runtime", controlsPanel.transform, "JUMP Space/A DASH Shift/LB RESCUE/RIDE E/RB", new Vector2(16f, -138f), TextAnchor.UpperLeft, 14);
            CreateText("ControlsSystem_Runtime", controlsPanel.transform, "RESTART Enter/Start on Game Over", new Vector2(16f, -158f), TextAnchor.UpperLeft, 13);
        }

        EnsureHealthHearts();
        EnsureLifeHearts();
    }

    private void EnsureGameOverPanel()
    {
        if (gameOverText != null)
            return;

        GameObject panel = CreatePanel("GameOverPanel_Runtime", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480f, 184f), new Color(0f, 0f, 0f, 0.86f));
        AddPanelAccent(panel.transform, new Color(1f, 0.2f, 0.12f, 0.95f), true);

        gameOverText = CreateText("GameOverText_Runtime", panel.transform, "MISSION FAILED\nPress Enter or Start to restart", Vector2.zero, TextAnchor.MiddleCenter, 34);
        panel.transform.SetAsLastSibling();
        RectTransform textRect = gameOverText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        panel.SetActive(false);
    }

    private void EnsureGameplayMenus()
    {
        if (startMenuPanel == null)
        {
            startMenuPanel = CreatePanel("StartMenuPanel_Runtime", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 240f), new Color(0.015f, 0.018f, 0.028f, 0.9f));
            AddPanelAccent(startMenuPanel.transform, new Color(0.05f, 1f, 0.95f, 0.95f), true);
            Text title = CreateText("StartTitle_Runtime", startMenuPanel.transform, "REMNANT SQUAD", new Vector2(0f, 58f), TextAnchor.MiddleCenter, 38);
            title.color = new Color(1f, 0.9f, 0.2f, 1f);
            CreateText("StartBody_Runtime", startMenuPanel.transform, "Level 1 - Ashline Outpost\nPress Enter / Start / Click", new Vector2(0f, -34f), TextAnchor.MiddleCenter, 24);
            startMenuPanel.transform.SetAsLastSibling();
