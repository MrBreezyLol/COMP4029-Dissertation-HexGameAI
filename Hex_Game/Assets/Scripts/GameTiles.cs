using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;
public class GameTiles : MonoBehaviour
{
    
    
    [SerializeField] private Tilemap gameTiles;
    [SerializeField] private GameObject _highlight;
    [SerializeField] private TMP_Text TurnText;
    [SerializeField] private string gameMode = "";
    private bool isGameEnded = false;
    private bool AITurn = false;
    private Color red = new Color(0.86f, 0.28f, 0.23f, 1.0f);
    private Color blue = new Color(0.08f, 0.46f, 0.90f, 1.0f);
    private string currentTurn = "red";
    private List<Vector3Int> gameTileList = new List<Vector3Int>();
    private HashSet<Vector3Int> clickedBlueTiles = new HashSet<Vector3Int>();
    private HashSet<Vector3Int> clickedRedTiles = new HashSet<Vector3Int>();
    private GameObject _currentTile;
    private Vector3Int _previousTile;
    private MTCS mtcs;
    private ABPruning alphaBeta;
    private ABPruning2 alphaBeta2;
    void Start()
    {
        //retrieve the type of game mode from the mainscene user input
        gameMode = MainScene.gameMode;
        //populate a list with the tilemap 
        foreach (Vector3Int tile in gameTiles.cellBounds.allPositionsWithin)
        {
            if(gameTiles.HasTile(tile))
            {
                gameTileList.Add(tile);
            }
        }
        _currentTile = Instantiate(_highlight);
        _currentTile.SetActive(false);
        mtcs = new MTCS();
        alphaBeta = new ABPruning();
        alphaBeta2 = new ABPruning2();
    }
    void Update()
    {
        if(isGameEnded == true)
        {
            return;
        }
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0; 

        // world pos to grid pos
        Vector3Int cellPosition = gameTiles.WorldToCell(mouseWorldPos);
        
        if(currentTurn == "red")
        {
            TurnText.text = "Red's Turn";
        }
        else
        {
            TurnText.text = "Blue's Turn";
        }
        // check mouse cursor over tile
        TileGameLogic(cellPosition);
    }
    private void TileGameLogic(Vector3Int cellPosition)
    {
        if (gameTiles.HasTile(cellPosition) && !clickedRedTiles.Contains(cellPosition) && !clickedBlueTiles.Contains(cellPosition))
        {
            if (cellPosition != _previousTile) // check if cursor moved to a different tile
            {
                // highlight the tile below mouse cursor 
                _currentTile.SetActive(true);
                _currentTile.transform.position = gameTiles.GetCellCenterWorld(cellPosition);

                
                _previousTile = cellPosition;
            }
            if(gameMode == "PlayLocal")
            {
                LocalGameTurnLogic(cellPosition);
            }
            else
            {
                AIGameTurnLogic(cellPosition);
            }
        }
        else
        {
            // dont highlight if not over a tile
            _currentTile.SetActive(false);
            _previousTile = Vector3Int.zero;
        }
    }
    private void AIGameTurnLogic(Vector3Int cellPosition)
    {
        if(currentTurn == "red")
        {
            
            if(Input.GetMouseButtonDown(0) && !AITurn)
            {
                Debug.Log("You played the move " + TileOffset(cellPosition));
                PaintTile(cellPosition, red);
                clickedRedTiles.Add(cellPosition);
                gameTileList.Remove(cellPosition);
                currentTurn = "blue";
                
                if(CheckForWin(clickedRedTiles, true))
                {
                    Debug.Log("You Win!");
                    EndGame("red");
                }
                AITurn = true;
            }
        }
        else
        {
            

            //Vector3Int aiMove = mtcs.MTCSFetchBestMove(new HashSet<Vector3Int>(gameTileList), clickedRedTiles, clickedBlueTiles, false);
            Vector3Int aiMove = alphaBeta2.FetchBestMove(new HashSet<Vector3Int>(gameTileList), clickedRedTiles, clickedBlueTiles, false);
            
            Debug.Log("AI Played the move" + TileOffset(aiMove));
            PaintTile(aiMove, blue);
            clickedBlueTiles.Add(aiMove);
            gameTileList.Remove(aiMove);
            currentTurn = "red";
            
            if(CheckForWin(clickedBlueTiles, false))
            {
                Debug.Log("AI Wins!");
                EndGame("blue");
            }
            AITurn = false;
            
        }
    }
    private void LocalGameTurnLogic(Vector3Int cellPosition)
    {
        if(Input.GetMouseButtonDown(0))
            {
                
                if(currentTurn == "red")
                {
                    Debug.Log("Red played the move " + TileOffset(cellPosition));
                    PaintTile(cellPosition, red);
                    clickedRedTiles.Add(cellPosition);
                    currentTurn = "blue";
                    if(CheckForWin(clickedRedTiles, true))
                    {
                        Debug.Log("Red Wins!");
                        EndGame("red");
                    }
                }
                else
                {
                    Debug.Log("Blue played the move " + TileOffset(cellPosition));
                    PaintTile(cellPosition, blue);
                    clickedBlueTiles.Add(cellPosition);
                    currentTurn = "red";
                    if(CheckForWin(clickedBlueTiles, false))
                    {
                        Debug.Log("Blue Wins!");
                        EndGame("blue");
                    }
                }
            }
    }
    private void PaintTile(Vector3Int cellPosition, Color color)
    {
        
        gameTiles.SetTileFlags(cellPosition, TileFlags.None);
        gameTiles.SetColor(cellPosition, color);
    }
    private Vector2Int TileOffset(Vector3Int cell)
    {
        int y = cell.y;
        int row = 5 - y;
        int rowCalc = (6 - y) / 2;
        int xOffset = -7 + rowCalc;
        int column = cell.x - xOffset;
        return new Vector2Int(column, row);
    }
    public bool CheckForWin(HashSet<Vector3Int> playerTiles, bool isRed)
    {
        var offsetTiles = new HashSet<Vector2Int>();
        var startEdge = new HashSet<Vector2Int>();
        var endEdge = new HashSet<Vector2Int>();

        
        foreach (var cell in playerTiles)
        {
            Vector2Int preOffsetTiles = TileOffset(cell);
            offsetTiles.Add(preOffsetTiles);

            if (isRed)
            {
                if (preOffsetTiles.y == 0) startEdge.Add(preOffsetTiles);  //top
                if (preOffsetTiles.y == 10) endEdge.Add(preOffsetTiles);   //bottom
            }
            else
            {
                if (preOffsetTiles.x == 0) startEdge.Add(preOffsetTiles);  //left
                if (preOffsetTiles.x == 10) endEdge.Add(preOffsetTiles);   //right
            }
        }

        if (startEdge.Count == 0 || endEdge.Count == 0) return false;

        
        foreach (var start in startEdge)
        {
            if (BreadthFirstSearch(start, endEdge, offsetTiles))
                return true;
        }
        return false;
    }
    
