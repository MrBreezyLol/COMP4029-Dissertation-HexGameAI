using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random=UnityEngine.Random;
using System.Linq;
public class MTCS
{
    private float exploration = 1.5f;   //set exploration constant
    private int maxIterations = 1000;   //set maximum iterations to run mtcs
    private Vector3Int firstCenterMove = new Vector3Int(1,0,0); // center move

    //function to get the best current move
    public Vector3Int MTCSFetchBestMove(HashSet<Vector3Int> availableMoves, HashSet<Vector3Int> redTiles, HashSet<Vector3Int> blueTiles, bool redTurn)
    {
        //check for errors
        if(availableMoves == null || redTiles == null || blueTiles == null)
        {
            Debug.LogError("Available moves is null");
            return Vector3Int.zero;
        }
        //initialize the root
        RaveNode root = new RaveNode(null, Vector3Int.zero, availableMoves, redTiles, blueTiles, redTurn);
        if(root.move == Vector3Int.zero && availableMoves.Count == 0)
        {
            return Vector3Int.zero;
        }
        if(availableMoves.Count == 120) //check for first move, if so play it in the center
        {
            if(!redTiles.Contains(firstCenterMove))
            {
                return firstCenterMove;
            }
            return new Vector3Int(0,1,0);
        }
        Vector3Int quickMove = QuickMove(availableMoves, redTiles, blueTiles, redTurn); //check for quick move, playing or blocking an immediate winning move
        if(quickMove != Vector3Int.zero)
        {
            return quickMove;
        }
        for(int i = 0; i < maxIterations; i++)  //loop until max iterations
        {
            
            RaveNode selectedNode = SelectNode(root);   //selection phase
           
            if(!selectedNode.isExpanded() && !IsTerminalNode(selectedNode)) //if its not expanded or a leaf node, expand the node
            {
                
                selectedNode = Expansion(selectedNode);
                
            }
            
            var (outcome, raveMoves) = RollOut(selectedNode);   //run the roll-out phase and store outcome and moves played
            
            BackPropagate(selectedNode, outcome, raveMoves);    //backpropagate to update the node statistics
            

        }
        return BestMove(root);  //return best move
    }
    //function to play or block an immediate winning move
    private Vector3Int QuickMove(HashSet<Vector3Int> availableMoves, HashSet<Vector3Int> redTiles, HashSet<Vector3Int> blueTiles, bool redTurn)
    {
        //for playing winning move
        foreach(var move in availableMoves)
        {
            if(redTurn)
            {
                HashSet<Vector3Int> checkRedTiles = new HashSet<Vector3Int>(redTiles);  //create a copy to store red tiles
                checkRedTiles.Add(move);
                if(CheckSimulationWin(checkRedTiles, true)) //if red and this move wins, play it
                {
                    return move;
                }
            }
            else
            {
                HashSet<Vector3Int> checkBlueTiles = new HashSet<Vector3Int>(blueTiles);
                checkBlueTiles.Add(move);
                if(CheckSimulationWin(checkBlueTiles, false))   //if blue and this move wins, play it
                {
                    return move;
                }
            }
        }
        // for blocking winning move
        foreach(var move in availableMoves)
        {
            if(redTurn)
            {
                HashSet<Vector3Int> checkBlueTiles = new HashSet<Vector3Int>(blueTiles);
                checkBlueTiles.Add(move);
                if(CheckSimulationWin(checkBlueTiles, false))   //if red and blue wins with this move, block it
                {
                    return move;
                }
            }
            else
            {
                HashSet<Vector3Int> checkRedTiles = new HashSet<Vector3Int>(redTiles);
                checkRedTiles.Add(move);
                if(CheckSimulationWin(checkRedTiles, true)) //if blue and red wins with this move, block it
                {
                    return move;
                }
            }
        }
        return Vector3Int.zero;
        
    }
    //function to select best node
    public RaveNode SelectNode(RaveNode node)
    {
        while (!IsTerminalNode(node) && node.isExpanded())
        {
            
            node = BestChild(node);
            
        }
        
        return node;
    }
    //function to determine best child
    private RaveNode BestChild(RaveNode node)
    {
        RaveNode bestNode = null;
        float maxValue = float.MinValue;
        foreach (var child in node.children)    //for each child, calculate the uct value and return best child
        {
            float uctValue = UpperConfidenceBoundTree(child);
            if (uctValue > maxValue)
            {
                maxValue = uctValue;
                bestNode = child;
            }
        }
        return bestNode;
    }
    //function to calculate the uct value
    public float UpperConfidenceBoundTree(RaveNode node)
    {
        if(node.visits == 0)
        {
            return float.MaxValue;
        }
        //store uct value
        float uctValue = (float)node.wins / node.visits + exploration * Mathf.Sqrt(Mathf.Log(node.parent.visits) / node.visits);
        //calculate beta
        float beta = (float)Math.Sqrt(1000 / (3 * node.visits + 1000));
        float raveScore = node.RaveScore(node.move);    //calculate rave score

        return beta * raveScore + (1 - beta) * uctValue;    
        
    }
    //function for expansion phase
    public RaveNode Expansion(RaveNode node)
    {
        
        foreach(var move in node.availableMoves)    //loop through each move, if the node children contains move, create new node from there
        {
            
            if(!node.lookupChildren.ContainsKey(move))
            {
                RaveNode newNode = new RaveNode(node, move, node.availableMoves, node.redTiles, node.blueTiles, !node.redTurn);
                node.children.Add(newNode);
                node.lookupChildren[move] = newNode; 
                return newNode;
            }
        }
        return node;
    }
    //function for simulation phase
    public (int outcome, List<Vector3Int>) RollOut(RaveNode node)
    {
        //create copies of the played moves and available ones
        List<Vector3Int> trackRaveMoves = new List<Vector3Int>();
        HashSet<Vector3Int> tempRedTiles = new HashSet<Vector3Int>(node.redTiles);
        HashSet<Vector3Int> tempBlueTiles = new HashSet<Vector3Int>(node.blueTiles);
        HashSet<Vector3Int> tempMoves = new HashSet<Vector3Int>(node.availableMoves);

        bool turn = node.redTurn;   //set turn
        while (tempMoves.Count > 0)
        {
            Vector3Int selectMove = tempMoves.ElementAt(Random.Range(0, tempMoves.Count));  //use random move selection for rollout
            trackRaveMoves.Add(selectMove); //store the simulation moves played

            tempMoves.Remove(selectMove);
            if(turn)
            {
                tempRedTiles.Add(selectMove);
                if(CheckSimulationWin(tempRedTiles, true))  //if red won, return 1 as score and the moves played
                {
                    return (1, trackRaveMoves);
                }
            } 
            else
            {
                tempBlueTiles.Add(selectMove);
                if(CheckSimulationWin(tempBlueTiles, false))    //if blue won, return -1 as score and the moves played
                {
                    return (-1, trackRaveMoves);
                }
            } 
            turn = !turn;
            
        }
        return (0, trackRaveMoves);
    }
    //function for backpropagation to update node statistics
    public void BackPropagate(RaveNode node, int outcome, List<Vector3Int> raveMoves)
    {
        while(node != null) //loop through all nodes from the selected one to update visits and wins
        {
            node.visits++;
            if(outcome == 1)
            {
                node.wins++;
            }
            else if(outcome == -1)
            {
                node.wins--;
            }
            foreach(var move in raveMoves)  //also update the nodes visits and wins for each specific move played during simulation
            {
                if(!node.raveVisits.ContainsKey(move))
                {
                    node.raveVisits[move] = 0;
                    node.raveWins[move] = 0;
                }
                node.raveVisits[move] += 1;
                if((outcome == 1 && node.redTurn) || (outcome == -1 && !node.redTurn))
                {
                    node.raveWins[move] += 1;
                }
            }
            node = node.parent; //go to parent node of current node
        }
    }
    //function to choose the best move from their statistics (visits)
    public Vector3Int BestMove(RaveNode root)
    {
        if(root.children.Count == 0)
        {
            throw new InvalidOperationException("No available moves");
        }
        RaveNode bestNode = root.children[0];
        for(int i = 0; i < root.children.Count; i++)    //for each child, select the one with most visits
        {
            if(root.children[i].visits > bestNode.visits)
            {
                bestNode = root.children[i];
            }
        }
        return bestNode.move;
    }
    //function to check if its terminal node
    private bool IsTerminalNode(RaveNode node)
    {
        return node.availableMoves.Count == 0;
    }
    //function to evaluate adjacency 
    private int EvaluateAdjFactor(HashSet<Vector3Int> playerTiles, bool redTurn)
    {
        int count = 0;
        int reward = 0;
        HashSet<Vector2Int> offsetTiles = new HashSet<Vector2Int>();
        foreach(Vector3Int tile in playerTiles) //offset all tiles 
        {
            offsetTiles.Add(SimulationTileOffset(tile));
        }

        foreach(var tile in offsetTiles)    //for each tile, get its neighbours and check if the played moves contains this neighbour, if so add to the count
        {
            foreach(Vector2Int neighbour in GetSimulationNeighbors(tile))
            {
                if(offsetTiles.Contains(neighbour) && neighbour.x >= 0 && neighbour.x <= 10 && neighbour.y >= 0 && neighbour.y <= 10)
                {
                    count = count + 1;
                }
            }
        }
        
        reward += count * 5;    //set the reward
        return reward;
    }
    //function to choose the best heuristic move based on djikstra
    // private Vector3Int BestHeuristicMove(HashSet<Vector3Int> availableMoves, HashSet<Vector3Int> currentTiles,  bool redTurn)
    // {
    //     Vector3Int bestMove = Vector3Int.zero;
    //     int bestScore = int.MaxValue;
    //     foreach(var move in availableMoves)
    //     {
    //         HashSet<Vector3Int> currTiles = new HashSet<Vector3Int>(currentTiles);
    //         currTiles.Add(move);
    //         int pathLength = Djikstras(currentTiles, redTurn);
    //         int score = 2 * pathLength;
    //         if(score > bestScore)   //pick best move based on score of the move and its pathlength 
    //         {
    //             bestScore = score;
    //             bestMove = move;
    //         }
    //     }
        
