---
id: "#108"
name: Wind-Up Duck
owner: "#2"
parent: "#107"
location: "#101"
flags:
  - readable
aliases:
  - "Wind-Up Duck"
  - "Duck"
updated: 2026-06-02T18:52:55-05:00
---

# Wind-Up Duck

```yaml
name: continue_msg
type: string
value: "swivels its neck and emits a >> Quack <<"
flags:
  - readable
```

```yaml
name: description
type: string
value: "A yellow plastic duck with wheels at the bottom and a knob for winding."
flags:
  - readable
```

```yaml
name: going_msg
type: string
value: "The duck is rolling forward with a slight waddle."
flags:
  - readable
```

```yaml
name: startup_msg
type: string
value: "waddles about and starts rolling forward."
flags:
  - readable
```

```yaml
name: wind_down_msg
type: string
value: "hiccups once and stops rolling."
flags:
  - readable
```

```yaml
name: wound
type: integer
value: 0
flags:
  - readable
```

## Verb: continue_msg

```yaml
names: ["continue_msg"]
dobj: this
prep: none
iobj: this
owner: "#2"
flags:
  - readable
  - executable
  - debug
```

```csharp

times = {"once","twice","thrice"}[random(3)];
return "swivels its neck and quacks " + times + ".";
```

