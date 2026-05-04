// =============================================================================
// EnemyShooter.cs
//
// PURPOSE:
//   Handles the actual act of firing a projectile.
//   EnemyCombatAI calls TryShoot() when it decides the enemy should fire.
//   This script decides whether enough time has passed since the last shot
//   (fire rate cooldown) and, if so, spawns a projectile.
//
// WHY SEPARATE FROM EnemyCombatAI?
//   EnemyCombatAI makes decisions ("should I shoot?").
//   EnemyShooter executes the action ("here is the bullet").
//   Separating them means you can change the weapon (faster fire rate,
//   burst fire, shotgun spread) without touching the AI decision logic.
//
// HOW FIRE RATE WORKS:
//   fireRate is expressed in shots-per-second.
//     fireRate = 1.0  → one shot per second
//     fireRate = 2.0  → two shots per second (shot every 0.5 s)
//     fireRate = 0.5  → one shot every 2 seconds
//
//   Internally we track a cooldown timer (_cooldownRemaining).
//   After each shot it is set to (1 / fireRate).
//   Each frame it counts down by Time.deltaTime.
//   TryShoot() only fires when the cooldown has reached zero.
//
// HOW THE PROJECTILE IS SPAWNED:
//   We use Object.Instantiate() to create a copy of the projectileP prefab.
//   The copy is placed at the shootPoint Transform's position.
//   Then we call SetDirection() on the new projectile so it knows which
//   way to travel.
//
// =============================================================================

using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    // ── Inspector Fields ──────────────────────────────────────────────────────

    [Header("Projectile")]

    [Tooltip("The projectile prefab to spawn when the enemy fires.\n" +
             "This should be a GameObject with an EnemyProjectile component on it.")]
    public GameObject projectilePrefab;

    [Tooltip("Where the projectile spawns.\n" +
             "Create an empty child GameObject at the enemy's gun/mouth position\n" +
             "and drag it here. The projectile will appear at this point.")]
    public Transform shootPoint;

    [Header("Fire Rate")]

    [Tooltip("How many shots per second.\n" +
             "1.0 = one shot per second. 2.0 = two shots per second.")]
    [Range(0.1f, 10f)]
    public float fireRate = 1.0f;

    // ── Private State ─────────────────────────────────────────────────────────

    // Time remaining before the next shot is allowed (in seconds).
    // Counts down to zero each frame; resets to (1 / fireRate) after each shot.
    private float _cooldownRemaining = 0f;

    // ── Unity Messages ────────────────────────────────────────────────────────

    private void Update()
    {
        // Count the cooldown down every frame regardless of whether we shoot.
        // This ensures the timer is always ready when EnemyCombatAI calls TryShoot.
        if (_cooldownRemaining > 0f)
            _cooldownRemaining -= Time.deltaTime;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    // TryShoot is called by EnemyCombatAI every frame while in the Aiming state.
    //
    // Parameters:
    //   direction — normalized 2D vector pointing from enemy toward the player.
    //               Calculated in EnemyCombatAI and passed in here.
    //
    // The method fires ONLY if:
    //   1. The cooldown has expired (_cooldownRemaining <= 0)
    //   2. A projectile prefab is assigned
    //   3. A shoot point is assigned
    //
    public void TryShoot(Vector2 direction)
    {
        // Not ready to fire yet — wait for the cooldown to expire
        if (_cooldownRemaining > 0f) return;

        // Safety checks — missing references will just skip the shot silently
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[EnemyShooter] projectilePrefab is not assigned on " + gameObject.name);
            return;
        }

        if (shootPoint == null)
        {
            Debug.LogWarning("[EnemyShooter] shootPoint is not assigned on " + gameObject.name);
            return;
        }

        // ── Spawn the projectile ───────────────────────────────────────────────
        // Instantiate creates a new copy of the prefab in the scene.
        // We place it at the shootPoint's position with no rotation (identity).
        // We do NOT parent it to the enemy — it should fly freely through the world.
        GameObject projectileObj = Instantiate(
            projectilePrefab,
            shootPoint.position,
            Quaternion.identity
        );

        // ── Set the projectile's travel direction ──────────────────────────────
        // The EnemyProjectile script on the prefab needs to know which way to go.
        EnemyProjectile projectile = projectileObj.GetComponent<EnemyProjectile>();

        if (projectile != null)
        {
            projectile.SetDirection(direction);
        }
        else
        {
            Debug.LogWarning("[EnemyShooter] The projectile prefab does not have an " +
                             "EnemyProjectile component. Was the wrong prefab assigned?");
        }

        // ── Reset the fire cooldown ────────────────────────────────────────────
        // 1 / fireRate converts "shots per second" into "seconds per shot".
        // Example: fireRate = 2.0 → cooldown = 0.5 s between shots.
        _cooldownRemaining = 1f / fireRate;
    }
}
