using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random=UnityEngine.Random;
using System.Linq;
public class MTCS
{
    private float exploration = 1.5f;
    private int maxIterations = 1000;

    public Vector3Int MTCSFetchBestMove(HashSet<Vector3Int> availableMoves, HashSet<Vector3Int> redTiles, HashSet<Vector3Int> blueTiles, bool redTurn)
    {
        
        if(availableMoves == null || redTiles == null || blueTiles == null)
        {
            Debug.LogError("Available moves is null");
            return Vector3Int.zero;
        }
        
        Node root = new Node(null, Vector3Int.zero, availableMoves, redTiles, blueTiles, redTurn);
        if(root.move == Vector3Int.zero && availableMoves.Count == 0)
        {
            
            return Vector3Int.zero;
        }
        
        for(int i = 0; i < maxIterations; i++)
        {
            
            Node selectedNode = SelectNode(root);
           
            if(!selectedNode.isExpanded() && !IsTerminalNode(selectedNode))
            {
                
                selectedNode = Expansion(selectedNode);
                
            }
            
            int outcome = RollOut(selectedNode);
            
            BackPropagate(selectedNode, outcome);
            

        }
        return BestMove(root);
    }
    public Node SelectNode(Node node)
    {
        while (!IsTerminalNode(node) && node.isExpanded())
        {
            
            node = BestChild(node);
            
        }
        
        return node;
    }
    private Node BestChild(Node node)
    {
        Node bestNode = null;
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

    public float UpperConfidenceBoundTree(Node node)
    {
        if(node.visits == 0)
        {
            return float.MaxValue;
        }
        return (float)node.wins / node.visits + exploration * Mathf.Sqrt(Mathf.Log(node.parent.visits) / node.visits);
    }
    public Node Expansion(Node node)
    {
        
        foreach(var move in node.availableMoves)
        {
            
            if(!node.lookupChildren.ContainsKey(move))
            {
                Node newNode = new Node(node, move, node.availableMoves, node.redTiles, node.blueTiles, !node.redTurn);
                node.children.Add(newNode);
                node.lookupChildren[move] = newNode; 
                return newNode;
            }
        }
        return node;
    }
    public int RollOut(Node node)
    {
        HashSet<Vector3Int> tempRedTiles = new HashSet<Vector3Int>(node.redTiles);
        HashSet<Vector3Int> tempBlueTiles = new HashSet<Vector3Int>(node.blueTiles);
        HashSet<Vector3Int> tempMoves = new HashSet<Vector3Int>(node.availableMoves);

        bool turn = node.redTurn;
        while (tempMoves.Count > 0)
        {
            Vector3Int selectMove = tempMoves.ElementAt(Random.Range(0, tempMoves.Count));
            //Vector3Int selectMove = SelectHeuristicMove(tempMoves, turn ? tempRedTiles : tempBlueTiles);

            tempMoves.Remove(selectMove);
            if(turn)
            {
                tempRedTiles.Add(selectMove);
                if(CheckSimulationWin(tempRedTiles, true))
                {
                    return 1;
                }
            } 
            else
            {
                tempBlueTiles.Add(selectMove);
                if(CheckSimulationWin(tempBlueTiles, false))
                {
                    return -1;
                }
            } 
            turn = !turn;
            
        }
        return 0;
    }
    public void BackPropagate(Node node, int outcome)
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
            node = node.parent;
        }
    }
    public Vector3Int BestMove(Node root)
    {
        if(root.children.Count == 0)
        {
            throw new InvalidOperationException("No available moves");
        }
        Node bestNode = root.children[0];
        for(int i = 0; i < root.children.Count; i++)
        {
            if(root.children[i].visits > bestNode.visits)
            {
                bestNode = root.children[i];
            }
        }
        return bestNode.move;
    }
    private bool IsTerminalNode(Node node)
    {
        return node.availableMoves.Count == 0;
    }
    //  private Vector3Int SelectHeuristicMove(HashSet<Vector3Int> availableMoves, HashSet<Vector3Int> currentTiles)
    // {
    //     List<Vector3Int> moves = new List<Vector3Int>(availableMoves);
    //     List<int> weights = new List<int>();

    //     foreach (var move in moves)
    //     {
    //         int count = 0;
    //         foreach (var neighbor in GetCellNeighbors(move))
    //             if (currentTiles.Contains(neighbor)) count++;
    //         weights.Add(1 + count);
    //     }

    //     int totalWeight = weights.Sum();
    //     int randomWeight = Random.Range(0, totalWeight);
    //     int currentWeight = 0;

    //     for (int i = 0; i < moves.Count; i++)
    //     {
    //         currentWeight += weights[i];
    //         if (randomWeight < currentWeight)
    //             return moves[i];
    //     }
    //     return moves[0];
    // }
    // private List<Vector3Int> GetCellNeighbors(Vector3Int cell)
    // {
    //     return new List<Vector3Int>
    //     {
    //         cell + new Vector3Int(1, 0, 0),
    //         cell + new Vector3Int(-1, 0, 0),
    //         cell + new Vector3Int(0, 1, 0),
    //         cell + new Vector3Int(0, -1, 0),
    //         cell + new Vector3Int(1, -1, 0),
    //         cell + new Vector3Int(-1, 1, 0)
    //     };
    // }

    private bool CheckSimulationWin(HashSet<Vector3Int> playerTiles, bool isRed)
    {
        var offsetTiles = new HashSet<Vector2Int>();
        var startEdge = new HashSet<Vector2Int>();
        var endEdge = new HashSet<Vector2Int>();

        foreach (var cell in playerTiles)
        {
            Vector2Int pos = SimulationTileOffset(cell);
            offsetTiles.Add(pos);

            if (isRed)
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