    //     return bestMove;
    // }
    //function to calculate shortest path
    private Vector3Int DjikstraNextMove(HashSet<Vector3Int> availableMoves, HashSet<Vector3Int> clickedRedTiles, HashSet<Vector3Int> clickedBlueTiles, bool redTurn)
    {
        Dictionary<Vector3Int, int> distance = new Dictionary<Vector3Int, int>();
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        Vector3Int bestMove = Vector3Int.zero;
        int minDistance = int.MaxValue;
        
        foreach(var move in redTurn ? clickedRedTiles : clickedBlueTiles)
        {
            distance[move] = 0;
            queue.Enqueue(move);
        }
        while(queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();
            foreach(var neighbour in GetSimulationNeighbors3(current))
            {
                if((!availableMoves.Contains(neighbour)) || distance.ContainsKey(neighbour))
                {
                    continue;
                }
                distance[neighbour] = distance[current] + 1;
                queue.Enqueue(neighbour);
            }
        }
        foreach(var move in availableMoves)
        {
            if(!distance.ContainsKey(move))
            {
                continue;
            }
            Vector2Int offset = SimulationTileOffset(move);
            int distanceToEdge = redTurn ? Mathf.Min(offset.y, 10-offset.y) : Mathf.Min(offset.x, 10-offset.x);
            int totalDistance = distanceToEdge + distance[move];
            if(totalDistance < minDistance)
            {
                minDistance = totalDistance;
                bestMove = move;
            }
        }
        return bestMove;
        
        
    }
    
