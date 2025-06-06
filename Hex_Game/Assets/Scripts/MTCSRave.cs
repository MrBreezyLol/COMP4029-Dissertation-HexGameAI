using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random=UnityEngine.Random;
using System.Linq;
public class MTCSRave
{
    //same class as MTCS but without the heuristics
    private float exploration = 1.5f;
    private int maxIterations = 1000;

    public Vector3Int MTCSFetchBestMove(HashSet<Vector3Int> availableMoves, HashSet<Vector3Int> redTiles, HashSet<Vector3Int> blueTiles, bool redTurn)
    {
        
        if(availableMoves == null || redTiles == null || blueTiles == null)
        {
            Debug.LogError("Available moves is null");
            return Vector3Int.zero;
        }
        
        RaveNode root = new RaveNode(null, Vector3Int.zero, availableMoves, redTiles, blueTiles, redTurn);
        if(root.move == Vector3Int.zero && availableMoves.Count == 0)
        {
            
            return Vector3Int.zero;
        }
        Vector3Int quickMove = QuickMove(availableMoves, redTiles, blueTiles, redTurn);
        if(quickMove != Vector3Int.zero)
        {
            return quickMove;
        }
        for(int i = 0; i < maxIterations; i++)
        {
            
            RaveNode selectedNode = SelectNode(root);
           
            if(!selectedNode.isExpanded() && !IsTerminalNode(selectedNode))
            {
                
                selectedNode = Expansion(selectedNode);
                
            }
            
            var (outcome, raveMoves) = RollOut(selectedNode);
            
            BackPropagate(selectedNode, outcome, raveMoves);
            

        }
        return BestMove(root);
    }
    private Vector3Int QuickMove(HashSet<Vector3Int> availableMoves, HashSet<Vector3Int> redTiles, HashSet<Vector3Int> blueTiles, bool redTurn)
    {
        //for playing winning move
        foreach(var move in availableMoves)
        {
            if(redTurn)
            {
                HashSet<Vector3Int> checkRedTiles = new HashSet<Vector3Int>(redTiles);
                checkRedTiles.Add(move);
                if(CheckSimulationWin(checkRedTiles, true))
                {
                    return move;
                }
            }
            else
            {
                HashSet<Vector3Int> checkBlueTiles = new HashSet<Vector3Int>(blueTiles);
                checkBlueTiles.Add(move);
                if(CheckSimulationWin(checkBlueTiles, false))
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
                if(CheckSimulationWin(checkBlueTiles, false))
                {
                    return move;
                }
            }
            else
            {
                HashSet<Vector3Int> checkRedTiles = new HashSet<Vector3Int>(redTiles);
                checkRedTiles.Add(move);
                if(CheckSimulationWin(checkRedTiles, true))
                {
                    return move;
                }
            }
        }
        return Vector3Int.zero;
        
    }
    public RaveNode SelectNode(RaveNode node)
    {
        while (!IsTerminalNode(node) && node.isExpanded())
        {
            
            node = BestChild(node);
            
        }
        
        return node;
    }
    private RaveNode BestChild(RaveNode node)
    {
        RaveNode bestNode = null;
        float maxValue = float.MinValue;
        foreach (var child in node.children)
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

    public float UpperConfidenceBoundTree(RaveNode node)
    {
        if(node.visits == 0)
        {
            return float.MaxValue;
        }
        float uctValue = (float)node.wins / node.visits + exploration * Mathf.Sqrt(Mathf.Log(node.parent.visits) / node.visits);

        float beta = (float)Math.Sqrt(1000 / (3 * node.visits + 1000));
        float raveScore = node.RaveScore(node.move);

        return beta * raveScore + (1 - beta) * uctValue;
        
    }
    public RaveNode Expansion(RaveNode node)
    {
        
        foreach(var move in node.availableMoves)
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
    public (int outcome, List<Vector3Int>) RollOut(RaveNode node)
    {
        List<Vector3Int> trackRaveMoves = new List<Vector3Int>();
        HashSet<Vector3Int> tempRedTiles = new HashSet<Vector3Int>(node.redTiles);
        HashSet<Vector3Int> tempBlueTiles = new HashSet<Vector3Int>(node.blueTiles);
        HashSet<Vector3Int> tempMoves = new HashSet<Vector3Int>(node.availableMoves);

        bool turn = node.redTurn;
        while (tempMoves.Count > 0)
        {
            Vector3Int selectMove = tempMoves.ElementAt(Random.Range(0, tempMoves.Count));
            trackRaveMoves.Add(selectMove);

            tempMoves.Remove(selectMove);
            if(turn)
            {
                tempRedTiles.Add(selectMove);
                if(CheckSimulationWin(tempRedTiles, true))
                {
                    return (1, trackRaveMoves);
                }
            } 
            else
            {
                tempBlueTiles.Add(selectMove);
                if(CheckSimulationWin(tempBlueTiles, false))
                {
                    return (-1, trackRaveMoves);
                }
            } 
            turn = !turn;
            
        }
        return (0, trackRaveMoves);
    }
    public void BackPropagate(RaveNode node, int outcome, List<Vector3Int> raveMoves)
    {
        while(node != null)
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
            foreach(var move in raveMoves)
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
            node = node.parent;
        }
    }
    public Vector3Int BestMove(RaveNode root)
    {
        if(root.children.Count == 0)
        {
            throw new InvalidOperationException("No available moves");
        }
        RaveNode bestNode = root.children[0];
        for(int i = 0; i < root.children.Count; i++)
        {
            if(root.children[i].visits > bestNode.visits)
            {
                bestNode = root.children[i];
            }
        }
        return bestNode.move;
    }
    private bool IsTerminalNode(RaveNode node)
    {
        return node.availableMoves.Count == 0;
    }
    
    

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
    
}


