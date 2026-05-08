// =============================================================================
// EnemyPathFollower.cs
//
// PURPOSE:
//   Attach this to an enemy GameObject.
//   It asks GridPathfinder2D for a path to the player, then moves the enemy
//   along that path one waypoint at a time.
//
// HOW IT WORKS (overview):
//   Every repathInterval seconds:
//     1. Call pathfinder.FindPath(myPosition, playerPosition)
//     2. Receive a List<Vector3> of world-space cell centers
//     3. Store it as _currentPath and reset the waypoint index to 0
//
//   Every frame (Update):
//     1. If the enemy is close enough to the player → stop moving
//     2. Otherwise, move toward _currentPath[_waypointIndex]
//     3. When the enemy arrives at a waypoint, advance to the next one
//
// MOVEMENT APPROACH:
//   We use Vector3.MoveTowards() for movement. This moves a fixed number of
//   world units per second (moveSpeed) and never overshoots the target.
//   It gives smooth, predictable movement with no physics jitter.
//   No Rigidbody needed for the movement itself (though you may still want
//   one for collision detection with other objects).
//
// RELATIONSHIP WITH GridPathfinder2D:
//   This script only handles movement and timing.
//   All A* logic stays in GridPathfinder2D — these two scripts never overlap.
//   You can swap out the pathfinding algorithm later without touching this file.
//
// HOW IT WORKS WITH THE ENEMY LIGHT SYSTEM:
//   EnemyLightController and this script are completely independent.
//   The glow follows the enemy Transform automatically (it's a child object).
//   No code changes needed — both scripts just live on the same enemy prefab.
//
// HIERARCHY EXAMPLE:
//   Enemy (root)
//   ├── [EnemyPathFollower]    ← this script
//   ├── [EnemyLightController] ← your existing glow system
//   ├── SpriteRenderer
//   └── GlowVisual             ← auto-created by EnemyLightController
//
// =============================================================================

using System.Collections.Generic;
using UnityEngine;

public class EnemyPathFollower : MonoBehaviour
{
    // ── Inspector Fields ──────────────────────────────────────────────────────

    [Header("Required References")]

    [Tooltip("The player's Transform. The enemy will pathfind toward this.")]
    public Transform playerTransform;

    [Tooltip("The GridPathfinder2D component in the scene.\n" +
             "Put it on a dedicated empty 'Pathfinding' GameObject.")]
    public GridPathfinder2D pathfinder;

    [Header("Movement")]

    [Tooltip("How fast the enemy moves along the path in world units per second.")]
    [Range(0.5f, 15f)]
    public float moveSpeed = 3f;

    [Tooltip("How close the enemy must be to a waypoint before advancing to the next one.\n" +
             "Keep this small (0.05–0.15) so the enemy follows the path tightly.")]
    [Range(0.02f, 0.5f)]
    public float waypointArriveDistance = 0.1f;

    [Header("Pathfinding")]

    [Tooltip("How often (in seconds) the enemy recalculates its path.\n" +
             "Lower = reacts faster to the player moving, but costs more CPU.\n" +
             "0.3–0.8 is a good range for most cases.")]
    [Range(0.1f, 3f)]
    public float repathInterval = 0.5f;

    [Header("Stopping")]

    [Tooltip("The enemy stops moving when it gets this close to the player.\n" +
             "Set this to roughly 1 tile width so the enemy doesn't push into the player.")]
    [Range(0.1f, 5f)]
    public float stoppingDistance = 0.8f;

    // ── Private State ─────────────────────────────────────────────────────────

    // The current list of world positions the enemy is walking along.
    // Rebuilt each time RequestNewPath() runs.
    private List<Vector3> _currentPath = new List<Vector3>();

    // Index into _currentPath — which waypoint are we heading toward right now?
    // Increments each time the enemy arrives at a waypoint.
    private int _waypointIndex = 0;

    // Countdown timer for path recalculation.
    // When it reaches zero we request a new path and reset it.
    private float _repathTimer = 0f;

    // ── Unity Messages ────────────────────────────────────────────────────────

    private void Start()
    {
        // Validate required references early so errors are clear
        if (playerTransform == null)
            Debug.LogError("[EnemyPathFollower] playerTransform is not assigned on " + gameObject.name);

        if (pathfinder == null)
            Debug.LogError("[EnemyPathFollower] pathfinder is not assigned on " + gameObject.name);

        // Request the first path immediately on start instead of waiting
        // for the first repathInterval to expire
        RequestNewPath();
    }

    private void Update()
    {
        if (playerTransform == null || pathfinder == null) return;

        // ── Stopping condition ─────────────────────────────────────────────────
        // If we are already close enough to the player, don't move.
        // This prevents the enemy from jittering against the player.
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer <= stoppingDistance)
            return;

