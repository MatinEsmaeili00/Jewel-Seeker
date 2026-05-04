// =============================================================================
// EnemyCombatAI.cs
//
// PURPOSE:
//   This is the "brain" of the enemy. Every frame it reads the situation
//   (how far is the player? is there a wall in the way?) and decides what
//   the enemy should do: chase, aim and shoot, or reposition.
//
//   It does NOT do movement math — that stays in EnemyPathFollower.
//   It does NOT do shooting math — that stays in EnemyShooter.
//   It ONLY makes decisions and tells the other scripts what to do.
//
// THE THREE STATES:
//
//   Chasing        — player is outside attack range.
//                    Let EnemyPathFollower move normally.
//
//   Aiming         — player is inside attack range AND line of sight is clear.
//                    Stop EnemyPathFollower. Tell EnemyShooter to fire.
//
//   Repositioning  — player is inside attack range BUT a wall is in the way.
//                    Resume EnemyPathFollower so the enemy walks around the
//                    wall and finds a clear shot. Enemy never stands frozen.
//
// HOW IT CONNECTS TO EnemyPathFollower:
//   We flip pathFollower.enabled on and off.
//   When enabled  = true  → pathFollower's Update() runs → enemy moves.
//   When enabled  = false → pathFollower's Update() is skipped → enemy stops.
//   No changes to EnemyPathFollower.cs are needed.
//
// HOW LINE OF SIGHT WORKS:
//   Physics2D.Raycast fires an invisible ray from the enemy toward the player.
//   If the ray hits a wall collider before reaching the player → blocked.
//   If the ray hits nothing (reaches the player freely) → clear.
//   We only cast against the wall layer, so the enemy's own collider and
//   other enemies do not interfere.
//
// HIERARCHY:
//   Enemy (root)
//   ├── [EnemyCombatAI]        ← this script
//   ├── [EnemyPathFollower]    ← movement (controlled by this script)
//   ├── [EnemyShooter]         ← weapon (called by this script)
//   ├── [EnemyLightController] ← glow (completely independent)
//   └── SpriteRenderer
//
// =============================================================================

using UnityEngine;

public class EnemyCombatAI : MonoBehaviour
{
    // ── The Three States ──────────────────────────────────────────────────────
    //
    // Using an enum makes state reading very clear in the Inspector and gizmos.
    // We will show the current state in OnDrawGizmos so you can see it live
    // in the Scene view without adding a UI overlay.
    //
    private enum CombatState
    {
        Chasing,        // player is far — move toward them
        Aiming,         // player is in range, LOS is clear — stop and shoot
        Repositioning   // player is in range, LOS is blocked — keep moving
    }

    // ── Inspector Fields ──────────────────────────────────────────────────────

    [Header("Required References")]

    [Tooltip("The player's Transform. This is the target the enemy chases and shoots at.")]
    public Transform playerTransform;

    [Tooltip("The EnemyPathFollower on this enemy.\n" +
             "This AI enables/disables it to start and stop movement.")]
    public EnemyPathFollower pathFollower;

    [Tooltip("The EnemyShooter on this enemy.\n" +
             "This AI calls TryShoot() on it when conditions are right.")]
    public EnemyShooter shooter;

    [Header("Combat Range")]

    [Tooltip("The enemy stops chasing and tries to shoot when the player enters this range.\n" +
             "Measured in world units. Tune this to match your tile size.")]
    [Range(1f, 20f)]
    public float attackRange = 6f;

    [Header("Line of Sight")]

    [Tooltip("The physics layer(s) that count as walls for the LOS check.\n" +
             "Set this to the layer your wall Tilemap collider is on.\n" +
             "IMPORTANT: do NOT include the player or enemy layer here.")]
    public LayerMask wallLayerMask;

    [Tooltip("Small offset applied to the cast origin so it starts just outside\n" +
             "the enemy's own collider and does not hit itself.\n" +
             "0.1 to 0.3 usually works well.")]
    [Range(0f, 0.5f)]
    public float rayOriginOffset = 0.15f;

