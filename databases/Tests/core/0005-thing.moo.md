---
id: "#5"
name: $thing
owner: "#0"
parent: "#1"
location:
flags:
  - readable
  - fertile
aliases: []
updated: 2026-06-02T18:52:55-05:00
---

# $thing

```yaml
name: description
type: string
value: "You see nothing special about it."
flags:
  - readable
```

```yaml
name: drop_failed_msg
type: string
value: ""
flags:
  - readable
```

```yaml
name: drop_succeeded_msg
type: string
value: ""
flags:
  - readable
```

```yaml
name: odrop_failed_msg
type: string
value: ""
flags:
  - readable
```

```yaml
name: odrop_succeeded_msg
type: string
value: ""
flags:
  - readable
```

```yaml
name: otake_failed_msg
type: string
value: ""
flags:
  - readable
```

```yaml
name: otake_succeeded_msg
type: string
value: ""
flags:
  - readable
```

```yaml
name: take_failed_msg
type: string
value: ""
flags:
  - readable
```

```yaml
name: take_succeeded_msg
type: string
value: ""
flags:
  - readable
```

## Verb: take_failed_msg/take_succeeded_msg/otake_failed_msg/otake_succeeded_msg/drop_failed_msg/drop_succeeded_msg/odrop_failed_msg/odrop_succeeded_msg

```yaml
names: ["take_failed_msg", "take_succeeded_msg", "otake_failed_msg", "otake_succeeded_msg", "drop_failed_msg", "drop_succeeded_msg", "odrop_failed_msg", "odrop_succeeded_msg"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
return this.(verb);
```

## Verb: drop/d/throw/th

```yaml
names: ["drop", "d", "throw", "th"]
dobj: this
prep: none
iobj: none
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
if (this.location != player)
  player:tell("You don't have that.");
elseif (!player.location:acceptable(this))
  player:tell("You can't drop that here.");
else
  this:moveto(player.location);
  if (this.location == player.location)
    player:tell_lines(this:drop_succeeded_msg() || "Dropped.");
    if (msg = this:odrop_succeeded_msg())
      player.location:announce(player.name, " ", msg);
    endif
  else
    player:tell_lines(this:drop_failed_msg() || "You can't seem to drop that here.");
    if (msg = this:odrop_failed_msg())
      player.location:announce(player.name, " ", msg);
    endif
  endif
endif
```

## Verb: get/g/take/t

```yaml
names: ["get", "g", "take", "t"]
dobj: this
prep: none
iobj: none
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
if (this.location == player)
  player:tell("You already have that!");
elseif (this.location != player.location)
  player:tell("I don't see that here.");
else
  this:moveto(player);
  if (this.location == player)
    player:tell(this:take_succeeded_msg() || "Taken.");
    if (msg = this:otake_succeeded_msg())
      player.location:announce(player.name, " ", msg);
    endif
  else
    player:tell(this:take_failed_msg() || "You can't pick that up.");
    if (msg = this:otake_failed_msg())
      player.location:announce(player.name, " ", msg);
    endif
  endif
endif
```

