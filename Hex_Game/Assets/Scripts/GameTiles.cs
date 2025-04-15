using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;
using System.IO;
using UnityEngine.SceneManagement;
public class GameTiles : MonoBehaviour
{
    
    
    [SerializeField] private Tilemap gameTiles; //tilemap 
    [SerializeField] private GameObject _highlight; //highlight of a hex when hovering over it
    [SerializeField] private TMP_Text TurnText; //text to display turn
    [SerializeField] private string gameMode = "";  //store which gamemode was picked
    private bool GameRunning = true;    
    private bool AITurn = false;    
    private int redWins = 0;
    private int blueWins = 0;
    private Color red = new Color(0.86f, 0.28f, 0.23f, 1.0f);   //red color for red player
    private Color blue = new Color(0.08f, 0.46f, 0.90f, 1.0f);  //blue color for blue player
    private Color grey = new Color(0.54f, 0.52f, 0.52f, 1.0f);  //board color
    private string currentTurn = "red"; //string to check whose turn it is
    private string fileName = "/Results.txt";   
    private List<Vector3Int> gameTileList = new List<Vector3Int>(); //list of game tiles
    private List<Vector3Int> copyTileList = new List<Vector3Int>(); //copy of game tiles list
    private HashSet<Vector3Int> clickedBlueTiles = new HashSet<Vector3Int>();   //hashset to store played blue tiles
    private HashSet<Vector3Int> clickedRedTiles = new HashSet<Vector3Int>();    //hashset to store played red tiles
    private GameObject _currentTile;    //current selected tile
    private Vector3Int _previousTile;   //previous selected tile
    private MTCS mtcs;
    private MTCSRave mtcsRave;
    private MTCSBasic mtcsBasic;
    private HeuristicAgent heuristicAgent;
    private ABPruning2 alphaBeta;
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
                copyTileList.Add(tile);
            }
        }
        _currentTile = Instantiate(_highlight);
        _currentTile.SetActive(false);
        //initialize AI class object
        mtcs = new MTCS();
        mtcsRave = new MTCSRave();
        mtcsBasic = new MTCSBasic();
        alphaBeta = new ABPruning2();
        heuristicAgent = new HeuristicAgent();

        if(gameMode == "SimulateAIGame")    //if simulateAI was chosen run the function
        {
            _currentTile.SetActive(false);
            StartCoroutine(SimulateAIGame());
        }
    }
    void Update()
    {
        // if game stopped or simulateAI then disable game
        if(GameRunning == false || gameMode == "SimulateAIGame")
        {
            return;
        }
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);    //world position of the mouse
        mouseWorldPos.z = 0; 

        // convert world pos to grid pos
        Vector3Int cellPosition = gameTiles.WorldToCell(mouseWorldPos);
        
        if(currentTurn == "red")    //change the text based on whose turn it is
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
    
    //function to handle tile logic in the game
    private void TileGameLogic(Vector3Int cellPosition)
    {
        //check if tilemap has current selected tile and it has not been played yet
        if (gameTiles.HasTile(cellPosition) && !clickedRedTiles.Contains(cellPosition) && !clickedBlueTiles.Contains(cellPosition))
        {
            if (cellPosition != _previousTile) // check if cursor moved to a different tile
            {
                // highlight the tile below mouse cursor 
                _currentTile.SetActive(true);
                _currentTile.transform.position = gameTiles.GetCellCenterWorld(cellPosition);

                
                _previousTile = cellPosition;
            }
            if(gameMode == "PlayLocal") //if gamemode is playlocal run appropriate function
            {
                LocalGameTurnLogic(cellPosition);
            }
            else if(gameMode == "PlayAI")   //if gamemode is playAI run appropriate function
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
    //function for play between user and AI
    private void AIGameTurnLogic(Vector3Int cellPosition)
    {
        if(currentTurn == "red")
        {
            
            if(Input.GetMouseButtonDown(0) && !AITurn) //if clicked a tile
            {
                Debug.Log("You played the move " + TileOffset(cellPosition));
                Debug.Log(cellPosition);
                PaintTile(cellPosition, red);   //paint the tile with corresponding color
                clickedRedTiles.Add(cellPosition);
                gameTileList.Remove(cellPosition);
                currentTurn = "blue";
                
                if(CheckForWin(clickedRedTiles, true))  //if this is a winning move, end the game
                {
                    Debug.Log("You Win!");
                    EndGame("red");
                }
                AITurn = true;  //end of turn, set Ai turn to true
            }
        }
        else
        {
            
            //AI turn, select best move with chosen AI agent
            Vector3Int aiMove = heuristicAgent.FetchBestMove(new HashSet<Vector3Int>(gameTileList), clickedRedTiles, clickedBlueTiles, false);
            Debug.Log("AI Played the move" + TileOffset(aiMove));
            PaintTile(aiMove, blue);    //paint the tile with corresponding color
            clickedBlueTiles.Add(aiMove);
            gameTileList.Remove(aiMove);
            currentTurn = "red";    
            
            if(CheckForWin(clickedBlueTiles, false))    //if this is a winning move, end the game
            {
                Debug.Log("AI Wins!");
                EndGame("blue");
            }
            AITurn = false; //set AI turn to false
            
        }
    }
    //function to handle local games between two players, same logic as AI except its two players playing
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
    //function to simulate AI games
    private IEnumerator SimulateAIGame()
    {
        int redWins = 0;
        int blueWins = 0;
        
        //run until either red or blue gets 5 wins
        while(redWins < 5 && blueWins < 5)
        {
            ResetGame();    //reset the game at the start or after a game finishes
            
            while(GameRunning)  //same game logic except its 2 AI playing now, no user interaction
            {

                if(currentTurn == "red")    //select AI and play the best move
                {
                    Vector3Int aiMove = mtcs.MTCSFetchBestMove(new HashSet<Vector3Int>(gameTileList), clickedRedTiles, clickedBlueTiles, true);
                    Debug.Log("Red played the move " + TileOffset(aiMove));
                    PaintTile(aiMove, red);
                    clickedRedTiles.Add(aiMove);
                    gameTileList.Remove(aiMove);
                    currentTurn = "blue";
                    if(CheckForWin(clickedRedTiles, true))
                    {
                        redWins++;
                        Debug.Log("Red Wins!");
                        EndGame("red");
                        break;
                    }

                }
                else
                {
                    Vector3Int aiMove = mtcsRave.MTCSFetchBestMove(new HashSet<Vector3Int>(gameTileList), clickedRedTiles, clickedBlueTiles, false);
                    Debug.Log("Blue played the move " + TileOffset(aiMove));
                    PaintTile(aiMove, blue);
                    clickedBlueTiles.Add(aiMove);
                    gameTileList.Remove(aiMove);
                    currentTurn = "red";
                    if(CheckForWin(clickedBlueTiles, false))
                    {
                        blueWins++;
                        Debug.Log("Blue Wins!");
                        EndGame("blue");
                        break;
                    }
                }
                yield return null;
            }
            
            WriteToFile("Red Wins:" + redWins.ToString());  //write the scores for both red and blue at the end
            WriteToFile("Blue Wins:" + blueWins.ToString());
            yield return new WaitForSeconds(2f);
        }
        
        
        

       
    }
    //function to write a string to a file
    private void WriteToFile(string text)
    {
        
        using (StreamWriter writer = new StreamWriter(Application.dataPath + fileName,true))
        {
            writer.WriteLine(text);
        }
        
    }
    //function to reset the game 
    private void ResetGame()
    {
        GameRunning = true;
        currentTurn = "red";
        gameTileList = new List<Vector3Int>(copyTileList);
        clickedRedTiles.Clear();    //clear all played moves 
        clickedBlueTiles.Clear();
        foreach(var tile in gameTileList)   //for each tile set it back to how it started
        {
            gameTiles.SetTileFlags(tile, TileFlags.None);
            gameTiles.SetColor(tile, grey);
        }
        TurnText.text = "Red's Turn";
        

    }
    //function to paint the tile after a player move
    private void PaintTile(Vector3Int cellPosition, Color color)
    {
        
        gameTiles.SetTileFlags(cellPosition, TileFlags.None);
        gameTiles.SetColor(cellPosition, color);
    }
    //function to calculate tile offset so the range of tile positions is 0-10
    private Vector2Int TileOffset(Vector3Int cell)
    {
        int y = cell.y;
        int row = 5 - y;
        int rowCalc = (6 - y) / 2;
        int xOffset = -7 + rowCalc;
        int column = cell.x - xOffset;
        return new Vector2Int(column, row);
    }
    //function to check whether a player has won
    public bool CheckForWin(HashSet<Vector3Int> playerTiles, bool isRed)
    {
        var offsetTiles = new HashSet<Vector2Int>();
        var startEdge = new HashSet<Vector2Int>();  //hashset of start tiles
        var endEdge = new HashSet<Vector2Int>();    //hashset of end tiles

        
        foreach (var cell in playerTiles)   //for each tile, calculate its offset
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

        if (startEdge.Count == 0 || endEdge.Count == 0) return false;   //early exit if there are no edge moves played

        
        foreach (var start in startEdge)    //use BFS to determine if there is a complete unbroken path
        {
            if (BreadthFirstSearch(start, endEdge, offsetTiles))
                return true;
        }
        return false;
    }
    
    //BFS function to determine if there is an unbroken path
    private bool BreadthFirstSearch(Vector2Int start, HashSet<Vector2Int> endEdge, HashSet<Vector2Int> offsetTiles)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();  //initialize a queue
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();    //initialize a visited list
        
        queue.Enqueue(start);
        visited.Add(start); //add the start tile

        while (queue.Count > 0) //until queue count drops to 0
        {
            Vector2Int current = queue.Dequeue();   //retrieve the element from queue
            if (endEdge.Contains(current)) return true; // if the end tiles list contains the current move return true

            foreach (var neighbor in GetNeighbors(current)) //for each neighbour of current tile, check if player tiles contains it and its not yet visited
            {
                if (offsetTiles.Contains(neighbor) && 
                    !visited.Contains(neighbor) &&
                    neighbor.x >= 0 && neighbor.x <= 10 &&
                    neighbor.y >= 0 && neighbor.y <= 10)
                {
                    visited.Add(neighbor);  //if so add it to the visited to list and queue the neighbour so it iteratively checks for neighbours
                    queue.Enqueue(neighbor);
                }
            }
        }
        return false;   //otherwise theres no connection
    }
    //function to get the neighbours of a tile
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
    //function to end the game by disabling interaction and display the winner
    private void EndGame(string winner)
    {
        GameRunning = false;
        TurnText.text = $"{winner} Wins!";
        _currentTile.SetActive(false);
        if (gameMode == "SimulateAI")
        {
            if (winner == "red")
                redWins++;
            else
                blueWins++;
            
            Debug.Log($"Red Wins: {redWins}, Blue Wins: {blueWins}");
        }
    }
    //function to play a random move
    private Vector3Int RandomAIMove(List<Vector3Int> gameTileList)
    {
        Vector3Int aiMove = gameTileList[Random.Range(0, gameTileList.Count)];
        return aiMove;

    }
    
}
