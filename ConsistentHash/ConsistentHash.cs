using System;
using System.Collections.Generic;

namespace ConsistentHashing
{
    public static class ConsistentHash
    {
        public static ConsistentHash<TNode> Empty<TNode>(IComparer<TNode> comparer = null, IEqualityComparer<TNode> equalityComparer = null, uint seed = 0) where TNode : IEquatable<TNode>, IComparable<TNode>
        {
            return Create(new Dictionary<TNode, int>(), comparer, equalityComparer, seed);
        }

        public static ConsistentHash<TNode> Create<TNode>(IEnumerable<KeyValuePair<TNode, int>> nodeToWeight, IComparer<TNode> comparer = null, IEqualityComparer<TNode> equalityComparer = null, uint seed = 0)
        {
            return new ConsistentHash<TNode>(nodeToWeight, comparer, equalityComparer, seed);
        }
    }

    // TODO: Add collision handling strategy
    // TODO: Add documentation to all public methods!
    // TODO: Add nice ToString implementation!
    public class ConsistentHash<TNode> : IEquatable<ConsistentHash<TNode>>
    {
        private readonly Dictionary<TNode, NodeMetadata> m_nodeToMetadata;
        private readonly Sector<TNode>[] m_sectors;
        private readonly uint m_seed;
        private readonly IComparer<TNode> m_nodeComparer;
        private readonly Comparer<Sector<TNode>> m_sectorComparer;
        private readonly IEqualityComparer<TNode> m_nodeEqualityComparer;

        #region Public methods
        public int Count => m_nodeToMetadata.Count;

        public int WeightCount { get; }

        internal ConsistentHash(IEnumerable<KeyValuePair<TNode, int>> nodeToWeight, IComparer<TNode> comparer = null, IEqualityComparer<TNode> equalityComparer = null, uint seed = 0)
        {
            m_seed = seed;
            m_nodeComparer = comparer ?? Comparer<TNode>.Default;
            m_sectorComparer = Utils.GetSectorComparer(m_nodeComparer);
            m_nodeEqualityComparer = equalityComparer ?? EqualityComparer<TNode>.Default;
            var (nodeInfo, totalWeight, _) = GetNodeInfo(nodeToWeight);
            (m_nodeToMetadata, m_sectors) = CreateSectors(nodeInfo, totalWeight, m_seed, m_nodeEqualityComparer);
            WeightCount = m_sectors.Length;
        }

        private ConsistentHash(Dictionary<TNode, NodeMetadata> nodeToMetadata, Sector<TNode>[] sectors, IComparer<TNode> nodeComparer, Comparer<Sector<TNode>> sectorComparer, uint seed)
        {
            m_seed = seed;
            m_nodeComparer = nodeComparer;
            m_sectorComparer = sectorComparer;
            m_nodeToMetadata = nodeToMetadata;
            m_sectors = sectors;
            WeightCount = sectors.Length;
        }

        public TNode Hash<TValue>(TValue value)
        {
            if (Count == 0)
            {
                throw new InvalidOperationException("Object is empty! No nodes to hash value to.");
            }

            var index = Array.BinarySearch(m_sectors, new Sector<TNode>(endAngle: (uint) value.GetHashCode(), node: default), m_sectorComparer);
            if (index < 0)
            {
                index = ~index;
                // Closing the full circle, by mapping elements after the last sector with the first sector.
                if (index == m_sectors.Length)
                {
                    index = 0;
                }
            }
            return m_sectors[index].m_node;
        }

        public ConsistentHash<TNode> AddOrSet(TNode node, int weight)
        {
            return AddOrSetRange(new [] { new KeyValuePair<TNode, int>(node, weight) });
        }

        public ConsistentHash<TNode> AddOrSetRange(IEnumerable<KeyValuePair<TNode, int>> nodeToWeight)
        {
            return Update(nodesToRemove: Array.Empty<TNode>(), nodesToAddOrSet: nodeToWeight);

        }

