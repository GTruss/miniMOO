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
updated: 2026-06-02T18:52:55-05:00
---

# $player

```yaml
name: brief
type: integer
value: 0
flags:
  - readable
```

```yaml
name: description
type: string
value: "A nondescript person."
flags:
  - readable
```

```yaml
name: display_options
type: list
value: []
flags:
  - readable
```

```yaml
name: features
type: list
value: []
flags:
  - readable
```

```yaml
name: linelen
type: integer
value: -79
flags:
  - readable
```

```yaml
name: namec
type: string
value: "generic player"
flags:
  - readable
```

## Verb: titlec

```yaml
names: ["titlec"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
return `this.namec ! E_PROPNF => this:title()';
```

## Verb: moveto

```yaml
names: ["moveto"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
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
flags:
  - readable
  - executable
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
names: ["home"]
dobj: none
prep: none
iobj: none
owner: "#0"
flags:
  - readable
  - executable
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
names: ["tell_lines"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
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
names: ["linelen"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
return abs(this.linelen);
```

## Verb: display_option

```yaml
names: ["display_option"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
return $display_options:get(this.display_options, args[1]);
```

## Verb: tell_contents

```yaml
names: ["tell_contents"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
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
names: ["notify_lines"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
for line in (typeof(lines = args[1]) != LIST ? {lines} | lines)
  this:notify(tostr(line));
endfor
```

## Verb: @describe/@desc

```yaml
names: ["@describe", "@desc"]
dobj: any
prep: as
iobj: any
owner: "#0"
flags:
  - readable
  - executable
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
names: ["my_match_object"]
dobj: any
prep: none
iobj: none
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
return $string_utils:match_object(@{@args, this.location}[1..2], this);
```

## Verb: wave

```yaml
names: ["wave"]
dobj: none
prep: none
iobj: none
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
player:tell("You wave.");
player.location:announce(player.name, " waves.");
```

## Verb: list inventory/i

```yaml
names: ["list inventory", "i"]
dobj: none
prep: none
iobj: none
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
if (c = player:contents())
  this:tell_contents(c);
else
  player:tell("You are empty-handed.");
endif
```

## Verb: @examine/@exam

```yaml
names: ["@examine", "@exam"]
dobj: any
prep: none
iobj: none
owner: "#0"
flags:
  - readable
  - executable
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

## Verb: @messages

```yaml
names: ["@messages"]
dobj: any
prep: none
iobj: none
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
set_task_perms(player);
if (dobjstr == "")
  player:notify(tostr("Usage:  ", verb, " <object>"));
  return;
endif
dobj = player:my_match_object(dobjstr);
if ($command_utils:object_match_failed(dobj, dobjstr))
  return;
endif
found_one = 0;
props = $object_utils:all_properties(dobj);
if (typeof(props) == ERR)
  player:notify("You can't read the messages on that.");
  return;
endif
for pname in (props)
  len = length(pname);
  if (len > 4 && pname[len - 3..len] == "_msg")
    found_one = 1;
    msg = `dobj.(pname) ! ANY';
    if (msg == E_PERM)
      value = "isn't readable by you.";
    elseif (!msg)
      value = "isn't set.";
    elseif (typeof(msg) == LIST)
      value = "is a list.";
    elseif (typeof(msg) != STR)
      value = "is corrupted! **";
    else
      value = "is " + $string_utils:print(msg);
    endif
    player:notify(tostr("@", pname[1..len - 4], " ", dobjstr, " ", value));
  endif
endfor
if (!found_one)
  player:notify("That object doesn't have any messages to set.");
endif
```

## Verb: my_huh

```yaml
names: ["my_huh"]
dobj: any
prep: none
iobj: none
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
verb = args[1];
pass = args[2];
plist = {"any", prepstr ? $code_utils:full_prep(prepstr) | "none"};
dlist = dobjstr ? {"any"} | {"none", "any"};
ilist = iobjstr ? {"any"} | {"none", "any"};
for fobj in (this.features)
  if (!$recycler:valid(fobj))
    this:remove_feature(fobj);
  elseif (`valid(loc = $object_utils:has_callable_verb(fobj, verb)[1]) ! ANY => 0')
    vargs = verb_args(loc, verb);
    if (vargs[2] in plist && (vargs[1] in dlist && vargs[3] in ilist))
      "(got rid of notify_huh - should write a @which command)";
      "if (this.notify_huh)";
      "player:notify(tostr(\"Using \", what.name, \" (\", what, \")\"));";
      "endif";
      set_task_perms(permissions);
      fobj:(verb)(@pass);
      "Problem with verbs of the same name. If we use which=vrb in the loop instead, we have a problem with verbs that use the variable verb.";
      return 1;
    endif
  endif
  if ($command_utils:running_out_of_time())
    player:tell("You have too many features.  Parsing your command runs out of ticks while checking ", fobj.name, " (", fobj, ").");
    return 1;
  endif
endfor
```

## Verb: last_huh

```yaml
names: ["last_huh"]
dobj: any
prep: none
iobj: none
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
{command, command_args} = args;
if (command[1] == "@" && prepstr == "is")
  set_task_perms(player);
  $last_huh:(command)(@command_args);
  return 1;
elseif (command in {"give", "hand", "get", "take", "drop", "throw"})
  $last_huh:(command)(@command_args);
  return 1;
else
  return 0;
endif
```

## Verb: @desc*ribe

```yaml
names: ["@desc*ribe"]
dobj: any
prep: as
iobj: any
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
set_task_perms(player);
dobj = player:my_match_object(dobjstr);
if ($command_utils:object_match_failed(dobj, dobjstr))
  "...lose...";
elseif (e = dobj:set_description(iobjstr))
  player:notify("Description set.");
else
  player:notify(tostr(e));
endif
```

