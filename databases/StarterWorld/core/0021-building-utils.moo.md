---
id: "#21"
name: $building_utils
owner: "#0"
parent: "#78"
location:
flags:
  - readable
aliases: []
created: 2026-05-24T15:10
updated: 2026-05-24T15:10
---

# $building_utils

## Verb: parse_names

```yaml
names: [parse_names]
dobj: none
prep: none
iobj: none
owner: "#0"
flags: [readable, executable]
```

```csharp
spec = args[1];
colon = index(spec, ":");
if (colon)
  name = substr(spec, 1, colon - 1);
  alias_str = substr(spec, colon + 1);
else
  name = "";
  alias_str = spec;
endif
aliases = $string_utils:explode(alias_str, ",");
if (name == "")
  name = aliases[1];
endif
return {name, aliases};
```

## Verb: set_names

```yaml
names: [set_names]
dobj: none
prep: none
iobj: none
owner: "#0"
flags: [readable, executable]
```

```csharp
obj = args[1];
names = this:parse_names(args[2]);
name = names[1];
aliases = names[2];
set_name(obj, name);
i = 1;
while (i <= length(aliases))
  add_alias(obj, aliases[i]);
  i = i + 1;
endwhile
```

## Verb: make_exit

```yaml
names: [make_exit]
dobj: none
prep: none
iobj: none
owner: "#0"
flags: [readable, executable]
```

```csharp
{spec, source, dest, ?use_recycler, ?exit_kind = $exit} = args;
exit = player:_create(exit_kind);
if (typeof(exit) == ERR)
  player:notify(tostr("Cannot create new exit as a child of ", $string_utils:nn(exit_kind), ": ", exit, ".  See `help @build-options' for information on how to specify the kind of exit this command tries to create."));
  return;
endif
for f in ($string_utils:char_list(player:build_option("create_flags") || ""))
  exit.(f) = 1;
endfor
$building_utils:set_names(exit, spec);
exit.source = source;
exit.dest = dest;
source_ok = source:add_exit(exit);
dest_ok = dest:add_entrance(exit);
move(exit, $nothing);
via = $string_utils:from_value(setadd(exit.aliases, exit.name), 1);
if (source_ok)
  player:tell("Exit from ", source.name, " (", source, ") to ", dest.name, " (", dest, ") via ", via, " created with id ", exit, ".");
  if (!dest_ok)
    player:tell("However, I couldn't add ", exit, " as a legal entrance to ", dest.name, ".  You may have to get its owner, ", dest.owner.name, " to add it for you.");
  endif
  return {exit};
elseif (dest_ok)
  player:tell("Exit to ", dest.name, " (", dest, ") via ", via, " created with id ", exit, ".  However, I couldn't add ", exit, " as a legal exit from ", source.name, ".  Get its owner, ", source.owner.name, " to add it for you.");
  return {exit};
else
  "player:_recycle(exit);";
  player:tell("I couldn't add a new exit as EITHER a legal exit from ", source.name, " OR as a legal entrance to ", dest.name, ".  Get their owners, ", source.owner.name, " and ", dest.owner.name, ", respectively, to add it for you.");
  return 0;
endif
```
