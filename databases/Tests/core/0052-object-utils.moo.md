---
id: "#52"
name: $object_utils
owner: "#0"
parent: "#78"
location:
flags:
  - readable
aliases: []
updated: 2026-06-02T18:52:55-05:00
---

# $object_utils

## Verb: has_callable_verb

```yaml
names: ["has_callable_verb"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
{object, verbname} = args;
while (valid(object))
  if (`index(verb_info(object, verbname)[2], "x") ! E_VERBNF' && verb_code(object, verbname))
    return {object};
  endif
  object = parent(object);
endwhile
return 0;
```

## Verb: accessible_verbs

```yaml
names: ["accessible_verbs"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
thing = args[1];
verbs = {};
i = 1;
while ((info = `verb_info(thing, i) ! ANY') != E_VERBNF)
  verbs = {@verbs, info ? info[3] | E_PERM};
  i = i + 1;
endwhile
return verbs;
```

## Verb: ancestors

```yaml
names: ["ancestors"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
ret = {};
for o in (args)
  what = o;
  while (valid(what = parent(what)))
    ret = setadd(ret, what);
  endwhile
endfor
return ret;
```

## Verb: isa

```yaml
names: ["isa"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
what = args[1];
targ = args[2];
while (valid(what))
  if (what == targ)
    return 1;
  endif
  what = parent(what);
endwhile
return 0;
```

## Verb: contains

```yaml
names: ["contains"]
dobj: none
prep: none
iobj: none
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
loc = args[1];
what = args[2];
while (valid(what))
  what = what.location;
  if (what == loc)
    return 1;
  endif
endwhile
return 0;
```

## Verb: locations

```yaml
names: ["locations"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
ret = {};
what = args[1];
while (valid(what = what.location))
  ret = {@ret, what};
endwhile
return ret;
```

## Verb: descendants/descendents

```yaml
names: ["descendants", "descendents"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
r = children(args[1]);
i = 1;
while (i <= length(r))
  kids = children(r[i]);
  if (kids)
    r = {@r, @kids};
  endif
  i = i + 1;
endwhile
return r;
```

## Verb: isoneof

```yaml
names: ["isoneof"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
what = args[1];
targ = args[2];
while (valid(what))
  i = 1;
  while (i <= length(targ))
    if (what == targ[i])
      return 1;
    endif
    i = i + 1;
  endwhile
  what = parent(what);
endwhile
return 0;
```

## Verb: isoneof

```yaml
names: ["isoneof"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
what = args[1];
targ = args[2];
while (valid(what))
  i = 1;
  while (i <= length(targ))
    if (what == targ[i])
      return 1;
    endif
    i = i + 1;
  endwhile
  what = parent(what);
endwhile
return 0;
```

## Verb: has_verb

```yaml
names: ["has_verb"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
":has_verb(OBJ object, STR verbname)";
"Find out if an object has a verb matching the given verbname.";
"Returns {location} if so, 0 if not, where location is the object or the ancestor on which the verb is actually defined.";
{object, verbname} = args;
while (valid(object))
  try
    if (verb_info(object, verbname))
      return {object};
    endif
  except (E_VERBNF)
    object = parent(object);
  endtry
endwhile
return 0;
```

## Verb: has_property

```yaml
names: ["has_property"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
{object, prop} = args;
try
  object.(prop);
  return 1;
except (E_PROPNF, E_INVIND)
  return 0;
endtry
```

## Verb: all_properties/all_verbs

```yaml
names: ["all_properties", "all_verbs"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
what = args[1];
return verb == "all_verbs" ? all_verbs(what) | all_properties(what);
```

