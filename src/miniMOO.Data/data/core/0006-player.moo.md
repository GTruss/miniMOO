---
id: "#6"
name: $player
owner: "#0"
parent: "#1"
location:
flags:
  - readable
  - fertile
aliases: []
created: 2026-05-24T14:35
updated: 2026-05-24T16:36
---

# $player

Generic player prototype.

```yaml
name: description
type: string
value: A nondescript person.
```

```yaml
name: namec
type: string
value: generic player
```

```yaml
name: brief
type: integer
value: 0
```

```yaml
name: linelen
type: integer
value: -79
```

```yaml
name: display_options
type: list
value: []
```

## Verb: titlec

```yaml
names: [titlec]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
return `this.namec ! E_PROPNF => this:title()';
```

## Verb: moveto

```yaml
names: [moveto]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
if (args[1] == #-1)
  return E_INVARG;
  this:notify("You are now in #-1, The Void.  Type `home' to get back.");
endif
pass(@args);
```

## Verb: @sethome

```yaml
names: ["@sethome"]
dobj: none
prep: none
iobj: none
owner: "#0"
flags: [readable, executable]
```

```csharp
here = this.location;
if (!$object_utils:has_callable_verb(here, "accept_for_abode"))
  player:notify("This is a pretty odd place.  You should make your home in an actual room.");
elseif (here:accept_for_abode(this))
  this.home = here;
  player:notify(tostr(here.name, " is your new home."));
else
  player:notify(tostr("This place doesn't want to be your home.  Contact ", here.owner.name, " to be added to the residents list of this place, or choose another place as your home."));
endif
```

## Verb: home

```yaml
names: [home]
dobj: none
prep: none
iobj: none
owner: "#0"
flags: [readable, executable]
```

```csharp
start = this.location;
if (start == this.home)
  player:tell("You're already home!");
  return;
elseif (typeof(this.home) != OBJ)
  player:tell("You've got a weird home, pal.  I've reset it to the default one.");
  this.home = $player_start;
elseif (!valid(this.home))
  player:tell("Oh no!  Your home's been recycled.  Time to look around for a new one.");
  this.home = $player_start;
else
  player:tell("You click your heels three times.");
endif
this:moveto(this.home);
if (!valid(start))
elseif (start == this.location)
  start:announce(player.name, " ", $gender_utils:get_conj("learns", player), " that you can never go home...");
else
  try
    start:announce(player.name, " ", $gender_utils:get_conj("goes", player), " home.");
  except e (E_VERBNF)
    "start did not support announce";
  endtry
endif
if (this.location == this.home)
  this.location:announce(player.name, " ", $gender_utils:get_conj("comes", player), " home.");
elseif (this.location == start)
  player:tell("Either home doesn't want you, or you don't really want to go.");
else
  player:tell("Wait a minute!  This isn't your home...");
  if (valid(this.location))
    this.location:announce(player.name, " ", $gender_utils:get_conj("arrives", player), ", looking quite bewildered.");
  endif
endif
```

## Verb: tell_lines

```yaml
names: [tell_lines]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
lines = args[1];
if (typeof(lines) != LIST)
  lines = {lines};
endif
this:notify_lines(lines);
```

## Verb: linelen

```yaml
names: [linelen]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
return abs(this.linelen);
```

## Verb: display_option

```yaml
names: [display_option]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
return $display_options:get(this.display_options, args[1]);
```

## Verb: tell_contents

```yaml
names: [tell_contents]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
c = args[1];
if (c)
  longear = {};
  gear = {};
  width = player:linelen();
  half = width / 2;
  player:tell("Carrying:");
  for thing in (c)
    cx = tostr(" ", thing:title());
    if (length(cx) > half)
      longear = {@longear, cx};
    else
      gear = {@gear, cx};
    endif
  endfor
  player:tell_lines($string_utils:columnize(gear, 2, width));
  player:tell_lines(longear);
endif
```

## Verb: notify_lines

```yaml
names: [notify_lines]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
for line in (typeof(lines = args[1]) != LIST ? {lines} | lines)
  this:notify(tostr(line));
endfor
```

## Verb: @describe

```yaml
names: ["@describe", "@desc"]
dobj: any
prep: as
iobj: any
owner: "#0"
flags: [readable, executable]
```

```csharp
dobj = player:my_match_object(dobjstr);
if ($command_utils:object_match_failed(dobj, dobjstr))
  "...lose...";
elseif (e = dobj:set_description(iobjstr))
  player:notify("Description set.");
else
  player:notify(tostr(e));
endif
```

## Verb: my_match_object

```yaml
names: [my_match_object]
dobj: any
prep: none
iobj: none
owner: "#0"
flags: [readable, executable]
```

```csharp
return $string_utils:match_object(@{@args, this.location}[1..2], this);
```

## Verb: wave

```yaml
names: [wave]
dobj: none
prep: none
iobj: none
owner: "#0"
flags: [readable, executable]
```

```csharp
player:tell("You wave.");
player.location:announce(player.name, " waves.");
```

## Verb: inventory

```yaml
names: ["list inventory", i]
dobj: none
prep: none
iobj: none
owner: "#0"
flags: [readable, executable]
```

```csharp
if (c = player:contents())
  this:tell_contents(c);
else
  player:tell("You are empty-handed.");
endif
```

## Verb: @examine

```yaml
names: ["@examine", "@exam"]
dobj: any
prep: none
iobj: none
owner: "#0"
flags: [readable, executable]
```

```csharp
if (dobjstr == "")
  player:notify(tostr("Usage:  ", verb, " <object>"));
  return;
endif
what = $string_utils:match_object(dobjstr, player.location);
if ($command_utils:object_match_failed(what, dobjstr))
  return;
endif
player:notify(tostr(what.name, " (", what, ") is owned by ", valid(what.owner) ? what.owner.name | "a recycled player", " (", what.owner, ")."));
player:notify(tostr("Aliases:  ", $string_utils:english_list(what.aliases)));
desc = what:description();
if (desc)
  player:notify_lines(desc);
else
  player:notify("(No description set.)");
endif
contents = what.contents;
if (contents)
  player:notify("Contents:");
  for item in (contents)
    player:notify(tostr("  ", item.name, " (", item, ")"));
  endfor
endif
"Use dobjstr, not shortest alias.";
name = dobjstr;
"name = what.name;";
"if (typeof(what.aliases) == LIST && what.aliases != {})";
"for alias in (what.aliases)";
"if (length(alias) <= length(name))";
"name = alias;";
"endif";
"endfor";
"endif";
vrbs = {};
commands_ok = what in {player, player.location};
dull_classes = {$root_class, $room, $player, $prog};
what = what;
printed_working_msg = 0;
while (what != $nothing)
  if (!(what in dull_classes))
    for i in [1..length(verbs(what))]
      info = verb_info(what, i);
      syntax = verb_args(what, i);
      if (index(info[2], "r") && (syntax[2..3] != {"none", "this"} && (commands_ok || "this" in syntax)) && verb_code(what, i))
        {dobj, prep, iobj} = syntax;
        if (syntax == {"any", "any", "any"})
          prep = "none";
        endif
        if (prep != "none")
          for x in ($string_utils:explode(prep, "/"))
            if (length(x) <= length(prep))
              prep = x;
            endif
          endfor
        endif
        "This is the correct way to handle verbs ending in *";
        vname = info[3];
        while (j = index(vname, "* "))
          vname = tostr(vname[1..j - 1], "<anything>", vname[j + 1..$]);
        endwhile
        if (vname[$] == "*")
          vname = vname[1..$ - 1] + "<anything>";
        endif
        vname = strsub(vname, " ", "/");
        rest = "";
        if (prep != "none")
          rest = " " + (prep == "any" ? "<anything>" | prep);
          if (iobj != "none")
            rest = tostr(rest, " ", iobj == "this" ? name | "<anything>");
          endif
        endif
        if (dobj != "none")
          rest = tostr(" ", dobj == "this" ? name | "<anything>", rest);
        endif
        vrbs = setadd(vrbs, "  " + vname + rest);
      endif
    endfor
  endif
  what = parent(what);
endwhile
if (vrbs)
   player:notify("Obvious Verbs:");
   player:notify_lines(vrbs);
   printed_working_msg && player:notify("(End of list.)");
 elseif (printed_working_msg)
   player:notify("No obvious verbs found.");
 endif
```
