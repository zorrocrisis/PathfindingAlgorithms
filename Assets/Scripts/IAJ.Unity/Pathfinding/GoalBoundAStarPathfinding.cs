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
        // Cost of moving through the grid
        public bool PreComputationInProgress { get; set; }

        // Goal Bounding Box for each Node  direction - Bounding limits: minX, maxX, minY, maxY
        public Dictionary<Vector2,Dictionary<string, Vector4>> goalBounds;

        public NodeRecord CurrentPreprocessingNode;

        public GoalBoundAStarPathfinding(IOpenSet open, IClosedSet closed, IHeuristic heuristic) : base(open, closed, heuristic)
        {
            grid = new Grid<NodeRecord>((Grid<NodeRecord> global, int x, int y) => new NodeRecord(x, y));
            this.Open = open;
            this.Closed = closed;
            this.InProgress = false;
            this.PreComputationInProgress = false;
            this.Heuristic = heuristic;
            this.NodesPerSearch = 100; //by default we process all nodes in a single request, but we changed this
            this.goalBounds = new Dictionary<Vector2, Dictionary<string, Vector4>>();

        }

        public virtual void InitializePrecomputation(int startX, int startY)
        {
            
            this.StartPositionX = startX;
            this.StartPositionY = startY;
            this.StartNode = grid.GetGridObject(StartPositionX, StartPositionY);
            this.CurrentPreprocessingNode = this.StartNode;

            //if it is not possible to quantize the positions and find the corresponding nodes, then we cannot proceed
            if (this.StartNode == null) return;

            // Reset debug and relevant variables here
            this.TotalProcessedNodes = 0;
            this.TotalProcessingTime = 0.0f;
            this.MaxOpenNodes = 0;

            //Starting with the first node
            var initialNode = new NodeRecord(StartNode.x, StartNode.y)
            {
                gCost = 0,
                hCost = 0,
                index = StartNode.index
            };

            initialNode.CalculateFCost();
            this.Open.Initialize();
            this.Open.AddToOpen(initialNode);
            this.Closed.Initialize();
            this.PreComputationInProgress = true;

        }

        public void MapPreprocess()
        {
            string[] directions = { "up", "down", "left", "right"};
           
            for (int i = 0; i < this.grid.getHeight(); i++)
            {
                for (int j = 0; j < this.grid.getWidth(); j++)
                {
                    
                    NodeRecord currentNode = grid.GetGridObject(j, i);
                    var nodeKey = new Vector2(j, i);
                    this.CurrentPreprocessingNode = currentNode;
                    var auxiliaryDict = new Dictionary<string, Vector4>();
                    this.goalBounds.Add(nodeKey, auxiliaryDict);

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

                    ResetMapPreprocess();
                    InitializePrecomputation(currentNode.x, currentNode.y);

                    var finished = false;

                    while (!finished)
                    {
                        finished = Floodfill(currentNode);
                    }

                }
            }

            this.PreComputationInProgress = false;
            
        }

        public bool Floodfill(NodeRecord initialNode)
        {

            var key = new Vector2(initialNode.x, initialNode.y);

            uint ProcessedNodes = 0;
            int OpenNodes = 0; 
            NodeRecord CurrentNode;

            //While Open is not empty or if nodes havent been all processed 
            while (ProcessedNodes <= 100)
            {   

                if(Open.CountOpen() == 0)
                {
                    //this.PreComputationInProgress = false;
                    return true;
                }

                // CurrentNode is the best one from the Open set, start with that
                CurrentNode = Open.GetBestAndRemove();
                Closed.AddToClosed(CurrentNode);

                //If we don't update the grid value here also, some nodes (like the ones in the corners) will not be updated visually
                //because they aren't neighbours to any other opens nodes and thus are not processed through the "ProcessChildNode"
                grid.SetGridObject(CurrentNode.x, CurrentNode.y, CurrentNode);

                //Handle the neighbours/children with something like this
                foreach (var neighbourNode in GetNeighbourList(CurrentNode))
                {

                    this.ProcessChildNode(CurrentNode, neighbourNode);
                    ProcessedNodes++;

                    //Keeps the maximum size that the open list had
                    OpenNodes = this.Open.CountOpen();
                        if (OpenNodes > this.MaxOpenNodes)
                            this.MaxOpenNodes = OpenNodes;

                    if(CurrentNode.Equals(initialNode))
                    {
                       if(neighbourNode.y > initialNode.y)
                       {
                            neighbourNode.bestGoalBoundEdge = BestGoalBoundEdge.Up;
                            Vector4 v = new Vector4(Math.Min(goalBounds[key]["up"].x, neighbourNode.x), Math.Max(goalBounds[key]["up"].y, neighbourNode.x), Math.Min(goalBounds[key]["up"].z, neighbourNode.y), Math.Max(goalBounds[key]["up"].w, neighbourNode.y));
                            goalBounds[key]["up"] = v;
                       }
                       else if(neighbourNode.y < initialNode.y)
                       {
                            neighbourNode.bestGoalBoundEdge = BestGoalBoundEdge.Down;
                            Vector4 v = new Vector4(Math.Min(goalBounds[key]["down"].x, neighbourNode.x), Math.Max(goalBounds[key]["down"].y, neighbourNode.x), Math.Min(goalBounds[key]["down"].z, neighbourNode.y), Math.Max(goalBounds[key]["down"].w, neighbourNode.y));
                            goalBounds[key]["down"] = v;
                       }
                       else
                       {
                            if(neighbourNode.x > initialNode.x)
                            {
                                neighbourNode.bestGoalBoundEdge = BestGoalBoundEdge.Right;
                                Vector4 v = new Vector4(Math.Min(goalBounds[key]["right"].x, neighbourNode.x), Math.Max(goalBounds[key]["right"].y, neighbourNode.x), Math.Min(goalBounds[key]["right"].z, neighbourNode.y), Math.Max(goalBounds[key]["right"].w, neighbourNode.y));
                                goalBounds[key]["right"] = v;
                            }
                            else
                            {
                                neighbourNode.bestGoalBoundEdge = BestGoalBoundEdge.Left;
                                Vector4 v = new Vector4(Math.Min(goalBounds[key]["left"].x, neighbourNode.x), Math.Max(goalBounds[key]["left"].y, neighbourNode.x), Math.Min(goalBounds[key]["left"].z, neighbourNode.y), Math.Max(goalBounds[key]["left"].w, neighbourNode.y));
                                goalBounds[key]["left"] = v;
                            }
                       }

                       Debug.Log("X " + neighbourNode.x + ":" + neighbourNode.y);
                       Debug.Log("Best Edge " + neighbourNode.bestGoalBoundEdge);
                    }
                    else if(neighbourNode.bestGoalBoundEdge == BestGoalBoundEdge.None) 
                    {
                        var bestEdge = CurrentNode.bestGoalBoundEdge;
                        neighbourNode.bestGoalBoundEdge = bestEdge;

                        if(bestEdge == BestGoalBoundEdge.Up)
                        {
                            Vector4 v = new Vector4(Math.Min(goalBounds[key]["up"].x, neighbourNode.x), Math.Max(goalBounds[key]["up"].y, neighbourNode.x), Math.Min(goalBounds[key]["up"].z, neighbourNode.y), Math.Max(goalBounds[key]["up"].w, neighbourNode.y));
                            goalBounds[key]["up"] = v;
                            // Update bounding box limits
                        }
                        else if(bestEdge == BestGoalBoundEdge.Down)
                        {
                            Vector4 v = new Vector4(Math.Min(goalBounds[key]["down"].x, neighbourNode.x), Math.Max(goalBounds[key]["down"].y, neighbourNode.x), Math.Min(goalBounds[key]["down"].z, neighbourNode.y), Math.Max(goalBounds[key]["down"].w, neighbourNode.y));
                            goalBounds[key]["down"] = v;
                            // Update bounding box limits
                        }
                        else if(bestEdge == BestGoalBoundEdge.Left)
                        {
                            Vector4 v = new Vector4(Math.Min(goalBounds[key]["left"].x, neighbourNode.x), Math.Max(goalBounds[key]["left"].y, neighbourNode.x), Math.Min(goalBounds[key]["left"].z, neighbourNode.y), Math.Max(goalBounds[key]["left"].w, neighbourNode.y));
                            goalBounds[key]["left"] = v;
                            // Update bounding box limits
                        }
                        else if(bestEdge == BestGoalBoundEdge.Right)
                        {
                            Vector4 v = new Vector4(Math.Min(goalBounds[key]["right"].x, neighbourNode.x), Math.Max(goalBounds[key]["right"].y, neighbourNode.x), Math.Min(goalBounds[key]["right"].z, neighbourNode.y), Math.Max(goalBounds[key]["right"].w, neighbourNode.y));
                            goalBounds[key]["right"] = v;
                            // Update bounding box limits
                        }

                        /*
                        Debug.Log("Neighbour X " + neighbourNode.x + ":" + neighbourNode.y);
                        Debug.Log("CurrentNode X " + CurrentNode.x + ":" + CurrentNode.y);
                        Debug.Log("Best Edge Current " + CurrentNode.bestGoalBoundEdge);
                        Debug.Log("Best Edge neighbourNode " + neighbourNode.bestGoalBoundEdge);
                        Debug.Log(goalBounds[key]["up"]);
                        Debug.Log(goalBounds[key]["down"]);
                        Debug.Log(goalBounds[key]["left"]);
                        Debug.Log(goalBounds[key]["right"]);
                        */
                    }
                }
            } 

            return false;
        }

        private bool IsDirectionValid(NodeRecord currentNode, NodeRecord neighbourNode, string direction)
        {
            switch (direction)
            {
                case "up":
                    return neighbourNode.y > currentNode.y;
                case "down":
                    return neighbourNode.y < currentNode.y;
                case "left":
                    return neighbourNode.x < currentNode.x;
                case "right":
                    return neighbourNode.x > currentNode.x;
                default:
                    return false;
            }

        }

        public bool GoalBoundBothInside(int startX, int startY, int genericX, int genericY, int goalX, int goalY)
        {
            string[] directions = { "up", "down", "left", "right"};
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


        // Checks if node(x,Y) is in the node(startx, starty) bounding box for the direction: direction
        public bool InsideGoalBoundBox(int startX, int startY, int x, int y, string direction)
        {

            var key = new Vector2(startX, startY);
            
            if (!this.goalBounds.ContainsKey(key))
                return false;

            if (!this.goalBounds[key].ContainsKey(direction))
                return false;

            var box = this.goalBounds[key][direction];
            
            //This is very ugly
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
    
    }

}
