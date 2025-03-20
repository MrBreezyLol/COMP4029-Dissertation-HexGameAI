using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ABPruning 
{
    private int maxDepth = 3; // Reduced depth for faster computation
    private Dictionary<long, int> transpositionTable = new Dictionary<long, int>();
    private Vector2Int[] precalculatedNeighbors = new Vector2Int[] 
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, 1)
    };
    
    // Cache for offset calculations
    private Dictionary<Vector3Int, Vector2Int> offsetCache = new Dictionary<Vector3Int, Vector2Int>();
    
    // Zobrist hashing keys for fast board state hashing
    private long[,,] zobristKeys;
    private long zobristTurn;
    
    public ABPruning()
    {
        // Initialize Zobrist keys for fast hashing
        System.Random rng = new System.Random(42); // Fixed seed for consistency
        zobristKeys = new long[11, 11, 2]; // [x, y, player]
        for (int x = 0; x < 11; x++)
        {
            for (int y = 0; y < 11; y++)
            {
                for (int p = 0; p < 2; p++)
                {
                    zobristKeys[x, y, p] = (long)rng.NextDouble() * long.MaxValue;
                }
            }
        }
        zobristTurn = (long)rng.NextDouble() * long.MaxValue;
    }
    
    public Vector3Int FetchBestMove(HashSet<Vector3Int> availableMoves, HashSet<Vector3Int> redTiles, HashSet<Vector3Int> blueTiles, bool redTurn)
    {
        // Clear caches at the start of a new search
        transpositionTable.Clear();
        offsetCache.Clear();
        
        // Make a quick check for immediate winning moves or blocking moves
        Vector3Int quickMove = QuickMoveCheck(availableMoves, redTiles, blueTiles, redTurn);
        if (quickMove != Vector3Int.zero)
        {
            return quickMove;
        }
        
        // Iterative deepening - start with shallow searches and go deeper if time permits
        Vector3Int bestMove = Vector3Int.zero;
        for (int depth = 1; depth <= maxDepth; depth++)
        {
            int alpha = int.MinValue;
            int beta = int.MaxValue;
            int bestVal = redTurn ? int.MinValue : int.MaxValue;
            
            // Sort moves to improve pruning - use our heuristic evaluation
            List<MoveScore> scoredMoves = new List<MoveScore>();
            foreach (var move in availableMoves)
            {
                int score = EvaluateMoveScore(move, redTiles, blueTiles, redTurn);
                scoredMoves.Add(new MoveScore(move, score));
            }
            
            // Sort in descending order for red, ascending for blue
            scoredMoves.Sort((a, b) => redTurn ? 
                b.Score.CompareTo(a.Score) : 
                a.Score.CompareTo(b.Score));
            
            foreach (var moveScore in scoredMoves)
            {
                Vector3Int move = moveScore.Move;
                
                HashSet<Vector3Int> nextRedTiles = redTurn ? 
                    new HashSet<Vector3Int>(redTiles) { move } : 
                    redTiles;
                    
                HashSet<Vector3Int> nextBlueTiles = !redTurn ? 
                    new HashSet<Vector3Int>(blueTiles) { move } : 
                    blueTiles;
                
                HashSet<Vector3Int> nextAvailableMoves = new HashSet<Vector3Int>(availableMoves);
                nextAvailableMoves.Remove(move);
                
                int val = AlphaBeta(nextAvailableMoves, nextRedTiles, nextBlueTiles, !redTurn, depth - 1, alpha, beta);
                
                if (redTurn && val > bestVal)
                {
                    bestVal = val;
                    bestMove = move;
                    alpha = Math.Max(alpha, bestVal);
                }
                else if (!redTurn && val < bestVal)
                {
                    bestVal = val;
                    bestMove = move;
                    beta = Math.Min(beta, bestVal);
                }
            }
        }
        
        return bestMove;
    }
    
    // Quick check for immediate wins or blocks
    private Vector3Int QuickMoveCheck(HashSet<Vector3Int> availableMoves, HashSet<Vector3Int> redTiles, HashSet<Vector3Int> blueTiles, bool redTurn)
    {
        // Check for winning moves
        foreach (var move in availableMoves)
        {
            if (redTurn)
            {
                HashSet<Vector3Int> testRedTiles = new HashSet<Vector3Int>(redTiles) { move };
                if (CheckForWin(testRedTiles, true))
                {
                    return move; // This move wins for red
                }
            }
            else
            {
                HashSet<Vector3Int> testBlueTiles = new HashSet<Vector3Int>(blueTiles) { move };
                if (CheckForWin(testBlueTiles, false))
                {
                    return move; // This move wins for blue
                }
            }
        }
        
        // Check for blocking opponent's potential win
        foreach (var move in availableMoves)
        {
            if (redTurn)
            {
                HashSet<Vector3Int> testBlueTiles = new HashSet<Vector3Int>(blueTiles) { move };
                if (CheckForWin(testBlueTiles, false))
                {
                    return move; // Block this winning move for blue
                }
            }
            else
            {
                HashSet<Vector3Int> testRedTiles = new HashSet<Vector3Int>(redTiles) { move };
                if (CheckForWin(testRedTiles, true))
                {
                    return move; // Block this winning move for red
                }
            }
        }
        
        return Vector3Int.zero; // No immediate win or block found
    }
    
    // Simple struct to pair moves with their scores for sorting
    private struct MoveScore
    {
        public Vector3Int Move;
        public int Score;
        
        public MoveScore(Vector3Int move, int score)
        {
            Move = move;
            Score = score;
        }
    }
    
    private int AlphaBeta(HashSet<Vector3Int> availableMoves, HashSet<Vector3Int> redTiles, HashSet<Vector3Int> blueTiles, bool redTurn, int depth, int alpha, int beta)
    {
        // Fast win check
        if (CheckForWin(redTiles, true)) return 1000 + depth;
        if (CheckForWin(blueTiles, false)) return -1000 - depth;
        
        // Terminal node check
        if (depth <= 0 || availableMoves.Count == 0)
        {
            return FastEvaluateBoard(redTiles, blueTiles);
        }
        
        // Check transposition table using Zobrist hashing
        long boardHash = ComputeZobristHash(redTiles, blueTiles, redTurn);
        if (transpositionTable.TryGetValue(boardHash, out int cachedValue))
        {
            return cachedValue;
        }
        
        int bestVal;
        
        // For very shallow depths, evaluate fewer moves to save time
        int movesToEvaluate = Math.Min(availableMoves.Count, depth == 1 ? 3 : availableMoves.Count);
        
        // Sort and select top moves
        List<MoveScore> scoredMoves = new List<MoveScore>();
        foreach (var move in availableMoves)
        {
            int score = EvaluateMoveScore(move, redTiles, blueTiles, redTurn);
            scoredMoves.Add(new MoveScore(move, score));
        }
        
        // Sort in order of most promising first
        scoredMoves.Sort((a, b) => redTurn ? 
            b.Score.CompareTo(a.Score) : 
            a.Score.CompareTo(b.Score));
            
        // Keep only the most promising moves
        if (scoredMoves.Count > movesToEvaluate)
        {
            scoredMoves.RemoveRange(movesToEvaluate, scoredMoves.Count - movesToEvaluate);
        }
        
        if (redTurn)
        {
            bestVal = int.MinValue;
            foreach (var moveScore in scoredMoves)
            {
                Vector3Int move = moveScore.Move;
                
                HashSet<Vector3Int> nextRedTiles = new HashSet<Vector3Int>(redTiles) { move };
                
                HashSet<Vector3Int> nextAvailableMoves = new HashSet<Vector3Int>(availableMoves);
                nextAvailableMoves.Remove(move);
                
                int val = AlphaBeta(nextAvailableMoves, nextRedTiles, blueTiles, false, depth - 1, alpha, beta);
                bestVal = Math.Max(bestVal, val);
                alpha = Math.Max(alpha, bestVal);
                
                if (beta <= alpha) break; // Beta cutoff
            }
        }
        else
        {
            bestVal = int.MaxValue;
            foreach (var moveScore in scoredMoves)
            {
                Vector3Int move = moveScore.Move;
                
                HashSet<Vector3Int> nextBlueTiles = new HashSet<Vector3Int>(blueTiles) { move };
                
                HashSet<Vector3Int> nextAvailableMoves = new HashSet<Vector3Int>(availableMoves);
                nextAvailableMoves.Remove(move);
                
                int val = AlphaBeta(nextAvailableMoves, redTiles, nextBlueTiles, true, depth - 1, alpha, beta);
                bestVal = Math.Min(bestVal, val);
                beta = Math.Min(beta, bestVal);
                
                if (beta <= alpha) break; // Alpha cutoff
            }
        }
        
        // Store result in transposition table
        transpositionTable[boardHash] = bestVal;
        
        return bestVal;
    }
    
    // Zobrist hashing for fast board state identification
    private long ComputeZobristHash(HashSet<Vector3Int> redTiles, HashSet<Vector3Int> blueTiles, bool redTurn)
    {
        long hash = 0;
        
        foreach (var tile in redTiles)
        {
            Vector2Int offset = GetCachedOffset(tile);
            if (offset.x >= 0 && offset.x < 11 && offset.y >= 0 && offset.y < 11)
            {
                hash ^= zobristKeys[offset.x, offset.y, 0]; // 0 for red
            }
        }
        
        foreach (var tile in blueTiles)
        {
            Vector2Int offset = GetCachedOffset(tile);
            if (offset.x >= 0 && offset.x < 11 && offset.y >= 0 && offset.y < 11)
            {
                hash ^= zobristKeys[offset.x, offset.y, 1]; // 1 for blue
            }
        }
        
        if (redTurn) hash ^= zobristTurn;
        
        return hash;
    }
    
    // Use cached offset calculation
    private Vector2Int GetCachedOffset(Vector3Int cell)
    {
        if (offsetCache.TryGetValue(cell, out Vector2Int offset))
        {
            return offset;
        }
        
        offset = TileOffset(cell);
        offsetCache[cell] = offset;
        return offset;
    }
    
    // Fast move evaluation for sorting
    private int EvaluateMoveScore(Vector3Int move, HashSet<Vector3Int> redTiles, HashSet<Vector3Int> blueTiles, bool redTurn)
    {
        Vector2Int offset = GetCachedOffset(move);
        int score = 0;
        
        // Center proximity (higher is better)
        Vector2Int boardCenter = new Vector2Int(5, 5);
        int distanceToCenter = Math.Abs(offset.x - boardCenter.x) + Math.Abs(offset.y - boardCenter.y);
        score += (11 - distanceToCenter) * 2;
        
        // Edge positions for connection strategy are valuable
        if (redTurn)
        {
            if (offset.y == 0 || offset.y == 10) score += 15; // Top/bottom edges for red
        }
        else
        {
            if (offset.x == 0 || offset.x == 10) score += 15; // Left/right edges for blue
        }
        
        // Adjacent to friendly tiles
        int adjacentFriendly = 0;
        HashSet<Vector3Int> friendlyTiles = redTurn ? redTiles : blueTiles;
        foreach (var tile in friendlyTiles)
        {
            Vector2Int offsetTile = GetCachedOffset(tile);
            if (IsAdjacent(offset, offsetTile))
            {
                adjacentFriendly++;
            }
        }
        score += adjacentFriendly * 10;
        
        // Adjacent to enemy tiles
        int adjacentEnemy = 0;
        HashSet<Vector3Int> enemyTiles = redTurn ? blueTiles : redTiles;
        foreach (var tile in enemyTiles)
        {
            Vector2Int offsetTile = GetCachedOffset(tile);
            if (IsAdjacent(offset, offsetTile))
            {
                adjacentEnemy++;
            }
        }
        score += adjacentEnemy * 5;
        
        return score;
    }
    
    // Fast check if two positions are adjacent
    private bool IsAdjacent(Vector2Int a, Vector2Int b)
    {
        foreach (var dir in precalculatedNeighbors)
        {
            if (a.x + dir.x == b.x && a.y + dir.y == b.y)
            {
                return true;
            }
        }
        return false;
    }
    
    // Simplified and fast board evaluation
    private int FastEvaluateBoard(HashSet<Vector3Int> redTiles, HashSet<Vector3Int> blueTiles)
    {
        // Quickly check connection potentials
        int redConnectScore = EvaluateConnectivity(redTiles, true);
        int blueConnectScore = EvaluateConnectivity(blueTiles, false);
        
        return redConnectScore - blueConnectScore;
    }
    
    // Evaluate connectivity potential
    private int EvaluateConnectivity(HashSet<Vector3Int> playerTiles, bool isRed)
    {
        // Convert to offset coordinates
        HashSet<Vector2Int> offsetTiles = new HashSet<Vector2Int>();
        foreach (var tile in playerTiles)
        {
            offsetTiles.Add(GetCachedOffset(tile));
        }
        
        int score = 0;
        
        // Count tiles on opposing edges (more is better)
        int startEdgeCount = 0;
        int endEdgeCount = 0;
        
        foreach (var tile in offsetTiles)
        {
            if (isRed)
            {
                if (tile.y == 0) startEdgeCount++;  // Top edge for red
                if (tile.y == 10) endEdgeCount++;   // Bottom edge for red
            }
            else
            {
                if (tile.x == 0) startEdgeCount++;  // Left edge for blue
                if (tile.x == 10) endEdgeCount++;   // Right edge for blue
            }
        }
        
        // Base score from edge presence
        score += (startEdgeCount + endEdgeCount) * 20;
        
        // Count connected groups (fewer is better - more consolidated)
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        int connectedGroups = 0;
        
        foreach (var tile in offsetTiles)
        {
            if (!visited.Contains(tile))
            {
                connectedGroups++;
                CountConnectedTiles(tile, offsetTiles, visited);
            }
        }
        
        // Penalize multiple disconnected groups
        score -= connectedGroups * 15;
        
        // Bonus for tiles
        score += offsetTiles.Count * 10;
        
        // Add connectivity density - more connections is better
        int connectionCount = 0;
        foreach (var tile in offsetTiles)
        {
            foreach (var neighbor in GetNeighborsForTile(tile))
            {
                if (offsetTiles.Contains(neighbor))
                {
                    connectionCount++;
                }
            }
        }
        
        score += connectionCount * 5;
        
        return score;
    }
    
    // Count connected tiles in a group using DFS
    private void CountConnectedTiles(Vector2Int start, HashSet<Vector2Int> tiles, HashSet<Vector2Int> visited)
    {
        visited.Add(start);
        
        foreach (var neighbor in GetNeighborsForTile(start))
        {
            if (tiles.Contains(neighbor) && !visited.Contains(neighbor))
            {
                CountConnectedTiles(neighbor, tiles, visited);
            }
        }
    }
    
    // Get neighbors with bounds checking
    private IEnumerable<Vector2Int> GetNeighborsForTile(Vector2Int coordinates)
    {
        foreach (var dir in precalculatedNeighbors)
        {
            Vector2Int neighbor = coordinates + dir;
            if (neighbor.x >= 0 && neighbor.x <= 10 && neighbor.y >= 0 && neighbor.y <= 10)
            {
                yield return neighbor;
            }
        }
    }
    
    private Vector2Int TileOffset(Vector3Int cell)
    {
        int y = cell.y;
        int row = 5 - y;
        int rowCalc = (6 - y) / 2;
        int xOffset = -7 + rowCalc;
        int column = cell.x - xOffset;
        return new Vector2Int(column, row);
    }
    
    private bool CheckForWin(HashSet<Vector3Int> playerTiles, bool isRed)
    {
        // Fast exit if not enough tiles
        if (playerTiles.Count < 11) return false;
        
        var offsetTiles = new HashSet<Vector2Int>();
        var startEdge = new HashSet<Vector2Int>();
        var endEdge = new HashSet<Vector2Int>();
        
        foreach (var cell in playerTiles)
        {
            Vector2Int preOffsetTiles = GetCachedOffset(cell);
            offsetTiles.Add(preOffsetTiles);
            
            if (isRed)
            {
                if (preOffsetTiles.y == 0) startEdge.Add(preOffsetTiles);  //top
                if (preOffsetTiles.y == 10) endEdge.Add(preOffsetTiles);   //bottom
            }
            else
            {
                if (preOffsetTiles.x == 0) startEdge.Add(preOffsetTiles);  //left
                if (preOffsetTiles.x == 10) endEdge.Add(preOffsetTiles);   //right
            }
        }
        
        // Quick exit if no tiles on either edge
        if (startEdge.Count == 0 || endEdge.Count == 0) return false;
        
        // Use BFS to check for connection
        foreach (var start in startEdge)
        {
            if (BreadthFirstSearch(start, endEdge, offsetTiles))
                return true;
        }
        return false;
    }
    
    private bool BreadthFirstSearch(Vector2Int start, HashSet<Vector2Int> endEdge, HashSet<Vector2Int> offsetTiles)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        
        queue.Enqueue(start);
        visited.Add(start);
        
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (endEdge.Contains(current)) return true;
            
            foreach (var dir in precalculatedNeighbors)
            {
                Vector2Int neighbor = current + dir;
                if (offsetTiles.Contains(neighbor) &&
                    !visited.Contains(neighbor) &&
                    neighbor.x >= 0 && neighbor.x <= 10 &&
                    neighbor.y >= 0 && neighbor.y <= 10)
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }
        return false;
    }
    
    private List<Vector2Int> GetNeighbors(Vector2Int coordinates)
    {
        return new List<Vector2Int>
        {
            coordinates + precalculatedNeighbors[0],
            coordinates + precalculatedNeighbors[1],
            coordinates + precalculatedNeighbors[2],
            coordinates + precalculatedNeighbors[3],
            coordinates + precalculatedNeighbors[4],
            coordinates + precalculatedNeighbors[5]
        };
    }
}