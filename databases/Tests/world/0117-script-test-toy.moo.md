---
id: "#117"
name: Script Test Toy
owner: "#2"
parent: "#5"
location: "#101"
flags:
  - readable
aliases:
  - "Script Test Toy"
  - "sttoy"
updated: 2026-06-02T18:52:55-05:00
---

# Script Test Toy

```yaml
name: startup_msg
type: string
value: "starts rolling."
flags:
  - readable
```

```yaml
name: wound
type: integer
value: 1
flags:
  - readable
```

## Verb: wind

```yaml
names: ["wind"]
dobj: this
prep: none
iobj: none
owner: "#2"
flags:
  - readable
  - debug
```

```csharp
player:tell("Script toy edited.");
```

## Verb: d*rop/th*row

```yaml
names: ["d*rop", "th*row"]
dobj: this
prep: none
iobj: none
owner: "#2"
flags:
  - readable
  - debug
```

```csharp
pass(@args);
player:tell("Script toy drops.");
```

