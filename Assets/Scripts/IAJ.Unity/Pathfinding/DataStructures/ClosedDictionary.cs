using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.IAJ.Unity.Pathfinding.DataStructures
{
    class ClosedDictionary : IClosedSet
    {

        //Tentative dictionary type structure, it is possible that there are better solutions...
        private Dictionary<Vector2, NodeRecord> Closed { get; set; }

        public ClosedDictionary()
        {
            this.Closed = new Dictionary<Vector2, NodeRecord>();
        }

        public void Initialize()
        {
            this.Closed = new Dictionary<Vector2, NodeRecord>();
        }


        public void AddToClosed(NodeRecord nodeRecord)
        {
            this.Closed.Add(new Vector2(nodeRecord.x, nodeRecord.y), nodeRecord);
            nodeRecord.status = NodeStatus.Closed;
        }

        public void RemoveFromClosed(NodeRecord nodeRecord)
        {
            this.Closed.Remove(new Vector2(nodeRecord.x, nodeRecord.y));
        }

        public NodeRecord SearchInClosed(NodeRecord nodeRecord)
        {
            if (this.Closed.ContainsKey(new Vector2(nodeRecord.x, nodeRecord.y)))
                return this.Closed[new Vector2(nodeRecord.x, nodeRecord.y)];
            else
                return null;
        }   

        public ICollection<NodeRecord> All()
        {
            return this.Closed.Values;
        }

    }

}

