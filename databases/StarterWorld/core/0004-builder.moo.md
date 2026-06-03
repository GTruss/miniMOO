---
id: "#4"
name: $builder
owner: "#0"
parent: "#88"
location:
flags:
  - readable
  - fertile
aliases: []
updated: 2026-06-03T07:55:16-05:00
---

# $builder

```yaml
name: build_options
type: list
value: []
flags:
  - readable
```

## Verb: build_option

```yaml
names: ["build_option"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
return $build_options:get(this.build_options, args[1]);
```

## Verb: _create

```yaml
names: ["_create"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
return `create(@args) ! ANY => E_NONE';
```

## Verb: @create

```yaml
names: ["@create"]
dobj: any
prep: any
iobj: any
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
parent = dobj;
if (!valid(parent))
  player:tell("Usage: @create <parent> [named [name:]alias,alias,...]");
  return;
endif
newobj = create(parent);
move(newobj, player);
if (iobjstr != "")
  $building_utils:set_names(newobj, iobjstr);
endif
player:tell("You now have ", newobj.name, " with object number ", newobj, " and parent ", parent.name, " (", parent, ").");
```

## Verb: @dig

```yaml
names: ["@dig"]
dobj: any
prep: any
iobj: any
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
nargs = length(args);
if (nargs == 1)
  room = args[1];
  exit_spec = "";
elseif (nargs >= 3 && args[2] == "to")
  exit_spec = args[1];
  room = $string_utils:from_list(args[3..$], " ");
elseif (argstr && !prepstr)
  room = argstr;
  exit_spec = "";
else
  player:notify(tostr("Usage:  ", verb, " <new-room-name>"));
  player:notify(tostr("    or  ", verb, " <exit-description> to <new-room-name-or-old-room-object-number>"));
  return;
endif
if (room != tostr(other_room = toobj(room)))
  room_kind = player:build_option("dig_room");
  if (room_kind == 0)
    room_kind = $room;
  endif
  other_room = player:_create(room_kind);
  if (typeof(other_room) == ERR)
    player:notify(tostr("Cannot create new room as a child of ", $string_utils:nn(room_kind), ": ", other_room, ".  See `help @build-options' for information on how to specify the kind of room this command tries to create."));
    return;
  endif
  for f in ($string_utils:char_list(player:build_option("create_flags") || ""))
    other_room.(f) = 1;
  endfor
  other_room.name = room;
  other_room.aliases = {room};
  move(other_room, $nothing);
  player:notify(tostr(other_room.name, " (", other_room, ") created."));
elseif (nargs == 1)
  player:notify("You can't dig a room that already exists!");
  return;
elseif (!valid(player.location) || !($room in $object_utils:ancestors(player.location)))
  player:notify(tostr("You may only use the ", verb, " command from inside a room."));
  return;
elseif (!valid(other_room) || !($room in $object_utils:ancestors(other_room)))
  player:notify(tostr(other_room, " doesn't look like a room to me..."));
  return;
endif
if (exit_spec)
  exit_kind = player:build_option("dig_exit");
  if (exit_kind == 0)
    exit_kind = $exit;
  endif
  exits = $string_utils:explode(exit_spec, "|");
  if (length(exits) < 1 || length(exits) > 2)
    player:notify("The exit-description must have the form");
    player:notify("     [name:]alias,...,alias");
    player:notify("or   [name:]alias,...,alias|[name:]alias,...,alias");
    return;
  endif
  do_recreate = !player:build_option("bi_create");
  to_ok = $building_utils:make_exit(exits[1], player.location, other_room, do_recreate, exit_kind);
  if (to_ok && length(exits) == 2)
    $building_utils:make_exit(exits[2], other_room, player.location, do_recreate, exit_kind);
  endif
endif
```

## Verb: @setprop/@set

```yaml
names: ["@setprop", "@set"]
dobj: any
prep: to
iobj: any
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
set_task_perms(player);
if (this != player)
  return player:tell(E_PERM);
endif
l = $code_utils:parse_propref(dobjstr);
if (l)
  dobj = player:my_match_object(l[1], player.location);
  if ($command_utils:object_match_failed(dobj, l[1]))
    return;
  endif
  prop = l[2];
  to_i = "to" in args;
  at_i = "at" in args;
  i = to_i && at_i ? min(to_i, at_i) | to_i || at_i;
  iobjstr = argstr[$string_utils:word_start(argstr)[i][2] + 1..$];
  iobjstr = $string_utils:trim(iobjstr);
  if (!iobjstr)
    try
      val = dobj.(prop) = "";
    except e (ANY)
      player:tell("Unable to set ", dobj, ".", prop, ": ", e[2]);
      return;
    endtry
    iobjstr = "\"\"";
    "elseif (iobjstr[1] == \"\\\"\")";
    "val = dobj.(prop) = iobjstr;";
    "iobjstr = \"\\\"\" + iobjstr + \"\\\"\";";
  else
    val = $string_utils:to_value(iobjstr);
    if (!val[1])
      player:tell("Could not parse: ", iobjstr);
      return;
    elseif (!$object_utils:has_property(dobj, prop))
      player:tell("That object does not define that property.");
      return;
    endif
    try
      val = dobj.(prop) = val[2];
    except e (ANY)
      player:tell("Unable to set ", dobj, ".", prop, ": ", e[2]);
      return;
    endtry
  endif
  player:tell("Property ", dobj, ".", prop, " set to ", $string_utils:print(val), ".");
else
  player:tell("Property ", dobjstr, " not found.");
endif
```

