# High performance Consistent Hash
* Immutable 
* Zero dependencies and super light weight
* Thoroughly tested
* Handles collisions

Internally uses GetHashCode and XxHash for mapping nodes to values, and RadixSort for sorting

## Usage
### Construction
```csharp
nodeToWeight = new Dictionary<string, int>() 
{
  { "NodeA", 100 },
  { "NodeB", 150 },
}
var hasher = ConsistentHash.Create(nodeToWeight);
```

### Hashing
```csharp
var hasher = ConsistentHash.Create(nodeToWeight);
var node = hasher.Hash(Guid.NewGuid());
```

## Performance 
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
### Distribution
