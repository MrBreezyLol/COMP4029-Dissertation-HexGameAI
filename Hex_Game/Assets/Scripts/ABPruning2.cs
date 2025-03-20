using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ABPruning2
{
    private int maxDepth = 2;

    public Vector3Int FetchBestMove(HashSet<Vector3Int> availableMoves, HashSet<Vector3Int> redTiles, HashSet<Vector3Int> blueTiles, bool redTurn)
    {
        int maxVal = redTurn ? int.MinValue : int.MaxValue;
        Vector3Int bestMove = Vector3Int.zero;
        int alpha = int.MinValue;
        int beta = int.MaxValue;

        Vector3Int quickMove = QuickMove(availableMoves, redTiles, blueTiles, redTurn);
        if (quickMove != Vector3Int.zero)
        {
            return quickMove;
        }
        foreach(var move in availableMoves)
        {
            HashSet<Vector3Int> currentAvailableMoves = new HashSet<Vector3Int>(availableMoves);
            HashSet<Vector3Int> currentRedTiles = new HashSet<Vector3Int>(redTiles);
            HashSet<Vector3Int> currentBlueTiles = new HashSet<Vector3Int>(blueTiles);
            currentAvailableMoves.Remove(move);
            currentRedTiles.Add(move);
            
            int val = AlphaBeta(currentAvailableMoves, currentRedTiles, currentBlueTiles, false, maxDepth, alpha, beta);
            if(redTurn && val > maxVal)
            {
                maxVal = val;
                bestMove = move;
                alpha = Mathf.Max(maxVal, alpha);
            }
            else if(!redTurn && val < maxVal)
            {
                maxVal = val;
                bestMove = move;
                beta = Mathf.Min(maxVal, beta);
            }
            
        }
        return bestMove;
        
    }
    private int AlphaBeta(HashSet<Vector3Int> availableMoves, HashSet<Vector3Int> redTiles, HashSet<Vector3Int> blueTiles, bool redTurn, int depth, int alpha, int beta)
    {
        if(CheckForWin(redTiles, true))
        {
            return 1000 + depth;
        }
        if(CheckForWin(blueTiles, false))
        {
            return -1000 - depth;
        }
        if(depth == 0 || availableMoves.Count == 0)
        {
            return EvaluateBoard(availableMoves, redTiles, blueTiles);
        }
        if(redTurn)
        {
            int maxVal = int.MinValue;
            foreach(var move in availableMoves)
            {
                HashSet<Vector3Int> currentAvailableMoves = new HashSet<Vector3Int>(availableMoves);
                HashSet<Vector3Int> currentRedTiles = new HashSet<Vector3Int>(redTiles);
                HashSet<Vector3Int> currentBlueTiles = new HashSet<Vector3Int>(blueTiles);
                currentAvailableMoves.Remove(move);
                currentRedTiles.Add(move);

                int val = AlphaBeta(currentAvailableMoves, currentRedTiles, currentBlueTiles, false, depth - 1, alpha, beta);
                maxVal = Mathf.Max(maxVal, val);
                alpha = Mathf.Max(val, alpha);
                if(beta <= alpha)
                {
                    break;
                }
            }
            return maxVal;
        }
        else
        {
            int minVal = int.MaxValue;
            foreach(var move in availableMoves)
            {
                HashSet<Vector3Int> currentAvailableMoves = new HashSet<Vector3Int>(availableMoves);
                HashSet<Vector3Int> currentRedTiles = new HashSet<Vector3Int>(redTiles);
                HashSet<Vector3Int> currentBlueTiles = new HashSet<Vector3Int>(blueTiles);
                currentAvailableMoves.Remove(move);
                currentBlueTiles.Add(move);

                int val = AlphaBeta(currentAvailableMoves, currentRedTiles, currentBlueTiles, true, depth - 1, alpha, beta);
                minVal = Mathf.Min(minVal, val);
                beta = Mathf.Min(val, beta);
                if(beta <= alpha)
                {
                    break;
                }
            }
            return minVal;
        }
    }
    private Vector3Int QuickMove(HashSet<Vector3Int> availableMoves, HashSet<Vector3Int> redTiles, HashSet<Vector3Int> blueTiles, bool redTurn)
    {
        foreach(var move in availableMoves)
        {
            if(redTurn)
            {
                HashSet<Vector3Int> checkRedTiles = new HashSet<Vector3Int>(redTiles);
                checkRedTiles.Add(move);
                if(CheckForWin(checkRedTiles, true))
                {
                    return move;
                }
            }
            else
            {
                HashSet<Vector3Int> checkBlueTiles = new HashSet<Vector3Int>(blueTiles);
                checkBlueTiles.Add(move);
                if(CheckForWin(checkBlueTiles, false))
                {
                    return move;
                }
            }
        }
        foreach(var move in availableMoves)
        {
            if(redTurn)
            {
                HashSet<Vector3Int> checkBlueTiles = new HashSet<Vector3Int>(blueTiles);
                checkBlueTiles.Add(move);
                if(CheckForWin(checkBlueTiles, false))
                {
                    return move;
                }
                else
                {
                    HashSet<Vector3Int> checkRedTiles = new HashSet<Vector3Int>(redTiles);
                    checkRedTiles.Add(move);
                    if(CheckForWin(checkRedTiles, true))
                    {
                        return move;
                    }
                }
            }
            
        }
        return Vector3Int.zero;
        
    }
    private int EvaluateBoard(HashSet<Vector3Int> availableMoves, HashSet<Vector3Int> redTiles, HashSet<Vector3Int> blueTiles)
    {
        int redAdjFactor = EvaluateAdjFactor(redTiles, true);
        int blueAdjFactor = EvaluateAdjFactor(blueTiles, false);
        int redCentrality = EvaluateCentrality(redTiles);
        int blueCentrality = EvaluateCentrality(blueTiles);
        // int redPathFactor = EvaluatePathFactor(redTiles, true);
        // int bluePathFactor = EvaluatePathFactor(blueTiles, false);

        return (redAdjFactor + redCentrality) - (blueAdjFactor + blueCentrality);
    }
    private int EvaluateAdjFactor(HashSet<Vector3Int> playerTiles, bool redTurn)
    {
        int count = 0;
        int reward = 0;
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
                    count = count + 1;
                }
            }
        }
        
        reward += count * 5;
        return reward;
    }
    private int EvaluateCentrality(HashSet<Vector3Int> playerTiles)
    {
        int reward = 0;
        HashSet<Vector2Int> offsetTiles = new HashSet<Vector2Int>();
        Vector2Int center = new Vector2Int(5,5);
        
        foreach(Vector3Int tile in playerTiles)
        {
            offsetTiles.Add(TileOffset(tile));
        }

        foreach(var tile in offsetTiles)
        {
            int distance = Mathf.Abs(tile.x - center.x) + Mathf.Abs(tile.y - center.y);
            int val = 11 - distance;
            reward += val * 2;
            if(tile.x == center.x && tile.y == center.y)
            {
                reward += 5;
            }
        }
        return reward;

    }
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
        int distanceVal = Djikstras(offsetTiles, startEdge, endEdge);
        
        return distanceVal * 2;
    }
    private int Djikstras(HashSet<Vector2Int> offsetTiles, HashSet<Vector2Int> startEdge, HashSet<Vector2Int> endEdge)
    {
        // Use a Dictionary to track distances
        var distances = new Dictionary<Vector2Int, int>();
        var visited = new HashSet<Vector2Int>();
        
        // Initialize distances for start edge tiles
        foreach (var start in startEdge)
        {
            distances[start] = 0;
        }
        
        // Dijkstra's algorithm with simple priority implementation
        while (distances.Count > 0)
        {
            // Find the unvisited node with minimum distance
            Vector2Int current = Vector2Int.zero;
            int minDistance = int.MaxValue;
            
            foreach (var pair in distances)
            {
                if (!visited.Contains(pair.Key) && pair.Value < minDistance)
                {
                    current = pair.Key;
                    minDistance = pair.Value;
                }
            }
            
            // If no unvisited nodes found, break
            if (minDistance == int.MaxValue)
                break;
                
            // Mark as visited
            visited.Add(current);
            
            // If we reached the end edge, return the distance
            if (endEdge.Contains(current))
                return minDistance;
                
            // Remove from unvisited set
            distances.Remove(current);
            
            // Check all neighbors
            foreach (var neighbor in GetNeighbors(current))
            {
                // Skip if outside the board or already visited
                if (neighbor.x < 0 || neighbor.x > 10 || neighbor.y < 0 || neighbor.y > 10 || visited.Contains(neighbor))
                    continue;
                    
                // Calculate weight based on whether the tile is occupied
                int weight = offsetTiles.Contains(neighbor) ? 0 : 1;
                int newDistance = minDistance + weight;
                
                // Update distance if better
                if (!distances.ContainsKey(neighbor) || newDistance < distances[neighbor])
                {
                    distances[neighbor] = newDistance;
                }
            }
        }
        
        // No path found - return a large value
        return 100;
    }
    private bool IsTerminalNode(Node node)
    {
        return node.availableMoves.Count == 0;
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
