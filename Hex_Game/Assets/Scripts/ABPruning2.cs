using System.Collections.Generic;
using UnityEngine;

public class ABPruning2
{
    // How deep to search – increase this for stronger AI (but slower decision times).
    private int maxDepth = 3;
    private int winScore = 10000;  // score for win; adding depth can reward faster wins
    private int loseScore = -10000; // score for loss

    /// <summary>
    /// Returns the best move for the current player (isRedTurn true means red is maximizing).
    /// </summary>
    public Vector3Int FetchBestMove(HashSet<Vector3Int> availableMoves, HashSet<Vector3Int> redMoves, HashSet<Vector3Int> blueMoves, bool isRedTurn)
    {
        int bestScore = isRedTurn ? int.MinValue : int.MaxValue;
        Vector3Int bestMove = new Vector3Int();
        foreach (var move in availableMoves)
        {
            // Create copies of state to simulate the move
            HashSet<Vector3Int> newAvailable = new HashSet<Vector3Int>(availableMoves);
            newAvailable.Remove(move);
            HashSet<Vector3Int> newRedMoves = new HashSet<Vector3Int>(redMoves);
            HashSet<Vector3Int> newBlueMoves = new HashSet<Vector3Int>(blueMoves);

            if (isRedTurn)
                newRedMoves.Add(move);
            else
                newBlueMoves.Add(move);

            // Evaluate this move with minimax
            int score = Minimax(newAvailable, newRedMoves, newBlueMoves, !isRedTurn, maxDepth - 1, int.MinValue, int.MaxValue);
            if (isRedTurn && score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
            else if (!isRedTurn && score < bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }
        return bestMove;
    }

    /// <summary>
    /// Recursively evaluates the game state using minimax with alpha–beta pruning.
    /// </summary>
    private int Minimax(HashSet<Vector3Int> availableMoves, HashSet<Vector3Int> redMoves, HashSet<Vector3Int> blueMoves, bool isRedTurn, int depth, int alpha, int beta)
    {
        // Terminal check: if either player has won
        if (CheckForWin(redMoves, true))
            return winScore + depth;  // earlier win is better
        if (CheckForWin(blueMoves, false))
            return loseScore - depth; // earlier loss is worse

        // Depth limit or draw situation
        if (depth == 0 || availableMoves.Count == 0)
        {
            return Evaluate(redMoves, blueMoves);
        }

        if (isRedTurn)
        {
            int maxEval = int.MinValue;
            foreach (var move in availableMoves)
            {
                HashSet<Vector3Int> newAvailable = new HashSet<Vector3Int>(availableMoves);
                newAvailable.Remove(move);
                HashSet<Vector3Int> newRedMoves = new HashSet<Vector3Int>(redMoves);
                newRedMoves.Add(move);
                int eval = Minimax(newAvailable, newRedMoves, blueMoves, false, depth - 1, alpha, beta);
                maxEval = Mathf.Max(maxEval, eval);
                alpha = Mathf.Max(alpha, eval);
                if (beta <= alpha)
                    break;
            }
            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            foreach (var move in availableMoves)
            {
                HashSet<Vector3Int> newAvailable = new HashSet<Vector3Int>(availableMoves);
                newAvailable.Remove(move);
                HashSet<Vector3Int> newBlueMoves = new HashSet<Vector3Int>(blueMoves);
                newBlueMoves.Add(move);
                int eval = Minimax(newAvailable, redMoves, newBlueMoves, true, depth - 1, alpha, beta);
                minEval = Mathf.Min(minEval, eval);
                beta = Mathf.Min(beta, eval);
                if (beta <= alpha)
                    break;
            }
            return minEval;
        }
    }

    /// <summary>
    /// A simple evaluation function that returns the difference in move counts.
    /// In a more advanced implementation, you might estimate path connectivity, distance to goal edges, etc.
    /// </summary>
    private int Evaluate(HashSet<Vector3Int> redMoves, HashSet<Vector3Int> blueMoves)
    {
        return redMoves.Count - blueMoves.Count;
    }

    /// <summary>
    /// Checks for a win for a given player (red if isRed is true, blue otherwise) by converting the
    /// positions to board coordinates and performing a breadth-first search.
    /// </summary>
    public bool CheckForWin(HashSet<Vector3Int> playerMoves, bool isRed)
    {
        HashSet<Vector2Int> offsetTiles = new HashSet<Vector2Int>();
        HashSet<Vector2Int> startEdge = new HashSet<Vector2Int>();
        HashSet<Vector2Int> endEdge = new HashSet<Vector2Int>();

        foreach (var cell in playerMoves)
        {
            Vector2Int offset = TileOffset(cell);
            offsetTiles.Add(offset);
            if (isRed)
            {
                if (offset.y == 0) startEdge.Add(offset);   // top edge
                if (offset.y == 10) endEdge.Add(offset);      // bottom edge
            }
            else
            {
                if (offset.x == 0) startEdge.Add(offset);     // left edge
                if (offset.x == 10) endEdge.Add(offset);        // right edge
            }
        }

        if (startEdge.Count == 0 || endEdge.Count == 0)
            return false;

        foreach (var start in startEdge)
        {
            if (BreadthFirstSearch(start, endEdge, offsetTiles))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Uses a breadth-first search to see if there is a connected path from start to any tile in endEdge.
    /// </summary>
    private bool BreadthFirstSearch(Vector2Int start, HashSet<Vector2Int> endEdge, HashSet<Vector2Int> offsetTiles)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (endEdge.Contains(current))
                return true;
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

    /// <summary>
    /// Returns the six adjacent coordinates on a hex grid.
    /// </summary>
    private List<Vector2Int> GetNeighbors(Vector2Int coordinate)
    {
        return new List<Vector2Int>
        {
            coordinate + new Vector2Int(1, 0),
            coordinate + new Vector2Int(-1, 0),
            coordinate + new Vector2Int(0, 1),
            coordinate + new Vector2Int(0, -1),
            coordinate + new Vector2Int(1, -1),
            coordinate + new Vector2Int(-1, 1)
        };
    }

    /// <summary>
    /// Converts the tile’s grid coordinate (Vector3Int) to an offset coordinate (Vector2Int) used for win detection.
    /// This is similar to your GameTiles.TileOffset implementation.
    /// </summary>
    private Vector2Int TileOffset(Vector3Int cell)
    {
        int y = cell.y;
        int row = 5 - y;
        int rowCalc = (6 - y) / 2;
        int xOffset = -7 + rowCalc;
        int column = cell.x - xOffset;
        return new Vector2Int(column, row);
    }
}
