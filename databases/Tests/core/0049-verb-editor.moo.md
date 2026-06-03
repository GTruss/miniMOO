---
id: "#49"
name: $verb_editor
owner: "#0"
parent: "#50"
location:
flags:
  - readable
  - fertile
aliases: []
updated: 2026-06-02T18:52:55-05:00
---

# $verb_editor

```yaml
name: active
type: list
value: []
flags:
  - readable
```

```yaml
name: changes
type: list
value: []
flags:
  - readable
```

```yaml
name: objects
type: list
value: []
flags:
  - readable
```

```yaml
name: texts
type: list
value: []
flags:
  - readable
```

```yaml
name: times
type: list
value: []
flags:
  - readable
```

```yaml
name: verbnames
type: list
value: []
flags:
  - readable
```

## Verb: invoke

```yaml
names: ["invoke"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
{command, ?source = "@edit", ?initial_code = 0} = args;
parsed = this:parse_invoke(command, source, initial_code);
if (!parsed)
  return;
elseif (typeof(parsed) == ERR)
  return parsed;
endif

{object, verbref, text} = parsed;
who = this:loaded(player);
if (!who)
  this.active = {@this.active, player};
  this.objects = {@this.objects, object};
  this.verbnames = {@this.verbnames, verbref};
  this.texts = {@this.texts, text};
  this.changes = {@this.changes, 0};
  this.times = {@this.times, time()};
  who = length(this.active);
else
  this.objects[who] = object;
  this.verbnames[who] = verbref;
  this.texts[who] = text;
  this.changes[who] = 0;
  this.times[who] = time();
endif

player:tell("Now editing ", this:working_on(who), ".");
player:tell("Type `help' for editor commands.");

while (1)
  line = read();
  cmdline = $string_utils:trim(line);
  words = $string_utils:words(cmdline);
  cmd = words ? words[1] | "";

  if (cmd == "" || cmd == "list" || cmd == "l" || cmd == "print" || cmd == "p")
    this:list(who);
  elseif (cmd == "help" || cmd == "?")
    this:help();
  elseif (cmd == "abort" || cmd == "@abort" || cmd == "quit" || cmd == "q")
    this:unload(who);
    player:tell("Editor aborted.");
    return;
  elseif (cmd == "compile" || cmd == "save" || cmd == ".")
    this:compile(who);
    return;
  elseif (cmd == "append" || cmd == "a")
    block = this:read_block();
    if (typeof(block) == ERR)
      this:unload(who);
      player:tell("Editor aborted.");
      return;
    endif
    this.texts[who] = {@this.texts[who], @block};
    this:set_changed(who, 1);
    player:tell(length(block), " line", length(block) == 1 ? "" | "s", " appended.");
  elseif (cmd == "insert" || cmd == "i")
    if (length(words) < 2 || typeof(n = toint(words[2])) == ERR || n < 1 || n > length(this.texts[who]) + 1)
      player:tell("Usage: insert <line-number>");
    else
      block = this:read_block();
      if (typeof(block) == ERR)
        this:unload(who);
        player:tell("Editor aborted.");
        return;
      endif
      text = this.texts[who];
      text[n..n - 1] = block;
      this.texts[who] = text;
      this:set_changed(who, 1);
      player:tell(length(block), " line", length(block) == 1 ? "" | "s", " inserted.");
    endif
  elseif (cmd == "delete" || cmd == "del" || cmd == "d")
    if (length(words) < 2 || typeof(n = toint(words[2])) == ERR || n < 1 || n > length(this.texts[who]))
      player:tell("Usage: delete <line-number>");
    else
      this.texts[who] = listdelete(this.texts[who], n);
      this:set_changed(who, 1);
      player:tell("Line deleted.");
    endif
  elseif (cmd == "replace" || cmd == "r")
    if (length(words) < 2 || typeof(n = toint(words[2])) == ERR || n < 1 || n > length(this.texts[who]))
      player:tell("Usage: replace <line-number>");
    else
      player:tell("Enter replacement line.");
      newline = read();
      if ($string_utils:trim(newline) == "@abort")
        this:unload(who);
        player:tell("Editor aborted.");
        return;
      endif
      this.texts[who][n] = newline;
      this:set_changed(who, 1);
      player:tell("Line replaced.");
    endif
  else
    player:tell("Unknown editor command. Type `help' for commands.");
  endif
