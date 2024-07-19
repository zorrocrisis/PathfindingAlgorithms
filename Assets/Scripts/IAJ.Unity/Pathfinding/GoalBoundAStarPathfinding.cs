using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Scripts.Grid;
using Assets.Scripts.IAJ.Unity.Pathfinding.DataStructures;
using Assets.Scripts.IAJ.Unity.Pathfinding.Heuristics;
using UnityEngine;

namespace Assets.Scripts.IAJ.Unity.Pathfinding
{
    public class GoalBoundAStarPathfinding : AStarPathfinding
    {
        // Auxiliary variables for precomputation process
        public bool PreComputationInProgress { get; set; }
        public IHeuristic PreprocessingHeuristic { get; protected set; }

        // Goal Bounding Box for each node's edge - bounding limits: minX, maxX, minY, maxY
        public Dictionary<Vector2,Dictionary<string, Vector4>> goalBounds;

        // In this case, we consider only 4 bound boxes
        private string[] directions = { "up", "down", "left", "right"};
        private Dictionary<BestGoalBoundEdge, string> directionsToString = new Dictionary<BestGoalBoundEdge, string>
        {
            { BestGoalBoundEdge.Up, "up" },
            { BestGoalBoundEdge.Down, "down" },
            { BestGoalBoundEdge.Left, "left" },
            { BestGoalBoundEdge.Right, "right" },
            { BestGoalBoundEdge.None, "nones" }
        };


        public GoalBoundAStarPathfinding(IOpenSet open, IClosedSet closed, IHeuristic heuristic) : base(open, closed, heuristic)
        {
            grid = new Grid<NodeRecord>((Grid<NodeRecord> global, int x, int y) => new NodeRecord(x, y));
            this.Open = open;
            this.Closed = closed;
            this.InProgress = false;
            this.PreComputationInProgress = false;
            this.Heuristic = heuristic;
            this.PreprocessingHeuristic = new ZeroHeuristic(); // do not change
            this.NodesPerSearch = 100;
            this.goalBounds = new Dictionary<Vector2, Dictionary<string, Vector4>>();

        }

        // Similar to InitializePathfindingSearch of A* but without a goal node
        public virtual void InitializePrecomputation(int startX, int startY)
        {
            // Define important pathfinding nodes
            this.StartPositionX = startX;
            this.StartPositionY = startY;
            this.StartNode = grid.GetGridObject(StartPositionX, StartPositionY);

            // If it is not possible to quantize the starting node, then we cannot proceed
            if (this.StartNode == null) return;

            // Reset debug and auxiliary variables here
            this.TotalProcessedNodes = 0;
            this.TotalProcessingTime = 0.0f;
            this.MaxOpenNodes = 0;

            // Define initial node 
            var initialNode = new NodeRecord(startX, startY)
            {
                gCost = 0,
                hCost = 0,
                index = StartNode.index
            };

            // Get ready for map preprocessing 
            initialNode.CalculateFCost();
            this.Open.Initialize();
            this.Open.AddToOpen(initialNode);
            this.Closed.Initialize();

        }

        public void MapPreprocess()
        {
            // Ensure to use the zero heuristic during the preprocessing
            IHeuristic heuristicPlaceholder = this.Heuristic;
            this.Heuristic = this.PreprocessingHeuristic;

            this.PreComputationInProgress = true;

            // Go through all the nodes
            for (int i = 0; i < this.grid.getHeight(); i++)
            {
                for (int j = 0; j < this.grid.getWidth(); j++)
                {
                    
                    // Register new goal bounds entries for this node
                    NodeRecord currentNode = grid.GetGridObject(j, i);
                    var nodeKey = new Vector2(j, i);
                    var auxiliaryDict = new Dictionary<string, Vector4>();
                    this.goalBounds.Add(nodeKey, auxiliaryDict);

                    // Populate new goal bounds with initial values
                    foreach(string direction in directions)
                    {
                        switch (direction)
                        {
                            case "up":
                                this.goalBounds[nodeKey]["up"] = new Vector4(currentNode.x, currentNode.x, currentNode.y + 1, currentNode.y);
                                break;
                            case "down":
                                this.goalBounds[nodeKey]["down"] = new Vector4(currentNode.x, currentNode.x, currentNode.y, currentNode.y - 1);
                                break;
                            case "left":
                                this.goalBounds[nodeKey]["left"] = new Vector4(currentNode.x, currentNode.x - 1, currentNode.y, currentNode.y);
                                break;
                            case "right":
                                this.goalBounds[nodeKey]["right"] = new Vector4(currentNode.x + 1, currentNode.x, currentNode.y, currentNode.y);
                                break;
                        }
                    }

                    // Ensure to start in a clean slate and prepare for floodfilling process
                    ResetMapPreprocess();
                    InitializePrecomputation(currentNode.x, currentNode.y);

                    // Stop only once we have floodfilled the entire map
                    var finishedFloodfill = false;
                    while (!finishedFloodfill)
                    {
                        finishedFloodfill = Floodfill(currentNode);
                    }
                }
            }

            // Return to the desired heuristic
            this.Heuristic = heuristicPlaceholder;

            this.PreComputationInProgress = false;
        }

