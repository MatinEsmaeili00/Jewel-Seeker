// =============================================================================
// EnemyProjectile.cs
//
// PURPOSE:
//   A simple projectile that moves in a straight line and destroys itself
//   when it hits a wall, the player, or runs out of lifetime.
//
// HOW IT WORKS:
//   After being spawned, EnemyShooter calls SetDirection() to give this
//   projectile its travel direction. From that point, Update() moves it
//   forward by (moveSpeed × Time.deltaTime) every frame.
//
// HOW IT DETECTS HITS:
//   We use OnTriggerEnter2D — the projectile's collider must be set to
//   Is Trigger = true in the Inspector (or on the prefab).
//   When anything enters the trigger:
//     - If it is the player (tag "Player") → apply damage placeholder, destroy
//     - If it is a wall → destroy
//   Everything else is ignored (other enemies, decorations, etc.)
//
// LIFETIME:
//   The projectile destroys itself after 'lifetime' seconds even if it
//   never hits anything. This prevents projectiles from flying forever off-screen.
//
// SETUP CHECKLIST (for the prefab):
//   1. Add a CircleCollider2D (or CapsuleCollider2D) — set Is Trigger = true
//   2. Add a Rigidbody2D — set Body Type = Kinematic, Gravity Scale = 0
//      (Kinematic + no gravity = moves only where we tell it, no physics)
//   3. Add a SpriteRenderer for the bullet visual
//   4. Add this EnemyProjectile component
//   5. Set the projectile's layer so it doesn't collide with itself or enemies
//
// NOTE ON WALL DETECTION:
//   The simplest way to detect walls is by checking the layer of what we hit.
//   Set your wall Tilemap to a specific layer (e.g. "Walls") and assign it
//   to the wallLayerMask field. Anything on that layer destroys the projectile.
//
// =============================================================================

using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyProjectile : MonoBehaviour
{
    // ── Inspector Fields ──────────────────────────────────────────────────────

    [Header("Movement")]

    [Tooltip("How fast this projectile travels in world units per second.")]
    [Range(1f, 30f)]
    public float moveSpeed = 10f;

    [Header("Lifetime")]

    [Tooltip("The projectile destroys itself after this many seconds.\n" +
             "Prevents it from flying forever if it misses everything.")]
    [Range(0.5f, 10f)]
    public float lifetime = 4f;

    [Header("Collision")]

    [Tooltip("The physics layer mask that counts as walls.\n" +
             "Projectile destroys itself when it hits any collider on these layers.")]
    public LayerMask wallLayerMask;

    // ── Private State ─────────────────────────────────────────────────────────

    // The direction this projectile travels. Set by EnemyShooter via SetDirection().
    // It is a normalized Vector2 (length = 1) so moveSpeed controls the speed cleanly.
    private Vector2 _direction = Vector2.right;

    // How many seconds of lifetime remain before auto-destruction
    private float _lifetimeRemaining;

    // ── Unity Messages ────────────────────────────────────────────────────────

    private void Awake()
    {
        // Start the lifetime countdown as soon as the object is created
        _lifetimeRemaining = lifetime;
    }

    private void Update()
    {
        // ── Move forward ───────────────────────────────────────────────────────
        // Translate = move the transform by an amount each frame.
        // Time.deltaTime ensures the speed is the same regardless of frame rate.
        // A projectile moving at moveSpeed=10 travels 10 world units per second.
        //transform.Translate(_direction * moveSpeed * Time.deltaTime);
        transform.Translate(_direction * moveSpeed * Time.deltaTime, Space.World);

        // ── Count down lifetime ────────────────────────────────────────────────
        _lifetimeRemaining -= Time.deltaTime;
        if (_lifetimeRemaining <= 0f)
        {
            Destroy(gameObject);
        }
    }

    // ── Trigger Hit Detection ─────────────────────────────────────────────────
    //
    // OnTriggerEnter2D fires when another collider overlaps ours.
    // Our collider must have Is Trigger = true for this to work.
    //
    // We check what we hit by:
    //   a) The tag   — "Player" tag tells us it's the player
    //   b) The layer — wallLayerMask tells us it's a wall
    //
    private void OnTriggerEnter2D(Collider2D other)
    {
        // ── Did we hit the player? ─────────────────────────────────────────────
        if (other.CompareTag("Player"))
        {
            // Placeholder: log the hit. Replace this with your damage system later.
            // Example future upgrade:
            //   PlayerHealth health = other.GetComponent<PlayerHealth>();
            //   if (health != null) health.TakeDamage(damage);
            Debug.Log("[EnemyProjectile] Hit the player!");
            SceneManager.LoadScene("SampleScene");
            Destroy(gameObject);
            return;
        }

        // ── Did we hit a wall? ─────────────────────────────────────────────────
        // LayerMask bit check: (wallLayerMask.value & (1 << other.gameObject.layer)) != 0
        // This returns true if the hit object's layer is in our wallLayerMask.
        bool hitWall = (wallLayerMask.value & (1 << other.gameObject.layer)) != 0;
        if (hitWall)
        {
            Destroy(gameObject);
            return;
        }

        // Anything else (other enemies, decorations, etc.) — ignore the hit
    }

    // ── Public API ────────────────────────────────────────────────────────────

    // Called by EnemyShooter immediately after spawning this projectile.
    // direction should be a normalized Vector2 pointing toward the target.
    public void SetDirection(Vector2 direction)
    {
        _direction = direction.normalized;

        // Optional: rotate the projectile sprite to face the travel direction.
        // Angle in degrees: Mathf.Atan2 converts a 2D direction to an angle.
        // This only matters if your bullet sprite isn't a circle.
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        // Draw the travel direction as a short arrow in Scene view
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position,
                        transform.position + (Vector3)(_direction * 0.4f));
    }
}
