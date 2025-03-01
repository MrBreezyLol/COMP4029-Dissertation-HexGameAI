using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public Node parent;
    public List<Node> children = new List<Node>();
    public Dictionary<Vector3Int, Node> lookupChildren = new Dictionary<Vector3Int, Node>();
    public Vector3Int move;
    public int wins;
    public int visits;
    public HashSet<Vector3Int> availableMoves; 
    public HashSet<Vector3Int> redTiles;
    public HashSet<Vector3Int> blueTiles;
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
    public bool isExpanded()
    {
        
        return lookupChildren.Count == availableMoves.Count;
    }
}