        // Very similar to the A star base algorithm, but without a goal node
        public bool Floodfill(NodeRecord initialNode)
        {

            var key = new Vector2(initialNode.x, initialNode.y);

            uint ProcessedNodes = 0;
            int OpenNodes = 0; 
            NodeRecord currentNode;


            while (ProcessedNodes <= NodesPerSearch)
            {   

                if(Open.CountOpen() == 0)
                {
                    // If there aren't any more open nodes to explore, we have successfully floodfilled the map
                    return true;
                }

                // CurrentNode is the best one from the Open set, start with that
                currentNode = Open.GetBestAndRemove();
                Closed.AddToClosed(currentNode);

                // If we don't update the grid value here also, some nodes (like the ones in the corners) will not be updated visually
                // because they aren't neighbours to any other opens nodes and thus are not processed through the "ProcessChildNode"
                grid.SetGridObject(currentNode.x, currentNode.y, currentNode);

                // Handle the neighbours/children
                foreach (var neighbourNode in GetNeighbourList(currentNode))
                {

                    // During the floodfill, we will process all the nodes
                    this.ProcessChildNode(currentNode, neighbourNode);

                    // If the current node is the initial one...
                    if(currentNode.Equals(initialNode))
                    {
                       PopulateStartingNeighboursWithBestEdge(currentNode, neighbourNode);
                    }

                    // If the current node is not the initial one and the neighbours do not have a best goal bound edge assigned...
                    else if(neighbourNode.bestGoalBoundEdge == BestGoalBoundEdge.None) 
                    {
                        // Propagate the best goal bound edge to the neighbours
                        var bestEdge = currentNode.bestGoalBoundEdge;
                        neighbourNode.bestGoalBoundEdge = bestEdge;

                        if (directionsToString.TryGetValue(bestEdge, out var direction))
                        {

                            // Update bounding box limits
                            var bound = goalBounds[key][direction];
                            var updatedBound = new Vector4
                            (
                                Math.Min(bound.x, neighbourNode.x),
                                Math.Max(bound.y, neighbourNode.x),
                                Math.Min(bound.z, neighbourNode.y),
                                Math.Max(bound.w, neighbourNode.y)
                            );

                            goalBounds[key][direction] = updatedBound;
                        }
                    }
                }
            } 

            return false;
        }

        
        // Again very similar to the A star algorithm, now with a goal node
        public override bool Search(out List<NodeRecord> solution) {

            uint ProcessedNodes = 0;
            int OpenNodes = 0; 
            NodeRecord currentNode;
            
            // While Open is not empty or if nodes havent been all processed, we continue to search (also controlled from the pahtfinding manager) 
            while (ProcessedNodes <= NodesPerSearch)
            {
                if (Open.CountOpen() == 0)
                {
                    solution = null;
                    return true;
                }

                // CurrentNode is the best one from the Open set, start with that
                currentNode = Open.GetBestAndRemove();

                // Check if current node is the goal
                if (currentNode.Equals(GoalNode))
                {
                    solution = CalculatePath(currentNode);
                    InProgress = false;
                    return true;
                }

                // Add current node to closed set
                Closed.AddToClosed(currentNode);

                // If we don't update the grid value here also, some nodes (like the ones in the corners) will not be updated visually
                // because they aren't neighbours to any other opens nodes and thus are not processed through the "ProcessChildNode"
                grid.SetGridObject(currentNode.x, currentNode.y, currentNode);

                // Handle the neighbours/children of current node
                foreach (var neighbourNode in GetNeighbourList(currentNode))
                {
                    // We will only process a neighbouring node if the goal is within the starting node's bounding box
                    // to which the neighbouring node also belongs to
                    if (GoalBoundBothInside(StartNode.x, StartNode.y, neighbourNode.x, neighbourNode.y, GoalNode.x, GoalNode.y) && neighbourNode.isWalkable)
                    {
                        this.ProcessChildNode(currentNode, neighbourNode);

                        //Keeps the maximum size that the open list had (debugging purposes)
                        OpenNodes = Open.CountOpen();
                        if (OpenNodes > this.MaxOpenNodes)
                            this.MaxOpenNodes = OpenNodes;

                        //Increment de processed nodes (debugging purposes)
                        ProcessedNodes += 1;
                    }
                }

            }

            //Keep track of processed nodes
            this.TotalProcessedNodes += ProcessedNodes;

            solution = null;
            return false;
        
        }