    //following functions are the same as those in gameTile class
    private bool CheckSimulationWin(HashSet<Vector3Int> playerTiles, bool redTurn)
    {
        var offsetTiles = new HashSet<Vector2Int>();
        var startEdge = new HashSet<Vector2Int>();
        var endEdge = new HashSet<Vector2Int>();

        foreach (var cell in playerTiles)
        {
            Vector2Int pos = SimulationTileOffset(cell);
            offsetTiles.Add(pos);

            if (redTurn)
            {
                if (pos.y == 0) startEdge.Add(pos);
                if (pos.y == 10) endEdge.Add(pos);
            }
            else
            {
                if (pos.x == 0) startEdge.Add(pos);
                if (pos.x == 10) endEdge.Add(pos);
            }
        }

        if (startEdge.Count == 0 || endEdge.Count == 0) return false;

        foreach (var start in startEdge)
            if (SimulationBFS(start, endEdge, offsetTiles))
                return true;
        return false;
    }

    private Vector2Int SimulationTileOffset(Vector3Int cell)
    {
        int y = cell.y;
        int row = 5 - y;
        int rowCalc = (6 - y) / 2;
        int xOffset = -7 + rowCalc;
        int column = cell.x - xOffset;
        return new Vector2Int(column, row);
    }

    private bool SimulationBFS(Vector2Int start, HashSet<Vector2Int> endEdge, HashSet<Vector2Int> tiles)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (endEdge.Contains(current)) return true;

            foreach (var neighbor in GetSimulationNeighbors(current))
                if (tiles.Contains(neighbor) && !visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
        }
        return false;
    }

    private List<Vector2Int> GetSimulationNeighbors(Vector2Int pos)
    {
        return new List<Vector2Int>
        {
            pos + new Vector2Int(1, 0),
            pos + new Vector2Int(-1, 0),
            pos + new Vector2Int(0, 1),
            pos + new Vector2Int(0, -1),
            pos + new Vector2Int(1, -1),
            pos + new Vector2Int(-1, 1)
        };
    }
    private List<Vector3Int> GetSimulationNeighbors3(Vector3Int pos)
    {
        return new List<Vector3Int>
        {
            pos + new Vector3Int(1, 0,0),
            pos + new Vector3Int(-1, 0,0),
            pos + new Vector3Int(0, 1,0),
            pos + new Vector3Int(0, -1,0),
            pos + new Vector3Int(1, -1,0),
            pos + new Vector3Int(-1, 1,0)
        };
    }
    
}


