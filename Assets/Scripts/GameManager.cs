using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Tile Prefabs")]
    public GameObject emptyTilePrefab;
    public GameObject goalTilePrefab;
    public GameObject startTilePrefab;

    private List<LevelDesign> _levelDesigns;
    private LevelDesign _currentLevel;
    private List<GameObject> _currentLevelGameObjects;

    // Start is called before the first frame update
    void Start()
    {
        _levelDesigns = new List<LevelDesign>();
        _currentLevel = null;
        _currentLevelGameObjects = new List<GameObject>();

        GetLevelsFromFile();
        BeginGame();
    }

    // Update is called once per frame
    void Update()
    {
        //TODO take input from player
        if (Input.GetButtonDown("Fire1")) //LMB
        {
            Vector3 pressedCoords = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int coordX = Mathf.RoundToInt(pressedCoords.x);
            int coordY = Mathf.RoundToInt(pressedCoords.y);
            print("X: " + coordX + " Y: " + coordY);
            LoadLevel(1);
        }
    }

    private void GetLevelsFromFile()
    {
        //we'll hold the levels in a csv format file
        //we'll load the levels from the folder here

        //TODO path will be changed
        String path = Application.dataPath + "/CSV/";

        var info = new DirectoryInfo(path);
        var fileInfo = info.GetFiles("*.csv");
        foreach (FileInfo file in fileInfo)
        {
            StreamReader reader = new StreamReader(file.OpenRead());

            List<List<string>> newLevel = new List<List<string>>();

            string parametersLine = reader.ReadLine();
            string[] splitParameters = parametersLine.Split(';');
            int columnCount = splitParameters.Length;
            int cameraSize = Convert.ToInt32(splitParameters[0]);
            Vector3 cameraTargetPos = new Vector3(  Convert.ToInt32(splitParameters[1]),
                                                    Convert.ToInt32(splitParameters[2]),
                                                    Convert.ToInt32(splitParameters[3]));

            string wholeFile = reader.ReadToEnd();
            string[] singleLines = wholeFile.Split('\n');

            //TODO this is very bad. a custom offset here really hurts modding, players might create maps that crash the game
            int lineCount = singleLines.Length - 1; 

            int[,] levelLayout = new int[lineCount, columnCount];

            for (int y = 0; y < lineCount; ++y)
            {
                string line = singleLines[y];
                string[] splitLine = line.Split(';');

                for (int x = 0; x < columnCount; ++x)
                {
                    if (splitLine[x].Length != 0)
                    {
                        print("i: " + y + " j: " + x);
                        levelLayout[x, y] = Convert.ToInt32(splitLine[x]);
                    }                    
                }
            }

            _levelDesigns.Add(new LevelDesign(cameraSize, cameraTargetPos, levelLayout));

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
        _currentLevel = null;
    }

    private void LoadLevel(int levelIndex)
    {
        ClearExistingLevel();

        _currentLevel = _levelDesigns[levelIndex];

        for (int x = 0, coordX = 0; x < _currentLevel.LevelWidth; ++x, ++coordX)
        {
            for (int y = 0, coordY = _currentLevel.LevelHeight - 1; y < _currentLevel.LevelHeight; ++y, --coordY)
            {
                Vector3 coordsVec = new Vector3(coordX, coordY, 0);
                switch (_currentLevel.LevelLayout[x,y])
                {
                    case 0:
                        //empty, but we might need something later on
                        break;
                    case 1:
                        //void tile
                        _currentLevelGameObjects.Add(Instantiate(emptyTilePrefab, coordsVec, Quaternion.identity) as GameObject);
                        break;
                    case 2:
                        //start tile
                        _currentLevelGameObjects.Add(Instantiate(startTilePrefab, coordsVec, Quaternion.identity) as GameObject);
                        break;
                    case 3:
                        //goal tile
                        _currentLevelGameObjects.Add(Instantiate(goalTilePrefab, coordsVec, Quaternion.identity) as GameObject);
                        break;
                    default:
                        throw new System.NotImplementedException();
                }
            }
        }
    }
}