endwhile
```

## Verb: parse_invoke

```yaml
names: ["parse_invoke"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
{command, source, ?initial_code = 0} = args;
vref = $string_utils:words(command);
if (!vref || !(spec = $code_utils:parse_verbref(vref[1])))
  player:tell("Usage: ", source, " <object>:<verb>");
  return 0;
endif

if ($command_utils:object_match_failed(object = player:my_match_object(spec[1]), spec[1]))
  return 0;
endif

vname = spec[2];
argspec = listdelete(vref, 1);
if (argspec)
  pas = $code_utils:parse_argspec(@argspec);
  if (typeof(pas) != LIST)
    player:tell(pas);
    return 0;
  elseif (pas[2])
    player:tell("I don't understand \"", $string_utils:from_list(pas[2], " "), "\"");
    return 0;
  endif

  fullargs = {@pas[1], "none", "none"}[1..3];
  if (!(fullargs[2] in {"none", "any"}))
    fullargs[2] = $code_utils:full_prep(fullargs[2]) || fullargs[2];
  endif

  vnum = $code_utils:find_verb_named(object, vname);
  while (vnum && verb_args(object, vnum) != fullargs)
    vnum = $code_utils:find_verb_named(object, vname, vnum + 1);
  endwhile
  if (!vnum)
    player:tell("That object does not define that verb with those args.");
    return 0;
  endif
  verbref = vnum;
  code_ref = vnum;
else
  verbref = vname;
  code_ref = vname;
endif

if (typeof(initial_code) == LIST)
  code = initial_code;
else
  code = `this:fetch_verb_code(object, code_ref) ! ANY';
endif

if (typeof(code) == ERR)
  player:tell(code != E_VERBNF ? code | "That object does not define that verb.");
  return code;
endif

return {object, verbref, code};
```

## Verb: fetch_verb_code

```yaml
names: ["fetch_verb_code"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
return verb_code(args[1], args[2]);
```

## Verb: set_verb_code

```yaml
names: ["set_verb_code"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
return set_verb_code(args[1], args[2], args[3]);
```

## Verb: loaded

```yaml
names: ["loaded"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
return args[1] in this.active;
```

## Verb: unload

```yaml
names: ["unload"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
who = args[1];
this.active = listdelete(this.active, who);
this.objects = listdelete(this.objects, who);
this.verbnames = listdelete(this.verbnames, who);
this.texts = listdelete(this.texts, who);
this.changes = listdelete(this.changes, who);
this.times = listdelete(this.times, who);
```

## Verb: working_on

```yaml
names: ["working_on"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
who = args[1];
object = this.objects[who];
verbref = this.verbnames[who];
if (typeof(verbref) == LIST)
  return tostr(object, ":", verbref[1], " (", $string_utils:from_list(verbref[2..$], " "), ")");
elseif (typeof(verbref) == INT)
  return tostr(object, ":", verbref);
else
  return tostr(object, ":", verbref);
endif
```

## Verb: set_changed

```yaml
names: ["set_changed"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
who = args[1];
this.changes[who] = args[2];
this.times[who] = time();
```

## Verb: read_block

```yaml
names: ["read_block"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
player:tell("Enter lines; `.' to finish or `@abort' to abort.");
lines = {};
while (1)
  line = read();
  trimline = $string_utils:trim(line);
  if (trimline == ".")
    return lines;
  elseif (trimline == "@abort")
    return E_INVARG;
  else
    lines = {@lines, line};
  endif
endwhile
```

## Verb: list

```yaml
names: ["list"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
text = this.texts[args[1]];
if (!text)
  player:tell("(no lines)");
else
  for i in [1..length(text)]
    player:tell(i, ": ", text[i]);
  endfor
endif
```

## Verb: compile

```yaml
names: ["compile"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
who = args[1];
object = this.objects[who];
verbref = this.verbnames[who];
text = this.texts[who];
objverbname = this:working_on(who);

try
  result = this:set_verb_code(object, verbref, text);
  if (result)
    player:notify_lines(result);
    player:tell(length(result), " error(s).");
    player:tell(objverbname, " not compiled.");
  else
    player:tell("0 errors.");
    player:tell(objverbname, " successfully compiled.");
    this:unload(who);
  endif
except error (ANY)
  player:tell(error[2]);
  player:tell(objverbname, " not compiled.");
endtry
```

## Verb: help

```yaml
names: ["help"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
player:tell("Editor commands:");
player:tell("  list                 show the current verb code");
player:tell("  append               append lines; finish with `.'");
player:tell("  insert <n>           insert lines before line n; finish with `.'");
player:tell("  delete <n>           delete line n");
player:tell("  replace <n>          replace line n with one new line");
player:tell("  compile              save the verb code");
player:tell("  abort                leave without saving");
```

