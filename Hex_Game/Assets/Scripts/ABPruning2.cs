using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ABPruning2
{
    // maximum depth of tree
    private int maxDepth = 2;

    //function to get the best current move
    public Vector3Int FetchBestMove(HashSet<Vector3Int> availableMoves, HashSet<Vector3Int> redTiles, HashSet<Vector3Int> blueTiles, bool redTurn)
    {
        int maxVal = redTurn ? int.MinValue : int.MaxValue; // if red player turn, set maxVal to minimum, if blue set to maximum
        Vector3Int bestMove = Vector3Int.zero;  //initialize best move to zero
        int alpha = int.MinValue;   //initialize both alpha and beta to min and max respectively
        int beta = int.MaxValue;

        Vector3Int quickMove = QuickMove(availableMoves, redTiles, blueTiles, redTurn); //use quickmove to check winning or blocking moves
        if (quickMove != Vector3Int.zero)
        {
            return quickMove;
        }
        foreach(var move in availableMoves)
        {
            //initialize hashsets to store function parameters copy
            HashSet<Vector3Int> currentAvailableMoves = new HashSet<Vector3Int>(availableMoves);
            HashSet<Vector3Int> currentRedTiles = new HashSet<Vector3Int>(redTiles);
            HashSet<Vector3Int> currentBlueTiles = new HashSet<Vector3Int>(blueTiles);
            currentAvailableMoves.Remove(move);
            currentRedTiles.Add(move);
            
            // call alpha beta with max depth
            int val = AlphaBeta(currentAvailableMoves, currentRedTiles, currentBlueTiles, false, maxDepth, alpha, beta);
            if(redTurn && val > maxVal) //modify maxVal for red player if val is higher
            {
                maxVal = val;
                bestMove = move;
                alpha = Mathf.Max(maxVal, alpha);   //set alpha to bigger value between itself and maxVal
            }
            else if(!redTurn && val < maxVal)   // do the same for blue player
            {
                maxVal = val;
                bestMove = move;
                beta = Mathf.Min(maxVal, beta);     //set beta to minimum between itself and maxVal
            }
            
        }
        return bestMove;
        
    }
    // minimax function with alpha beta pruning
    private int AlphaBeta(HashSet<Vector3Int> availableMoves, HashSet<Vector3Int> redTiles, HashSet<Vector3Int> blueTiles, bool redTurn, int depth, int alpha, int beta)
    {
        if(CheckForWin(redTiles, true)) // check if red or blue wins, if they do add max reward for value
        {
            return 1000 + depth;
        }
        if(CheckForWin(blueTiles, false))
        {
            return -1000 - depth;
        }
        if(depth == 0 || availableMoves.Count == 0) // if we reach a leaf or no moves left, evaluate it
        {
            return EvaluateBoard(availableMoves, redTiles, blueTiles);
        }
        if(redTurn)
        {
            int maxVal = int.MinValue;  //set maxVal to minimum integer value to start
            foreach(var move in availableMoves)
            {
                // create copies of hashsets of the function parameters
                HashSet<Vector3Int> currentAvailableMoves = new HashSet<Vector3Int>(availableMoves);
                HashSet<Vector3Int> currentRedTiles = new HashSet<Vector3Int>(redTiles);
                HashSet<Vector3Int> currentBlueTiles = new HashSet<Vector3Int>(blueTiles);
                currentAvailableMoves.Remove(move);
                currentRedTiles.Add(move);

                // call alpha beta recursively with -1 depth
                int val = AlphaBeta(currentAvailableMoves, currentRedTiles, currentBlueTiles, false, depth - 1, alpha, beta);   
                maxVal = Mathf.Max(maxVal, val);
                alpha = Mathf.Max(val, alpha);
                // if beta is less than alpha then prune the branch 
                if(beta <= alpha)
                {
                    break;
                }
            }
            return maxVal;
        }
        else
        {
            int minVal = int.MaxValue;      //set minVal to maximum integer value to start
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
                // if beta is less than alpha prune the branch
                if(beta <= alpha)
                {
                    break;
                }
            }
            return minVal;
        }
    }
    //quick move function to play or block an immediate winning move
    private Vector3Int QuickMove(HashSet<Vector3Int> availableMoves, HashSet<Vector3Int> redTiles, HashSet<Vector3Int> blueTiles, bool redTurn)
    {
        //loop through all moves available. loop for playing an immediate winning move
        foreach(var move in availableMoves)
        {
            if(redTurn)
            {
                // copy red tile parameter into local copy
                HashSet<Vector3Int> checkRedTiles = new HashSet<Vector3Int>(redTiles);
                checkRedTiles.Add(move);   
                if(CheckForWin(checkRedTiles, true))    //if reds turn and this move wins the game for red, return the move
                {
                    return move;
                }
            }
            else
            {
                //copy blue tile parameter to local copy
                HashSet<Vector3Int> checkBlueTiles = new HashSet<Vector3Int>(blueTiles);
                checkBlueTiles.Add(move);
                if(CheckForWin(checkBlueTiles, false))  //if blues turn and this move wins the game for blue, return the move
                {
                    return move;
                }
            }
        }
        //loop through all moves available. loop  for blocking immediate winning move
        foreach(var move in availableMoves)
        {
            if(redTurn)
            {
                HashSet<Vector3Int> checkBlueTiles = new HashSet<Vector3Int>(blueTiles);
                checkBlueTiles.Add(move);
                if(CheckForWin(checkBlueTiles, false))  //if reds turn and blue wins with this move, block it
                {
                    return move;
                }
                else
                {
                    HashSet<Vector3Int> checkRedTiles = new HashSet<Vector3Int>(redTiles);
                    checkRedTiles.Add(move);
                    if(CheckForWin(checkRedTiles, true))    //if blues turn and red wins with this move, block it
                    {
                        return move;
                    }
                }
            }
            
        }
        return Vector3Int.zero;
        
    }
    //function to evaluate board
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
    //function to evaluate adjacency factor
    private int EvaluateAdjFactor(HashSet<Vector3Int> playerTiles, bool redTurn)
    {
        int count = 0;
        int reward = 0;
        HashSet<Vector2Int> offsetTiles = new HashSet<Vector2Int>();
        //offset each tile to correct coordinates
        foreach(Vector3Int tile in playerTiles)
        {
            offsetTiles.Add(TileOffset(tile));
        }
        //loop through each tile
        foreach(var tile in offsetTiles)
        {   
            //for each tile, loop through each neighbour of the tile
            foreach(Vector2Int neighbour in GetNeighbors(tile))
            {
                //if the played tiles contains a neighbour and bounds check
                if(offsetTiles.Contains(neighbour) && neighbour.x >= 0 && neighbour.x <= 10 && neighbour.y >= 0 && neighbour.y <= 10)
                {
                    count = count + 1;  //increase the count
                }
            }
        }
        
        reward += count * 5;    //set the reward
        return reward;
    }
    //function to evaluate the centrality of the game state for a player
    private int EvaluateCentrality(HashSet<Vector3Int> playerTiles)
    {
        int reward = 0;
        HashSet<Vector2Int> offsetTiles = new HashSet<Vector2Int>();
        Vector2Int center = new Vector2Int(5,5);
        
        //offset each tile in player tiles
        foreach(Vector3Int tile in playerTiles)
        {
            offsetTiles.Add(TileOffset(tile));
        }

        //loop through each tile and calculate the centrality of the move, if its close reward more
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
    
    //function to check if its a leaf node
    private bool IsTerminalNode(Node node)
    {
        return node.availableMoves.Count == 0;
    }
    //follwing functions copied from gameTiles class
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
