---
id: "#59"
name: $code_utils
owner: "#0"
parent: "#78"
location:
flags:
  - readable
aliases: []
created: 2026-05-24T15:10
updated: 2026-05-24T22:16
---

# $code_utils

```yaml
name: prepositions
type: list
value: ["with/using", "at/to", "in front of", "in/inside/into", "on top of/on/onto/upon", "out of/from inside/from", over, through, "under/underneath/beneath", behind, beside, "for/about", is, as, "off/off of"]
```

```yaml
name: error_names
type: list
value: ["E_NONE", "E_TYPE", "E_DIV", "E_PERM", "E_PROPNF", "E_VERBNF", "E_VARNF", "E_INVIND", "E_RECMOVE", "E_MAXREC", "E_RANGE", "E_ARGS", "E_NACC", "E_INVARG", "E_QUOTA", "E_FLOAT"]
```

```yaml
name: _short_preps
type: list
value: [with, to, "in front of", in, on, from, over, through, under, behind, beside, for, is, as, off]
```

```yaml
name: _other_preps
type: list
value: [using, at, inside, into, "on top of", onto, upon, "out of", "from inside", underneath, beneath, about, "off of"]
```

```yaml
name: _multi_preps
type: list
value: [off, from, out, on, "on top", in, "in front"]
```

```yaml
name: _other_preps_n
type: list
value: [1, 2, 4, 4, 5, 5, 5, 6, 6, 9, 9, 12, 15]
```

## Verb: error_name

```yaml
names: [error_name]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
return toliteral(@args);
return this.error_names[toint(args[1]) + 1];
```

## Verb: toerr

```yaml
names: [toerr]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
if (typeof(s = args[1]) != STR)
  n = toint(s) + 1;
  if (n > length(this.error_list))
    return 1;
  endif
elseif (!(n = s in this.error_names || "E_" + s in this.error_names))
  return 1;
endif
return this.error_list[n];
```

## Verb: toobj

```yaml
names: [toobj]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
return match(s = args[1], "^ *#[-+]?[0-9]+ *$") ? toobj(s) | E_TYPE;;
```

## Verb: parse_verbref

```yaml
names: [parse_verbref]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
s = args[1];
if (colon = index(s, ":"))
  object = s[1..colon - 1];
  verbname = s[colon + 1..$];
  if (!(object && verbname))
    return 0;
  elseif (object[1] == "$")
    pname = object[2..$];
    if (!(pname in properties(#0)) || typeof(object = #0.(pname)) != OBJ)
      return 0;
    endif
    object = tostr(object);
  endif
  return {object, verbname};
else
  return 0;
endif
```

## Verb: parse_propref

```yaml
names: [parse_propref]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
s = args[1];
if (dot = index(s, "."))
  object = s[1..dot - 1];
  prop = s[dot + 1..$];
  if (object == "" || prop == "")
    return 0;
  elseif (object[1] == "$")
    object = `#0.(object[2..$]) ! ANY';
    if (typeof(object) != OBJ)
      return 0;
    endif
    object = tostr(object);
  endif
elseif (index(s, "$") == 1)
  object = "#0";
  prop = s[2..$];
else
  return 0;
endif
return {object, prop};
```

## Verb: get_prep

```yaml
names: [get_prep]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
prep = "";
allpreps = {@this._short_preps, @this._other_preps};
rest = 1;
for i in [1..length(args)]
  accum = i == 1 ? args[1] | tostr(accum, " ", args[i]);
  if (accum in allpreps)
    prep = accum;
    rest = i + 1;
  endif
  if (!(accum in this._multi_preps))
    return {prep, @args[rest..$]};
  endif
endfor
return {prep, @args[rest..$]};
```

## Verb: full_prep

```yaml
names: [full_prep]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
prep = args[1];
if (p = prep in this._short_preps)
  return this.prepositions[p];
elseif (p = prep in this._other_preps)
  return this.prepositions[this._other_preps_n[p]];
else
  return "";
endif
```

## Verb: verbname_match

```yaml
names: [verbname_match]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
verblist = " " + args[1] + " ";
if (index(verblist, " " + (name = args[2]) + " ") && !(index(name, "*") || index(name, " ")))
  "Note that if name has a * or a space in it, then it can only match one of the * verbnames";
  return 1;
else
  namelen = length(name);
  while (star = index(verblist, "*"))
    vstart = rindex(verblist[1..star], " ") + 1;
    vlast = vstart + index(verblist[vstart..$], " ") - 2;
    if (namelen >= star - vstart && (!(v = strsub(verblist[vstart..vlast], "*", "")) || index(v, verblist[vlast] == "*" ? name[1..min(namelen, length(v))] | name) == 1))
      return 1;
    endif
    verblist = verblist[vlast + 1..$];
  endwhile
endif
return 0;
```

## Verb: find_verb_named

```yaml
names: [find_verb_named]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
":find_verb_named(object,name[,n])";
"  returns the *number* of the first verb on object matching the given name.";
"  optional argument n, if given, starts the search with verb n,";
"  causing the first n verbs (1..n-1) to be ignored.";
"  0 is returned if no verb is found.";
"  This routine does not find inherited verbs.";
{object, name, ?start = 1} = args;
for i in [start..length(verbs(object))]
  verbinfo = verb_info(object, i);
  if (this:verbname_match(verbinfo[3], name))
    return i;
  endif
endfor
return 0;
```

## Verb: parse_argspec

```yaml
names: [parse_argspec]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
nargs = length(args);
if (nargs < 1)
  return {{}, {}};
elseif ((ds = args[1]) == "tnt")
  return {{"this", "none", "this"}, listdelete(args, 1)};
elseif (!(ds in {"this", "any", "none"}))
  return tostr("\"", ds, "\" is not a valid direct object specifier.");
elseif (nargs < 2 || args[2] in {"none", "any"})
  verbargs = args[1..min(3, nargs)];
  rest = args[4..nargs];
elseif (!(gp = $code_utils:get_prep(@args[2..nargs]))[1])
  return tostr("\"", args[2], "\" is not a valid preposition.");
else
  verbargs = {ds, @gp[1..min(2, nargs = length(gp))]};
  rest = gp[3..nargs];
endif
if (length(verbargs) >= 3 && !(verbargs[3] in {"this", "any", "none"}))
  return tostr("\"", verbargs[3], "\" is not a valid indirect object specifier.");
endif
return {verbargs, rest};
```

## Verb: explain_verb_syntax

```yaml
names: [explain_verb_syntax]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
if (args[4..5] == {"none", "this"})
  return 0;
endif
{thisobj, verb, adobj, aprep, aiobj} = args;
prep_part = aprep == "any" ? "to" | this:short_prep(aprep);
".........`any' => `to' (arbitrary),... `none' => empty string...";
if (adobj == "this" && dobj == thisobj)
  dobj_part = dobjstr;
  iobj_part = !prep_part || aiobj == "none" ? "" | (aiobj == "this" ? dobjstr | iobjstr);
elseif (aiobj == "this" && iobj == thisobj)
  dobj_part = adobj == "any" ? dobjstr | (adobj == "this" ? iobjstr | "");
  iobj_part = iobjstr;
elseif (!("this" in args[3..5]))
  dobj_part = adobj == "any" ? dobjstr | "";
  iobj_part = prep_part && aiobj == "any" ? iobjstr | "";
else
  return 0;
endif
return tostr(verb, dobj_part ? " " + dobj_part | "", prep_part ? " " + prep_part | "", iobj_part ? " " + iobj_part | "");
```
