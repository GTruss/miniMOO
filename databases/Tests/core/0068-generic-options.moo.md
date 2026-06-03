---
id: "#68"
name: Generic Option Package
owner: "#0"
parent: "#1"
location:
flags:
  - readable
  - fertile
aliases: []
created: 2026-05-24T15:10
updated: 2026-05-24T15:10
---

# Generic Option Package

## Verb: get

```yaml
names: [get]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
{options, name} = args;
if (name in options)
  return 1;
elseif (a = $list_utils:assoc(name, options))
  return a[2];
else
  return 0;
endif
```
