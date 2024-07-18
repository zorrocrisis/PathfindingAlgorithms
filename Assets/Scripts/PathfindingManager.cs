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

    //Struct for default positions
    [Serializable]
    public struct SearchPos
    {
        public Vector2 startingPos;
        public Vector2 goalPos;
    }

    //Struct to store default positions by grid
    [Serializable]
    public struct SearchPosPerGrid
    {
        public string gridName;
        public List<SearchPos> searchPos;
    }

    // "Default Positions are quite useful for testing"
    public List<SearchPosPerGrid> defaultPositions;

    [Header("Grid Settings")]
    [Tooltip("Change grid name to change grid properties")]
    public string gridName;

    public enum algorithmEnum{
    AStarZeroHeuristic,
    AStarEuclideanHeuristic,
    AStarClosedDictionary,
    AStarPriorityHeap,
    NodeArrayAStar,
    GoalBoundAStar
    };

    [Header("Pahfinding Settings - only select one")]
    [Tooltip("Add settings to your liking, useful for faster testing")]
    //public properties useful for testing, you can add other booleans here such as which heuristic to use
    public algorithmEnum activeAlgorithm;
    private int algorithmIndex;
    public bool partialPath;
    public bool useGoalBound;
   
    //Grid configuration
    public static int width;
    public static int height;
    public static float cellSize;
 
    //Essential Pathfind classes 
    public AStarPathfinding pathfinding { get; set; }

    //The Visual Grid
    private VisualGridManager visualGrid;
    private string[,] textLines;

    //Private fields for internal use only
    public static int startingX = -1;
    public static int startingY = -1;
    public static int goalX = -1;
    public static int goalY = -1;

    //Path
    List<NodeRecord> solution;

    // For goal bound pathfinding
    private bool boundingBoxesOn = false;

    private void Start()
    {
        // Finding reference of Visual Grid Manager
        visualGrid = GameObject.FindObjectOfType<VisualGridManager>();

        // Creating the Path for the Grid and Creating it
        var gridPath = "Assets/Resources/Grid/" + gridName + ".txt";
        this.LoadGrid(gridPath);

        //Initializing the chosen algorithm from the inspector window of the manager
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

        algorithmIndex = (int) activeAlgorithm;

        visualGrid.GridMapVisual(textLines, this.pathfinding.grid);

        
        if (this.pathfinding is GoalBoundAStarPathfinding p)
        {
            //p.MapPreprocess();

            p.InitializePrecomputation(1, 2);
            this.pathfinding.goalBound = useGoalBound;
            this.pathfinding.goalBoundPath = (GoalBoundAStarPathfinding)this.pathfinding;
        }
        

        pathfinding.grid.OnGridValueChanged += visualGrid.Grid_OnGridValueChange;

    }

    // Update is called once per frame
    void Update()
    {
        // WHAT TO DO AFTER PREPROCES??
        if(activeAlgorithm == algorithmEnum.GoalBoundAStar)
        {
            if((startingX !=-1 || startingY != -1) && (goalX == -1 || goalY == -1) && !boundingBoxesOn)
            {
                var key = new Vector2(startingX, startingY);
                var p = (GoalBoundAStarPathfinding)this.pathfinding;
                Debug.Log(p.goalBounds[key]["up"]);
                visualGrid.fillBoundingBox(pathfinding.grid.GetGridObject(startingX, startingY));
                boundingBoxesOn = true;
            }
            else if((startingX !=-1 || startingY != -1) && (goalX != -1 || goalY != -1) && boundingBoxesOn)
            {
                boundingBoxesOn = false;
            }
        }


        // The first mouse click goes here, it defines the starting position;
        if (Input.GetMouseButtonDown(0))
        {
            //Retrieving clicked position
            var clickedPosition = UtilsClass.GetMouseWorldPosition();

            int positionX, positionY = 0;

            // Retrieving the grid's corresponding X and Y from the clicked position
            pathfinding.grid.GetXY(clickedPosition, out positionX, out positionY);

            // Getting the corresponding Node 
            var node = pathfinding.grid.GetGridObject(positionX, positionY);

            if (node != null && node.isWalkable)
            {
                // If we don't have a starting position, set it
                if (startingX == -1)
                {
                    startingX = positionX;
                    startingY = positionY;
                    this.visualGrid.SetObjectColor(startingX, startingY, Color.cyan);

                }

                // If we don't have a goal position, set it
                else if (goalX == -1)
                {
                    goalX = positionX;
                    goalY = positionY;
                    this.visualGrid.SetObjectColor(startingX, startingY, Color.cyan);
                    //We can now start the search
                    InitializeSearch(startingX, startingY, goalX, goalY);
                }

                // If we press while the algorithm is in progress or after clearing the grid, set another starting position
                else
                {
                    goalY = -1;
                    goalX = -1;
                    this.visualGrid.ClearGrid();
                    startingX = positionX;
                    startingY = positionY;
                    this.visualGrid.SetObjectColor(startingX, startingY, Color.cyan);
                }
            }
        }

        // Input Handler: deals with most keyboard inputs
        InputHandler();


        if(activeAlgorithm == algorithmEnum.GoalBoundAStar)
        {
            var p = (GoalBoundAStarPathfinding)this.pathfinding;
            var key = new Vector2(1, 2);
            
            if(p.PreComputationInProgress)
            {
                NodeRecord node = new NodeRecord(1, 2);
                p.Floodfill(node);
            }
            else if (p.goalBounds[key]["up"] != null)
            {
                Debug.Log("Up Box: " + p.goalBounds[key]["up"]);
                Debug.Log("Down Box: " + p.goalBounds[key]["down"]);
                Debug.Log("Left Box: " + p.goalBounds[key]["left"]);
                Debug.Log("Right Box: " + p.goalBounds[key]["right"]);
            }
        }

        // Make sure you tell the pathfinding algorithm to keep searching
        if (this.pathfinding.InProgress)
        {
            var finished = this.pathfinding.Search(out this.solution, partialPath);
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
        // Space clears the grid
        if (Input.GetKeyDown(KeyCode.Space))
        {
            this.visualGrid.ClearGrid();
        }

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


        // If you press 1-5 keys you pathfinding will use default positions
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

                    if (node != null && node.isWalkable)
                    {
                        if(activeAlgorithm == algorithmEnum.GoalBoundAStar)
                        {
                            visualGrid.fillBoundingBox(pathfinding.grid.GetGridObject(startingX, startingY));
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

    // Reads the text file that where the grid "definition" is stored, I don't recomend changing this ^^ 
    public void LoadGrid(string gridPath)
    {

        //Read the text from directly from the test.txt file
        StreamReader reader = new StreamReader(gridPath);
        var fileContent = reader.ReadToEnd();
        reader.Close();
        var lines = fileContent.Split("\n"[0]);

        //Calculating Height and Width from text file
        height = lines.Length;
        width = lines[0].Length - 1;

        // CellSize Formula 
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
        //Initializing the chosen algorithm from the inspector window of the manager
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

        // Reset visual grid to account for new pathfinding
        visualGrid.DestroyGrid();
        visualGrid.GridMapVisual(textLines, this.pathfinding.grid);

        // Prepare grid for updates
        this.pathfinding.grid.OnGridValueChanged += visualGrid.Grid_OnGridValueChange;
    }

}