        public ConsistentHash<TNode> Remove(TNode node)
        {
            return RemoveRange(new [] { node });
        }

        public ConsistentHash<TNode> RemoveRange(IEnumerable<TNode> nodesToRemove)
        {
            return Update(nodesToRemove: nodesToRemove, nodesToAddOrSet: Array.Empty<KeyValuePair<TNode, int>>());
        }

        public ConsistentHash<TNode> Update(IEnumerable<TNode> nodesToRemove, IEnumerable<KeyValuePair<TNode, int>> nodesToAddOrSet)
        {
            // Removing sectors
            var (verifiedNodesToRemove, verifiedWeightToRemoved) = GetVerifiedNodesToRemove(nodesToRemove, nodesToAddOrSet);
            var sectorsAfterRemoval = RemoveSectors(verifiedNodesToRemove, verifiedWeightToRemoved);
            // Adding sectors
            var (sortedNodesInfoToAddOrSet, verifiedAddedWeight, nodeCountThatIsAddingWeight) = GetNodeInfo(nodesToAddOrSet);
            var (addOrSetNodeToMetadata, addedSortedSectors) = CreateSectors(sortedNodesInfoToAddOrSet, verifiedAddedWeight, m_seed, m_nodeEqualityComparer);
            var mergedSortedSectors = MergeSortedWithSorted(sectorsAfterRemoval, addedSortedSectors);
            var mergedNodeToMetadata = CreateNewNodeToMetadata(m_nodeToMetadata, verifiedNodesToRemove, addOrSetNodeToMetadata, nodeCountThatIsAddingWeight);
            return new ConsistentHash<TNode>(mergedNodeToMetadata, mergedSortedSectors, m_nodeComparer, m_sectorComparer, m_seed);
        }

        public bool Contains(TNode node)
        {
            return m_nodeToMetadata.ContainsKey(node);
        }

        public bool TryGetWeight(TNode node, out int weight)
        {
            if (m_nodeToMetadata.TryGetValue(node, out var metadata))
            {
                weight = metadata.m_weight;
                return true;
            }
            weight = 0;
            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj is ConsistentHash<TNode> other)
            {
                return Equals(other);
            }
            return false;
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(m_seed);
            hash.Add(Count);
            foreach (var nodeAndMetadata in m_nodeToMetadata)
            {
                hash.Add(nodeAndMetadata);
            }
            return hash.ToHashCode();
        }

        public bool Equals(ConsistentHash<TNode> other)
        {
            if (m_seed != other.m_seed)
            {
                return false;
            }
            if (Count != other.Count)
            {
                return false;
            }
            foreach (var nodeAndMetadata in m_nodeToMetadata)
            {
                if (!other.m_nodeToMetadata.TryGetValue(nodeAndMetadata.Key, out var metadata))
                {
                    return false;
                }
                if (nodeAndMetadata.Value.m_weight != metadata.m_weight)
                {
                    return false;
                }
                if (nodeAndMetadata.Value.m_nodeSeed != metadata.m_nodeSeed)
                {
                    return false;
                }
            }
            return true;
        }
        #endregion Public methods

        private Sector<TNode>[] MergeSortedWithSorted(Sector<TNode>[] sectorsAfterRemoval, Sector<TNode>[] addedSortedSectors)
        {
            if (sectorsAfterRemoval.Length == 0)
            {
                return addedSortedSectors;
            }
            if (addedSortedSectors.Length == 0)
            {
                return sectorsAfterRemoval;
            }

            return Utils.MergeSortedWithSorted(sectorsAfterRemoval, addedSortedSectors, m_sectorComparer);
        }

