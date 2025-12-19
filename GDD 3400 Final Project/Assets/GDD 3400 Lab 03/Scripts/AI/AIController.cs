using UnityEngine;

// IMPORTANT: I Used ChatGPT to polished and clean up the code.
// Also used Chatgpt to add in additional comments throughout the code
// so it is easier to follow along with the logic.


public class AIController : MonoBehaviour
{
    // ----------------------------
    // Enemy stats and settings
    // ----------------------------

    // The enemy's total health
    [SerializeField] int _Health = 100;

    // If true, the enemy will continuously move toward the player
    [SerializeField] bool _TrackPlayer = false;

    // How often the enemy updates its path toward the player
    [SerializeField] float _ReNavigateInterval = .5f;

    // ----------------------------
    // References
    // ----------------------------

    // Reference to the player controller
    PlayerController _player;

    // Reference to the navigation system used to move the enemy
    AINavigation _navigation;

    // ----------------------------
    // Internal variables
    // ----------------------------

    // Tracks how much time has passed since the last navigation update
    float _timeSinceLastNavigate = 0f;

    // Used to prevent the enemy from dying or scoring multiple times
    bool _isDead = false;

    // Awake runs once when the object is first created
    void Awake()
    {
        // Find the player in the scene
        _player = FindFirstObjectByType<PlayerController>();

        // Get the navigation component used for movement
        _navigation = this.GetComponent<AINavigation>();
    }

    // Update runs every frame
    void Update()
    {
        // Only track the player if tracking is enabled and the player exists
        if (_TrackPlayer && _player != null)
        {
            // Count up time since the last navigation update
            _timeSinceLastNavigate += Time.deltaTime;

            // If enough time has passed, update the destination
            if (_timeSinceLastNavigate >= _ReNavigateInterval)
            {
                // Move toward the player's current position
                _navigation.SetDestination(_player.transform.position);

                // Reset the timer
                _timeSinceLastNavigate = 0f;
            }
        }
    }

    // ----------------------------
    // DAMAGE HANDLING
    // ----------------------------

    // Called when the enemy takes damage (ex: from a projectile)
    public void TakeDamage(int damage)
    {
        // If the enemy is already dead, ignore further damage
        if (_isDead) return;

        // Print damage info for debugging
        Debug.Log("AI took damage: " + damage);

        // Subtract damage from health
        _Health -= damage;

        // If health reaches zero or below, kill the enemy
        if (_Health <= 0)
        {
            Die();
        }
    }

    // ----------------------------
    // DEATH HANDLING
    // ----------------------------

    // Handles what happens when the enemy dies
    private void Die()
    {
        // Prevent death logic from running more than once
        if (_isDead) return;

        // Mark the enemy as dead
        _isDead = true;

        // Add score to the player when the enemy dies
        if (SurvivalGameManager.Instance != null)
        {
            SurvivalGameManager.Instance.AddKillScore();
        }

        // Remove the enemy from the scene
        Destroy(this.gameObject);
    }
}
