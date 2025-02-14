using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random=UnityEngine.Random;
using System.Linq;
public class MTCS
{
    private float exploration = 1.4f;
    private int maxIterations = 1000;

    public Vector3Int FetchBestMove(HashSet<Vector3Int> availableMoves, HashSet<Vector3Int> redTiles, HashSet<Vector3Int> blueTiles, bool redTurn)
    {
        Debug.Log("in FetchBestMove method");
        if(availableMoves == null)
        {
            Debug.LogError("Available moves is null");
            return Vector3Int.zero;
        }
        if(redTiles == null || blueTiles == null)
        {
            Debug.LogError("redTiles or blueTiles is null");
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
           
            if(!selectedNode.isExpanded())
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
        if(node == null || node.children.Count == 0)
        {
            Debug.LogError("No children for selection");
            return node;
        }
        Node bestNode = null;
        float maxValue = float.MinValue;
        foreach(var child in node.children)
        {
            float uctValue = UpperConfidenceBoundTree(child);
            if(uctValue > maxValue)
            {
                maxValue = uctValue;
                bestNode = child;
            }
        }
        node = bestNode;
        return node;
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
            bool exists = false;
            foreach(var child in node.children)
            {
                if(child.move == move)
                {
                    exists = true;
                    break;
                }
            }
            if(!exists)
            {
                Node newNode = new Node(node, move, node.availableMoves, node.redTiles, node.blueTiles, !node.redTurn);
                node.children.Add(newNode);
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
            Vector3Int randomMove = tempMoves.ElementAt(Random.Range(0, tempMoves.Count));
            tempMoves.Remove(randomMove);
            if(turn)
            {
                tempRedTiles.Add(randomMove);
            } 
            else
            {
                tempBlueTiles.Add(randomMove);
            } 
            turn = !turn;
            GameTiles instance = GameObject.FindObjectOfType<GameTiles>();
            if (instance.CheckForWin(turn ? tempRedTiles : tempBlueTiles, turn)) 
                if(turn)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
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
}

