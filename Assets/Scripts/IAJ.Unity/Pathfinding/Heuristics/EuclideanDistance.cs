using Assets.Scripts.IAJ.Unity.Pathfinding.DataStructures;
using UnityEngine;
using System;


namespace Assets.Scripts.IAJ.Unity.Pathfinding.Heuristics
{
    public class EuclideanDistance : IHeuristic
    {
        public float H(NodeRecord node, NodeRecord goalNode)
        {
            float distance = (float)Math.Sqrt((Math.Pow(goalNode.x - node.x, 2)) + (Math.Pow(goalNode.y - node.y, 2)));
            return distance;
        }
    }
}
