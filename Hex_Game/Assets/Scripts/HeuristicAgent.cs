using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random=UnityEngine.Random;
using System.Linq;

public class HeuristicAgent 
{
    private Vector3Int center = new Vector3Int(5,5,0);
    private bool first = true;
    private Vector3Int FetchBestMove(HashSet<Vector3Int> availableMoves, HashSet<Vector3Int> clickedRedTiles, HashSet<Vector3Int> clickedBlueTiles, bool redTurn)
    {
        if(first && availableMoves.Contains(center))
        {
            first = false;
            return center;
        }
        if(first)
        {
            first = false;
            foreach(var move in GetSimulationNeighbors3(center))
            {
                if (availableMoves.Contains(move))
                {

                    return move;
                }
            }
        }
        return DjikstraNextMove(availableMoves, clickedRedTiles, clickedBlueTiles, redTurn);
    }
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
