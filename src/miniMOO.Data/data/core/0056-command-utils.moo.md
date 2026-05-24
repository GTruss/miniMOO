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
updated: 2026-05-24T15:10
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
