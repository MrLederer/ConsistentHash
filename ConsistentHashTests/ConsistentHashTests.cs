using ConsistentHashing;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ConsistentHashTests
{
    [ExcludeFromCodeCoverage]
    public class ConsistentHashTests
    {
        public void usageExamples()
        {
            var nodeToWeight = new Dictionary<string, int>()
            {
              { "NodeA", 100 },
              { "NodeB", 150 },
            };
            var hasher = ConsistentHash.Create(nodeToWeight);
            var value = Guid.NewGuid();
            var node = hasher.Hash(value);

            // {NodeA: 100, NodeB: 150}
            hasher = hasher.AddOrSet(node: "NodeA", weight: 200);
            // {NodeA: 200, NodeB: 150}
            hasher = hasher.AddOrSetRange(new Dictionary<string, int>() { { "NodeC", 500 }, { "NodeD", 35 } });
            // {NodeA: 200, NodeB: 150, NodeC: 500, NodeD: 35}
            hasher = hasher.AddOrSet(node: "NodeC", weight: 0);
            // {NodeA: 200, NodeB: 150, NodeD: 35}
            hasher = hasher.AddOrSet(node: "NodeD", weight: -100);
            // {NodeA: 200, NodeB: 150}

            // {NodeA: 200, NodeB: 150, NodeC: 500, NodeD: 35}
            hasher = hasher.Remove("NodeA");
            // {NodeB: 150, NodeC: 500, NodeD: 35}
            hasher = hasher.Remove("NonExistingNode");
            // {NodeB: 150, NodeC: 500, NodeD: 35}
            hasher = hasher.RemoveRange(new[] { "NodeC", "NodeD" });
            // {NodeB: 150}
        }

        [Test]
        public void ConsistencyTest()
        {
            // Given.. 
            var nodeToWeight = GenerateNodeToWeight(amountOfNodes: 100);
            var consistentHasher = ConsistentHash.Create(nodeToWeight);

            var keys = GenerateKeys(amount: 1_000).ToList();
            var expectedResult = GetNodeToKeys(consistentHasher, keys);

            // When.. 2nd time mapping
            var actualResult = GetNodeToKeys(consistentHasher, keys);

            // Then..
            Assert.IsTrue(DeepCompare(expectedResult, actualResult), "Mapping was not consistent");
        }

        [Test]
        public void DeterministicConstructorTest()
        {
            // Given.. 
            var nodeToWeight1 = GenerateNodeToWeight(amountOfNodes: 100);
            var consistentHasher1 = ConsistentHash.Create(nodeToWeight1);

            // When..
            var nodeToWeight2 = GenerateNodeToWeight(amountOfNodes: 100);
            var consistentHasher2 = ConsistentHash.Create(nodeToWeight2);


            // Then..
            AreEqual(consistentHasher1, consistentHasher2);

        }

        [Test]
        public void EqualsTest()
        {
            // Given.. 
            var nodeToWeight1 = GenerateNodeToWeight(amountOfNodes: 100);
            var consistentHasher1 = ConsistentHash.Create(nodeToWeight1);

            var nodeToWeight2 = GenerateNodeToWeight(amountOfNodes: 100);
            var consistentHasher2 = ConsistentHash.Create(nodeToWeight2);

            // Then..
            Assert.AreEqual(consistentHasher1, consistentHasher2, "instances with identical creation param turned out different");
        }

        [Test]
        public void NotEqualsWithDifferentSeedTest()
        {
            // Given.. 
            var nodeToWeight1 = GenerateNodeToWeight(amountOfNodes: 100);
            var consistentHasher1 = ConsistentHash.Create(nodeToWeight1);

            var nodeToWeight2 = GenerateNodeToWeight(amountOfNodes: 100);
            var consistentHasher2 = ConsistentHash.Create(nodeToWeight2, seed: 1);

            // Then..
            Assert.AreNotEqual(consistentHasher1, consistentHasher2, "instances with different creation param turned out equal");
        }

        [Test]
        public void NotEqualsWithDifferentNodesTest()
        {
            // Given.. 
            var nodeToWeight1 = GenerateNodeToWeight(amountOfNodes: 100);
            var consistentHasher1 = ConsistentHash.Create(nodeToWeight1);

            var nodeToWeight2 = GenerateNodeToWeight(amountOfNodes: 99);
            var consistentHasher2 = ConsistentHash.Create(nodeToWeight2);

            // Then..
            Assert.AreNotEqual(consistentHasher1, consistentHasher2, "instances with different creation param turned out equal");
        }

        [Test]
        public void HashCodeTest()
        {
            // Given.. 
            var nodeToWeight1 = GenerateNodeToWeight(amountOfNodes: 100);
            var consistentHasher1 = ConsistentHash.Create(nodeToWeight1);

            var nodeToWeight2 = GenerateNodeToWeight(amountOfNodes: 100);
            var consistentHasher2 = ConsistentHash.Create(nodeToWeight2);

            // Then..
            Assert.AreEqual(consistentHasher1.GetHashCode(), consistentHasher2.GetHashCode(), "instances with identical creation param gave different hashcode");
        }

        [Test]
        public void DifferentHashcodeWithDifferentSeedTest()
        {
            // Given.. 
            var nodeToWeight1 = GenerateNodeToWeight(amountOfNodes: 100);
            var consistentHasher1 = ConsistentHash.Create(nodeToWeight1);

            var nodeToWeight2 = GenerateNodeToWeight(amountOfNodes: 100);
            var consistentHasher2 = ConsistentHash.Create(nodeToWeight2, seed: 1);

            // Then..
            Assert.AreNotEqual(consistentHasher1.GetHashCode(), consistentHasher2.GetHashCode(), "instances with different creation param gave same hashcode");
        }

        [Test]
        public void DifferentHashcodeWithDifferentNodesTest()
        {
            // Given.. 
            var nodeToWeight1 = GenerateNodeToWeight(amountOfNodes: 100);
            var consistentHasher1 = ConsistentHash.Create(nodeToWeight1);

            var nodeToWeight2 = GenerateNodeToWeight(amountOfNodes: 99);
            var consistentHasher2 = ConsistentHash.Create(nodeToWeight2);

            // Then..
            Assert.AreNotEqual(consistentHasher1.GetHashCode(), consistentHasher2.GetHashCode(), "instances with different creation param gave same hashcode");
        }

        [Test]
        public void ContainsTest()
        {
            // Given.. 
            var nodeToWeight = GenerateNodeToWeight(amountOfNodes: 100);
            var consistentHasher = ConsistentHash.Create(nodeToWeight);

            // Then..
            foreach (var nodeAndWeight in nodeToWeight)
            {
                if (nodeAndWeight.Value > 0)
                {
                    Assert.IsTrue(consistentHasher.Contains(nodeAndWeight.Key), "instance was missing a defined node");
                }
            }
        }

        [Test]
        public void TryGetWeightTest()
        {
            // Given.. 
            var nodeToWeight = GenerateNodeToWeight(amountOfNodes: 100);
            var consistentHasher = ConsistentHash.Create(nodeToWeight);

            // Then..
            foreach (var nodeAndWeight in nodeToWeight)
            {
                if (nodeAndWeight.Value > 0)
                {
                    Assert.IsTrue(consistentHasher.TryGetWeight(nodeAndWeight.Key, out var weight));
                    Assert.AreEqual(nodeAndWeight.Value, weight, "instance had different weight than sent to ctor");
                }
            }
        }

        [Test]
        public void AddRangeTest()
        {
            // Given..
            var nodeToWeight = GenerateNodeToWeight(amountOfNodes: 100);
            var partialConsistentHasher = ConsistentHash.Create(nodeToWeight.Take(50));


            // When..
            var consistentHasherAfterAddition = partialConsistentHasher.AddOrSetRange(nodeToWeight.Skip(50));
            var expectedConsistentHasher = ConsistentHash.Create(nodeToWeight);

            // Then..
            AreEqual(expectedConsistentHasher, consistentHasherAfterAddition);
        }

        [Test]
        public void SetRangeWithLargerWeightsOnAllNodesTest()
        {
            // Given.. 
            var nodeToWeight = GenerateNodeToWeight(amountOfNodes: 100);
            var initialConsistentHasher = ConsistentHash.Create(nodeToWeight);


            // When..
            var nodeToWeightAfterSet = nodeToWeight.ToDictionary(nodeAndWeight => nodeAndWeight.Key, nodeAndWeight => nodeAndWeight.Value + 100);
            var consistentHasherAfterSet = initialConsistentHasher.AddOrSetRange(nodeToWeightAfterSet);
            var expectedConsistentHasher = ConsistentHash.Create(nodeToWeightAfterSet);

            // Then..
            AreEqual(expectedConsistentHasher, consistentHasherAfterSet);
        }

        [Test]
        public void SetRangeWithLargerWeightsTest()
        {
            // Given.. 
            var nodeToWeight = GenerateNodeToWeight(amountOfNodes: 100);
            var initialConsistentHasher = ConsistentHash.Create(nodeToWeight);


            // When..
            var nodeToWeightAfterSet = nodeToWeight.Take(10).ToDictionary(nodeAndWeight => nodeAndWeight.Key, nodeAndWeight => nodeAndWeight.Value + 100);
            var consistentHasherAfterSet = initialConsistentHasher.AddOrSetRange(nodeToWeightAfterSet);
            var expectedConsistentHasher = ConsistentHash.Create(nodeToWeight.Skip(10).Concat(nodeToWeightAfterSet));

            // Then..
            AreEqual(expectedConsistentHasher, consistentHasherAfterSet);
        }

        [Test]
        public void SetWithLowerWeightsTest()
        {
            // Given.. 
            var nodeToWeight = GenerateNodeToWeight(amountOfNodes: 100).ToDictionary(nodeAndWeight => nodeAndWeight.Key, nodeAndWeight => nodeAndWeight.Value + 100);
            var initialConsistentHasher = ConsistentHash.Create(nodeToWeight);


            // When..
            var consistentHasherAfterSet = initialConsistentHasher.AddOrSet(nodeToWeight.Last().Key, nodeToWeight.Last().Value - 100);
            var expectedConsistentHasher = ConsistentHash.Create(nodeToWeight.Select(nodeAndWeight => nodeAndWeight.Equals(nodeToWeight.Last()) ? new KeyValuePair<Node, int>(nodeAndWeight.Key, nodeAndWeight.Value - 100) : nodeAndWeight));

            // Then..
            AreEqual(consistentHasherAfterSet, expectedConsistentHasher);
        }

        [Test]
        public void SetWithZeroWeightsTest()
        {
            // Given.. 
            var nodeToWeight = GenerateNodeToWeight(amountOfNodes: 100).ToDictionary(nodeAndWeight => nodeAndWeight.Key, nodeAndWeight => nodeAndWeight.Value + 100);
            var initialConsistentHasher = ConsistentHash.Create(nodeToWeight);


            // When..
            var consistentHasherAfterSet = initialConsistentHasher.AddOrSet(nodeToWeight.Last().Key, 0);
            var expectedConsistentHasher = ConsistentHash.Create(nodeToWeight.SkipLast(1));

            // Then..
            AreEqual(expectedConsistentHasher, consistentHasherAfterSet);
        }

        [Test]
        public void SetWithNegativeWeightsTest()
        {
            // Given.. 
            var nodeToWeight = GenerateNodeToWeight(amountOfNodes: 100).ToDictionary(nodeAndWeight => nodeAndWeight.Key, nodeAndWeight => nodeAndWeight.Value + 100);
            var initialConsistentHasher = ConsistentHash.Create(nodeToWeight);


            // When..
            var consistentHasherAfterSet = initialConsistentHasher.AddOrSet(nodeToWeight.Last().Key, -1);
            var expectedConsistentHasher = ConsistentHash.Create(nodeToWeight.SkipLast(1));

            // Then..
            AreEqual(expectedConsistentHasher, consistentHasherAfterSet);
        }

        [Test]
        public void SetRangeWithLowerWeightsAllNodesTest()
        {
            // Given.. 
            var nodeToWeight = GenerateNodeToWeight(amountOfNodes: 100).ToDictionary(nodeAndWeight => nodeAndWeight.Key, nodeAndWeight => nodeAndWeight.Value + 100);
            var initialConsistentHasher = ConsistentHash.Create(nodeToWeight);


            // When..
            var nodeToWeightAfterSet = nodeToWeight.ToDictionary(nodeAndWeight => nodeAndWeight.Key, nodeAndWeight => nodeAndWeight.Value - 100);
            var consistentHasherAfterSet = initialConsistentHasher.AddOrSetRange(nodeToWeightAfterSet);
            var expectedConsistentHasher = ConsistentHash.Create(nodeToWeightAfterSet);

            // Then..
            AreEqual(expectedConsistentHasher, consistentHasherAfterSet);
        }

        [Test]
        public void SetRangeWithLowerWeightsTest()
        {
            // Given.. 
            var nodeToWeight = GenerateNodeToWeight(amountOfNodes: 100).ToDictionary(nodeAndWeight => nodeAndWeight.Key, nodeAndWeight => nodeAndWeight.Value + 100);
            var initialConsistentHasher = ConsistentHash.Create(nodeToWeight);


            // When..
            var nodeToWeightAfterSet = nodeToWeight.Skip(50).Take(25).ToDictionary(nodeAndWeight => nodeAndWeight.Key, nodeAndWeight => nodeAndWeight.Value - 100);
            var consistentHasherAfterSet = initialConsistentHasher.AddOrSetRange(nodeToWeightAfterSet);
            var expectedConsistentHasher = ConsistentHash.Create(nodeToWeight.Take(50).Concat(nodeToWeightAfterSet).Concat(nodeToWeight.TakeLast(25)));

            // Then..
            AreEqual(expectedConsistentHasher, consistentHasherAfterSet);
        }

        [Test]
        public void RemoveTest()
        {
            // Given.. 
            var nodeToWeight = GenerateNodeToWeight(amountOfNodes: 100);
            var initialConsistentHasher = ConsistentHash.Create(nodeToWeight);


            // When..
            var consistentHasherAfterRemoval = initialConsistentHasher.Remove(nodeToWeight.First().Key);
            var expectedConsistentHasher = ConsistentHash.Create(nodeToWeight.Skip(1));

            // Then..
            AreEqual(expectedConsistentHasher, consistentHasherAfterRemoval);
        }

        [Test]
        public void RemoveRangeTest()
        {
            // Given.. 
            var nodeToWeight = GenerateNodeToWeight(amountOfNodes: 100);
            var initialConsistentHasher = ConsistentHash.Create(nodeToWeight);


            // When..
            var consistentHasherAfterRemoval = initialConsistentHasher.RemoveRange(nodeToWeight.Take(25).Select(nodeToWeight => nodeToWeight.Key));
            var expectedConsistentHasher = ConsistentHash.Create(nodeToWeight.Skip(25));

            // Then..
            AreEqual(expectedConsistentHasher, consistentHasherAfterRemoval);
        }

        [Test]
        public void MinimumMovementTest()
        {
            // Given.. 
            var nodeToWeight = GenerateNodeToWeight(amountOfNodes: 100);
            var consistentHasher = ConsistentHash.Create(nodeToWeight);


            // When..
            var consistentHashAfterRemovingNode = consistentHasher.Remove(nodeToWeight.Last().Key);

            // Then..
            var keys = GenerateKeys(amount: 131_072).ToList();
            var result1 = GetNodeToKeys(consistentHasher, keys);
            var result2 = GetNodeToKeys(consistentHashAfterRemovingNode, keys);
            foreach (var (node, _) in nodeToWeight.Where(nodeAndWeight => nodeAndWeight.Value > 0).SkipLast(1))
            {
                Assert.IsTrue(result2[node].IsSupersetOf(result1[node]), "Mapping did not stay consistent when removing a a node");
            }
        }

        [Test]
        public void EmptyCollectionTest()
        {
            var emptyHasher = ConsistentHash.Empty<Node>();

            try
            {
                var node = emptyHasher.Hash(Guid.NewGuid());
                Assert.Fail("Should fail when hashing into an empty consistent hasher.");
            }
            catch (InvalidOperationException ex)
            {
                Assert.IsTrue(ex.Message.Contains("Empty", StringComparison.OrdinalIgnoreCase));
            }
        }

        [Test]
        public void ToStringTest()
        {
            var nodeToWeight = GenerateNodeToWeight(amountOfNodes: 5);
            var consistentHasher = ConsistentHash.Create(nodeToWeight);

            var displayString = consistentHasher.ToString();
            Assert.AreEqual("{Node1:100, Node2:200, Node3:300, Node4:400}", displayString);
        }

        [Test]
        public void ToStringEmptyInstanceTest()
        {
            var consistentHasher = ConsistentHash.Empty<Node>();

            var displayString = consistentHasher.ToString();
            Assert.AreEqual("{}", displayString);
        }

        #region Test Utilities
        private void AreEqual(ConsistentHash<Node> expectedConsistentHash, ConsistentHash<Node> actualConsistentHash)
        {
            var keys = GenerateKeys(amount: 131_072).ToList();
            var expectedResult = GetNodeToKeys(expectedConsistentHash, keys);
            var actualResult = GetNodeToKeys(actualConsistentHash, keys);
            Assert.IsTrue(DeepCompare(expectedResult, actualResult), "Mapping was not identical");
        }

        private static bool DeepCompare(Dictionary<Node, HashSet<Guid>> expectedResult, Dictionary<Node, HashSet<Guid>> actualResult)
        {
            if (expectedResult.Count != actualResult.Count)
            {
                throw new AssertionException($"expectedResult.Count ({expectedResult.Count}) != actualResult.Count ({actualResult.Count})");
            }
            foreach (var expectedRes in expectedResult)
            {
                if (!actualResult.TryGetValue(expectedRes.Key, out var value))
                {
                    throw new AssertionException($"actualResult does not contains {expectedRes.Key} with [{string.Join(",", expectedRes.Value)}]");
                }
                if (!expectedRes.Value.SetEquals(value))
                {
                    throw new AssertionException($"different values for {expectedRes.Key} actualResult: [{string.Join(",", value)}] != expectedResult: [{string.Join(",", expectedRes.Value)}]");
                }
            }
            return true;
        }

        private static Dictionary<Node, HashSet<Guid>> GetNodeToKeys(ConsistentHash<Node> consistentHasher, List<Guid> keys)
        {
            var expectedResult = new Dictionary<Node, HashSet<Guid>>();
            foreach (var key in keys)
            {
                var node = consistentHasher.Hash(key);
                if (expectedResult.TryGetValue(node, out var keySet))
                {
                    keySet.Add(key);
                }
                else
                {
                    expectedResult[node] = new HashSet<Guid> { key };
                }
            }

            return expectedResult;
        }

        private IEnumerable<Guid> GenerateKeys(int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                yield return Guid.NewGuid();
            }
        }

        private Dictionary<Node, int> GenerateNodeToWeight(int amountOfNodes)
        {
            return Enumerable.Range(0, amountOfNodes)
                .Select(index => (name: new Node("Node"+index.ToString()), weight: index * 100))
                .ToDictionary(tuple => tuple.name, tuple => tuple.weight);
        }

        public class Node : IEquatable<Node>, IComparable<Node>
        {
            private string m_name;

            public Node(string name)
            {
                m_name = name;
            }
            public int CompareTo(Node other)
            {
                return StringComparer.Ordinal.Compare(m_name, other?.m_name);
            }

            public bool Equals(Node other)
            {
                return m_name.Equals(other?.m_name, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                if (obj is Node other)
                {
                    return Equals(other);
                }
                return false;
            }

            public override int GetHashCode()
            {
                return m_name.GetHashCode();
            }

            public override string ToString()
            {
                return m_name;
            }
        }
        #endregion Utility methods
    }
}
