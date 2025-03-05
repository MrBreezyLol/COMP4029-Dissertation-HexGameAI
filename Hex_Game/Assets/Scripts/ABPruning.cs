using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ABPruning 
{
    private int maxDepth = 3;
    private Dictionary<string, int> transpositionTable = new Dictionary<string, int>();

    public Vector3Int FetchBestMove(HashSet<Vector3Int> availableMoves, HashSet<Vector3Int> redTiles, HashSet<Vector3Int> blueTiles, bool redTurn)
    {
        // Sort moves to improve pruning effectiveness
        List<Vector3Int> orderedMoves = OrderMoves(availableMoves, redTiles, blueTiles, redTurn);
        
        int bestVal = redTurn ? int.MinValue : int.MaxValue;
        Vector3Int bestMove = Vector3Int.zero;
        int alpha = int.MinValue;
        int beta = int.MaxValue;
        
        // Clear the transposition table before each new search
        transpositionTable.Clear();
        
        foreach(var move in orderedMoves)
        {
            // Use shallow copies with a temporary collection
            HashSet<Vector3Int> nextRedTiles = redTurn ? AddToSet(redTiles, move) : redTiles;
            HashSet<Vector3Int> nextBlueTiles = !redTurn ? AddToSet(blueTiles, move) : blueTiles;
            
            int val = AlphaBeta(RemoveFromSet(availableMoves, move), nextRedTiles, nextBlueTiles, !redTurn, maxDepth, alpha, beta);
            
            if(redTurn && val > bestVal)
            {
                bestVal = val;
                bestMove = move;
                alpha = Mathf.Max(bestVal, alpha);
            }
            else if(!redTurn && val < bestVal)
            {
                bestVal = val;
                bestMove = move;
                beta = Mathf.Min(bestVal, beta);
            }
        }
        
        return bestMove;
    }
    
    private int AlphaBeta(HashSet<Vector3Int> availableMoves, HashSet<Vector3Int> redTiles, HashSet<Vector3Int> blueTiles, bool redTurn, int depth, int alpha, int beta)
    {
        // Immediate win check
        if(CheckForWin(redTiles, true))
        {
            return 1000 + depth;
        }
        if(CheckForWin(blueTiles, false))
        {
            return -1000 - depth;
        }
        
        // Terminal node check
        if(depth == 0 || availableMoves.Count == 0)
        {
            return EvaluateBoard(availableMoves, redTiles, blueTiles);
        }
        
        // Check transposition table
        string boardKey = GetBoardHash(redTiles, blueTiles, redTurn);
        if (transpositionTable.TryGetValue(boardKey, out int cachedValue))
        {
            return cachedValue;
        }
        
        // Order moves to improve pruning
        List<Vector3Int> orderedMoves = OrderMoves(availableMoves, redTiles, blueTiles, redTurn);
        
        int bestVal;
        if(redTurn)
        {
            bestVal = int.MinValue;
            foreach(var move in orderedMoves)
            {
                // Use shallow copies with a temporary collection
                HashSet<Vector3Int> nextRedTiles = AddToSet(redTiles, move);
                
                int val = AlphaBeta(RemoveFromSet(availableMoves, move), nextRedTiles, blueTiles, false, depth - 1, alpha, beta);
                bestVal = Mathf.Max(bestVal, val);
                alpha = Mathf.Max(alpha, bestVal);
                
                if(beta <= alpha)
                {
                    break; // Beta cutoff
                }
            }
        }
        else
        {
            bestVal = int.MaxValue;
            foreach(var move in orderedMoves)
            {
                // Use shallow copies with a temporary collection
                HashSet<Vector3Int> nextBlueTiles = AddToSet(blueTiles, move);
                
                int val = AlphaBeta(RemoveFromSet(availableMoves, move), redTiles, nextBlueTiles, true, depth - 1, alpha, beta);
                bestVal = Mathf.Min(bestVal, val);
                beta = Mathf.Min(beta, bestVal);
                
                if(beta <= alpha)
                {
                    break; // Alpha cutoff
                }
            }
        }
        
        // Store result in transposition table
        transpositionTable[boardKey] = bestVal;
        
        return bestVal;
    }
    
    // Efficient method to get a new set with one additional item
    private HashSet<Vector3Int> AddToSet(HashSet<Vector3Int> original, Vector3Int item)
    {
        HashSet<Vector3Int> newSet = new HashSet<Vector3Int>(original);
        newSet.Add(item);
        return newSet;
    }
    
    // Efficient method to get a new set with one removed item
    private HashSet<Vector3Int> RemoveFromSet(HashSet<Vector3Int> original, Vector3Int item)
    {
        HashSet<Vector3Int> newSet = new HashSet<Vector3Int>(original);
        newSet.Remove(item);
        return newSet;
    }
    
    // Move ordering heuristic to improve pruning efficiency
    private List<Vector3Int> OrderMoves(HashSet<Vector3Int> availableMoves, HashSet<Vector3Int> redTiles, HashSet<Vector3Int> blueTiles, bool redTurn)
    {
        List<Vector3Int> orderedMoves = new List<Vector3Int>(availableMoves);
        
        // Use a simple heuristic to order moves: prioritize center and moves adjacent to existing pieces
        orderedMoves.Sort((a, b) => {
            int scoreA = EvaluateMoveScore(a, redTiles, blueTiles, redTurn);
            int scoreB = EvaluateMoveScore(b, redTiles, blueTiles, redTurn);
            return redTurn ? scoreB.CompareTo(scoreA) : scoreA.CompareTo(scoreB);
        });
        
        return orderedMoves;
    }
    
    // Simple move scoring for move ordering
    private int EvaluateMoveScore(Vector3Int move, HashSet<Vector3Int> redTiles, HashSet<Vector3Int> blueTiles, bool redTurn)
    {
        Vector2Int offset = TileOffset(move);
        int score = 0;
        
        // Prioritize center positions
        Vector2Int boardCenter = new Vector2Int(5, 5);
        int distanceToCenter = Mathf.Abs(offset.x - boardCenter.x) + Mathf.Abs(offset.y - boardCenter.y);
        score += (11 - distanceToCenter) * 2;
        
        // Prioritize positions adjacent to existing pieces of the same color
        HashSet<Vector3Int> friendlyTiles = redTurn ? redTiles : blueTiles;
        foreach (var tile in friendlyTiles)
        {
            Vector2Int offsetTile = TileOffset(tile);
            if (IsAdjacent(offset, offsetTile))
            {
                score += 5;
            }
        }
        
        // Consider blocking opponent pieces too
        HashSet<Vector3Int> enemyTiles = redTurn ? blueTiles : redTiles;
        foreach (var tile in enemyTiles)
        {
            Vector2Int offsetTile = TileOffset(tile);
            if (IsAdjacent(offset, offsetTile))
            {
                score += 3;
            }
        }
        
        // Prioritize edge positions for connecting strategy
        if (redTurn)
        {
            if (offset.y == 0 || offset.y == 10) score += 8;  // Top/bottom edges for red
        }
        else
        {
            if (offset.x == 0 || offset.x == 10) score += 8;  // Left/right edges for blue
        }
        
        return score;
    }
    
    private bool IsAdjacent(Vector2Int a, Vector2Int b)
    {
        foreach (var neighbor in GetNeighbors(a))
        {
            if (neighbor.Equals(b)) return true;
        }
        return false;
    }
    
    // Create a unique board state hash for the transposition table
    private string GetBoardHash(HashSet<Vector3Int> redTiles, HashSet<Vector3Int> blueTiles, bool redTurn)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        // Add a tag for whose turn it is
        sb.Append(redTurn ? "R:" : "B:");
        
        // Add all red tiles (sorted to ensure consistency)
        List<Vector3Int> sortedRedTiles = new List<Vector3Int>(redTiles);
        sortedRedTiles.Sort((a, b) => {
            if (a.x != b.x) return a.x.CompareTo(b.x);
            if (a.y != b.y) return a.y.CompareTo(b.y);
            return a.z.CompareTo(b.z);
        });
        
        foreach (var tile in sortedRedTiles)
        {
            sb.Append($"{tile.x},{tile.y},{tile.z};");
        }
        
        sb.Append("|");
        
        // Add all blue tiles (sorted to ensure consistency)
        List<Vector3Int> sortedBlueTiles = new List<Vector3Int>(blueTiles);
        sortedBlueTiles.Sort((a, b) => {
            if (a.x != b.x) return a.x.CompareTo(b.x);
            if (a.y != b.y) return a.y.CompareTo(b.y);
            return a.z.CompareTo(b.z);
        });
        
        foreach (var tile in sortedBlueTiles)
        {
            sb.Append($"{tile.x},{tile.y},{tile.z};");
        }
        
        return sb.ToString();
    }
    
    private int EvaluateBoard(HashSet<Vector3Int> availableMoves, HashSet<Vector3Int> redTiles, HashSet<Vector3Int> blueTiles)
    {
        int redAdjFactor = EvaluateAdjFactor(redTiles, true);
        int blueAdjFactor = EvaluateAdjFactor(blueTiles, false);

        int redPositionFactor = EvaluatePositionFactor(redTiles, true);
        int bluePositionFactor = EvaluatePositionFactor(blueTiles, false);
        
        // int redPathFactor = EvaluatePathFactor(redTiles, true);
        // int bluePathFactor = EvaluatePathFactor(blueTiles, false);

        return (redAdjFactor + redPositionFactor) - (blueAdjFactor + bluePositionFactor);
    }
    
    private int EvaluateAdjFactor(HashSet<Vector3Int> playerTiles, bool redTurn)
    {
        int count = 0;
        HashSet<Vector2Int> offsetTiles = new HashSet<Vector2Int>();
        foreach(Vector3Int tile in playerTiles)
        {
            offsetTiles.Add(TileOffset(tile));
        }

        foreach(var tile in offsetTiles)
        {
            foreach(Vector2Int neighbour in GetNeighbors(tile))
            {
                if(offsetTiles.Contains(neighbour) && neighbour.x >= 0 && neighbour.x <= 10 && neighbour.y >= 0 && neighbour.y <= 10)
                {
                    count++;
                }
            }
        }
        
        return count * 5;
    }
    
    private int EvaluatePositionFactor(HashSet<Vector3Int> playerTiles, bool redTurn)
    {
        int reward = 0;
        HashSet<Vector2Int> offsetTiles = new HashSet<Vector2Int>();
        Vector2Int boardCenter = new Vector2Int(5,5);
        foreach(Vector3Int tile in playerTiles)
        {
            offsetTiles.Add(TileOffset(tile));
        }
        foreach(var tile in offsetTiles)
        {
            int distance = Mathf.Abs(tile.x - boardCenter.x) + Mathf.Abs(tile.y - boardCenter.y);

            int val = 11 - distance;

            reward += 2 * val;
            if(tile.x == boardCenter.x || tile.y == boardCenter.y)
            {
                reward += 5;
            }
        }
        return reward;
    }
    
    // Improved path evaluation that actually returns a score
    private int EvaluatePathFactor(HashSet<Vector3Int> playerTiles, bool redTurn)
    {
        var offsetTiles = new HashSet<Vector2Int>();
        var startEdge = new HashSet<Vector2Int>();
        var endEdge = new HashSet<Vector2Int>();
        
        foreach (var cell in playerTiles)
        {
            Vector2Int preOffsetTiles = TileOffset(cell);
            offsetTiles.Add(preOffsetTiles);
            
            if (redTurn)
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
        
        if (startEdge.Count == 0 || endEdge.Count == 0) return 0;
        
        // Calculate shortest distance between any start and end edge tiles
        int shortestPath = int.MaxValue;
        foreach (var start in startEdge)
        {
            int distance = FindShortestPath(start, endEdge, offsetTiles);
            if (distance < shortestPath && distance > 0)
            {
                shortestPath = distance;
            }
        }
        
        // If no path found, return 0
        if (shortestPath == int.MaxValue) return 0;
        
        // A shorter path is better
        return 100 - (shortestPath * 5);
    }
    
    // Find the shortest path length between start and any end edge
    private int FindShortestPath(Vector2Int start, HashSet<Vector2Int> endEdge, HashSet<Vector2Int> offsetTiles)
    {
        Queue<(Vector2Int, int)> queue = new Queue<(Vector2Int, int)>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        
        queue.Enqueue((start, 0));
        visited.Add(start);
        
        while (queue.Count > 0)
        {
            (Vector2Int current, int distance) = queue.Dequeue();
            
            if (endEdge.Contains(current)) return distance;
            
            foreach (var neighbor in GetNeighbors(current))
            {
                if (offsetTiles.Contains(neighbor) &&
                    !visited.Contains(neighbor) &&
                    neighbor.x >= 0 && neighbor.x <= 10 &&
                    neighbor.y >= 0 && neighbor.y <= 10)
                {
                    visited.Add(neighbor);
                    queue.Enqueue((neighbor, distance + 1));
                }
            }
        }
        
        return int.MaxValue; // No path found
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
        var offsetTiles = new HashSet<Vector2Int>();
        var startEdge = new HashSet<Vector2Int>();
        var endEdge = new HashSet<Vector2Int>();
        
        foreach (var cell in playerTiles)
        {
            Vector2Int preOffsetTiles = TileOffset(cell);
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
        
        if (startEdge.Count == 0 || endEdge.Count == 0) return false;
        
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
            
            foreach (var neighbor in GetNeighbors(current))
            {
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
            coordinates + new Vector2Int(1, 0),
            coordinates + new Vector2Int(-1, 0),
            coordinates + new Vector2Int(0, 1),
            coordinates + new Vector2Int(0, -1),
            coordinates + new Vector2Int(1, -1),
            coordinates + new Vector2Int(-1, 1),
        };
    }
}