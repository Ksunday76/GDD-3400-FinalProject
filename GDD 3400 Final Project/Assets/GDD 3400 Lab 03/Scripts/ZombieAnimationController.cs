using UnityEngine;
using UnityEngine.AI;

// IMPORTANT: I Used ChatGPT to polished and clean up the code.
// Also used Chatgpt to add in additional comments throughout the code
// so it is easier to follow along with the logic.


public class ZombieAnimationController : MonoBehaviour
{
    // ----------------------------
    // References
    // ----------------------------

    // Reference to the Animator component
    Animator animator;

    // Reference to the NavMeshAgent used for movement
    NavMeshAgent agent;

    // Awake runs once when the object is created
    void Awake()
    {
        // Get the Animator component on this object
        animator = GetComponent<Animator>();

        // Get the NavMeshAgent component on this object
        agent = GetComponent<NavMeshAgent>();
    }

    // Update runs every frame
    void Update()
    {
        // If either the agent or animator is missing, stop here
        if (agent == null || animator == null) return;

        // Get the current movement speed of the zombie
        float speed = agent.velocity.magnitude;

        // If the zombie is moving faster than a small threshold,
        // set IsMoving to true so the walk animation plays.
        // Otherwise, the idle animation will play.
        animator.SetBool("IsMoving", speed > 0.1f);
    }
}
