---
id: "#56"
name: $command_utils
owner: "#2"
parent: "#78"
location:
flags:
  - readable
aliases: []
created: 2026-05-24T15:10
updated: 2026-05-24T18:59
---

# $command_utils

## Verb: object_match_failed

```yaml
names: [object_match_failed]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
{match_result, string} = args;

if (index(string, "#") == 1 && $code_utils:toobj(string) != E_TYPE)
  "...avoid the `I don't know which `#-2' you mean' message...";
  if (!valid(match_result))
    player:tell(tostr(string, " does not exist."));
  endif
  return !valid(match_result);
elseif (match_result == $nothing)
  player:tell("You must give the name of some object.");
elseif (match_result == $failed_match)
  player:tell(tostr("I see no \"", string, "\" here."));
elseif (match_result == $ambiguous_match)
  player:tell(tostr("I don't know which \"", string, "\" you mean."));
elseif (!valid(match_result))
  player:tell(tostr(match_result, " does not exist."));
else
  return 0;
endif
return 1;
```

## Verb: read_lines

```yaml
names: [read_lines]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
{?max = 0} = args;
"c = callers();";
"p = c[$][5];";
player:notify(tostr("[Type", max ? tostr(" up to ", max) | "", " lines of input; use `.' to end or `@abort' to abort the command.]"));
ans = {};
while (1)
  try
    line = read();
    if ((line[1..min(6, $)] == "@abort") && ((tail = line[7..$]) == $string_utils:space(tail)))
      player:notify(">> Command Aborted <<");
      kill_task(task_id());
    elseif (!line || line[1] != ".")
      ans = {@ans, line};
    elseif ((tail = line[2..$]) == $string_utils:space(tail))
      return ans;
    else
      ans = {@ans, tail};
    endif
    if (max && length(ans) >= max)
      return ans;
    endif
  except error (ANY)
    return error[1];
  endtry
endwhile
```

## Verb: read_lines_escape

```yaml
names: [read_lines_escape]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
{escapes, ?help = "You are currently in a read loop."} = args;
"c = callers();";
"p = c[$][5];";
p = player;
escapes = {".", "@abort", @typeof(escapes) == LIST ? escapes | {escapes}};
p:notify(tostr("[Type lines of input; `?' for help; end with `", $string_utils:english_list(escapes, "", "' or `", "', `", ""), "'.]"));
ans = {};
escapes[1..0] = {"?"};
"... set up the help text...";
if (typeof(help) != LIST)
  help = {help};
endif
help[2..1] = {"Type `.' on a line by itself to finish.", "Anything else with a leading period is entered with the period removed.", "Type `@abort' to abort the command completely."};
while (1)
  try
    line = read();
    if ((trimline = $string_utils:trimr(line)) in escapes)
      if (trimline == ".")
        return {0, ans};
      elseif (trimline == "@abort")
        p:notify(">> Command Aborted <<");
        kill_task(task_id());
      elseif (trimline == "?")
        p:notify_lines(help);
      else
        return {trimline, ans};
      endif
    else
      if (line && line[1] == ".")
        line[1..1] = "";
      endif
      ans = {@ans, line};
    endif
  except error (ANY)
    return error[1];
  endtry
endwhile
```
