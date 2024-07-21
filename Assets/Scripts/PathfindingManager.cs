using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;
using Assets.Scripts.Grid;
using UnityEngine.UIElements;
using UnityEngine.UI;
using System.IO;
using System;
using Assets.Scripts.IAJ.Unity.Pathfinding;
using Assets.Scripts.IAJ.Unity.Pathfinding.DataStructures;
using Assets.Scripts.IAJ.Unity.Pathfinding.Heuristics;
using UnityEngine.Networking;

public class PathfindingManager : MonoBehaviour
{

    // DEFAULT POSITIONS =====================================================

    // Struct for default positions
    [Serializable]
    public struct SearchPos
    {
        public Vector2 startingPos;
        public Vector2 goalPos;
    }

    // Struct to store default positions by grid
    [Serializable]
    public struct SearchPosPerGrid
    {
        public string gridName;
        public List<SearchPos> searchPos;
    }

    // Default positions, useful for testing
    public List<SearchPosPerGrid> defaultPositions;

    //=======================================================================


    // SETTINGS =============================================================

    [Header("Grid Settings")]
    [Tooltip("Change grid name to change grid properties")]
    public string gridName;

    [Header("Pahfinding Settings")]
    [Tooltip("Add settings to your liking, useful for faster testing. Can also be changed via arrow keys")]
    
    // Public properties useful for testing
    public algorithmEnum activeAlgorithm;

    //=======================================================================


    // AUXILIARY VARIABLES ==================================================

    // Grid configuration
    public static int width;
    public static int height;
    public static float cellSize;

    // Visual grid
    private VisualGridManager visualGrid;
    private string[,] textLines;
    private bool visualGridCreated;

    // Fields for internal use only
    public static int startingX = -1;
    public static int startingY = -1;
    public static int goalX = -1;
    public static int goalY = -1;

    // Pahfinding algorithms
    public AStarPathfinding pathfinding { get; set; }

    public enum algorithmEnum{
    AStarZeroHeuristic,
    AStarEuclideanHeuristic,
    AStarClosedDictionary,
    AStarPriorityHeap,
    NodeArrayAStar,
    GoalBoundNodeArrayAStar
    };

    // Final path / solution
    List<NodeRecord> solution;

    // For goal bound pathfinding
    public bool usePreviousMapPreprocessedData;
    private bool boundingBoxesOn = false;
    private int algorithmIndex;
    private bool mapPreprocessingDone = false;
    private Dictionary<Vector2,Dictionary<string, Vector4>> storedGoalBounds;

    //=======================================================================


    private void Start()
    {
        // Finding reference of Visual Grid Manager
        visualGrid = GameObject.FindObjectOfType<VisualGridManager>();

        // Creating the path for the grid and generating it
        #if UNITY_EDITOR
            string gridPath = "Assets/Resources/Grid/" + gridName + ".txt";
        #else
            string gridPath = "Assets/Resources/Grid/" + GridSceneParameters.gridName + ".txt";
        #endif

        this.LoadGrid(gridPath);

        // By default, we start with base A*
        #if UNITY_EDITOR
            Debug.Log("Starting with algorithm " + algorithmIndex);
        #else
            algorithmIndex = 0;
        #endif

        //Initialize the chosen algorithm
        initializePathfindingAlgorithm();

        // Finish generating the visual grid
        visualGrid.GridMapVisual(textLines, this.pathfinding.grid);
        pathfinding.grid.OnGridValueChanged += visualGrid.Grid_OnGridValueChange;


    }

    void Update()
    {

        // Input Handler: deals with all keyboard inputs
        InputHandler();


        // If we're using the goal bound pathfinding, draw the bounding boxes
        if(activeAlgorithm == algorithmEnum.GoalBoundNodeArrayAStar)
        {
            drawBoundingBoxes();
        }


        // Tell the pathfinding algorithm to keep searching
        if (this.pathfinding.InProgress)
        {
            var finished = this.pathfinding.Search(out this.solution);
            if (finished)
            {
                this.pathfinding.InProgress = false;
                this.visualGrid.DrawPath(this.solution);
            }

            this.pathfinding.TotalProcessingTime += Time.deltaTime;
        }


    }


