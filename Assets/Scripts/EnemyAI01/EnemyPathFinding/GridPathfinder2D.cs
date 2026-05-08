// =============================================================================
// GridPathfinder2D.cs
//
// PURPOSE:
//   Finds a path across a 2D Tilemap grid using the A* algorithm.
//   Returns a list of world positions the enemy can walk along.
//
// HOW TO USE:
//   1. Attach this component to an empty "Pathfinding" GameObject in the scene.
//   2. Assign your wall/blocked Tilemap to the wallTilemap field.
//   3. Call FindPath(startWorldPos, endWorldPos) from EnemyPathFollower.
//   4. Get back a List<Vector3> of cell-center world positions to walk along.
//
// HOW BLOCKED CELLS WORK:
//   If wallTilemap.GetTile(cell) returns anything other than null,
//   that cell is treated as blocked (a wall). No tile = walkable.
//   This means your wall Tilemap IS the pathfinding grid — no extra data needed.
//
// HOW WORLD POSITION <-> CELL WORKS:
//   Unity Tilemaps use integer cell coordinates (Vector3Int).
//   wallTilemap.WorldToCell(worldPos)        converts world → cell
//   wallTilemap.GetCellCenterWorld(cellPos)  converts cell center → world
//   These are the only two conversions needed.
//
// A* OVERVIEW:
//   A* is a best-first search algorithm. It explores the grid by always
//   picking the node that looks cheapest to reach the goal, based on:
//
//     gCost = exact cost from the start to this node (steps taken)
//     hCost = estimated cost from this node to the goal (Manhattan distance)
//     fCost = gCost + hCost   ← the combined "priority" score
//
//   Nodes with lower fCost are explored first. When we reach the goal node,
//   we trace back through parent links to reconstruct the full path.
//
// VERSION 1 SCOPE:
//   - 4-direction movement only (up, down, left, right)
//   - Manhattan distance heuristic (correct for 4-direction grids)
//   - Simple List-based open set (readable, not the fastest, but fine for
//     typical room/dungeon maps)
// =============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridPathfinder2D : MonoBehaviour
{
    // ── Inspector Fields ──────────────────────────────────────────────────────

    [Header("Tilemap Reference")]

    [Tooltip("The Tilemap that contains your wall or blocked tiles.\n" +
             "Any cell that has a tile in this Tilemap is treated as blocked.\n" +
             "Leave floor/walkable tiles on a DIFFERENT Tilemap layer.")]
    public Tilemap wallTilemap;

    // ── Public API ────────────────────────────────────────────────────────────

    // FindPath is the one method EnemyPathFollower will call.
    //
    // Parameters:
    //   startWorldPos  — the enemy's current world position
    //   endWorldPos    — the player's current world position
    //
    // Returns:
    //   A List<Vector3> of world positions from start to end.
    //   Each position is the CENTER of a tile cell.
    //   Returns an empty list if no path exists (surrounded by walls).
    //
    public List<Vector3> FindPath(Vector3 startWorldPos, Vector3 endWorldPos)
    {
        if (wallTilemap == null)
        {
            Debug.LogWarning("[GridPathfinder2D] wallTilemap is not assigned!");
            return new List<Vector3>();
        }

        // ── Step 1: Convert world positions to tile cell coordinates ──────────
        // WorldToCell snaps any world position to the integer cell it falls in.
        // This is how we go from "enemy is at (3.7, 1.2)" to "enemy is in cell (3,1)".
        Vector3Int startCell = wallTilemap.WorldToCell(startWorldPos);
        Vector3Int endCell   = wallTilemap.WorldToCell(endWorldPos);

        // If start and end are the same cell there is nothing to do
        if (startCell == endCell)
            return new List<Vector3>();

        // If the destination cell is blocked (inside a wall), no path is possible
        if (IsCellBlocked(endCell))
            return new List<Vector3>();

        // ── Step 2: Initialize A* data structures ─────────────────────────────
        //
        // Open Set  — cells we have DISCOVERED but not yet fully processed.
        //             We always pick the one with the lowest fCost next.
        //
        // Closed Set — cells we have ALREADY processed and won't revisit.
        //              We use a HashSet for O(1) lookups.
        //
        var openSet   = new List<PathNode>();
        var closedSet = new HashSet<Vector3Int>();

        // Create the starting node.
        // gCost = 0 because we haven't moved yet.
        // hCost = Manhattan distance to the goal.
        // parent = null because it has no predecessor.
        PathNode startNode = new PathNode(
            cell:   startCell,
            gCost:  0,
            hCost:  ManhattanDistance(startCell, endCell),
            parent: null
        );

        openSet.Add(startNode);

        // ── Step 3: A* main loop ──────────────────────────────────────────────
        //
        // Keep exploring until we find the goal or run out of reachable cells.
        //
        while (openSet.Count > 0)
        {
            // Pick the open node with the lowest fCost.
            // If two nodes tie on fCost, prefer the one closer to the goal (lower hCost).
            // This tie-breaking makes the path feel more direct.
            PathNode current = GetLowestFCostNode(openSet);

            // ── Did we reach the goal? ─────────────────────────────────────────
            if (current.cell == endCell)
            {
                // Trace back from goal → start using parent links,
                // then reverse to get start → goal order.
                return BuildWorldPath(current);
            }

            // ── Move current from open → closed ───────────────────────────────
            openSet.Remove(current);
            closedSet.Add(current.cell);

            // ── Examine all 4 neighbors ───────────────────────────────────────
            foreach (Vector3Int neighborCell in GetFourNeighbors(current.cell))
            {
                // Skip cells we already fully processed
                if (closedSet.Contains(neighborCell))
                    continue;

                // Skip blocked wall cells
                if (IsCellBlocked(neighborCell))
                    continue;

                // Each step costs 1 (uniform grid, no weighted terrain in v1)
                int newGCost = current.gCost + 1;

                // Is this neighbor already in the open set?
                PathNode existingNode = FindInOpenSet(openSet, neighborCell);

                if (existingNode == null)
                {
                    // Not yet discovered — add it to the open set
                    openSet.Add(new PathNode(
                        cell:   neighborCell,
                        gCost:  newGCost,
                        hCost:  ManhattanDistance(neighborCell, endCell),
                        parent: current
                    ));
                }
                else if (newGCost < existingNode.gCost)
                {
                    // We found a CHEAPER route to a node that was already in
                    // the open set. Update it so it now uses this better route.
                    existingNode.gCost  = newGCost;
                    existingNode.parent = current;
                    // hCost stays the same — the destination hasn't changed
                }
            }
        }

        // ── Step 4: No path found ─────────────────────────────────────────────
        // The open set emptied without reaching the goal.
        // The enemy is completely surrounded or the player is unreachable.
        Debug.Log("[GridPathfinder2D] No path found from " + startCell + " to " + endCell);
        return new List<Vector3>();
    }

    // ── Helper: Is this cell blocked? ────────────────────────────────────────
    //
    // GetTile returns null for empty cells and a TileBase object for cells
    // that have a tile painted on them. We use this as our "is this a wall?"
    // check — no physics raycasts, no extra data, just the Tilemap itself.
    //
    public bool IsCellBlocked(Vector3Int cell)
    {
        return wallTilemap.GetTile(cell) != null;
    }

    // ── Helper: Manhattan Distance (heuristic) ────────────────────────────────
    //
    // Manhattan distance counts how many horizontal + vertical steps it would
    // take to reach B from A if there were no walls in the way.
    // It is the correct heuristic for 4-direction grids — it never over-estimates
    // (which is required for A* to guarantee the shortest path).
    //
    // Example:  A=(1,1)  B=(4,3)  → distance = |4-1| + |3-1| = 3 + 2 = 5
    //
    private int ManhattanDistance(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    // ── Helper: Get 4 orthogonal neighbors ───────────────────────────────────
    //
    // Returns the cells directly above, below, left, and right of the given cell.
    // Diagonal movement is excluded in version 1.
    //
    private List<Vector3Int> GetFourNeighbors(Vector3Int cell)
    {
        return new List<Vector3Int>
        {
            new Vector3Int(cell.x + 1, cell.y,     cell.z),  // right
            new Vector3Int(cell.x - 1, cell.y,     cell.z),  // left
            new Vector3Int(cell.x,     cell.y + 1, cell.z),  // up
            new Vector3Int(cell.x,     cell.y - 1, cell.z),  // down
        };
    }

    // ── Helper: Get the node with the lowest fCost from the open set ──────────
    //
    // This is a simple linear search — O(n) per call.
    // For small-to-medium maps (under ~500 cells) this is perfectly fast.
    // Future upgrade: replace with a min-heap (priority queue) for large maps.
    //
    private PathNode GetLowestFCostNode(List<PathNode> openSet)
    {
        PathNode best = openSet[0];

        for (int i = 1; i < openSet.Count; i++)
        {
            PathNode candidate = openSet[i];

            bool lowerF    = candidate.fCost < best.fCost;
            bool sameF     = candidate.fCost == best.fCost;
            bool lowerH    = candidate.hCost < best.hCost;

            // Prefer lower fCost; on ties prefer the node closer to the goal
            if (lowerF || (sameF && lowerH))
                best = candidate;
        }

        return best;
    }

    // ── Helper: Find a node in the open set by cell coordinate ───────────────
    //
    // Returns the PathNode if found, or null if the cell is not in the open set.
    // Used to check if we already have a route to a neighbor,
    // and whether the new route we found is cheaper.
    //
    private PathNode FindInOpenSet(List<PathNode> openSet, Vector3Int cell)
    {
        foreach (PathNode node in openSet)
        {
            if (node.cell == cell)
                return node;
        }
        return null;
    }

    // ── Helper: Reconstruct the path from goal back to start ─────────────────
    //
    // A* records each node's parent (the node we came from).
    // To get the full path we start at the goal and follow parent links
    // back to the start. This gives us the path in REVERSE order,
    // so we reverse the list before returning.
    //
    // Each cell is converted to its CENTER in world space using
    // GetCellCenterWorld — this gives us the exact world position
    // the enemy should walk toward.
    //
    private List<Vector3> BuildWorldPath(PathNode goalNode)
    {
        var path    = new List<Vector3>();
        var current = goalNode;

        // Walk backwards from goal → start through parent links
        while (current != null)
        {
            Vector3 worldPos = wallTilemap.GetCellCenterWorld(current.cell);
            path.Add(worldPos);
            current = current.parent;
        }

        // Path is currently [goal, ..., start]. Reverse to get [start, ..., goal].
        path.Reverse();
        return path;
    }

    // =========================================================================
    // PathNode — Inner Class
    //
    // Represents one cell in the A* search.
    // Stores all the information A* needs to decide which cell to visit next
    // and how to reconstruct the path once the goal is found.
    //
    // WHY AN INNER CLASS?
    //   PathNode only makes sense in the context of this pathfinder.
    //   Keeping it here avoids cluttering the project with extra files.
    //
    // FIELDS:
    //   cell   — the Tilemap cell this node represents
    //   gCost  — exact number of steps to reach this cell from the start
    //   hCost  — estimated steps remaining to reach the goal (Manhattan)
    //   fCost  — gCost + hCost  (the priority score — lower is better)
    //   parent — the node we came from (used to trace the path back)
    // =========================================================================
    private class PathNode
    {
        public Vector3Int cell;    // which tile cell this node represents
        public int        gCost;   // cost from start to here (exact)
        public int        hCost;   // cost from here to goal (estimate)
        public int        fCost => gCost + hCost;  // combined priority score
        public PathNode   parent;  // where we came from (null for start node)

        public PathNode(Vector3Int cell, int gCost, int hCost, PathNode parent)
        {
            this.cell   = cell;
            this.gCost  = gCost;
            this.hCost  = hCost;
            this.parent = parent;
        }
    }
}
