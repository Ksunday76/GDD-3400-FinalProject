using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

// IMPORTANT: I Used ChatGPT to polished and clean up the code.
// Also used Chatgpt to add in additional comments throughout the code
// so it is easier to follow along with the logic.

// Overall game loop


public class SurvivalGameManager : MonoBehaviour
{
    // This is a simple singleton so other scripts can call:
    // SurvivalGameManager.Instance.AddKillScore()
    public static SurvivalGameManager Instance { get; private set; }

    // ----------------------------
    // Round settings
    // ----------------------------
    [Header("Win/Lose Settings")]
    // Total time the player needs to survive to win
    public float roundTimeSeconds = 120f;

    // ----------------------------
    // Auto reset settings
    // ----------------------------
    [Header("Auto Reset")]
    // If true, the scene reloads automatically when the player survives
    public bool autoResetOnWin = false;

    // If true, the scene reloads automatically when the player dies
    public bool autoResetOnLose = true;

    // How long to wait before reloading the scene
    public float resetDelaySeconds = 2f;

    // ----------------------------
    // Scoring settings
    // ----------------------------
    [Header("Scoring")]
    // How many points the player earns per zombie kill
    public int pointsPerKill = 1;

    // ----------------------------
    // UI references (TextMeshPro)
    // ----------------------------
    [Header("UI (TMP)")]
    // Shows the timer countdown
    public TMP_Text timerText;

    // Shows the score
    public TMP_Text scoreText;

    // Shows messages like "SURVIVED!" or "YOU DIED!"
    public TMP_Text statusText;

    // ----------------------------
    // Optional references for cleanup when the game ends
    // ----------------------------
    [Header("Optional References")]
    // Scripts to disable when the round ends 
    public MonoBehaviour[] disableOnEnd;

    // GameObjects to disable when the round ends 
    public GameObject[] disableOnEndObjects;

    // ----------------------------
    // Internal variables
    // ----------------------------
    // How much time is left in the current round
    private float timeLeft;

    // Current player score
    private int score;

    // True when the round has ended (win or lose)
    private bool isGameOver;

    // Awake runs once when the object is created
    void Awake()
    {
        // Simple singleton so zombies can call AddKillScore
        // This prevents multiple game managers from existing at the same time
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Store this instance so other scripts can access it
        Instance = this;
    }

    // Start runs once right before the first Update
    void Start()
    {
        // Begin the round when the game starts
        StartRound();
    }

    // Update runs every frame
    void Update()
    {
        // If the round ended, stop updating the timer
        if (isGameOver) return;

        // Subtract time each frame
        timeLeft -= Time.deltaTime;

        // If time runs out, the player wins
        if (timeLeft <= 0f)
        {
            // Clamp the timer to 0 so it never goes negative
            timeLeft = 0f;

            // Trigger win logic
            WinRound();
        }

        // Update UI every frame so the player sees timer/score changes
        UpdateUI();
    }

    // ----------------------------
    // ROUND CONTROL
    // ----------------------------

    // Resets all values and starts the round fresh
    public void StartRound()
    {
        // The round is active again
        isGameOver = false;

        // Reset score back to 0
        score = 0;

        // Reset time back to the full round time
        timeLeft = roundTimeSeconds;

        // Clear any win/lose message
        if (statusText != null)
            statusText.text = "";

        // Re-enable gameplay scripts/objects
        SetEndStateEnabled(true);

        // Refresh UI right away
        UpdateUI();
    }

    // Handles winning (surviving until time reaches 0)
    private void WinRound()
    {
        // Prevent win from happening twice
        if (isGameOver) return;

        // Mark the game as ended
        isGameOver = true;

        // Show win message
        if (statusText != null)
            statusText.text = "SURVIVED!";

        // Disable gameplay scripts/objects so everything stops cleanly
        SetEndStateEnabled(false);

        // Update UI one last time after ending
        UpdateUI();

        // Optionally reload the scene after a delay
        if (autoResetOnWin)
            ResetScene(resetDelaySeconds);
    }

    // Handles losing (called when a zombie touches the player)
    public void LoseRound()
    {
        // Prevent lose from happening twice
        if (isGameOver) return;

        // Mark the game as ended
        isGameOver = true;

        // Show lose message
        if (statusText != null)
            statusText.text = "YOU DIED!";

        // Disable gameplay scripts/objects so everything stops cleanly
        SetEndStateEnabled(false);

        // Update UI one last time after ending
        UpdateUI();

        // Optionally reload the scene after a delay
        if (autoResetOnLose)
            ResetScene(resetDelaySeconds);
    }

    // ----------------------------
    // SCORING
    // ----------------------------

    // Adds score for a zombie kill
    public void AddKillScore()
    {
        // Do not add points after the round ends
        if (isGameOver) return;

        // Add the points for a kill
        score += pointsPerKill;

        // Update UI so score changes immediately
        UpdateUI();
    }

    // Adds any custom amount of points 
    public void AddScore(int amount)
    {
        // Do not add points after the round ends
        if (isGameOver) return;

        // Add the points
        score += amount;

        // Update UI so score changes immediately
        UpdateUI();
    }

    public int GetScore() => score;
    public float GetTimeLeft() => timeLeft;
    public bool IsGameOver() => isGameOver;

    // ----------------------------
    // UI
    // ----------------------------

    // Updates all UI text fields
    private void UpdateUI()
    {
        // Update the timer text if it exists
        if (timerText != null)
            timerText.text = FormatTime(timeLeft);

        // Update the score text if it exists
        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }

    // Converts seconds into MM:SS format
    private string FormatTime(float seconds)
    {
        // Round up so the timer feels fair 
        int total = Mathf.CeilToInt(seconds);

        // Convert total seconds into minutes and seconds
        int minutes = total / 60;
        int secs = total % 60;

        // Force 2 digits for minutes and seconds
        return minutes.ToString("00") + ":" + secs.ToString("00");
    }

    // ----------------------------
    // RESET
    // ----------------------------

    // Reloads the current scene after a delay
    public void ResetScene(float delay = 0f)
    {
        // Cancel any previous reload call so it doesn't happen twice
        CancelInvoke(nameof(ReloadScene));

        // If delay is 0, reload instantly
        if (delay <= 0f)
            ReloadScene();
        else
            Invoke(nameof(ReloadScene), delay);
    }

    // Actually reloads the current scene
    private void ReloadScene()
    {
        // Reload the active scene by build index
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ----------------------------
    // END STATE HANDLING
    // ----------------------------

    // Enables/disables scripts and objects when the game ends
    private void SetEndStateEnabled(bool enabled)
    {
        // Toggle scripts 
        if (disableOnEnd != null)
        {
            for (int i = 0; i < disableOnEnd.Length; i++)
            {
                if (disableOnEnd[i] != null)
                    disableOnEnd[i].enabled = enabled;
            }
        }

        // Toggle game objects 
        if (disableOnEndObjects != null)
        {
            for (int i = 0; i < disableOnEndObjects.Length; i++)
            {
                if (disableOnEndObjects[i] != null)
                    disableOnEndObjects[i].SetActive(enabled);
            }
        }
    }
}
