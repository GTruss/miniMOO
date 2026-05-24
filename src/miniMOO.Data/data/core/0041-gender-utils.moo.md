---
id: "#41"
name: $gender_utils
owner: "#0"
parent: "#78"
location:
flags:
  - readable
aliases: []
created: 2026-05-24T15:10
updated: 2026-05-24T15:10
---

# $gender_utils

```yaml
name: genders
type: list
value: [neuter, male, female, either, spivak, splat, plural, egotistical, royal, 2nd]
```

```yaml
name: is_plural
type: list
value: [0, 0, 0, 0, 0, 0, 1, 0, 1, 0]
```

```yaml
name: have
type: list
value: [has, has, has, has, has, has, have, have, have, have]
```

```yaml
name: be
type: list
value: [is, is, is, is, is, is, are, are, are, are]
```

## Verb: get_conj

```yaml
names: [get_conj, get_conjugation]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
{spec, ?object = player} = args;
i = index(spec + "/", "/");
sing = spec[1..i - 1];
if (i < length(spec))
  plur = spec[i + 1..$];
else
  plur = "";
endif
cap = "a" > ((i == 1) ? spec[2] | spec);
if (((valid(object) && (STR == typeof(g = `object.gender ! ANY => ""'))) && (i = g in this.genders)) && this.is_plural[i])
  vb = plur || this:_verb_plural(sing, i);
else
  vb = sing || this:_verb_singular(plur, i);
endif
if (cap)
  return $string_utils:capitalize(vb);
else
  return vb;
endif
```

## Verb: _verb_plural

```yaml
names: [_verb_plural]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
{st, idx} = args;
if (typeof(st) != STR)
  return E_INVARG;
endif
len = length(st);
if ((len >= 3) && (st[len - 2..$] == "n't"))
  return this:_verb_plural(st[1..len - 3], idx) + "n't";
elseif (i = st in {"has", "is"})
  return this.({"have", "be"}[i])[idx];
elseif (st == "was")
  return (idx > 6) ? "were" | st;
elseif ((len <= 3) || (st[len] != "s"))
  return st;
elseif (st[len - 1] != "e")
  return st[1..len - 1];
elseif ((len >= 4) && (st[len - 3..$] == "zzes"))
  return st[1..len - 3];
elseif ((len >= 4) && ((((st[len - 2] == "h") && index("cs", st[len - 3])) || index("ox", st[len - 2])) || (st[len - 3..len - 2] == "ss")))
  return st[1..len - 2];
elseif (st[len - 2] == "i")
  return st[1..len - 3] + "y";
else
  return st[1..len - 1];
endif
```

## Verb: _verb_singular

```yaml
names: [_verb_singular]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
{st, ?idx = 1} = args;
if (typeof(st) != STR)
  return E_INVARG;
endif
len = length(st);
if (!len)
  return "";
elseif ((len >= 3) && (st[len - 2..$] == "n't"))
  return this:_verb_singular(st[1..len - 3], idx) + "n't";
elseif (i = st in {"have", "are"})
  return this.({"have", "be"}[i])[idx];
elseif ((len > 1) && (st[len] == "y") && (!index("aeiou", st[len - 1])))
  return st[1..len - 1] + "ies";
elseif ((len > 1) && index("sz", st[len]) && index("aeiou", st[len - 1]))
  return (st + st[len]) + "es";
elseif (index("osx", st[len]) || ((len > 1) && (index("chsh", st[len - 1..len]) % 2)))
  return st + "es";
else
  return st + "s";
endif
```
