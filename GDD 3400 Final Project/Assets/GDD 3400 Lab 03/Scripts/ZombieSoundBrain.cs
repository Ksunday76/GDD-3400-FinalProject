using UnityEngine;
using UnityEngine.AI;

// IMPORTANT: I Used ChatGPT to polished and clean up the code.
// Also used Chatgpt to add in additional comments throughout the code
// so it is easier to follow along with the logic.


public class ZombieSoundBrain : MonoBehaviour
{
    // This enum lists every behavior/state the zombie can be in.
    // Only one state should be active at a time.
    private enum State
    {
        Wander,
        InvestigateSound,
        Chase,
        Search
    }

    // ----------------------------
    // References 
    // ----------------------------
    [Header("References")]
    // The player transform (where the player is in the world)
    public Transform player;

    // Custom navigation script from the class lab (optional)
    public AINavigation aiNavigation;

    // Unity's built-in navigation component (optional if AINavigation is used)
    public NavMeshAgent agent;

    // ----------------------------
    // Audio
    // ----------------------------
    [Header("Audio")]
    // AudioSource used to play the zombie screech sound
    public AudioSource screechAudio;

    // These control how much the screech pitch changes each time
    [Header("Screech Pitch Variation")]
    public float minScreechPitch = 0.9f;
    public float maxScreechPitch = 1.1f;

    // ----------------------------
    // Perception (Vision)
    // ----------------------------
    [Header("Perception")]
    // How far the zombie can "see" the player
    public float visionRadius = 6f;

    // How wide the zombie's vision cone is
    [Range(0f, 180f)] public float visionAngle = 60f;

    // Layers that block the zombie's view (ex: walls, props)
    public LayerMask visionBlockers;

    // ----------------------------
    // Perception (Hearing)
    // ----------------------------
    [Header("Hearing")]
    // Extra hearing range added on top of the sound radius
    public float hearingBoost = 0f;

    // ----------------------------
    // Movement speeds for each state
    // ----------------------------
    [Header("Movement Speeds")]
    public float wanderSpeed = 2.5f;
    public float investigateSpeed = 3.0f;
    public float chaseSpeed = 5.0f;

    // ----------------------------
    // Wander behavior settings
    // ----------------------------
    [Header("Wander")]
    // How far the zombie can roam from its spawn point
    public float wanderRadius = 10f;

    // How close the zombie must be to count as "arrived"
    public float wanderPointTolerance = 1.2f;

    // How often the zombie chooses a new wander destination
    public float wanderRepathTime = 1.5f;

    // ----------------------------
    // Investigate/Search settings
    // ----------------------------
    [Header("Investigate/Search")]
    // How close the zombie must be to the sound location to count as "arrived"
    public float investigatePointTolerance = 1.5f;

    // How long the zombie searches before giving up
    public float searchDuration = 3f;

    // How far away search points can be from the last known player position
    public float searchRadius = 4f;

    // ----------------------------
    // Group screech settings (used to attract other zombies)
    // ----------------------------
    [Header("Group Screech")]
    // Turns screeching on/off
    public bool enableScreech = true;

    // How far the screech sound event spreads (AI hearing radius)
    public float screechRadius = 15f;

    // Cooldown before this zombie can screech again
    public float screechCooldown = 5f;

    // ----------------------------
    // Internal state and memory values
    // ----------------------------

    // Tracks the current state of the zombie
    private State state = State.Wander;

    // Where the zombie started (used for wandering)
    private Vector3 spawnPoint;

    // Stores the most recent sound position heard
    private Vector3 lastSoundPos;

    // True if the zombie currently has a sound location to investigate
    private bool hasSoundTarget = false;

    // Remembers where the zombie last saw the player
    private Vector3 lastKnownPlayerPos;

    // Timer used while searching
    private float searchTimer = 0f;

    // Timer used to decide when to repick wander points
    private float wanderTimer = 0f;

    // Timer used to control screech cooldown
    private float screechTimer = 0f;

    // Used to make sure the zombie only screeches once per chase start
    private bool screechUsedThisChase = false;

    // Awake runs once when the object is created
    void Awake()
    {
        // If the NavMeshAgent was not assigned, try to grab it automatically
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        // If the AINavigation script was not assigned, try to grab it automatically
        if (aiNavigation == null) aiNavigation = GetComponent<AINavigation>();

        // If the screech AudioSource was not assigned, try to grab one automatically
        if (screechAudio == null)
            screechAudio = GetComponent<AudioSource>();
    }

    // Start runs once right before the first Update
    void Start()
    {
        // Save the spawn location so wandering stays around this area
        spawnPoint = transform.position;

        // If we do not have the player reference, try to find the Player by tag
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        // Start the zombie in the Wander state
        SetState(State.Wander);

        // Pick an initial wander point right away
        PickNewWanderPoint();
    }

    // OnEnable is called when the object becomes active
    void OnEnable()
    {
        // Subscribe to the SoundEventManager so the zombie can hear sound events
        SoundEventManager.OnSoundEmitted += OnSoundHeard;
    }