        private static Dictionary<TNode, NodeMetadata> CreateNewNodeToMetadata(Dictionary<TNode, NodeMetadata> currentNodeToMetadata, HashSet<TNode> nodesToRemove, Dictionary<TNode, NodeMetadata> addOrSetNodeToMetadata, int nodeThatWasSetCount)
        {
            var nodesToAddCount = addOrSetNodeToMetadata.Count - nodeThatWasSetCount;
            var result = new Dictionary<TNode, NodeMetadata>(capacity: currentNodeToMetadata.Count - nodesToRemove.Count + nodesToAddCount);
            foreach (var currentNode in currentNodeToMetadata)
            {
                if (!nodesToRemove.Contains(currentNode.Key))
                {
                    result.Add(currentNode.Key, currentNode.Value);
                }
            }
            foreach (var addedNodeAndMetadata in addOrSetNodeToMetadata)
            {
                result[addedNodeAndMetadata.Key] = addedNodeAndMetadata.Value;
            }
            return result;
        }

        private Sector<TNode>[] RemoveSectors(HashSet<TNode> nodesToRemove, int weightToRemove)
        {
            if (nodesToRemove.Count == 0)
            {
                return m_sectors;
            }

            var sectorsAfterRemoval = new Sector<TNode>[m_sectors.Length - weightToRemove];
            var sectorsAfterRemovalIndex = 0;
            for (var i = 0; i < m_sectors.Length; i++)
            {
                if (!nodesToRemove.Contains(m_sectors[i].m_node))
                {
                    sectorsAfterRemoval[sectorsAfterRemovalIndex++] = m_sectors[i];
                }
            }
            return sectorsAfterRemoval;
        }

        private (HashSet<TNode> verifiedNodesToRemove, int weightToRemove) GetVerifiedNodesToRemove(IEnumerable<TNode> nodesToRemove, IEnumerable<KeyValuePair<TNode, int>> nodesToAddOrSet)
        {
            var weightToRemoved = 0;
            var verifiedNodesToRemove = new HashSet<TNode>(nodesToRemove, m_nodeEqualityComparer);
            foreach (var nodeToRemove in verifiedNodesToRemove)
            {
                if (m_nodeToMetadata.TryGetValue(nodeToRemove, out var nodeMetadata))
                {
                    weightToRemoved += nodeMetadata.m_weight;
                }
                else
                {
                    verifiedNodesToRemove.Remove(nodeToRemove);
                }
            }
            foreach (var nodeToAddOrSet in nodesToAddOrSet)
            {
                
                if (m_nodeToMetadata.TryGetValue(nodeToAddOrSet.Key, out var nodeMetadata) && nodeToAddOrSet.Value < nodeMetadata.m_weight)
                {
                    // The strategy to set a lower weight for a node is to remove it entirely and than add it again.
                    if (verifiedNodesToRemove.Add(nodeToAddOrSet.Key))
                    {
                        weightToRemoved += nodeMetadata.m_weight;
                    }
                }
            }
            return (verifiedNodesToRemove, weightToRemoved);
        }

        private static (Dictionary<TNode, NodeMetadata>, Sector<TNode>[]) CreateSectors(List<(TNode, NodeMetadata, int)> sortedNodeInfo, int totalAmountOfSectors, uint seed, IEqualityComparer<TNode> nodeEqualityComparer)
        {
            var (nodeToMetadata, unsortedSectors) = CreateUnsortedSectors(sortedNodeInfo, totalAmountOfSectors, seed, nodeEqualityComparer);
            var sortedSectors = Utils.RadixSort(unsortedSectors);
            return (nodeToMetadata, sortedSectors);
        }

