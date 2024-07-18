using Assets.Scripts.IAJ.Unity.Pathfinding.Heuristics;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Grid;
using Assets.Scripts.IAJ.Unity.Pathfinding.DataStructures;
using System.Runtime.CompilerServices;

namespace Assets.Scripts.IAJ.Unity.Pathfinding
{
    public class NodeArrayAStarPathfinding : AStarPathfinding
    {
        private static int index = 0;
        protected NodeRecordArray NodeRecordArray { get; set; }

        public NodeArrayAStarPathfinding(IHeuristic heuristic) : base(null, null, heuristic)
        {
            grid = new Grid<NodeRecord>((Grid<NodeRecord> global, int x, int y) => new NodeRecord(x, y, index++));
            this.InProgress = false;
            this.Heuristic = heuristic;
            this.NodesPerSearch = 100; //by default we process all nodes in a single request, but we changed this
            this.NodeRecordArray = new NodeRecordArray(grid.getAll());
            this.Open = this.NodeRecordArray;
            this.Closed = this.NodeRecordArray;

        }
       
        // In Node Array A* the only thing that changes is how you process the child node, the search occurs the exact same way so you can the parent's method
        protected override void ProcessChildNode(NodeRecord parentNode, NodeRecord neighbourNode)
        {
            // Calculate cost of neighbour node
            var newCost = parentNode.gCost + CalculateDistanceCost(parentNode, neighbourNode);
            var newFValue = newCost + Heuristic.H(neighbourNode, GoalNode);

            //Auxiliary variables to check whether node is in open/closed (node array methods)
            var closedNode = Closed.SearchInClosed(neighbourNode);
            var openNode = Open.SearchInOpen(neighbourNode);

            //If in Closed and with a higher F value...
            if (closedNode != null && closedNode.fCost >= newFValue)
            {
                //Remove from closed (changes status to unvisited), update values and add to open
                NodeRecordArray.RemoveFromClosed(closedNode);
                closedNode.gCost = newCost;
                closedNode.hCost = Heuristic.H(neighbourNode, GoalNode);
                closedNode.CalculateFCost();
                closedNode.parent = parentNode;
                NodeRecordArray.AddToOpen(closedNode); //updates status and adds the node to the open list
            }

            //If in Open and with a higher F value..
            else if (openNode != null && openNode.fCost >= newFValue)
            {
                //Update the costs
                openNode.gCost = newCost;
                openNode.hCost = Heuristic.H(neighbourNode, GoalNode);
                openNode.CalculateFCost();
                openNode.parent = parentNode;
            }

            //If node is not in any list ....
            else if (this.NodeRecordArray.GetNodeRecord(neighbourNode).status == NodeStatus.Unvisited)
            {
                //Update the costs and add to Open
                var unvisitedArray = this.NodeRecordArray.GetNodeRecord(neighbourNode);
                unvisitedArray.gCost = newCost;
                unvisitedArray.hCost = Heuristic.H(neighbourNode, GoalNode);
                unvisitedArray.CalculateFCost();
                unvisitedArray.parent = parentNode;
                NodeRecordArray.AddToOpen(unvisitedArray); //updates status and adds the node to the open list
            }

            //Update the actual grid value
            grid.SetGridObject(neighbourNode.x, neighbourNode.y, neighbourNode);

        }
               
            
    }


       
}