    // OnDisable is called when the object becomes inactive
    void OnDisable()
    {
        // Unsubscribe to avoid memory leaks and duplicate event calls
        SoundEventManager.OnSoundEmitted -= OnSoundHeard;
    }

    // Update runs every frame
    void Update()
    {
        // Count down the screech cooldown timer
        if (screechTimer > 0f) screechTimer -= Time.deltaTime;

        // Check if the zombie can currently see the player
        bool canSeePlayer = CanSeePlayer();

        // If the zombie can see the player, it should chase immediately
        if (canSeePlayer)
        {
            // Save the player location in case the zombie loses sight later
            lastKnownPlayerPos = player.position;

            // Switch to chase if not already chasing
            if (state != State.Chase) SetState(State.Chase);
        }

        // Run logic based on the current state
        switch (state)
        {
            case State.Wander:
                // Roam around the spawn area
                TickWander();

                // If a sound was heard and we do not see the player, investigate the sound
                if (hasSoundTarget && !canSeePlayer) SetState(State.InvestigateSound);
                break;

            case State.InvestigateSound:
                // Walk toward the last sound heard
                TickInvestigate();

                // If we reached the sound and still don't see the player, start searching
                if (!canSeePlayer && ReachedDestination(investigatePointTolerance))
                {
                    SetState(State.Search);
                }
                break;

            case State.Chase:
                // Move directly toward the player while visible
                TickChase();

                // If the player is lost, switch to searching
                if (!canSeePlayer)
                {
                    SetState(State.Search);
                }
                break;

            case State.Search:
                // Move around last known player area for a short time
                TickSearch();

                // If a new sound is heard while searching, investigate it
                if (hasSoundTarget && !canSeePlayer) SetState(State.InvestigateSound);
                break;
        }
    }

    // -----------------------
    // SOUND LISTENER
    // -----------------------

    // This function gets called whenever ANY sound event happens in the scene
    private void OnSoundHeard(Vector3 soundPos, float radius)
    {
        // Measure how far this zombie is from the sound
        float dist = Vector3.Distance(transform.position, soundPos);

        // If close enough, the zombie "hears" the sound
        if (dist <= radius + hearingBoost)
        {
            // Store the sound location so we can move toward it
            lastSoundPos = soundPos;

            // Mark that we have a sound target to investigate
            hasSoundTarget = true;

            // If not currently chasing the player, switch to investigate mode
            if (state != State.Chase)
            {
                SetState(State.InvestigateSound);
                SetDestination(lastSoundPos);
            }
        }
    }

    // -----------------------
    // STATES
    // -----------------------

    // Changes the zombie to a new state and sets up that state's behavior
    private void SetState(State newState)
    {
        // If leaving Chase, allow screech next time chase starts
        if (state == State.Chase && newState != State.Chase)
        {
            screechUsedThisChase = false;
        }

        // Update the current state
        state = newState;

        // Set up the behavior for the new state
        switch (state)
        {
            case State.Wander:
                // Wander uses slower movement
                SetSpeed(wanderSpeed);

                // Reset wander timer so movement stays consistent
                wanderTimer = 0f;

                // Clear sound target so wandering isn't interrupted by old sounds
                hasSoundTarget = false;
                break;

            case State.InvestigateSound:
                // Investigating uses a medium speed
                SetSpeed(investigateSpeed);

                // Move toward the sound location if we have one
                if (hasSoundTarget) SetDestination(lastSoundPos);
                break;

            case State.Chase:
                // Chasing uses the fastest speed
                SetSpeed(chaseSpeed);

                // Clear sound target because the player is now the focus
                hasSoundTarget = false;

                // Screech once at the start of chasing to alert nearby zombies
                if (enableScreech && !screechUsedThisChase && screechTimer <= 0f)
                {
                    // Play the screech audio with a random pitch so it sounds less repetitive
                    if (screechAudio != null)
                    {
                        screechAudio.pitch = Random.Range(minScreechPitch, maxScreechPitch);
                        screechAudio.Play();
                    }

                    // Emit a sound event so other zombies can hear the screech
                    SoundEventManager.EmitSound(transform.position, screechRadius);

                    // Mark screech used so it doesn't spam every frame
                    screechUsedThisChase = true;

                    // Start the cooldown timer for the next screech
                    screechTimer = screechCooldown;
                }
                break;

            case State.Search:
                // Searching moves at investigate speed
                SetSpeed(investigateSpeed);

                // Reset the search timer so searching lasts the full duration
                searchTimer = searchDuration;

                // Pick the first search point near last known player location
                PickSearchPoint();
                break;
        }
    }

    // -----------------------
    // STATE TICKS
    // -----------------------

    // Handles wandering behavior
    private void TickWander()
    {
        // Count up how long we've been wandering toward a point
        wanderTimer += Time.deltaTime;

        // If we reached the point OR enough time passed, choose a new point
        if (ReachedDestination(wanderPointTolerance) || wanderTimer >= wanderRepathTime)
        {
            PickNewWanderPoint();
            wanderTimer = 0f;
        }
    }