    [Tooltip("Radius of the line-of-sight circle cast in world units.\n" +
             "Match this to your projectile's CircleCollider2D radius so the LOS\n" +
             "check and the actual bullet use the same amount of clearance.\n" +
             "Rule of thumb: set this equal to the projectile collider radius.\n" +
             "Start at 0.1 and increase until false-clear shots stop happening.")]
    [Range(0.01f, 1f)]
    public float losRadius = 0.1f;

    // ── Private State ─────────────────────────────────────────────────────────

    // The current combat state. Changes drive enabling/disabling pathFollower.
    private CombatState _state = CombatState.Chasing;

    // Cached result of the last LOS check.
    // Used by gizmos so we can draw the ray color without rechecking every frame.
    private bool _hasLineOfSight = false;

    // ── Unity Messages ────────────────────────────────────────────────────────

    private void Start()
    {
        if (playerTransform == null)
            Debug.LogError("[EnemyCombatAI] playerTransform not assigned on " + gameObject.name);
        if (pathFollower == null)
            Debug.LogError("[EnemyCombatAI] pathFollower not assigned on " + gameObject.name);
        if (shooter == null)
            Debug.LogError("[EnemyCombatAI] shooter not assigned on " + gameObject.name);
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // Every frame: measure situation, pick a state, act on it.
        EvaluateSituation();
        ActOnCurrentState();
    }

    // ── Core Logic: EvaluateSituation ────────────────────────────────────────
    //
    // Reads distance and line-of-sight, then transitions to the correct state.
    //
    // The decision tree:
    //
    //   distance > attackRange
    //     → Chase  (keep walking toward player)
    //
    //   distance <= attackRange  AND  LOS is clear
    //     → Aim    (stop walking, shoot)
    //
    //   distance <= attackRange  AND  LOS is blocked
    //     → Reposition  (keep walking to find a clear angle)
    //
    private void EvaluateSituation()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        bool playerIsInRange   = distanceToPlayer <= attackRange;

        if (!playerIsInRange)
        {
            // Too far to shoot — just chase
            _hasLineOfSight = false;
            TransitionToState(CombatState.Chasing);
            return;
        }

        // Player is within attack range — check if there's a clear shot
        _hasLineOfSight = CheckLineOfSight();

