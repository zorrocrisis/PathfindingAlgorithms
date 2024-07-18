using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Grid
{
    public class Node
    {
        private Grid<Node> grid;
        public int x;
        public int y;

        public int gCost; // Cost so far
        public int hCost; // Heuristic so far
        public int fCost; // Total Cost


        public bool isWalkable;
        public Node cameFromNode;
        public Node(Grid<Node> grid, int x, int y)
        {
            this.grid = grid;
            this.x = x;
            this.y = y;
            gCost = 0;
            hCost = 0;
            fCost = 0;
            isWalkable = true;
        }

        public override string ToString()
        {
            return x + "," + y;
        }

        public void CalculateFCost()
        {
            fCost = gCost + hCost;
        }

    }
}
