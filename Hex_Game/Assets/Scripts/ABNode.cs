using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ABNode : MonoBehaviour
{
    public ABNode parent;
    public List<ABNode> children = new List<ABNode>();
    public Dictionary<Vector3Int, ABNode> lookupChildren = new Dictionary<Vector3Int, ABNode>();
    public Vector3Int move;
    public int value;
    public HashSet<Vector3Int> availableMoves; 
    public HashSet<Vector3Int> redTiles;
    public HashSet<Vector3Int> blueTiles;
    public int Alpha = int.MaxValue;
    public int Beta = int.MinValue;
    int bestVal;
    public ABNode()
    {

    }
    public int AlphaBeta(Node node, int depth, int alpha, int beta, bool maximizingPlayer)
    {
        if(depth == 0 || IsTerminalNode(node))
        {
            return 0; //0 is a placeholder, should be node heuristic value and not 0
        }
        if(maximizingPlayer)
        {
            bestVal = int.MinValue;
            foreach(var child in node.children)
            {
                bestVal = Mathf.Max(bestVal, AlphaBeta(child, depth + 1, alpha, beta, false));
                if(bestVal > beta)
                {
                    break;
                }
                alpha = Mathf.Max(alpha, bestVal);
            }
            return bestVal;
        }
        else
        {
            bestVal = int.MaxValue;
            foreach(var child in node.children)
            {
                bestVal = Mathf.Min(bestVal, AlphaBeta(child, depth + 1, alpha, beta, true));
                if(bestVal < alpha)
                {
                    break;
                }
                beta = Mathf.Min(beta, bestVal);
            }
            return bestVal;
        }
    }
    private bool IsTerminalNode(Node node)
    {
        return node.availableMoves.Count == 0;
    }
}
