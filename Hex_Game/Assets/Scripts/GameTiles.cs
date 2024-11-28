using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;
public class GameTiles : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private Tilemap gameTiles;
    [SerializeField] private GameObject _highlight;
    [SerializeField] private TMP_Text TurnText;
    private Color red = new Color(0.86f, 0.28f, 0.23f, 1.0f);
    private Color blue = new Color(0.08f, 0.46f, 0.90f, 1.0f);
    private string currentTurn = "red";
    private HashSet<Vector3Int> clickedBlueTiles = new HashSet<Vector3Int>();
    private HashSet<Vector3Int> clickedRedTiles = new HashSet<Vector3Int>();
    private GameObject _currentTile;
    private Vector3Int _previousTile;
    void Start()
    {
        _currentTile = Instantiate(_highlight);
        _currentTile.SetActive(false);
        
    }
    void Update()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0; // Ensure z-coordinate is zero for 2D

        // Convert world position to Tilemap grid position
        Vector3Int cellPosition = gameTiles.WorldToCell(mouseWorldPos);
        if(currentTurn == "red")
        {
            TurnText.text = "Red's Turn";
        }
        else
        {
            TurnText.text = "Blue's Turn";
        }
        // Check if the mouse is over a tile
        if (gameTiles.HasTile(cellPosition) && !clickedRedTiles.Contains(cellPosition) && !clickedBlueTiles.Contains(cellPosition))
        {
            if (cellPosition != _previousTile) // Check if the tile has changed
            {
                // Activate and move the highlight object to the hovered tile's center
                _currentTile.SetActive(true);
                _currentTile.transform.position = gameTiles.GetCellCenterWorld(cellPosition);

                // Update the previous tile
                _previousTile = cellPosition;
            }
            if(Input.GetMouseButtonDown(0))
            {
                Debug.Log("Clicked on tile at " + cellPosition);
                if(currentTurn == "red")
                {
                    
                    PaintTile(cellPosition, red);
                    clickedRedTiles.Add(cellPosition);
                    currentTurn = "blue";
                    if(CheckForWin(clickedRedTiles, true))
                    {
                        Debug.Log("Red Wins");
                    }
                }
                else
                {
                    PaintTile(cellPosition, blue);
                    clickedBlueTiles.Add(cellPosition);
                    currentTurn = "red";
                    if(CheckForWin(clickedBlueTiles, false))
                    {
                        Debug.Log("Blue Wins");
                    }
                }
                
                
            }
        }
        else
        {
            // Deactivate the highlight object if the mouse is not over a tile
            _currentTile.SetActive(false);
            _previousTile = Vector3Int.zero;
        }
    }
    void PaintTile(Vector3Int cellPosition, Color color)
    {
        
        gameTiles.SetTileFlags(cellPosition, TileFlags.None);
        gameTiles.SetColor(cellPosition, color);
    }
    private bool CheckForWin(HashSet<Vector3Int> playerTiles, bool isRed)
    {
        // Get edges based on player
        HashSet<Vector3Int> startEdge = new HashSet<Vector3Int>();
        HashSet<Vector3Int> endEdge = new HashSet<Vector3Int>();

        foreach (var tile in playerTiles)
        {
            Vector3 worldPosition = gameTiles.CellToWorld(tile);

            if (isRed)
            {
                if (worldPosition.y <= gameTiles.cellBounds.yMin) startEdge.Add(tile);
                if (worldPosition.y >= gameTiles.cellBounds.yMax) endEdge.Add(tile);
            }
            else
            {
                if (worldPosition.x <= gameTiles.cellBounds.xMin) startEdge.Add(tile);
                if (worldPosition.x >= gameTiles.cellBounds.xMax) endEdge.Add(tile);
            }
        }

        // Use DFS to find a path from startEdge to endEdge
        foreach (var startTile in startEdge)
        {
            if (DepthFirstSearch(startTile, endEdge, playerTiles, new HashSet<Vector3Int>()))
                return true;
        }

        return false;
    }    
    private bool DepthFirstSearch(Vector3Int current, HashSet<Vector3Int> endEdge, HashSet<Vector3Int> playerTiles, HashSet<Vector3Int> visited)
    {
        if (visited.Contains(current)) return false;
        if (endEdge.Contains(current)) return true;

        visited.Add(current);

        // Get neighbors of the current tile
        foreach (var neighbor in CheckNeighbours(current))
        {
            if (playerTiles.Contains(neighbor) && DepthFirstSearch(neighbor, endEdge, playerTiles, visited))
            {
                return true;
            }
        }

        return false;
    }
    /*private bool DepthFirstSearch(Vector3Int start, Vector3Int end, HashSet<Vector3Int> clickedTiles)
    {
        Stack<Vector3Int> stack = new Stack<Vector3Int>();
        stack.Push(start);
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        while(stack.Count > 0)
        {
            Vector3Int current = stack.Pop();
            if(current == end)
            {
                return true;
            }
            if(visited.Contains(current))
            {
                continue;
            }
            visited.Add(current);
            List<Vector3Int> neighbours = CheckNeighbours(current);
            foreach(Vector3Int neighbour in neighbours)
            {
                if(!clickedTiles.Contains(neighbour))
                {
                    stack.Push(neighbour);
                }
            }
        }
        return false;
    }*/
    private List<Vector3Int> CheckNeighbours(Vector3Int position)
    {
        List<Vector3Int> neighbours = new List<Vector3Int>();
        neighbours.Add(new Vector3Int(position.x + 1, position.y, position.z));
        neighbours.Add(new Vector3Int(position.x - 1, position.y, position.z));
        neighbours.Add(new Vector3Int(position.x, position.y + 1, position.z));
        neighbours.Add(new Vector3Int(position.x, position.y - 1, position.z));
        neighbours.Add(new Vector3Int(position.x + 1, position.y - 1, position.z));
        neighbours.Add(new Vector3Int(position.x - 1, position.y + 1, position.z));
        return neighbours;
    }
    
}
