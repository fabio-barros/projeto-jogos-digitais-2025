using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool showStartMenu = true;

    public int Score { get; private set; }
    public int CoinsCollected { get; private set; }
    public bool IsWaitingToStart { get; private set; }
    public bool IsLevelComplete { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        IsWaitingToStart = showStartMenu;
        IsLevelComplete = false;
        Time.timeScale = IsWaitingToStart ? 0f : 1f;
    }

    private void Update()
    {
        if (IsWaitingToStart && RemnantInput.MenuConfirmDown())
            StartLevel();

        if (IsLevelComplete && RemnantInput.RestartDown())
            RestartScene();
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

    public void StartLevel()
    {
        IsWaitingToStart = false;
        if (!IsLevelComplete)
            Time.timeScale = 1f;
    }

    public void CompleteLevel()
    {
        if (IsLevelComplete)
            return;

        IsWaitingToStart = false;
        IsLevelComplete = true;
        Time.timeScale = 0f;
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;
        PlayerHealth.ResetPersistentLives();
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex >= 0)
            SceneManager.LoadScene(activeScene.buildIndex);
        else
            Scene