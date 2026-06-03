---
id: "#55"
name: $list_utils
owner: "#0"
parent: "#78"
location:
flags:
  - readable
aliases: []
updated: 2026-06-03T07:55:16-05:00
---

# $list_utils

## Verb: assoc

```yaml
names: ["assoc"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
{target, thelist, ?indx = 1} = args;
for t in (thelist)
  if (typeof(t) == LIST && `t[indx] == target ! E_RANGE')
    return t;
  endif
endfor
return {};
```

## Verb: make

```yaml
names: ["make"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
{n, ?elt = 0} = args;
if (n < 0)
  return E_INVARG;
endif
ret = {};
build = {elt};
while (1)
  if (n % 2)
    ret = {@ret, @build};
  endif
  if (n = n / 2)
    build = {@build, @build};
  else
    return ret;
  endif
endwhile
```

