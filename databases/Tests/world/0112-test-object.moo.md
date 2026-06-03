---
id: "#112"
name: test object
owner: "#2"
parent: "#5"
location:
flags:
  - readable
aliases:
  - "test-alias"
updated: 2026-06-02T19:33:36-05:00
---

# test object

```yaml
name: added_prop
type: string
value: "added value"
flags:
  - readable
  - writable
```

## Verb: ping

```yaml
names: ["ping"]
dobj: none
prep: none
iobj: none
owner: "#2"
flags:
  - readable
  - executable
```

```csharp
return "changed";
```

## Verb: lambda_ping/lp

```yaml
names: ["lambda_ping", "lp"]
dobj: none
prep: none
iobj: none
owner: "#2"
flags:
  - readable
  - executable
```

```csharp
return "lambda pong";
```