     private bool BreadthFirstSearch(Vector2Int start, HashSet<Vector2Int> endEdge, HashSet<Vector2Int> offsetTiles)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (endEdge.Contains(current)) return true;

            foreach (var neighbor in GetNeighbors(current))
            {
                if (offsetTiles.Contains(neighbor) && 
                    !visited.Contains(neighbor) &&
                    neighbor.x >= 0 && neighbor.x <= 10 &&
                    neighbor.y >= 0 && neighbor.y <= 10)
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }
        return false;
    }
    private List<Vector2Int> GetNeighbors(Vector2Int Coordinates)
    {
        return new List<Vector2Int>
        {
            Coordinates + new Vector2Int(1, 0),   
            Coordinates + new Vector2Int(-1, 0),  
            Coordinates + new Vector2Int(0, 1),   
            Coordinates + new Vector2Int(0, -1),  
            Coordinates + new Vector2Int(1, -1), 
            Coordinates + new Vector2Int(-1, 1),  
        };
    }
    private void EndGame(string winner)
    {
        isGameEnded = true;
        TurnText.text = $"{winner} Wins!";
        _currentTile.SetActive(false);
    }
    private Vector3Int RandomAIMove(List<Vector3Int> gameTileList)
    {
        Vector3Int aiMove = gameTileList[Random.Range(0, gameTileList.Count)];
        return aiMove;

    }
    
}
