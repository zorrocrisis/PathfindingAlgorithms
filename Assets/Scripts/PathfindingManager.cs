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
    GoalBoundAStar
    };

    // Final path / solution
    List<NodeRecord> solution;

    // For goal bound pathfinding
    private bool boundingBoxesOn = false;
    private int algorithmIndex;
    private bool mapPreprocessingDone = false;
    public Dictionary<Vector2,Dictionary<string, Vector4>> goalBounds;

    //=======================================================================


    private void Start()
    {
        // Finding reference of Visual Grid Manager
        visualGrid = GameObject.FindObjectOfType<VisualGridManager>();

        // Creating the path for the grid and generating it
        var gridPath = "Assets/Resources/Grid/" + gridName + ".txt";
        this.LoadGrid(gridPath);

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
        if(activeAlgorithm == algorithmEnum.GoalBoundAStar)
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
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            index = 4;
        else if (Input.GetKeyDown(KeyCode.Alpha5))
            index = 5;


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

                    Debug.Log(node);

                    if (node != null && node.isWalkable)
                    {
                        // Draw the bounding boxes in case of goal bound 
                        if(activeAlgorithm == algorithmEnum.GoalBoundAStar)
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
        else if (activeAlgorithm == algorithmEnum.GoalBoundAStar)
            this.pathfinding = new GoalBoundAStarPathfinding(new SimpleUnorderedNodeList(), new SimpleUnorderedNodeList(), new ZeroHeuristic());

        // Keep track of algorithm index for easier selection
        algorithmIndex = (int) activeAlgorithm;
        
        // When using goal bound pathfinding, we need to preprocess the map before proceeding
        if (activeAlgorithm == algorithmEnum.GoalBoundAStar)
        {
            var p = (GoalBoundAStarPathfinding)this.pathfinding;

            if(!mapPreprocessingDone)
            {
                p.MapPreprocess();
                mapPreprocessingDone = true;

                // Store the goal bounds for this particular map
                goalBounds = p.goalBounds;
            }

            // If we have done the map preprocessing before, retrieve the bounding boxes
            else
            {
                p.goalBounds = goalBounds;
            }
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

}
