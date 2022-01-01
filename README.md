![_Consistent_Hash🪐](https://user-images.githubusercontent.com/16527376/147839020-5e88c335-6275-44ab-9614-e77badd9a7d0.png)

![GitHub Workflow Status](https://img.shields.io/github/workflow/status/MrLederer/ConsistentHash/build-and-unit-tests)
![Azure DevOps coverage](https://img.shields.io/azure-devops/coverage/carmellederer/ConsistentHash/2)
![Nuget](https://img.shields.io/nuget/v/ConsistentHash)

A high performance, immutable, light-weight, and with no dependencies, ring consistent hash library.
## 💻 Usage
### Construction
```csharp
var nodeToWeight = new Dictionary<string, int>()
{
  { "NodeA", 100 },
  { "NodeB", 150 },
};
var hasher = ConsistentHash.Create(nodeToWeight);
```

### Hashing
```csharp
var hasher = ConsistentHash.Create(nodeToWeight);
var value = Guid.NewGuid();
var node = hasher.Hash(value);
// node = "NodeB"
```
### AddOrSet / AddOrSetRange
```csharp
var hasher = ConsistentHash.Create(nodeToWeight); 
// {NodeA: 100, NodeB: 150}
hasher = hasher.AddOrSet(node: "NodeA", weight: 200); 
// {NodeA: 200, NodeB: 150}
hasher = hasher.AddOrSetRange(new Dictionary<string, int>() { { "NodeC", 500 }, {"NodeD", 35 } });
// {NodeA: 200, NodeB: 150, NodeC: 500, NodeD: 35}
hasher = hasher.AddOrSet(node: "NodeC", weight: 0);
// {NodeA: 200, NodeB: 150, NodeD: 35}
hasher = hasher.AddOrSet(node: "NodeD", weight: -100);
// {NodeA: 200, NodeB: 150}
```

### Remove / RemoveRange
```csharp
// {NodeA: 200, NodeB: 150, NodeC: 500, NodeD: 35}
hasher = hasher.Remove("NodeA");
// {NodeB: 150, NodeC: 500, NodeD: 35}
hasher = hasher.Remove("NonExistingNode");
// {NodeB: 150, NodeC: 500, NodeD: 35}
hasher = hasher.RemoveRange(new[] { "NodeC", "NodeD" });
// {NodeB: 150}
```

# 🌟 Features
* **High performance** - Internally uses GetHashCode and XxHash for mapping nodes to values, and RadixSort for sorting
* **Immutable** 
* **Determinism**
* **Zero dependencies** and **super light weight**
* **Thoroughly tested**
* **Handles collisions** in a deterministic manner

## ⚡️ Performance 
### API runtime and memory
*Operation*|*Runtime*|*Memory usage*|*Details*
--- | --- | --- | :--
Hash | O(log(N)) | n/a | N = Number of nodes
Construction | O(&sum;<sup>n</sup><sub>i=0</sub>N<sub>i</sub>) | O(&sum;<sup>n</sup><sub>i=0</sub>N<sub>i</sub>) | N<sub>i</sub> = Weight defined for node i
AddOrSetRange | O(&sum;<sup>n</sup><sub>i=0</sub>N<sub>i</sub>) | O(&sum;<sup>n</sup><sub>i=0</sub>N<sub>i</sub>) | N<sub>i</sub> = Weight defined for node i
RemoveRange | O(&sum;<sup>n</sup><sub>i=0</sub>N<sub>i</sub>) | O(&sum;<sup>n</sup><sub>i=0</sub>N<sub>i</sub>) | N<sub>i</sub> = Weight defined for node i
Update | O(&sum;<sup>n</sup><sub>i=0</sub>N<sub>i</sub>) | O(&sum;<sup>n</sup><sub>i=0</sub>N<sub>i</sub>) | N<sub>i</sub> = Weight defined for node i
Contains | O(1) | n/a |
TryGetWeight | O(1) | n/a |
Equals | O(N) | n/a | N = Number of nodes
### Benchmarks Distribution

## 📃 License

