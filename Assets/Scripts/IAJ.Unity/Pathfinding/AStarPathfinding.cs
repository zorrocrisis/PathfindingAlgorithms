using Assets.Scripts.IAJ.Unity.Pathfinding.Heuristics;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Grid;
using Assets.Scripts.IAJ.Unity.Pathfinding.DataStructures;
using System.Runtime.CompilerServices;
using System;
using Assets.Scripts.IAJ.Unity.Pathfinding;

namespace Assets.Scripts.IAJ.Unity.Pathfinding
{
    [Serializable]
    public class AStarPathfinding
    {
        // Cost of moving through the grid
        protected const float MOVE_STRAIGHT_COST = 1;
        protected const float MOVE_DIAGONAL_COST = 1.5f;
        public Grid<NodeRecord> grid { get; set; }
        public uint NodesPerSearch { get; set; }
        public uint TotalProcessedNodes { get; protected set; }
        public int MaxOpenNodes { get; protected set; }
        public float TotalProcessingTime { get; set; }
        public bool InProgress { get; set; }
        public IOpenSet Open { get; protected set; }
        public IClosedSet Closed { get; protected set; }
        public IHeuristic Heuristic { get; protected set; }

        public NodeRecord GoalNode { get; set; }
        public NodeRecord StartNode { get; set; }
        public int StartPositionX { get; set; }
        public int StartPositionY { get; set; }
        public int GoalPositionX { get; set; }
        public int GoalPositionY { get; set; }

        public bool goalBound = false;
        public GoalBoundAStarPathfinding goalBoundPath;

        public AStarPathfinding(IOpenSet open, IClosedSet closed, IHeuristic heuristic)
        {
            grid = new Grid<NodeRecord>((Grid<NodeRecord> global, int x, int y) => new NodeRecord(x, y));
            this.Open = open;
            this.Closed = closed;
            this.InProgress = false;
            this.Heuristic = heuristic;
            this.NodesPerSearch = 100; //by default we process all nodes in a single request, but we changed this

        }
        public virtual void InitializePathfindingSearch(int startX, int startY, int goalX, int goalY)
        {
            this.StartPositionX = startX;
            this.StartPositionY = startY;
            this.GoalPositionX = goalX;
            this.GoalPositionY = goalY;
            this.StartNode = grid.GetGridObject(StartPositionX, StartPositionY);
            this.GoalNode = grid.GetGridObject(GoalPositionX, GoalPositionY);

            //if it is not possible to quantize the positions and find the corresponding nodes, then we cannot proceed
            if (this.StartNode == null || this.GoalNode == null) return;

            // Reset debug and relevant variables here
            this.InProgress = true;
            this.TotalProcessedNodes = 0;
            this.TotalProcessingTime = 0.0f;
            this.MaxOpenNodes = 0;

            //Starting with the first node
            var initialNode = new NodeRecord(StartNode.x, StartNode.y)
            {
                gCost = 0,
                hCost = this.Heuristic.H(this.StartNode, this.GoalNode),
                index = StartNode.index
            };

            initialNode.CalculateFCost();
            this.Open.Initialize();
            this.Open.AddToOpen(initialNode);
            this.Closed.Initialize();
        }
        public virtual bool Search(out List<NodeRecord> solution, bool returnPartialSolution = false) {

            uint ProcessedNodes = 0;
            int OpenNodes = 0; 
            NodeRecord CurrentNode;
            
            //While Open is not empty or if nodes havent been all processed 
            while (ProcessedNodes <= NodesPerSearch)
            {
                if (Open.CountOpen() == 0)
                {
                    solution = null;
                    return true;
                }

                // CurrentNode is the best one from the Open set, start with that
                CurrentNode = Open.GetBestAndRemove();

                //Check if current node is the goal
                if (CurrentNode.Equals(GoalNode))
                {
                    solution = CalculatePath(CurrentNode);
                    InProgress = false;
                    return true;
                }

                Closed.AddToClosed(CurrentNode);

                //If we don't update the grid value here also, some nodes (like the ones in the corners) will not be updated visually
                //because they aren't neighbours to any other opens nodes and thus are not processed through the "ProcessChildNode"
                grid.SetGridObject(CurrentNode.x, CurrentNode.y, CurrentNode);


                //Handle the neighbours/children with something like this
                foreach (var neighbourNode in GetNeighbourList(CurrentNode))
                {

                    //Implementation of the goalbounding (INCOMPLETE)
                    if(goalBound)
                    {
                        if (goalBoundPath.InsideGoalBoundBox(CurrentNode.x, CurrentNode.y, neighbourNode.x, neighbourNode.y, "up"))
                        {
                            this.ProcessChildNode(CurrentNode, neighbourNode);
                            //Keeps the maximum size that the open list had
                            OpenNodes = Open.CountOpen();
                            if (OpenNodes > this.MaxOpenNodes)
                                this.MaxOpenNodes = OpenNodes;

                            //Increment de processed nodes
                            ProcessedNodes += 1;
                        }
                    }
                    else
                    {

                        this.ProcessChildNode(CurrentNode, neighbourNode);
                        //Keeps the maximum size that the open list had
                        OpenNodes = Open.CountOpen();
                        if (OpenNodes > this.MaxOpenNodes)
                            this.MaxOpenNodes = OpenNodes;

                        //Increment de processed nodes
                        ProcessedNodes += 1;

                    }
                }

            }

            //Keep track of processed nodes
            this.TotalProcessedNodes += ProcessedNodes;

            solution = null;
            return false;
        
        }