        // ── Path recalculation timer ───────────────────────────────────────────
        // Count down and rebuild the path at the configured interval.
        // We don't want to recalculate every single frame — that is expensive
        // and causes the path to "flicker" as the enemy moves.
        _repathTimer -= Time.deltaTime;
        if (_repathTimer <= 0f)
        {
            RequestNewPath();
            _repathTimer = repathInterval;
        }

        // ── Follow the current path ────────────────────────────────────────────
        MoveAlongPath();
    }

    // ── Core Logic: Request a New Path ───────────────────────────────────────
    //
    // Calls the pathfinder with our current position and the player's position.
    // If a valid path is returned, we start following it from the beginning.
    //
    // Notes:
    //   - The first waypoint in the returned path is the center of the cell
    //     the enemy is currently standing in. We skip it if we are already
    //     close to it, to avoid a tiny backwards shuffle.
    //   - If no path is found (enemy boxed in), _currentPath is cleared
    //     and the enemy stops.
    //
    private void RequestNewPath()
    {
        List<Vector3> newPath = pathfinder.FindPath(transform.position, playerTransform.position);

        if (newPath.Count == 0)
        {
            // No path available — clear current path so enemy stops
            _currentPath.Clear();
            _waypointIndex = 0;
            return;
        }

        _currentPath   = newPath;
        _waypointIndex = 0;

        // The first waypoint is the center of the cell we are currently in.
        // If we are already very close to it (within arrive distance), skip it.
        // This avoids the enemy briefly moving backward to "touch" its own cell center.
        if (_currentPath.Count > 1)
        {
            float distToFirst = Vector2.Distance(transform.position, _currentPath[0]);
            if (distToFirst < waypointArriveDistance)
                _waypointIndex = 1;
        }
    }

    // ── Core Logic: Move Along the Current Path ───────────────────────────────
    //
    // Each frame, we move toward the current waypoint using MoveTowards.
    // When we arrive at the waypoint (within waypointArriveDistance),
    // we advance _waypointIndex to the next one.
    //
    // MoveTowards explanation:
    //   Vector3.MoveTowards(current, target, maxDelta)
    //   Moves 'current' toward 'target' by at most 'maxDelta' units.
    //   It NEVER overshoots — it stops exactly at target if close enough.
    //   maxDelta = moveSpeed * Time.deltaTime gives frame-rate-independent speed.
    //
    private void MoveAlongPath()
    {
        // Nothing to follow
        if (_currentPath.Count == 0) return;

        // All waypoints visited — wait for next repath
        if (_waypointIndex >= _currentPath.Count) return;

        Vector3 currentTarget = _currentPath[_waypointIndex];

        // Move toward the current waypoint this frame
        transform.position = Vector3.MoveTowards(
            transform.position,
            currentTarget,
            moveSpeed * Time.deltaTime
        );

        // Check if we arrived at this waypoint
        float distanceToWaypoint = Vector2.Distance(transform.position, currentTarget);
        bool arrivedAtWaypoint   = distanceToWaypoint <= waypointArriveDistance;

        if (arrivedAtWaypoint)
        {
            _waypointIndex++;
            // The next Update call will begin moving toward the next waypoint
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────
    // These methods let other scripts (e.g., an EnemyAI state machine) control
    // the follower at runtime.

    // Stop moving and clear the path.
    // Use this when the enemy dies, is stunned, or enters an idle state.
    public void StopFollowing()
    {
        _currentPath.Clear();
        _waypointIndex = 0;
    }

    // Force an immediate path recalculation on the next frame.
    // Useful after teleporting or when the player escapes through a door.
    public void ForceRepath()
    {
        _repathTimer = 0f;
    }

    // ── Gizmo Drawing ─────────────────────────────────────────────────────────
    //
    // These only run in the Editor — zero cost in a build.
    // Select the enemy in the Scene view to see:
    //   • Yellow spheres  = every waypoint on the current path
    //   • Green lines     = the path segments between waypoints
    //   • Red sphere      = the waypoint the enemy is currently heading toward
    //   • Cyan sphere     = stopping distance radius
    //
    private void OnDrawGizmos()
    {
        if (_currentPath == null || _currentPath.Count == 0) return;

        // Draw all waypoint positions
        Gizmos.color = Color.yellow;
        foreach (Vector3 waypoint in _currentPath)
        {
            Gizmos.DrawSphere(waypoint, 0.08f);
        }

        // Draw lines connecting the waypoints
        Gizmos.color = new Color(0.2f, 1f, 0.2f); // bright green
        for (int i = 0; i < _currentPath.Count - 1; i++)
        {
            Gizmos.DrawLine(_currentPath[i], _currentPath[i + 1]);
        }

        // Highlight the current target waypoint in red
        if (_waypointIndex < _currentPath.Count)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_currentPath[_waypointIndex], 0.13f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw the stopping distance as a wire circle (visible when selected)
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f); // transparent cyan
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
    }
}
