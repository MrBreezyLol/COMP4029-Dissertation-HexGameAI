using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ABPruning : MonoBehaviour
{
    private int maxDepth = 3;

    public Vector3Int FetchBestMove(HashSet<Vector3Int> availableMoves, HashSet<Vector3Int> redTiles, HashSet<Vector3Int> blueTiles, bool redTurn)
    {
        int maxVal = redTurn ? int.MinValue : int.MaxValue;
        Vector3Int bestMove = Vector3Int.zero;
        int alpha = int.MinValue;
        int beta = int.MaxValue;
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
    private int EvaluateBoard(HashSet<Vector3Int> availableMoves, HashSet<Vector3Int> redTiles, HashSet<Vector3Int> blueTiles)
    {
        int redAdjFactor = EvaluateAdjFactor(redTiles, true);
        int blueAdjFactor = EvaluateAdjFactor(blueTiles, false);

        

        return redAdjFactor - blueAdjFactor;
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
    private int EvaluatePathFactor(HashSet<Vector3Int> playerTiles, bool redTurn)
    {
        int score = 0;
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
        
        return 0; //placeholder
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