    void InputHandler()
    {
        // Left mouse click
        if (Input.GetMouseButtonDown(0))
        {
            // Retrieving clicked position and the corresponding grid's X and Y
            Vector2 clickedGridPosition = mouseToGridPosition();

            // Getting the corresponding node 
            var node = pathfinding.grid.GetGridObject((int)clickedGridPosition.x, (int)clickedGridPosition.y);

            // If the node is valid...
            if (node != null && node.isWalkable)
            {
                // If we don't have a starting position, set it
                if (!startingPositionSet())
                {
                    startingX = (int)clickedGridPosition.x;
                    startingY = (int)clickedGridPosition.y;

                    // Color in starting node
                    this.visualGrid.SetObjectColor(startingX, startingY, Color.cyan);
                }

                // If we have a starting position but don't have a goal position, set it
                else if (!goalPositionSet())
                {
                    goalX = (int)clickedGridPosition.x;
                    goalY = (int)clickedGridPosition.y;

                    // We can now start the search
                    InitializeSearch(startingX, startingY, goalX, goalY);
                }

                // If we press the left mouse button while the algorithm is in progress or after clearing the grid, set another starting position
                else
                {
                    // Clearing visual grid
                    this.visualGrid.ClearGrid();

                    // Resetting goal
                    goalY = -1;
                    goalX = -1;
                    
                    // Getting new start position
                    startingX = (int)clickedGridPosition.x;
                    startingY = (int)clickedGridPosition.y;

                    // Color in starting node
                    this.visualGrid.SetObjectColor(startingX, startingY, Color.cyan);
                }
            }
        }


        // Space clears the grid
        if (Input.GetKeyDown(KeyCode.Space))
        {
            startingX = -1;
            startingY = -1;
            goalX = -1;
            goalY = -1;
            this.visualGrid.ClearGrid();
        }

        // Arrow keys make it possible to change the algorithm
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            algorithmIndex--;
            if(algorithmIndex <= -1) algorithmIndex = 5;
            activeAlgorithm = (algorithmEnum)algorithmIndex;
            updateSearchAlgorithm();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            algorithmIndex++;
            if(algorithmIndex >= 6) algorithmIndex = 0;
            activeAlgorithm = (algorithmEnum)algorithmIndex;
            updateSearchAlgorithm();
        }

        // Pressing 1-5 keys will use default positions for the pathfinding (if available)
        int index = 0;
        if (Input.GetKeyDown(KeyCode.Alpha1))
            index = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            index = 2;
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            index = 3;


