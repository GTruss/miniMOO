---
id: "#78"
name: Generic Utilities Package
owner: "#0"
parent: "#1"
location:
flags:
  - readable
aliases: []
updated: 2026-06-02T18:52:55-05:00
---

# Generic Utilities Package

## Verb: help

```yaml
names: ["help"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
return "This is a collection of utility verbs for use in your MOO. It includes:\n\n" +
       "- $string_utils: string manipulation verbs\n" +
       "- $building_utils: object creation and building helper verbs\n" +
       "- $object_utils: general-purpose object query verbs";
```

