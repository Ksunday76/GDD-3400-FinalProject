using UnityEngine;

// IMPORTANT: I Used ChatGPT to polished and clean up the code.
// Also used Chatgpt to add in additional comments throughout the code
// so it is easier to follow along with the logic.

// This script is responsible for handling all player-related noise.
// It plays audio (gunshots and footsteps) and emits sound events
// that enemies can detect.
public class PlayerNoiseEmitter : MonoBehaviour
{
    // ----------------------------
    // Sound detection radii
    // ----------------------------

    // How far enemies can hear a gunshot
    public float gunshotRadius = 25f;

    // How far enemies can hear footsteps
    public float footstepRadius = 6f;

    // ----------------------------
    // Footstep timing settings
    // ----------------------------

    // Time between footstep sounds while moving
    public float stepInterval = 0.5f;

    // Minimum movement speed required to count as "moving"
    public float minMoveSpeed = 0.1f;

    // ----------------------------
    // Audio references
    // ----------------------------

    // AudioSource used to play the gunshot sound
    public AudioSource gunshotAudio;

    // AudioSource used to play the footstep sound
    public AudioSource footstepAudio;

    // ----------------------------
    // Internal variables
    // ----------------------------

    // Timer used to control how often footsteps play
    private float stepTimer = 0f;

    // Reference to the CharacterController so we can check movement speed
    private CharacterController characterController;

    // Awake runs once when the object is first created
    void Awake()
    {
        // Get the CharacterController on the player
        // This is used to read actual movement velocity
        characterController = GetComponent<CharacterController>();
    }

    // Update runs every frame
    void Update()
    {
        // Continuously check if footsteps should be played
        HandleFootsteps();
    }

    // ----------------------------
    // CALLED BY SHOOTING SCRIPT
    // ----------------------------

    // This function is called when the player fires a gun
    public void EmitGunshotNoise()
    {
        // If a gunshot AudioSource exists, play the sound
        if (gunshotAudio != null)
            gunshotAudio.Play();

        // Emit a sound event so enemies can hear the gunshot
        SoundEventManager.EmitSound(transform.position, gunshotRadius);
    }

    // ----------------------------
    // FOOTSTEP / MOVEMENT NOISE
    // ----------------------------

    // Handles footstep sounds and noise emission while the player moves
    private void HandleFootsteps()
    {
        // If the CharacterController is missing, do nothing
        if (characterController == null) return;

        // Calculate the player's horizontal movement speed
        float speed = new Vector3(
            characterController.velocity.x,
            0f,
            characterController.velocity.z
        ).magnitude;

        // Check if the player is moving fast enough AND is on the ground
        if (speed > minMoveSpeed && characterController.isGrounded)
        {
            // Increase the footstep timer
            stepTimer += Time.deltaTime;

            // If enough time has passed, play a footstep
            if (stepTimer >= stepInterval)
            {
                // Play the footstep sound if one exists
                if (footstepAudio != null)
                    footstepAudio.Play();

                // Emit a small sound event for enemies to hear
                SoundEventManager.EmitSound(transform.position, footstepRadius);

                // Reset the footstep timer
                stepTimer = 0f;
            }
        }
        else
        {
            // If the player stops moving, reset the timer
            stepTimer = 0f;
        }
    }
}