        if (_hasLineOfSight)
            TransitionToState(CombatState.Aiming);
        else
            TransitionToState(CombatState.Repositioning);
    }

    // ── Core Logic: ActOnCurrentState ────────────────────────────────────────
    //
    // Executes the behavior for whichever state we are in.
    // Movement on/off is handled in TransitionToState, so we only handle
    // shooting logic here.
    //
    private void ActOnCurrentState()
    {
        if (_state == CombatState.Aiming)
        {
            // Calculate the direction from enemy to player
            Vector2 shootDirection = ((Vector2)playerTransform.position
                                    - (Vector2)transform.position).normalized;

            // Ask the shooter to fire — it handles its own fire rate cooldown
            shooter.TryShoot(shootDirection);
        }

        // Chasing and Repositioning need no extra logic here —
        // EnemyPathFollower handles the actual movement automatically.
    }

    // ── Core Logic: TransitionToState ────────────────────────────────────────
    //
    // Changes the current state and performs any one-time setup needed.
    // We check if the state actually changed so we do not spam enable/disable.
    //
    private void TransitionToState(CombatState newState)
    {
        if (_state == newState) return;  // already in this state — do nothing

        _state = newState;

        switch (newState)
        {
            case CombatState.Chasing:
            case CombatState.Repositioning:
                // Resume movement — re-enable the path follower and ask it
                // to request a fresh path on its next Update tick.
                pathFollower.enabled = true;
                pathFollower.ForceRepath();
                break;

            case CombatState.Aiming:
                // Stop movement — disable the path follower so its Update
                // does not run and the enemy stands still to aim.
                pathFollower.enabled = false;
                break;
        }
    }

    // ── Core Logic: CheckLineOfSight ─────────────────────────────────────────
    //
    // Sweeps a CIRCLE from the enemy toward the player using CircleCast.
    // Returns true if the corridor is clear, false if a wall blocks the path.
    //
    // WHY CircleCast INSTEAD OF Raycast?
    //   Raycast is an infinitely thin line — it can report "clear" even when
    //   a wall is just a few pixels to the side of the ray. The actual
    //   projectile (which has thickness / a collider radius) then clips the
    //   wall on the way to the player, making the enemy look like it is
    //   shooting through walls.
    //
    //   CircleCast sweeps a disc of radius losRadius along the same path.
    //   If ANY part of that disc touches a wall, the cast returns a hit.
    //   This correctly models "is there enough open space for my bullet?"
    //
    // HOW TO CHOOSE losRadius:
    //   Set it equal to (or slightly larger than) the projectile's
    //   CircleCollider2D radius. That way the LOS check uses exactly the same
    //   amount of clearance the bullet needs.
    //   Example: projectile collider radius = 0.08  →  set losRadius = 0.08
    //   If bullets still clip, raise losRadius by 0.02 until it stops.
    //
    // STEP-BY-STEP:
    //
    //   1. ORIGIN    — slightly in front of the enemy (rayOriginOffset) so the
    //                  circle does not overlap the enemy's own collider at t=0.
    //
    //   2. RADIUS    — losRadius (matches projectile thickness)
    //
    //   3. DIRECTION — unit vector from enemy to player
    //
    //   4. DISTANCE  — straight-line distance to player (cast stops here)
    //
    //   5. LAYERMASK — wall layer only; player collider is excluded so the
    //                  cast is not stopped by the player themselves
    //
    //   6. RESULT    — hit.collider == null → corridor is clear → SHOOT
    //                  hit.collider != null → wall in the way  → REPOSITION
    //
    private bool CheckLineOfSight()
    {
        // Vector from enemy to player
        Vector2 toPlayer  = (Vector2)playerTransform.position - (Vector2)transform.position;
        float   distance  = toPlayer.magnitude;
        Vector2 direction = toPlayer / distance;  // normalized

        // Offset the start point so the circle clears the enemy's own collider
        Vector2 castOrigin = (Vector2)transform.position + direction * rayOriginOffset;

        // CircleCast: sweep a disc of radius losRadius along the direction
        RaycastHit2D hit = Physics2D.CircleCast(
            origin:    castOrigin,
            radius:    losRadius,
            direction: direction,
            distance:  distance,
            layerMask: wallLayerMask
        );

        // null collider = the swept circle reached the player without touching a wall
        return hit.collider == null;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    // OnDrawGizmos runs every frame in the Scene view (not just when selected).
    // This lets you see the LOS ray for all enemies at once.
    private void OnDrawGizmos()
    {
        if (playerTransform == null) return;

        // Draw the line-of-sight ray:
        //   Green = enemy currently has a clear shot
        //   Red   = wall is blocking the shot
        Gizmos.color = _hasLineOfSight
            ? new Color(0.2f, 1f, 0.2f)   // green
            : new Color(1f, 0.2f, 0.2f);  // red

        Gizmos.DrawLine(transform.position, playerTransform.position);

        // Draw a small dot at the enemy position colored by state
        switch (_state)
        {
            case CombatState.Chasing:        Gizmos.color = Color.cyan;   break;
            case CombatState.Aiming:         Gizmos.color = Color.green;  break;
            case CombatState.Repositioning:  Gizmos.color = Color.yellow; break;
        }
        Gizmos.DrawSphere(transform.position + Vector3.up * 0.6f, 0.12f);
    }

    // OnDrawGizmosSelected runs only when this GameObject is selected.
    // Good for detailed debug info without cluttering the Scene view.
    private void OnDrawGizmosSelected()
    {
        // Attack range — the circle within which the enemy tries to shoot
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.25f);  // semi-transparent yellow
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
