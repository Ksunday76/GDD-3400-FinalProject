// This script is responsible for the shoot mechanic of the gun, it also handles entering and exiting the shoot animation state

using UnityEngine;
using System.Collections;

// Code is mostly the same except for the gunshot audio portion that I added in

public class ShootMechanic : MonoBehaviour
{
    [Header("Shoot Mechanic Settings")]
    [SerializeField] float _Cooldown = .25f; // The duration before the next shoot can be performed
    [SerializeField] Transform _ShootPoint; // This is the point where the bullet will spawn
    public Transform ShootPoint => _ShootPoint;

    [Header("Projectile Settings")]
    [SerializeField] Projectile _ProjectilePrefab; // This is the projectile prefab that will be spawned

    [Header("Optional: Noise")]
    [SerializeField] PlayerNoiseEmitter _NoiseEmitter; // Drag the PlayerNoiseEmitter here (or it will auto-find)

    // This gets and sets the aim target point in the world, necessary to keep updated
    private Vector3 _aimTargetPoint = Vector3.zero;
    public Vector3 AimTargetPoint
    {
        get => _aimTargetPoint;
        set => _aimTargetPoint = value;
    }

    // This is the animator component
    Animator animator;

    Color gizmosColor = Color.green;

    Coroutine _ShootCooldown;

    // Initialize the animator and the gizmos color
    public void Awake()
    {
        animator = GetComponent<Animator>();
        gizmosColor = Random.ColorHSV(0f, 1f, 1f, 1f, .6f, .8f);

        // Auto-find if not assigned
        if (_NoiseEmitter == null)
            _NoiseEmitter = GetComponent<PlayerNoiseEmitter>();
    }

    // Draws a line and sphere to visualize the aim target point
    public void OnDrawGizmos()
    {
        if (_ShootPoint == null || _aimTargetPoint == Vector3.zero) return;

        Gizmos.color = gizmosColor;
        Gizmos.DrawLine(_ShootPoint.position, _aimTargetPoint);
        Gizmos.DrawSphere(_aimTargetPoint, 0.1f);
    }

    // Perform the shoot action
    public void PerformShoot()
    {
        // Check if we're on cooldown from our last shot
        if (_ShootCooldown != null) return;

        // Set the shoot animation to true so we can enter the shoot animation state
        animator.SetBool("Shoot", true);

        // Start the shoot action coroutine
        _ShootCooldown = StartCoroutine(ShootAction());
    }

    // Wait for the shoot animation to
    IEnumerator ShootAction()
    {
        // The initial time it takes to enter the shoot animation
        float initialWaitTime = 0.25f;

        // Wait for the initial time to enter the shoot animation
        yield return new WaitForSeconds(initialWaitTime);

        // Set the shoot animation to false so we can exit after the clip has finished
        animator.SetBool("Shoot", false);

        // Actually spawns the projectile when we are aimed up
        SpawnProjectile();

        // play gunshot noise when firing gun
        if (_NoiseEmitter != null)
            _NoiseEmitter.EmitGunshotNoise();
        else
            SoundEventManager.EmitSound(transform.position, 25f);

        // Wait for the cooldown before the next shoot can be performed
        yield return new WaitForSeconds(_Cooldown);
        _ShootCooldown = null;
    }

    // Spawns the projectile at the shoot point
    private void SpawnProjectile()
    {
        // Spawn the projectile at the shoot point and initialize it with the shooting direction and parent tag
        Projectile projectile = Instantiate(_ProjectilePrefab, _ShootPoint.position, Quaternion.identity).GetComponent<Projectile>();
        projectile.InitializeProjectile(_aimTargetPoint - _ShootPoint.position, this.gameObject.tag);
    }
}
