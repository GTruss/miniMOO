---
id: "#107"
name: Generic Wind-Up Toy
owner: "#2"
parent: "#5"
location: "#2"
flags:
  - readable
aliases:
  - "Generic Wind-Up Toy"
  - "Toy"
updated: 2026-06-02T19:33:36-05:00
---

# Generic Wind-Up Toy

```yaml
name: continue_msg
type: integer
value: 0
flags:
  - readable
  - chown
```

```yaml
name: going_msg
type: integer
value: 0
flags:
  - readable
  - chown
```

```yaml
name: maximum
type: integer
value: 20
flags:
  - readable
  - chown
```

```yaml
name: startup_msg
type: integer
value: 0
flags:
  - readable
  - chown
```

```yaml
name: wind_down_msg
type: integer
value: 0
flags:
  - readable
  - chown
```

```yaml
name: wound
type: integer
value: 0
flags:
  - readable
  - chown
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
if (this.location == player)
  if (this.wound < this.maximum)
    this.wound = this.wound + 2;
    player:tell("You wind up the ", this.name,".");
    player.location:announce(player.name, " winds up the ", this.name,".");
    if (this.wound >= this.maximum)
      player:tell("The knob comes to a stop while winding.");
    endif
  else
    player:tell("The ",this.name," is already fully wound.");
  endif
else
  player:tell("You have to be holding the ", this.name,".");
endif
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
if (this.wound)
  this.location:announce_all(this.name, " ", this:startup_msg());
  fork (15)
    this:do_the_work();
  endfork
endif
```

## Verb: wind_down_msg/continue_msg/startup_msg/going_msg

```yaml
names: ["wind_down_msg", "continue_msg", "startup_msg", "going_msg"]
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
return this.(verb);
```

## Verb: do_the_work

```yaml
names: ["do_the_work"]
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
if (this.wound)
  if ($object_utils:isa(this.location,$room))
    this.location:announce_all(this.name," ", this:continue_msg());
    this.wound = this.wound - 1;
    if (this.wound)
      fork (15)
        this:do_the_work();
      endfork
    else
      this.location:announce_all(this.name, " ", this:wind_down_msg());
    endif
  endif
  if (this.wound < 0)
    this.wound = 0;
  endif
endif
```

## Verb: description

```yaml
names: ["description"]
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
basic = pass(@args);
if (this.wound)
  return basic + " " + this:going_msg();
else
  return basic;
endif
```