        protected virtual void ProcessChildNode(NodeRecord parentNode, NodeRecord node)
        {

            //Calculate cost of neighbour node
            var newCost = parentNode.gCost + CalculateDistanceCost(parentNode, node);       
            var newFValue = newCost + Heuristic.H(node, GoalNode);

            //Auxiliary variables to check whether node is in open/closed
            var closedNode = Closed.SearchInClosed(node);
            var openNode = Open.SearchInOpen(node);

            //If in Closed and with a higher F value...
            if (closedNode != null && closedNode.fCost >= newFValue)
            {
                //Remove from Closed, update values and add to Open
                Closed.RemoveFromClosed(closedNode);
                closedNode.gCost = newCost;
                closedNode.hCost = Heuristic.H(node, GoalNode);
                closedNode.CalculateFCost();
                closedNode.parent = parentNode;
                Open.AddToOpen(closedNode);
            }

            //If in Open and with a higher F value..
            else if (openNode != null && openNode.fCost >= newFValue)
            {
                //Update the costs
                openNode.gCost = newCost;
                openNode.hCost = Heuristic.H(node, GoalNode);
                openNode.CalculateFCost();
                openNode.parent = parentNode;

            }

            //If node is not in any list ....
            else if (closedNode == null && openNode == null)
            {        
                //Update the costs and add to Open
                node.gCost = newCost;
                node.hCost = Heuristic.H(node, GoalNode);
                node.CalculateFCost();
                node.parent = parentNode;
                Open.AddToOpen(node);
            }

            //Update the actual grid value
            grid.SetGridObject(node.x, node.y, node);
        }


        protected float CalculateDistanceCost(NodeRecord a, NodeRecord b)
        {
            // Math.abs is quite slow, thus we try to avoid it
            int xDistance = 0;
            int yDistance = 0;
            int remaining = 0;

            if (b.x > a.x)
                xDistance = Math.Abs(a.x - b.x);
            else xDistance = a.x - b.x;

            if (b.y > a.y)
                yDistance = Math.Abs(a.y - b.y);
            else yDistance = a.y - b.y;

            if (yDistance > xDistance)
                remaining = Math.Abs(xDistance - yDistance);
            else remaining = xDistance - yDistance;

            // Diagonal Cost * Diagonal Size + Horizontal/Vertical Cost * Distance Left
            return MOVE_DIAGONAL_COST * Mathf.Min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
        }

        // You'll need to use this method during the Search, to get the neighboors
        protected List<NodeRecord> GetNeighbourList(NodeRecord currentNode)
        {
            List<NodeRecord> neighbourList = new List<NodeRecord>();

            if(currentNode.x - 1 >= 0)
            {
                // Left
                neighbourList.Add(GetNode(currentNode.x - 1, currentNode.y));
                //Left down
                if(currentNode.y - 1 >= 0)
                    neighbourList.Add(GetNode(currentNode.x - 1, currentNode.y - 1));
                //Left up
                if (currentNode.y + 1 < grid.getHeight())
                    neighbourList.Add(GetNode(currentNode.x - 1, currentNode.y + 1));
            }
            if (currentNode.x + 1 < grid.getWidth())
            {
                // Right
                neighbourList.Add(GetNode(currentNode.x + 1, currentNode.y));
                //Right down
                if (currentNode.y - 1 >= 0)
                    neighbourList.Add(GetNode(currentNode.x + 1, currentNode.y - 1));
                //Right up
                if (currentNode.y + 1 < grid.getHeight())
                    neighbourList.Add(GetNode(currentNode.x + 1, currentNode.y + 1));
            }
            // Down
            if (currentNode.y - 1 >= 0)
                neighbourList.Add(GetNode(currentNode.x, currentNode.y - 1));
            //Up
            if (currentNode.y + 1 < grid.getHeight())
                neighbourList.Add(GetNode(currentNode.x, currentNode.y + 1));

            neighbourList.RemoveAll(x => !x.isWalkable);

            return neighbourList;
        }

        public NodeRecord GetNode(int x, int y)
        {
            return grid.GetGridObject(x, y);
        }


        // Method to calculate the Path, starts from the end Node and goes up until the beggining
        public List<NodeRecord> CalculatePath(NodeRecord endNode)
        {
            List<NodeRecord> path = new List<NodeRecord>();
            path.Add(endNode);

            NodeRecord node = endNode;

            // Start from the end node and go up until the beggining of the path
            while (!node.Equals(StartNode))
            {
                path.Add(node.parent);
                node = node.parent;
            }

           path.Reverse();
            return path;
        }

    }
}
