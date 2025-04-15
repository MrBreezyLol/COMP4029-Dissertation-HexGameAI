using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaveNode
{
    //rave node class for MTCS RAVE, same as normal node class but with some additions
    public RaveNode parent;
    public List<RaveNode> children = new List<RaveNode>();
    public Dictionary<Vector3Int, RaveNode> lookupChildren = new Dictionary<Vector3Int, RaveNode>();
    public Vector3Int move;
    public Dictionary<Vector3Int, int> raveWins = new Dictionary<Vector3Int, int>();    //dictionary of rave wins for specific moves 
    public Dictionary<Vector3Int, int> raveVisits = new Dictionary<Vector3Int, int>();  //dictionary of rave visits for specific moves

    public int wins;
    public int visits;
    public HashSet<Vector3Int> availableMoves; 
    public HashSet<Vector3Int> redTiles;
    public HashSet<Vector3Int> blueTiles;
    public bool redTurn;

    public RaveNode(RaveNode parent1, Vector3Int move1, HashSet<Vector3Int> availableMoves1, HashSet<Vector3Int> redTiles1, HashSet<Vector3Int> blueTiles1, bool redTurn1)
    {
        parent = parent1;
        move = move1;
        raveWins[move] = 0;
        raveVisits[move] = 0;
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
    public float RaveScore(Vector3Int move) //function to calculate rave score
    {
        if(!raveVisits.ContainsKey(move) || raveVisits[move] == 0)  //if rave visits does not contain move or number of visits is 0, set it to 0
        {

            return 0;
        }
        else
        {
            return (float)raveWins[move] / raveVisits[move];    //else return wins / visits
        }
    }
}
