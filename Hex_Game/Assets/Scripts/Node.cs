using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    //node class for MTCS
    public Node parent; //parent node
    public List<Node> children = new List<Node>();  //list of children of type node
    public Dictionary<Vector3Int, Node> lookupChildren = new Dictionary<Vector3Int, Node>();    //dictionary to easily lookup children
    public Vector3Int move; //move corresponding to the node
    public int wins;    //wins from this node, updated in backpropagation phase in MTCS
    public int visits;  //visits of this node, updated in backpropagation phase in MTCS
    public HashSet<Vector3Int> availableMoves; //hashset of availableMoves
    public HashSet<Vector3Int> redTiles;    //hashset of played red tiles
    public HashSet<Vector3Int> blueTiles;   //hashset of played blue tiles
    public bool redTurn;

    public Node(Node parent1, Vector3Int move1, HashSet<Vector3Int> availableMoves1, HashSet<Vector3Int> redTiles1, HashSet<Vector3Int> blueTiles1, bool redTurn1)
    {
        parent = parent1;
        move = move1;
        wins = 0;
        visits = 0;
        availableMoves = new HashSet<Vector3Int>(availableMoves1);
        redTiles = new HashSet<Vector3Int>(redTiles1);
        blueTiles = new HashSet<Vector3Int>(blueTiles1);
        redTurn = redTurn1;
        if(move != null)
        {
            availableMoves.Remove(move);
            if(redTurn)
            {
                redTiles.Add(move);
            }
            else
            {
                blueTiles.Add(move);
            }
        }
    }
    //function to check if node is expanded
    public bool isExpanded()
    {
        
        return lookupChildren.Count == availableMoves.Count;
    }
}
