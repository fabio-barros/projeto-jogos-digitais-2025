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

    private Font hudFont;

    private void Awake()
    {
        hudFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureImprovedHUD();
        EnsureGameOverPanel();
    }

    private void Update()
    {
        if (playerHealth != null && healthText != null)
        {
            healthText.text = playerHealth.IsGameOver
                ? "GAME OVER"
                : "HP: " + playerHealth.CurrentHealth + "/" + playerHealth.MaxHealth;
            SetFill(healthFill, playerHealth.CurrentHealth, playerHealth.MaxHealth);
            SetFill(livesFill, playerHealth.CurrentLives, playerHealth.MaxLives);
        }

        if (playerHealth != null && livesText != null)
            livesText.text = "Lives: " + playerHealth.CurrentLives + "/" + playerHealth.MaxLives;

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

        if (playerHealth != null && gameOverText != null)
            gameOverText.transform.parent.gameObject.SetActive(playerHealth.IsGameOver);
    }

    private void SetFill(Image image, int current, int max)
    {
        if (image == null)
            return;

        image.fillAmount = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
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
    }

    private void EnsureGameOverPanel()
    {
        if (gameOverText != null)
            return;

        GameObject panel = CreatePanel("GameOverPanel_Runtime", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480f, 184f), new Color(0f, 0f, 0f, 0.86f));
        AddPanelAccent(panel.transform, new Color(1f, 0.2f, 0.12f, 0.95f), true);

        gameOverText = CreateText("GameOverText_Runtime", panel.transform, "GAME OVER\nPress Enter or Start to restart", Vector2.zero, TextAnchor.MiddleCenter, 34);
        panel.transform.SetAsLastSibling();
        RectTransform textRect = gameOverText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        panel.SetActive(false);
    }

    private GameObject CreatePanel(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(transform);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = panel.AddComponent<Image>();
        image.color = color;

        return panel;
    }

    private void AddPanelAccent(Transform parent, Color color, bool left)
    {
        GameObject accent = new GameObject("Accent");
        accent.transform.SetParent(parent);
        RectTransform rect = accent.AddComponent<RectTransform>();
        rect.anchorMin = left ? new Vector2(0f, 0f) : new Vector2(0f, 1f);
        rect.anchorMax = left ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
        rect.pivot = left ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = left ? new Vector2(4f, 0f) : new Vector2(0f, 4f);
        accent.AddComponent<Image>().color = color;
    }

    private Text CreateText(string name, Transform parent, string value, Vector2 anchoredPosition, TextAnchor alignment, int size)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = alignment == TextAnchor.UpperCenter ? new Vector2(0.5f, 1f) : new Vector2(0f, 1f);
        rect.anchorMax = alignment == TextAnchor.UpperCenter ? new Vector2(0.5f, 1f) : new Vector2(0f, 1f);
        rect.pivot = alignment == TextAnchor.UpperCenter ? new Vector2(0.5f, 1f) : new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = alignment == TextAnchor.UpperCenter ? new Vector2(340f, 32f) : new Vector2(350f, 26f);

        if (alignment == TextAnchor.MiddleCenter)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(460f, 160f);
        }

        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = hudFont;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        return text;
    }

    private Text FindText(string name)
    {
        Text[] texts = GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].name == name)
                return texts[i];
        }

        return null;
    }
}