        public bool GoalBoundBothInside(int startX, int startY, int genericX, int genericY, int goalX, int goalY)
        {
            var key = new Vector2(startX, startY);
            
            foreach(string direction in directions)
            {   
                var box = this.goalBounds[key][direction];
                bool genericNodeInsideBox = genericX >= box.x && genericX <= box.y && genericY >= box.z && genericY <= box.w;
                bool goalNodeInsideBox = goalX >= box.x && goalX <= box.y && goalY >= box.z && goalY <= box.w;

                if(genericNodeInsideBox && goalNodeInsideBox)
                {
                    return true;
                }
            }

            return false;
        }

        // Checks if node(x,y) is in the node(startx, starty) bounding box for the direction: direction -> useful for visual grid
        public bool InsideGoalBoundBox(int startX, int startY, int x, int y, string direction)
        {

            var key = new Vector2(startX, startY);
            
            if (!this.goalBounds.ContainsKey(key))
                return false;

            if (!this.goalBounds[key].ContainsKey(direction))
                return false;

            var box = this.goalBounds[key][direction];
            
            if (x >= box.x && x <= box.y && y >= box.z && y <= box.w)
                return true;

            return false;
        }

  
        public void ResetMapPreprocess()
        {
            for (int i = 0; i < this.grid.getHeight(); i++)
            {
                for (int j = 0; j < this.grid.getWidth(); j++)
                {
                    NodeRecord currentNode = grid.GetGridObject(j, i);
                    currentNode.Reset();
                }
            }
        }

        private void PopulateStartingNeighboursWithBestEdge(NodeRecord initialNode, NodeRecord neighbourNode)
        {
            var key = new Vector2(initialNode.x, initialNode.y);

            // We have to define the best edges for the initial neighbours
            if(neighbourNode.y > initialNode.y)
            {
                neighbourNode.bestGoalBoundEdge = BestGoalBoundEdge.Up;
                Vector4 updatedBound = new Vector4
                (
                    Math.Min(goalBounds[key]["up"].x, neighbourNode.x),
                    Math.Max(goalBounds[key]["up"].y, neighbourNode.x),
                    Math.Min(goalBounds[key]["up"].z, neighbourNode.y),
                    Math.Max(goalBounds[key]["up"].w, neighbourNode.y)
                );

                goalBounds[key]["up"] = updatedBound;
            }
            else if(neighbourNode.y < initialNode.y)
            {
                neighbourNode.bestGoalBoundEdge = BestGoalBoundEdge.Down;
                Vector4 updatedBound = new Vector4
                (
                    Math.Min(goalBounds[key]["down"].x, neighbourNode.x),
                    Math.Max(goalBounds[key]["down"].y, neighbourNode.x),
                    Math.Min(goalBounds[key]["down"].z, neighbourNode.y),
                    Math.Max(goalBounds[key]["down"].w, neighbourNode.y)
                );

                goalBounds[key]["down"] = updatedBound;
            }
            else
            {
                if(neighbourNode.x > initialNode.x)
                {
                    neighbourNode.bestGoalBoundEdge = BestGoalBoundEdge.Right;
                    Vector4 updatedBound = new Vector4
                    (
                        Math.Min(goalBounds[key]["right"].x, neighbourNode.x),
                        Math.Max(goalBounds[key]["right"].y, neighbourNode.x),
                        Math.Min(goalBounds[key]["right"].z, neighbourNode.y),
                        Math.Max(goalBounds[key]["right"].w, neighbourNode.y)
                    );

                    goalBounds[key]["right"] = updatedBound;
                }
                else
                {
                    neighbourNode.bestGoalBoundEdge = BestGoalBoundEdge.Left;
                    Vector4 updatedBound = new Vector4
                    (
                        Math.Min(goalBounds[key]["left"].x, neighbourNode.x),
                        Math.Max(goalBounds[key]["left"].y, neighbourNode.x),
                        Math.Min(goalBounds[key]["left"].z, neighbourNode.y),
                        Math.Max(goalBounds[key]["left"].w, neighbourNode.y)
                    );

                    goalBounds[key]["left"] = updatedBound;
                }
            }

        }
    
    }

}
