# Code Layout

## Properties with Backing Fields

**ReSharper Bug:** ReSharper's "Properties w/ Backing Field" file layout feature does not work correctly (see [RSRP-484963](https://youtrack.jetbrains.com/issue/RSRP-484963)). Backing fields are NOT automatically grouped with their properties.

**AI Agent Responsibility:** When writing or reorganizing code, manually place backing fields immediately before their associated properties:

```csharp
// CORRECT - backing field directly above its property
private string _name;
public string Name
{
    get => _name;
    set => _name = value;
}

private int _count;
public int Count
{
    get => _count;
    set => _count = value;
}

// WRONG - all backing fields grouped together, separate from properties
private string _name;
private int _count;

public string Name { get => _name; set => _name = value; }
public int Count { get => _count; set => _count = value; }
```

## Member Ordering

Follow this general order within a type:

1. Constants and static fields
2. Constructors
3. **Properties with their backing fields** (field immediately before property)
4. Other instance fields (non-backing fields)
5. Interface implementations
6. Other members (methods, etc.)
7. Nested types

## Do Not Rely On

- ReSharper's "Reorder Type Members" for backing field placement
- Any automated tool to group backing fields with properties
