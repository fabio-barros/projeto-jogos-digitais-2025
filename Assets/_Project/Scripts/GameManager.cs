using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool showStartMenu = true;
    public int requiredRescues = 5;

    public int Score { get; private set; }
    public int CoinsCollected { get; private set; }
    public int RescuedPOWs { get; private set; }
    public bool IsWaitingToStart { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsLevelComplete { get; private set; }
    public string ObjectiveMessage { get; private set; }
    public float ObjectiveMessageTimer { get; private set; }
    public bool CanFinishLevel { get { return RescuedPOWs >= requiredRescues; } }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        IsWaitingToStart = showStartMenu;
        IsPaused = false;
        IsLevelComplete = false;
        ObjectiveMessage = string.Empty;
        Time.timeScale = IsWaitingToStart ? 0f : 1f;
    }

    private void Update()
    {
        if (ObjectiveMessageTimer > 0f)
            ObjectiveMessageTimer -= Time.unscaledDeltaTime;

        if (IsWaitingToStart && RemnantInput.MenuConfirmDown())
            StartLevel();

        if (IsLevelComplete && RemnantInput.RestartDown())
            RestartScene();

        if (IsPaused && RemnantInput.RestartDown())
            RestartScene();

        if (!IsWaitingToStart && !IsLevelComplete && RemnantInput.PauseDown())
            TogglePause();
    }

    public void AddScore(int amount)
    {
        if (amount <= 0)
            return;

        Score += amount;
    }

    public void AddCoin(int scoreValue)
    {
        CoinsCollected++;
        AddScore(scoreValue);
    }

    public void RescuePOW(int scoreValue)
    {
        RescuePOW(scoreValue, 0, 0, 0);
    }

    public void RescuePOW(int scoreValue, int healReward, int bombReward, int ammoReward)
    {
        RescuedPOWs++;
        AddScore(scoreValue);
        ShowObjectiveMessage(BuildPOWRewardMessage(scoreValue, healReward, bombReward, ammoReward), 2.25f);
    }

    public void StartLevel()
    {
        IsWaitingToStart = false;
        if (!IsLevelComplete)
            Time.timeScale = 1f;
    }

    public void TogglePause()
    {
        if (IsLevelComplete)
            return;

        SetPaused(!IsPaused);
    }

    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = IsPaused ? 0f : 1f;
    }

    public void CompleteLevel()
    {
        if (IsLevelComplete)
            return;

        if (!CanFinishLevel)
        {
            ShowObjectiveMessage("Rescue all POWs first: " + RescuedPOWs + "/" + requiredRescues, 2.5f);
            return;
        }

        IsWaitingToStart = false;
        IsPaused = false;
        IsLevelComplete = true;
        Time.timeScale = 0f;
    }

    public void ShowObjectiveMessage(string message, float duration)
    {
        ObjectiveMessage = message;
        ObjectiveMessageTimer = duration;
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;
        PlayerHealth.ResetPersistentLives();
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex >= 0)
            SceneManager.LoadScene(activeScene.buildIndex);
        else
            SceneManager.LoadScene(activeScene.name);
    }

    private string BuildPOWRewardMessage(int scoreValue, int healReward, int bombReward, int ammoReward)
    {
        string message = "POW rescued: " + RescuedPOWs + "/" + requiredRescues + "\n+" + scoreValue + " score";

        if (healReward > 0)
            message += "  +" + healReward + " health";

        if (bombReward > 0)
            message += "  +" + bombReward + " bombs";

        if (ammoReward > 0)
            message += "  +" + ammoReward + " ammo";

        return message;
    }
}