        if (index != 0)
        {
            visualGrid.ClearGrid();
            var positions = defaultPositions.Find(x => x.gridName == this.gridName).searchPos;

            if (index - 1 <= positions.Count && index - 1 >= 0)
            {
                var actualPositions = positions[index - 1];

                startingX = (int)actualPositions.startingPos.x;
                startingY = (int)actualPositions.startingPos.y;

                // Getting the corresponding Node 
                var node = pathfinding.grid.GetGridObject(startingX, startingY);
                if (node != null && node.isWalkable)
                {
                    goalX = (int)actualPositions.goalPos.x;
                    goalY = (int)actualPositions.goalPos.y;
                    node = pathfinding.grid.GetGridObject(goalX, goalY);

                    if (node != null && node.isWalkable)
                    {
                        // Draw the bounding boxes in case of goal bound 
                        if(activeAlgorithm == algorithmEnum.GoalBoundNodeArrayAStar)
                        {
                            visualGrid.fillBoundingBox(this.pathfinding.grid.GetGridObject(startingX, startingY));
                            boundingBoxesOn = true;
                        }

                        InitializeSearch(startingX, startingY, goalX, goalY);
                    }
                }
            }
        }

    }


    public void InitializeSearch(int _startingX, int _startingY, int _goalX, int _goalY)
    {
        this.visualGrid.SetObjectColor(startingX, startingY, Color.cyan);
        this.visualGrid.SetObjectColor(goalX, goalY, Color.green);
        this.pathfinding.InitializePathfindingSearch(_startingX, _startingY, _goalX, _goalY);

    }

    // Reads the text file that where the grid "definition" is stored 
    public void LoadGrid(string gridPath)
    {

        // Read the text directly from the test.txt file
        StreamReader reader = new StreamReader(gridPath);
        var fileContent = reader.ReadToEnd();
        reader.Close();
        var lines = fileContent.Split("\n"[0]);

        // Calculating Height and Width from text file
        height = lines.Length;
        width = lines[0].Length - 1;

        // Cellsize formula 
        cellSize = 700.0f / (width + 2);
      
        textLines = new string[height, width];
        int i = 0;
        foreach (var l in lines)
        {
            var words = l.Split();
            var j = 0;

            var w = words[0];

            foreach (var letter in w)
            {
                textLines[i, j] = letter.ToString();
                j++;

                if (j == textLines.GetLength(1))
                    break;
            }

            i++;
            if (i == textLines.GetLength(0))
                break;
        }

    }

    public void updateSearchAlgorithm()
    {
        initializePathfindingAlgorithm();

        // Reset visual grid to account for new pathfinding
        visualGrid.DestroyGrid();

        // Finish generating the visual grid
        visualGrid.GridMapVisual(textLines, this.pathfinding.grid);
        pathfinding.grid.OnGridValueChanged += visualGrid.Grid_OnGridValueChange;
    }

    public void initializePathfindingAlgorithm()
    {
        if (activeAlgorithm == algorithmEnum.AStarZeroHeuristic)
            this.pathfinding = new AStarPathfinding(new SimpleUnorderedNodeList(), new SimpleUnorderedNodeList(), new ZeroHeuristic());
        else if (activeAlgorithm == algorithmEnum.AStarEuclideanHeuristic)
            this.pathfinding = new AStarPathfinding(new SimpleUnorderedNodeList(), new SimpleUnorderedNodeList(), new EuclideanDistance());
        else if (activeAlgorithm == algorithmEnum.AStarClosedDictionary)
            this.pathfinding = new AStarPathfinding(new SimpleUnorderedNodeList(), new ClosedDictionary(), new EuclideanDistance());
        else if (activeAlgorithm == algorithmEnum.AStarPriorityHeap)
            this.pathfinding = new AStarPathfinding(new NodePriorityHeap(), new ClosedDictionary(), new EuclideanDistance());
        else if (activeAlgorithm == algorithmEnum.NodeArrayAStar)
            this.pathfinding = new NodeArrayAStarPathfinding(new EuclideanDistance());
        else if (activeAlgorithm == algorithmEnum.GoalBoundNodeArrayAStar)
            this.pathfinding = new GoalBoundNodeArrayAStarPathfinding(new EuclideanDistance());

        // Keep track of algorithm index for easier selection
        algorithmIndex = (int)activeAlgorithm;
        
        // When using goal bound pathfinding, we need to preprocess the map before proceeding
        if (activeAlgorithm == algorithmEnum.GoalBoundNodeArrayAStar)
        {
            HandleGoalBoundMapPreprocessing();
        }

    }

    private bool startingPositionSet()
    {
        return startingX != -1;
    }

    private bool goalPositionSet()
    {
        return goalX != -1;
    }

    private Vector2 mouseToGridPosition()
    {
        var clickedPosition = UtilsClass.GetMouseWorldPosition();
        int positionX, positionY = 0;
        this.pathfinding.grid.GetXY(clickedPosition, out positionX, out positionY);

        return new Vector2(positionX, positionY);
        
    }

    private void drawBoundingBoxes()
    {
        if(startingPositionSet() && !goalPositionSet() && !boundingBoxesOn)
        {
            // Draw bounding boxes
            visualGrid.fillBoundingBox(this.pathfinding.grid.GetGridObject(startingX, startingY));
            boundingBoxesOn = true;
        }
        else if(startingPositionSet() && goalPositionSet() && boundingBoxesOn)
        {
            boundingBoxesOn = false;
        }
    }
   
    private void HandleGoalBoundMapPreprocessing()
    {
        var p = (GoalBoundNodeArrayAStarPathfinding)this.pathfinding;

        // If we haven't done the map preprocessing yet, do it now
        if(!mapPreprocessingDone)
        {
            // Saves preprocessing time if users are mainly interested in the build
            #if UNITY_EDITOR

                if(!usePreviousMapPreprocessedData)
                {
                    // Helps goal bound preprocessing decide what are walkable nodes and not
                    visualGrid.GridMapSimulated(textLines, this.pathfinding.grid);

                    p.MapPreprocess();

                    // Store the goal bounds for this particular map
                    storedGoalBounds = p.goalBounds;
                    RegisterMapPreprocessedData();

                }
                else
                {
                    // Use previously obtained map preprocessing data
                    ReadMapPreprocessedData();
                    p.goalBounds = storedGoalBounds;
                }

            #else
                // Use previously obtained map preprocessing data
                ReadMapPreprocessedData();
                p.goalBounds = storedGoalBounds;
            #endif

            mapPreprocessingDone = true;
        }

        // If we have done the map preprocessing before, retrieve the bounding boxes
        else
        {
            p.goalBounds = storedGoalBounds;
        }

    }

    private void RegisterMapPreprocessedData()
    {
        string[] directions = { "up", "down", "left", "right"};
        string path = Path.Combine(Application.dataPath, "Resources/Grid", GridSceneParameters.gridName + "PreprocessedData.txt");

        // Go through all the nodes
        for (int i = 0; i < this.pathfinding.grid.getHeight(); i++)
        {
 
            for (int j = 0; j < this.pathfinding.grid.getWidth(); j++)
            {
                NodeRecord currentNode = this.pathfinding.grid.GetGridObject(j, i);

                if(currentNode.isWalkable)
                {
                    var currentNodeKey = new Vector2(currentNode.x, currentNode.y);

                    // Write to .txt file the obtained bound boxes
                    foreach(string direction in directions)
                    {
                        string content = $"{currentNode.x}:{currentNode.y}:{storedGoalBounds[currentNodeKey][direction].x}:{storedGoalBounds[currentNodeKey][direction].y}:{storedGoalBounds[currentNodeKey][direction].z}:{storedGoalBounds[currentNodeKey][direction].w}";        
                        File.AppendAllText(path, content + Environment.NewLine);
                    }
                }
            }
        }

        Debug.Log("Preprocessed data registered successfully.");
    }

    private void ReadMapPreprocessedData()
    {
        string[] directions = { "up", "down", "left", "right"};
        string path;

        #if UNITY_EDITOR
            path = Path.Combine(Application.dataPath, "Resources/Grid/" + gridName + "PreprocessedData.txt");
        #else
            path = Path.Combine(Application.dataPath, "Resources/Grid", GridSceneParameters.gridName + "PreprocessedData.txt");
        #endif

        if (!File.Exists(path))
        {
            Debug.LogError("Preprocessed data file not found: " + path);
            return;
        }

        // Initialize the storedGoalBounds dictionary if not already initialized
        if (this.storedGoalBounds == null)
        {
            this.storedGoalBounds = new Dictionary<Vector2, Dictionary<string, Vector4>>();
        }

        // Read all lines from the file containing the bound boxes
        string[] lines = File.ReadAllLines(path);

        int directionIndex = 0;

        foreach (string line in lines)
        {
            // Split the line by colon to extract the data
            string[] parts = line.Split(':');

            int x = int.Parse(parts[0]);
            int y = int.Parse(parts[1]);
            int boundMinX = int.Parse(parts[2]);
            int boundMaxX = int.Parse(parts[3]);
            int boundMinY = int.Parse(parts[4]);
            int boundMaxY = int.Parse(parts[5]);

            Vector2 currentNodeKey = new Vector2(x, y);

            if (!this.storedGoalBounds.ContainsKey(currentNodeKey))
            {
                this.storedGoalBounds[currentNodeKey] = new Dictionary<string, Vector4>();
            }

            switch(directionIndex)
            {
                case 0: 
                    this.storedGoalBounds[currentNodeKey]["up"] = new Vector4(boundMinX, boundMaxX, boundMinY, boundMaxY);
                    directionIndex++;
                    break;

                case 1: 
                    this.storedGoalBounds[currentNodeKey]["down"] = new Vector4(boundMinX, boundMaxX, boundMinY, boundMaxY);
                    directionIndex++;
                    break;

                case 2: 
                    this.storedGoalBounds[currentNodeKey]["left"] = new Vector4(boundMinX, boundMaxX, boundMinY, boundMaxY);
                    directionIndex++;
                    break;

                case 3: 
                    this.storedGoalBounds[currentNodeKey]["right"] = new Vector4(boundMinX, boundMaxX, boundMinY, boundMaxY);
                    directionIndex = 0;
                    break;
            }
        }

        Debug.Log("Preprocessed data read successfully.");
    }

}