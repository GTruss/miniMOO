---
id: "#88"
name: Frand's player class
owner: "#0"
parent: "#40"
location:
flags:
  - readable
  - fertile
aliases: []
updated: 2026-06-02T19:33:36-05:00
---

# Frand's player class

## Verb: tell_ways

```yaml
names: ["tell_ways"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
exits = args[1];
answer = {};
for e in (exits)
  answer = {@answer, e.name + " (" + $string_utils:english_list(e.aliases) + ")"};
endfor
player:tell("Obvious exits: ", $string_utils:english_list(answer), ".");
```

## Verb: obvious_exits

```yaml
names: ["obvious_exits"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
return {"n", "ne", "e", "se", "s", "sw", "w", "nw", "north", "northeast", "east", "southeast", "south", "southwest", "west", "northwest", "u", "d", "up", "down", "out", "exit", "leave", "enter"};
```

## Verb: findexits

```yaml
names: ["findexits"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
{room, exits} = args;
alphabet = "abcdefghijklmnopqrstuvwxyz0123456789";
for i in [1..length(alphabet)]
  found = room:match_exit(alphabet[i]);
  if (valid(found) && !(found in exits))
    exits = {@exits, found};
  endif
endfor

return exits;
```

## Verb: checkexits

```yaml
names: ["checkexits"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
{to_check, room, exits} = args;
for word in (to_check)
  found = room:match_exit(word);
  if (valid(found) && !(found in exits))
    exits = {@exits, found};
  endif
endfor
return exits;
```

## Verb: @ways

```yaml
names: ["@ways"]
dobj: any
prep: none
iobj: none
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
if (dobjstr)
  room = dobj;
else
  room = this.location;
endif

if (!valid(room) || !($room in $object_utils:ancestors(room)))
  player:tell("You can only pry into the exits of a room.");
  return;
endif

exits = {};

if ($object_utils:has_verb(room, "obvious_exits"))
  exits = room:obvious_exits();
endif

exits = this:checkexits(this:obvious_exits(), room, exits);
exits = this:findexits(room, exits);
this:tell_ways(exits);
```

