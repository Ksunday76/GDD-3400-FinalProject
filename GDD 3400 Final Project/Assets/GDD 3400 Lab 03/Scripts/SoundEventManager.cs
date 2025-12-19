using UnityEngine;
using System;

// IMPORTANT: I Used ChatGPT to polished and clean up the code.
// Also used Chatgpt to add in additional comments throughout the code
// so it is easier to follow along with the logic.

public class SoundEventManager : MonoBehaviour
{
    // Sound event: position + radius
    public static Action<Vector3, float> OnSoundEmitted;

    // Call this to create a sound event
    public static void EmitSound(Vector3 soundPosition, float radius)
    {
        // Visualize sound radius in editor
        Debug.DrawLine(soundPosition, soundPosition + Vector3.up * 2f, Color.yellow, 1f);

        // Notify all listeners
        OnSoundEmitted?.Invoke(soundPosition, radius);
    }
}
