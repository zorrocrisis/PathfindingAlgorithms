using Assets.Scripts.Grid;
using Assets.Scripts.IAJ.Unity.Pathfinding;
using Assets.Scripts.IAJ.Unity.Pathfinding.DataStructures;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class VisualGridManager : MonoBehaviour
{
    //Pathfinding Manager reference
    private PathfindingManager manager;

    GameObject[,] visualGrid;    

    //Grid configuration
    private int width;
    private int height;
    private float cellSize;

    // Node Prefab
    [Header("Node Prefab")]
    public GameObject gridNodePrefab;

    [Header("Debug Options")]
    // Debug Option
    public bool showCoordinates;
    public Color openNodesColor;
    public Color closedNodesColor;

    [System.Serializable]
    public struct boxColor
    {
       public string direction;
       public Color color;
    }

    public List<boxColor> boundingColors;

    //Grid Reference
    private Grid<NodeRecord> grid;

    //Grid Parent
    private GameObject gridParent;

    // Start is called before the first frame update
    void Awake()
    {
        // Simple way of getting the manager's reference
        this.manager = GameObject.FindObjectOfType<PathfindingManager>();
        gridParent = new GameObject("Grid");
    }  

    // Create the grid according to the text file set in the "Assets/Resources/grid.txt"
    public void GridMapVisual(string[,] textLines, Grid<NodeRecord> _grid, bool preprocessing=false)
    {

        this.width = PathfindingManager.width;
        this.height = PathfindingManager.height;
        this.cellSize = PathfindingManager.cellSize;
        this.grid = _grid;

        visualGrid = new GameObject[width, height];

        //Creating visual grid from the text file
        for (int i = 0; i < textLines.GetLength(0); i++)
            for (int j = 0; j < textLines.GetLength(1); j++)
            {
                // We are reading the textLines from the top left till the bottom right, we need to adjust accordingly
                var x = j;
                var y = height - i - 1;
                visualGrid[x, y] = CreateGridObject(this.gridNodePrefab, this.grid.GetGridObject(x, y)?.ToString(), this.grid.GetWorldPosition(x, y) + new Vector3(cellSize, 2, cellSize) * 0.5f, 40, Color.black, Color.white);

                if (textLines[i, j] == "1")
                {
                    var node = this.grid.GetGridObject(x, y);
                    node.isWalkable = false;
                    this.SetObjectColor(x, y, Color.black);

                    // If we're doing some sort of preprocessing (like in goalbound), we probably don't have the visual grid active
                    // but we still need to detect whether nodes are walkable or not
                    if(preprocessing)
                    {
                        var node2 =_grid.GetGridObject(x, y);
                        node2.isWalkable = false;
                    }
                }
            
            }
    }

    // Create the grid according to the text file set in the "Assets/Resources/grid.txt". If we're doing some sort of preprocessing (like in goalbound),
    // we probably don't have the visual grid active but we still need to detect whether nodes are walkable or not
    public void GridMapSimulated(string[,] textLines, Grid<NodeRecord> _grid)
    {

        //Creating simulated grid from the text file
        for (int i = 0; i < textLines.GetLength(0); i++)
            for (int j = 0; j < textLines.GetLength(1); j++)
            {
                if (textLines[i, j] == "1")
                {
                    // We are reading the textLines from the top left till the bottom right, we need to adjust accordingly
                    var x = j;
                    var y = height - i - 1;
                    var node =_grid.GetGridObject(x, y);
                    node.isWalkable = false;
                }
            
            }
    }

    // Instantiating a grid object from the prefab
    private GameObject CreateGridObject(GameObject prefab, string value, Vector3 position, int fontSize, Color fontColor, Color imageColor)
    {

        var obj = GameObject.Instantiate(prefab, gridParent.transform);
        obj.name = value;
        Transform transform = obj.transform;
        transform.localScale = new Vector3(this.cellSize - 1, cellSize - 1, cellSize - 1);
        transform.localPosition = position;

        if (showCoordinates)
        {
            TextMesh text = obj.GetComponentInChildren<TextMesh>();
            text.text = value;
            text.fontSize = fontSize;
            text.color = fontColor;
        }
        SpriteRenderer s = obj.GetComponent<SpriteRenderer>();
        s.color = imageColor;
        return obj;

    }

    // Reset the Grid to black and white
    public void ClearGrid()
    {
        for (int x = 0; x < this.width; x++)
            for (int y = 0; y <this.height; y++)
            {
                if (this.grid.GetGridObject(x, y).isWalkable)
                    this.SetObjectColor(x, y, Color.white);
                else this.SetObjectColor(x, y, Color.black);
            }

        if(this.manager != null)
            this.manager.pathfinding.InProgress = false;

    }

    // Destroying all of the Grid cells
    public void DestroyGrid()
    {
        for (int x = 0; x < this.width; x++)
            for (int y = 0; y < this.height; y++)
                DestroyImmediate(visualGrid[x, y].gameObject);
                

    }


    //Setting the color of the node 
    public void SetObjectColor(int x, int y, Color color)
    {
        visualGrid[x, y].GetComponent<SpriteRenderer>().color = color;
    }


    // Update the grid's colours (manual)
    public void UpdateGrid()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                NodeRecord node = this.grid.GetGridObject(x, y);
                if (!node.isWalkable)
                    this.SetObjectColor(x, y, Color.black);
                else if(PathfindingManager.startingX == x && PathfindingManager.startingY == y)
                    this.SetObjectColor(x, y, Color.cyan);
                else if(PathfindingManager.goalX == x && PathfindingManager.goalY == y)
                    this.SetObjectColor(x, y, Color.green);
                else if (node.status == NodeStatus.Open)
                    this.SetObjectColor(x, y, Color.blue);
                else if (node.status == NodeStatus.Closed)
                {
                    this.SetObjectColor(x, y, Color.red);
                }

            }
    }

    // Update the grid's colours (automatic)
    public void Grid_OnGridValueChange(object sender, Grid<NodeRecord>.OnGridValueChangedEventArgs e)
    {
        NodeRecord node = this.grid.GetGridObject(e.x, e.y);
        if (node != null)
        {

            if (!node.isWalkable)
                this.SetObjectColor(e.x, e.y, Color.black);
            else if(PathfindingManager.startingX == e.x && PathfindingManager.startingY == e.y)
                this.SetObjectColor(e.x, e.y, Color.cyan);
            else if(PathfindingManager.goalX == e.x && PathfindingManager.goalY == e.y)
                this.SetObjectColor(e.x, e.y, Color.green);
            else if (manager.pathfinding.Open.SearchInOpen(node) != null)
                this.SetObjectColor(e.x, e.y, openNodesColor);
            else if (manager.pathfinding.Closed.SearchInClosed(node) != null)
                this.SetObjectColor(e.x, e.y, closedNodesColor);

        }
    }

    // Draw final path
    public void DrawPath(List<NodeRecord> path)
    {
        int index = 0;
        foreach (var p in path)
        {
            index += 1;
            if (index == 1)
            {
                this.SetObjectColor(p.x, p.y, Color.cyan);
                continue;
            }

            if (index == path.Count)
            {
                this.SetObjectColor(p.x, p.y, Color.green + new Color(0.5f, 0.0f, 0.5f));
                break;
            }

            this.SetObjectColor(p.x, p.y, Color.green);
        }
    }


    // Method that draws the bounding box according to the colors defined in the inspector
    public void fillBoundingBox(NodeRecord node)
    {
        var goalBoundingPathfinder = (GoalBoundAStarPathfinding)manager.pathfinding;
      
        for (int x = 0; x < this.width; x++)
            for (int y = 0; y < this.height; y++)
            {
                var currentNode = grid.GetGridObject(x, y);
                if (currentNode != node && currentNode.isWalkable)
                {
                    foreach (var c in boundingColors)
                        if (goalBoundingPathfinder.InsideGoalBoundBox(node.x, node.y, x, y, c.direction))
                            this.SetObjectColor(x, y, c.color);
                }
            }   
    }
}
