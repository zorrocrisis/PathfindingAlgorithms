using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Assets.Scripts.IAJ.Unity.Pathfinding.DataStructures
{
    //very simple (and unefficient) implementation of the open/closed sets
    public class SimpleUnorderedNodeList : IOpenSet, IClosedSet
    {
        private List<NodeRecord> NodeRecords { get; set; }

        public SimpleUnorderedNodeList()
        {
            this.NodeRecords = new List<NodeRecord>();
        }

        public void Initialize()
        {
            this.NodeRecords.Clear(); 
        }

        public int CountOpen()
        {
            return this.NodeRecords.Count;
        }

        public void AddToClosed(NodeRecord nodeRecord)
        {
            this.NodeRecords.Add(nodeRecord);
            nodeRecord.status = NodeStatus.Closed;

        }

        public void RemoveFromClosed(NodeRecord nodeRecord)
        {
            this.NodeRecords.Remove(nodeRecord);
        }

        public NodeRecord SearchInClosed(NodeRecord nodeRecord)
        {
            //here I cannot use the == comparer because the nodeRecord will likely be a different computational object
            //and therefore pointer comparison will not work, we need to use Equals
            //LINQ with a lambda expression
            return this.NodeRecords.FirstOrDefault(n => n.Equals(nodeRecord));
        }

        public void AddToOpen(NodeRecord nodeRecord)
        {
            this.NodeRecords.Add(nodeRecord);
            nodeRecord.status = NodeStatus.Open;
        }

        public void RemoveFromOpen(NodeRecord nodeRecord)
        {
            this.NodeRecords.Remove(nodeRecord);
        }

        public NodeRecord SearchInOpen(NodeRecord nodeRecord)
        {
            //here I cannot use the == comparer because the nodeRecord will likely be a different computational object
            //and therefore pointer comparison will not work, we need to use Equals
            //LINQ with a lambda expression
           
            
            return this.NodeRecords.FirstOrDefault(n => n.x == nodeRecord.x && n.y == nodeRecord.y);
           
            
            // return this.NodeRecords.FirstOrDefault(n => n.Equals(nodeRecord));
        }

        public ICollection<NodeRecord> All()
        {
            return this.NodeRecords;
        }

        public void Replace(NodeRecord nodeToBeReplaced, NodeRecord nodeToReplace)
        {
            //since the list is not ordered we do not need to remove the node and add the new one, just copy the different values
            //remember that if NodeRecord is a struct, for this to work we need to receive a reference
            nodeToBeReplaced.parent = nodeToReplace.parent;
            nodeToBeReplaced.fCost = nodeToReplace.fCost;
            nodeToBeReplaced.gCost = nodeToReplace.gCost;
            nodeToBeReplaced.hCost = nodeToReplace.hCost;
        }

        public NodeRecord GetBestAndRemove()
        {
            var best = this.PeekBest();
            this.NodeRecords.Remove(best);
            return best;
        }

        public NodeRecord PeekBest()
        {
            return this.NodeRecords.Aggregate((nodeRecord1, nodeRecord2) => nodeRecord1.fCost < nodeRecord2.fCost ? nodeRecord1 : nodeRecord2);
        }
    }
}