    // Handles investigating sound behavior
    private void TickInvestigate()
    {
        // Continue moving toward the last heard sound
        if (hasSoundTarget)
        {
            SetDestination(lastSoundPos);
        }
    }

    // Handles chasing behavior
    private void TickChase()
    {
        // Safety check
        if (player == null) return;

        // Move toward the player's current position
        SetDestination(player.position);

        // Keep updating last known position as we chase
        lastKnownPlayerPos = player.position;
    }

    // Handles searching behavior
    private void TickSearch()
    {
        // Count down how long the zombie will keep searching
        searchTimer -= Time.deltaTime;

        // If we reached the current search point, pick another one while time remains
        if (ReachedDestination(1.2f))
        {
            if (searchTimer > 0f)
                PickSearchPoint();
        }

        // If search time is over, go back to wandering
        if (searchTimer <= 0f)
        {
            SetState(State.Wander);
            PickNewWanderPoint();
        }
    }

    // -----------------------
    // MOVEMENT HELPERS
    // -----------------------

    // Sets the destination using either AINavigation or NavMeshAgent
    private void SetDestination(Vector3 worldPos)
    {
        // If the custom AINavigation exists, use it
        if (aiNavigation != null)
        {
            aiNavigation.SetDestination(worldPos);
        }
        // Otherwise fall back to the NavMeshAgent
        else if (agent != null)
        {
            agent.SetDestination(worldPos);
        }
    }

    // Changes how fast the zombie moves
    private void SetSpeed(float speed)
    {
        // NavMeshAgent controls movement speed
        if (agent != null) agent.speed = speed;
    }

    // Checks if the zombie has reached its destination
    private bool ReachedDestination(float tolerance)
    {
        // If there is no agent, treat it like we arrived
        if (agent == null) return true;

        // If the agent is still calculating a path, we haven't arrived yet
        if (agent.pathPending) return false;

        // If the agent has no path, treat it like we arrived
        if (!agent.hasPath) return true;

        // If remaining distance is small enough, we count it as arrived
        return agent.remainingDistance <= tolerance;
    }

    // Picks a random point near the spawn point for wandering
    private void PickNewWanderPoint()
    {
        // Pick a random direction and distance within wanderRadius
        Vector3 random = spawnPoint + Random.insideUnitSphere * wanderRadius;

        // Keep the height flat for ground movement
        random.y = spawnPoint.y;

        // Find the closest valid NavMesh position and move there
        if (TryGetNavPoint(random, 2.5f, out Vector3 navPoint))
        {
            SetDestination(navPoint);
        }
    }

    // Picks a random search point near the last known player position
    private void PickSearchPoint()
    {
        // Use the last seen player location as the search center
        Vector3 center = lastKnownPlayerPos;

        // Pick a random nearby point
        Vector3 random = center + Random.insideUnitSphere * searchRadius;

        // Keep the height flat for ground movement
        random.y = center.y;

        // If we find a valid NavMesh point, move there
        if (TryGetNavPoint(random, 2.0f, out Vector3 navPoint))
        {
            SetDestination(navPoint);
        }
        else
        {
            // If sampling fails, just go to last known player position
            SetDestination(lastKnownPlayerPos);
        }
    }

    // Attempts to convert a random point into a valid NavMesh point
    private bool TryGetNavPoint(Vector3 point, float maxDist, out Vector3 navPoint)
    {
        // NavMesh.SamplePosition finds the nearest valid point on the navmesh
        if (NavMesh.SamplePosition(point, out NavMeshHit hit, maxDist, NavMesh.AllAreas))
        {
            navPoint = hit.position;
            return true;
        }

        // If it fails, return the original point as a fallback
        navPoint = point;
        return false;
    }

    // -----------------------
    // VISION
    // -----------------------

    // Checks if the zombie can see the player using distance + angle + raycast
    private bool CanSeePlayer()
    {
        // If we don't have the player reference, we cannot see them
        if (player == null) return false;

        // Get direction to the player (ignoring vertical difference)
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        // If the player is too far away, we cannot see them
        float dist = toPlayer.magnitude;
        if (dist > visionRadius) return false;

        // If the player is outside the vision cone, we cannot see them
        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
        if (angle > visionAngle * 0.5f) return false;

        // Set raycast start and end points around "eye level"
        Vector3 origin = transform.position + Vector3.up * 1.2f;
        Vector3 target = player.position + Vector3.up * 1.0f;

        // Create a ray direction toward the player
        Vector3 dir = (target - origin).normalized;

        // Ray length equals the distance between origin and target
        float rayDist = Vector3.Distance(origin, target);

        // If something blocks the ray (like a wall), the zombie cannot see the player
        if (Physics.Raycast(origin, dir, rayDist, visionBlockers))
            return false;

        // If nothing blocks the ray, the zombie can see the player
        return true;
    }

#if UNITY_EDITOR
    // Gizmos are debug visuals that only show in the editor
    void OnDrawGizmosSelected()
    {
        // Draw the vision radius when this zombie is selected
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, visionRadius);

        // Draw the wander radius when this zombie is selected
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Application.isPlaying ? spawnPoint : transform.position, wanderRadius);
    }
#endif
}