        private static (Dictionary<TNode, NodeMetadata>, Sector<TNode>[]) CreateUnsortedSectors(List<(TNode, NodeMetadata, int)> sortedNodeInfo, int totalAmountOfSectors, uint seed, IEqualityComparer<TNode> nodeEqualityComparer)
        {
            var sectors = new Sector<TNode>[totalAmountOfSectors];
            var nodeToMetadata = new Dictionary<TNode, NodeMetadata>(sortedNodeInfo.Count, nodeEqualityComparer);
            var sectorsIndex = 0;
            foreach (var (node, metadata, newWeight) in sortedNodeInfo)
            {
                if (newWeight > 0 && newWeight != metadata.m_weight)
                {
                    int i;
                    uint currentHash;
                    uint nodeSeed;
                    if (metadata.Equals(default(NodeMetadata)))
                    {
                        i = 0;
                        nodeSeed = Utils.XxHash32(unchecked((uint)node.GetHashCode()), seed); // customize seed per node, inorder to avoid continual hash collision.
                        currentHash = nodeSeed;

                    }
                    else if (metadata.m_weight < newWeight)
                    {
                        // Expanding the weight for existing node.
                        i = metadata.m_weight;
                        nodeSeed = metadata.m_nodeSeed;
                        currentHash = metadata.m_lastCalculatedAngle;
                    }
                    else
                    {

                        // The strategy to set a lower weight for a node is to remove it entirely and than add it again.
                        i = 0;
                        nodeSeed = metadata.m_nodeSeed;
                        currentHash = nodeSeed;
                    }

                    while (i < newWeight)
                    {
                        currentHash = Utils.XxHash32(currentHash, nodeSeed);
                        sectors[sectorsIndex++] = new Sector<TNode>(currentHash, node);
                        i++;
                    }
                    nodeToMetadata[node] = new NodeMetadata(nodeSeed, currentHash, newWeight);
                }
            }
            return (nodeToMetadata, sectors);
        }

        private (List<(TNode node, NodeMetadata metadata, int newWeight)> nodeInfo, int totalWeightCount, int nodeCountThatIsAddingWeight) GetNodeInfo(IEnumerable<KeyValuePair<TNode, int>> nodeToWeight)
        {
            var totalWeightCount = 0;
            var nodeCountThatIsAddingWeight = 0;
            List<(TNode node, NodeMetadata metadata, int newWeight)> sortedNodeInfo;
            if (nodeToWeight is ICollection<KeyValuePair<TNode, int>> nodeToWeightCollection)
            {
                sortedNodeInfo = new List<(TNode node, NodeMetadata metadata, int newWeight)>(capacity: nodeToWeightCollection.Count);
            }
            else
            {
                sortedNodeInfo = new List<(TNode node, NodeMetadata metadata, int newWeight)>();
            }
            foreach (var (node, newWeight) in nodeToWeight)
            {
                if (newWeight > 0)
                {
                    if (m_nodeToMetadata != null && m_nodeToMetadata.TryGetValue(node, out var currentMetadata) && currentMetadata.m_weight <= newWeight)
                    {
                        totalWeightCount += (newWeight - currentMetadata.m_weight);
                        sortedNodeInfo.Add((node, currentMetadata, newWeight));
                        nodeCountThatIsAddingWeight++;
                    }
                    else
                    {
                        totalWeightCount += newWeight;
                        sortedNodeInfo.Add((node, default, newWeight));
                    }
                }
            }
            // Sort the collection to make the output deterministic.
            sortedNodeInfo.Sort((x, y) => m_nodeComparer.Compare(x.node, y.node));
            return (sortedNodeInfo, totalWeightCount, nodeCountThatIsAddingWeight);
        }
    }

    public struct NodeMetadata
    {
        public readonly uint m_nodeSeed;
        public readonly uint m_lastCalculatedAngle;
        public readonly int m_weight;

        public NodeMetadata(uint nodeSeed, uint lastcalculatedAngle, int weight)
        {
            m_nodeSeed = nodeSeed;
            m_lastCalculatedAngle = lastcalculatedAngle;
            m_weight = weight;
        }
    }

    internal struct Sector<TNode>
    {

        public readonly uint m_endAngle;
        public readonly TNode m_node;

        public Sector(uint endAngle, TNode node)
        {
            m_endAngle = endAngle;
            m_node = node;
        }
    }
}
