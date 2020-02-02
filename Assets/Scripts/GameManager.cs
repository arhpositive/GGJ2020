using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    public AudioClip[] explosionEffects;
    public AudioClip[] swipingEffects;
    public AudioClip[] timeLimitEffects;
    public AudioClip[] timeOverEffects;
    public AudioClip[] retryEffects;
    public AudioClip[] successEffects;

    public GameObject[] emptyTilePrefabs;
    public GameObject lPipeTilePrefab;
    public GameObject rPipeTilePrefab;
    public GameObject lPipe90TilePrefab;
    public GameObject rPipe90TilePrefab;
    public GameObject straightPipeTilePrefab;
    public GameObject straightPipe2TilePrefab;
    public GameObject startTilePrefab;
    public GameObject goalTilePrefab;
    public GameObject bombTilePrefab; //removes itself when connected, does not lock down pipes
    public GameObject timeBonusTilePrefab; //adds time when connected, does not lock down pipes but locks itself down

    public Button nextLevelButton;
    public Button retryLevelButton;
    public Text timeLimitText;

    private List<LevelDesign> _levelDesigns;
    private int _currentLevelDesignIndex;

    private List<GameObject> _currentLevelGameObjects;
    private Tile[,] _currentLevelTiles;
    //timelimit parameters
    private int _currentLevelTimeLimit;

    //swipe parameters
    private Vector2Int _swipeStartPos;
    private Vector2Int _swipeEndPos;
    private Vector3 _swipeMotionBegin;
    private Vector3 _swipeMotionEnd;
    private Tile _swipeTile;
    private bool _swipeInProgress;

    //connectivity parameters
    private List<StartTile> _startTiles;
    private List<GoalTile> _goalTiles;

    private AudioSource _audioSource;

    // Start is called before the first frame update
    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.Play();

        _levelDesigns = new List<LevelDesign>();
        _currentLevelGameObjects = new List<GameObject>();
        _currentLevelTimeLimit = 0;
        UpdateTimeLimitText();
        _startTiles = new List<StartTile>();
        _goalTiles = new List<GoalTile>();
        _swipeTile = null;
        _swipeInProgress = false;

        GetLevelsFromFile();
        BeginGame();
    }

    // Update is called once per frame
    void Update()
    {
        if (_swipeTile && _swipeTile.IsInMotion())
        {
            _swipeInProgress = true;
            return;
        }

        if (_swipeInProgress)
        {
            _swipeInProgress = false;
            CheckForConnectivity();
            if (_currentLevelTimeLimit == 0)
            {
                //light text color and retry button color to red!
                AudioSource.PlayClipAtPoint(timeOverEffects[Random.Range(0, timeOverEffects.Length)], transform.position);
                ApplyMoveLimitVisualEffects(Color.red, Color.red);
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            _swipeMotionBegin = Input.mousePosition;
            _swipeStartPos = GetTileCoordsFromMousePosition();
        }

        if (Input.GetMouseButtonUp(0))
        {
            _swipeMotionEnd = Input.mousePosition;

            Vector3 diff = _swipeMotionEnd - _swipeMotionBegin;
            Vector2Int swipeDir = Vector2Int.zero;

            if (Math.Abs(diff.x) > Math.Abs(diff.y))
            {
                //horizontal swipe
                swipeDir = (diff.x > 0 ? Vector2Int.right : Vector2Int.left);
            }
            else
            {
                //vertical swipe
                swipeDir = (diff.y > 0 ? Vector2Int.up : Vector2Int.down);
            }

            //this might be the same as _swipeStartPos, we have an if clause to catch this down in swipe execution function
            _swipeEndPos = _swipeStartPos + swipeDir;

            //execute swipe
            OnSwipe();
        }
    }

    private void ApplyMoveLimitVisualEffects(Color buttonColor, Color textColor)
    {
        retryLevelButton.GetComponent<Image>().color = buttonColor;
        timeLimitText.color = textColor;
    }

    private void UpdateTimeLimitText()
    {
        timeLimitText.text = _currentLevelTimeLimit.ToString("0000");
    }

    private Vector2Int GetTileCoordsFromMousePosition()
    {
        Vector3 pressedPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int pressedTileCoords = new Vector2Int(Mathf.RoundToInt(pressedPos.x), Mathf.RoundToInt(pressedPos.y));
        return pressedTileCoords;
    }

    private void GetLevelsFromFile()
    {
        //we'll hold the levels in a csv format file
        //we'll load the levels from the folder here

        String path = Application.streamingAssetsPath + "/Levels/";

        var info = new DirectoryInfo(path);
        var fileInfo = info.GetFiles("*.csv");
        foreach (FileInfo file in fileInfo)
        {
            StreamReader reader = new StreamReader(file.OpenRead());

            List<List<string>> newLevel = new List<List<string>>();

            string parametersLine = reader.ReadLine();
            string[] splitParameters = parametersLine.Split(';');
            
            int cameraSize = Convert.ToInt32(splitParameters[0]);
            Vector3 cameraPos = new Vector3(  Convert.ToInt32(splitParameters[1]),
                                                    Convert.ToInt32(splitParameters[2]),
                                                    Convert.ToInt32(splitParameters[3]));
            int levelTimeLimit = Convert.ToInt32(splitParameters[4]);

            string wholeFile = reader.ReadToEnd();
            string[] singleLines = wholeFile.Split('\n');

            //this part is so bad I'm ashamed
            string[] colSizeMeasureLine = singleLines[0].Split(';');
            int columnCount = 0;
            for (columnCount = 0; columnCount < colSizeMeasureLine.Length; ++columnCount)
            {
                if (colSizeMeasureLine[columnCount].Equals("\r"))
                {
                    break;
                }
            }

            //this is very bad. a custom offset here really hurts modding, players might create maps that crash the game
            int lineCount = singleLines.Length - 1; 

            int[,] levelLayout = new int[columnCount, lineCount];

            for (int x = 0; x < lineCount; ++x)
            {
                string line = singleLines[x];
                string[] splitLine = line.Split(';');

                for (int y = 0; y < columnCount; ++y)
                {
                    if (splitLine[y].Length != 0)
                    {
                        levelLayout[y, x] = Convert.ToInt32(splitLine[y]);
                    }                    
                }
            }

            _levelDesigns.Add(new LevelDesign(cameraSize, cameraPos, levelLayout, levelTimeLimit));

            //there might be a third section of the level designer 
            //indicating "lines that will come in the future"
        }
    }

    private void BeginGame()
    {
        //load the first level of game
        LoadLevel(0);
    }

    private void ClearExistingLevel()
    {
        foreach (GameObject go in _currentLevelGameObjects)
        {
            Destroy(go);
        }

        _startTiles.Clear();
        _goalTiles.Clear();
        _currentLevelTiles = null; //does this even work? how can we clear this? do we need to?
    }

    private void LoadLevel(int levelIndex)
    {
        _currentLevelDesignIndex = levelIndex;

        ClearExistingLevel();

        LevelDesign currentLevel = _levelDesigns[_currentLevelDesignIndex];

        //set camera position and size
        Camera.main.transform.position = currentLevel.CameraPos;
        Camera.main.orthographicSize = currentLevel.CameraSize;
        _currentLevelTimeLimit = currentLevel.LevelTimeLimit;
        UpdateTimeLimitText();
        ApplyMoveLimitVisualEffects(Color.white, Color.black);

        //set current level tiles
        _currentLevelTiles = new Tile[currentLevel.LevelWidth, currentLevel.LevelHeight];

        for (int x = 0, coordX = 0; x < currentLevel.LevelWidth; ++x, ++coordX)
        {
            for (int y = 0, coordY = currentLevel.LevelHeight - 1; y < currentLevel.LevelHeight; ++y, --coordY)
            {
                GameObject go;
                Vector3 coordsVec = new Vector3(coordX, coordY, 0);
                switch (currentLevel.LevelLayout[x,y])
                {
                    case 0:
                        //no tile, but we might need something later on
                        break;
                    case 1:
                        //empty tile                   
                        go = Instantiate(emptyTilePrefabs[Random.Range(0, emptyTilePrefabs.Length)], coordsVec, Quaternion.identity) as GameObject;
                        go.transform.Rotate(new Vector3(0, 0, Random.Range(0, 4) * 90));
                        _currentLevelTiles[coordX, coordY] = go.GetComponent<Tile>();
                        _currentLevelTiles[coordX, coordY].SetTilePosition(new Vector2Int(coordX, coordY));
                        _currentLevelGameObjects.Add(go);
                        break;
                    case 2:
                        //start tile
                        go = Instantiate(startTilePrefab, coordsVec, Quaternion.identity) as GameObject;
                        go.transform.Rotate(new Vector3(0, 0, Random.Range(0, 4) * 90));
                        StartTile startTile = go.GetComponent<StartTile>();
                        _startTiles.Add(startTile);
                        _currentLevelTiles[coordX, coordY] = startTile;
                        _currentLevelTiles[coordX, coordY].SetTilePosition(new Vector2Int(coordX, coordY));
                        _currentLevelGameObjects.Add(go);
                        break;
                    case 3:
                        //goal tile
                        AddLevelTileToLists(goalTilePrefab, coordX, coordY, coordsVec, true);
                        break;
                    case 4:
                        //straight pipe
                        AddLevelTileToLists(straightPipeTilePrefab, coordX, coordY, coordsVec, false);
                        break;
                    case 5:
                        //str2-pipe
                        AddLevelTileToLists(straightPipe2TilePrefab, coordX, coordY, coordsVec, false);
                        break;
                    case 6:
                        //l-pipe
                        AddLevelTileToLists(lPipeTilePrefab, coordX, coordY, coordsVec, false);
                        break;
                    case 7:
                        //l-pipe90
                        AddLevelTileToLists(lPipe90TilePrefab, coordX, coordY, coordsVec, false);
                        break;
                    case 8:
                        //r-pipe
                        AddLevelTileToLists(rPipeTilePrefab, coordX, coordY, coordsVec, false);
                        break;
                    case 9:
                        //r-pipe90
                        AddLevelTileToLists(rPipe90TilePrefab, coordX, coordY, coordsVec, false);
                        break;
                    case 10:
                        //bomb
                        AddLevelTileToLists(bombTilePrefab, coordX, coordY, coordsVec, false);
                        break;
                    case 11:
                        //timebonus
                        AddLevelTileToLists(timeBonusTilePrefab, coordX, coordY, coordsVec, false);
                        break;
                    default:
                        throw new System.NotImplementedException();
                }
            }
        }
    }

    private void AddLevelTileToLists(GameObject prefab, int coordX, int coordY, Vector3 coordsVec, bool rotate)
    {
        GameObject go = Instantiate(prefab, coordsVec, Quaternion.identity) as GameObject;
        if (rotate)
        {
            go.transform.Rotate(new Vector3(0, 0, Random.Range(0, 4) * 90));
        }
        _currentLevelTiles[coordX, coordY] = go.GetComponent<Tile>();
        _currentLevelTiles[coordX, coordY].SetTilePosition(new Vector2Int(coordX, coordY));
        _currentLevelGameObjects.Add(go);
    }

    private void OnSwipe()
    {
        if (_currentLevelTimeLimit > 0 && (_swipeStartPos != _swipeEndPos) && IsWithinBounds(_swipeStartPos) && IsWithinBounds(_swipeEndPos))
        {
            //coordinates are within bounds
            _swipeTile = _currentLevelTiles[_swipeStartPos.x, _swipeStartPos.y];

            //a swipe from a full tile to an empty tile is valid
            if (_swipeTile != null && _swipeTile.CanBeMoved && _currentLevelTiles[_swipeEndPos.x, _swipeEndPos.y] == null)
            {
                //swap tiles
                _currentLevelTiles[_swipeEndPos.x, _swipeEndPos.y] = _swipeTile;
                _currentLevelTiles[_swipeStartPos.x, _swipeStartPos.y] = null;

                bool swipeSuccessful = _swipeTile.SwipeTile(_swipeEndPos);

                if (swipeSuccessful)
                {
                    AudioSource.PlayClipAtPoint(swipingEffects[Random.Range(0, swipingEffects.Length)], transform.position);
                    --_currentLevelTimeLimit;
                    UpdateTimeLimitText();
                }
            }
        }
    }

    private bool IsWithinBounds(Vector2Int coords)
    {
        return (coords.x >= 0
            && coords.x < _currentLevelTiles.GetLength(0)
            && coords.y >= 0
            && coords.y < _currentLevelTiles.GetLength(1));
    }

    private void CheckForConnectivity()
    {
        bool connectionFound = false;
        foreach(StartTile st in _startTiles)
        {
            if (st.IsUsedUp())
            {
                continue;
            }

            //check if this tile reaches any goal tiles by connecting via the pipes
            List<Tile> pathToSuccess = new List<Tile>();
            GoalTile goalThatWasFound = null;
            connectionFound = CheckTileConnections(st, Vector2Int.zero, st, pathToSuccess, out goalThatWasFound);

            if (connectionFound)
            {
                JubilationInNewark(pathToSuccess, goalThatWasFound);

                break;
            }
        }
    }

    private bool CheckTileConnections(Tile tile, Vector2Int incomingDir, Tile lastEncounteredStartTile, List<Tile> pathToSuccess, out GoalTile goalThatWasFound)
    {
        if (tile is StartTile)
        {
            lastEncounteredStartTile = tile;
            pathToSuccess.Clear();
        }

        bool goalFound = tile is GoalTile && !tile.IsUsedUp();

        goalThatWasFound = goalFound ? (GoalTile)tile : null;

        bool linkedToPreviousTile = (tile == lastEncounteredStartTile);

        foreach (Vector2Int cd in tile.ConnectionDirs)
        {
            if (cd == incomingDir)
            {
                //this is where the line comes from, no need to take this into account for future
                //but we need to assure we have connection here
                linkedToPreviousTile = true;
                break;
            }
        }

        if (!linkedToPreviousTile)
        {
            return false;
        }
        else
        {
            pathToSuccess.Add(tile);
        }

        foreach (Vector2Int cd in tile.ConnectionDirs)
        {
            if (cd != incomingDir && !goalFound)
            {
                Vector2Int targetPosition = tile.GetTilePosition() + cd;

                if (IsWithinBounds(targetPosition))
                {
                    //recursive call, what's important is reversing the direction so that incoming tile is properly controlled
                    //we should be afraid of loops!
                    Tile targetTile = _currentLevelTiles[targetPosition.x, targetPosition.y];

                    if (targetTile) //space could be empty altogether
                    {
                        goalFound = CheckTileConnections(targetTile, new Vector2Int(-cd.x, -cd.y), lastEncounteredStartTile, pathToSuccess, out goalThatWasFound);
                    }
                }                
            }            
        }
        return goalFound;
    }

    //this is a meme name, i'm a nets fan
    //one connection succeeded
    private void JubilationInNewark(List<Tile> pathToSuccess, GoalTile goalThatWasFound)
    {
        switch (goalThatWasFound.typeOfGoal)
        {
            case GoalType.gt_bomb:
                goalThatWasFound.Jubilation();
                AudioSource.PlayClipAtPoint(explosionEffects[Random.Range(0, explosionEffects.Length)], transform.position);
                //this should actually come from removeOnUse
                Vector2Int tilePos = goalThatWasFound.GetTilePosition();
                _currentLevelTiles[tilePos.x, tilePos.y] = null;
                break;
            case GoalType.gt_score:
                AudioSource.PlayClipAtPoint(successEffects[Random.Range(0, successEffects.Length)], transform.position);
                foreach (Tile t in pathToSuccess)
                {
                    t.Jubilation();
                }
                break;
            case GoalType.gt_timeBonus:
                goalThatWasFound.Jubilation();
                AudioSource.PlayClipAtPoint(timeLimitEffects[Random.Range(0, timeLimitEffects.Length)], transform.position);
                _currentLevelTimeLimit += goalThatWasFound.timeBonus;
                UpdateTimeLimitText();
                ApplyMoveLimitVisualEffects(Color.white, Color.blue);
                break;
            default:
                break;
        }

        bool allGoalsDone = true;
        foreach(StartTile st in _startTiles)
        {
            if (!st.IsUsedUp())
            {
                allGoalsDone = false;
            }
        }

        if (allGoalsDone)
        {
            nextLevelButton.interactable = true;
            nextLevelButton.GetComponent<Image>().color = Color.green;
        }
    }

    public void RetryLevel()
    {
        AudioSource.PlayClipAtPoint(retryEffects[Random.Range(0, retryEffects.Length)], transform.position);
        LoadLevel(_currentLevelDesignIndex);
    }

    public void NextLevel()
    {
        AudioSource.PlayClipAtPoint(retryEffects[Random.Range(0, retryEffects.Length)], transform.position);
        if (_currentLevelDesignIndex < _levelDesigns.Count)
        {
            LoadLevel(_currentLevelDesignIndex + 1);
        }
        else
        {
            //TODO game is finished!
            LoadLevel(0);
        }
        nextLevelButton.interactable = false;
        nextLevelButton.GetComponent<Image>().color = Color.white;
    }
}
