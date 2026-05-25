---
id: "#20"
name: $string_utils
owner: "#0"
parent: "#78"
location:
flags:
  - readable
aliases: []
created: 2026-05-24T15:10
updated: 2026-05-24T21:02
---

# $string_utils

## Verb: explode

```yaml
names: [explode]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
"$string_utils:explode(subject [, break])";
"Return a list of those substrings of subject separated by runs of break[1].";
"break defaults to space.";
{subject, ?breakit = {" "}} = args;
breakit = breakit[1];
subject = subject + breakit;
parts = {};
while (subject)
  if ((i = index(subject, breakit)) > 1)
    parts = {@parts, subject[1..i - 1]};
  endif
  subject = subject[i + 1..$];
endwhile
return parts;
```

## Verb: english_list

```yaml
names: [english_list]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
{things, ?nothingstr = "nothing", ?andstr = " and ", ?commastr = ", ", ?finalcommastr = ","} = args;
nthings = length(things);
if (nthings == 0)
  return nothingstr;
elseif (nthings == 1)
  return tostr(things[1]);
elseif (nthings == 2)
  return tostr(things[1], andstr, things[2]);
else
  ret = "";
  for k in [1..nthings - 1]
    if (k == nthings - 1)
      commastr = finalcommastr;
    endif
    ret = tostr(ret, things[k], commastr);
  endfor
  return tostr(ret, andstr, things[nthings]);
endif
```

## Verb: regexp_quote

```yaml
names: [regexp_quote]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
string = args[1];
quoted = "";
while (m = rmatch(string, "[][$^.*+?%].*"))
  quoted = "%" + string[m[1]..m[2]] + quoted;
  string = string[1..m[1] - 1];
endwhile
return string + quoted;
```

## Verb: index_delimited

```yaml
names: [index_delimited, index_d]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
args[2] = "%(%W%|^%)" + $string_utils:regexp_quote(args[2]) + "%(%W%|$%)";
return (m = match(@args)) ? m[3][1][2] + 1 | 0;
```

## Verb: from_list

```yaml
names: [from_list]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
{thelist, ?separator = ""} = args;
if (separator == "")
  return tostr(@thelist);
elseif (thelist)
  result = tostr(thelist[1]);
  for elt in (listdelete(thelist, 1))
    result = tostr(result, separator, elt);
  endfor
  return result;
else
  return "";
endif
```

## Verb: columnize

```yaml
names: [columnize, columnise]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
{items, n, ?width = 79} = args;
height = (length(items) + n - 1) / n;
items = {@items, @$list_utils:make(height * n - length(items), "")};
colwidths = {};
for col in [1..n - 1]
  colwidths = listappend(colwidths, 1 - (width + 1) * col / n);
endfor
result = {};
for row in [1..height]
  line = tostr(items[row]);
  for col in [1..n - 1]
    line = tostr(this:left(line, colwidths[col]), " ", items[row + col * height]);
  endfor
  result = listappend(result, line[1..min($, width)]);
endfor
return result;
```

## Verb: left

```yaml
names: [left]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
{text, len, ?fill = " "} = args;
abslen = abs(len);
out = tostr(text);
if (length(out) < abslen)
  return out + this:space(length(out) - abslen, fill);
else
  return len > 0 ? out | out[1..abslen];
endif
```

## Verb: centre

```yaml
names: ["centre", "center"]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
{text, len, ?lfill = " ", ?rfill = lfill} = args;
out = tostr(text);
abslen = abs(len);
if (length(out) < abslen)
  return this:space((abslen - length(out)) / 2, lfill) + out + this:space((abslen - length(out) + 1) / -2, rfill);
else
  return len > 0 ? out | out[1..abslen];
endif
```

## Verb: right

```yaml
names: [right]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
{text, len, ?fill = " "} = args;
abslen = abs(len);
out = tostr(text);
if ((lenout = length(out)) < abslen)
  return this:space(abslen - lenout, fill) + out;
else
  return len > 0 ? out | out[$ - abslen + 1..$];
endif
```

## Verb: trim

```yaml
names: [trim]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
{string, ?space = " "} = args;
m = match(string, tostr("[^", space, "]%(.*[^", space, "]%)?%|$"));
return string[m[1]..m[2]];
```

## Verb: triml

```yaml
names: [triml]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
{string, ?what = " "} = args;
m = match(string, tostr("[^", what, "]%|$"));
return string[m[1]..$];
 ```

## Verb: trimr

```yaml
names: [trimr]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
{string, ?what = " "} = args;
return string[1..rmatch(string, tostr("[^", what, "]%|^"))[2]];
 ```

## Verb: space

```yaml
names: [space]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
{n, ?fill = " "} = args;
if (typeof(n) == STR)
  n = length(n);
endif
if (n > 1000)
  "Prevent someone from crashing the moo with $string_utils:space($maxint)";
  return E_INVARG;
endif
if (" " != fill)
  fill = fill + fill;
  fill = fill + fill;
  fill = fill + fill;
elseif ((n = abs(n)) < 70)
  return "                                                                      "[1..n];
else
  fill = "                                                                      ";
endif
m = (n - 1) / length(fill);
while (m)
  fill = fill + fill;
  m = m / 2;
endwhile
return n > 0 ? fill[1..n] | fill[$ + 1 + n..$];
```

## Verb: from_value

```yaml
names: [from_value]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
{value, ?quote_strings = 0, ?list_depth = 1} = args;
if (typeof(value) == LIST)
  if (value)
    if (list_depth)
      result = "{" + this:from_value(value[1], quote_strings, list_depth - 1);
      for v in (listdelete(value, 1))
        result = tostr(result, ", ", this:from_value(v, quote_strings, list_depth - 1));
      endfor
      return result + "}";
    else
      return "{...}";
    endif
  else
    return "{}";
  endif
elseif (quote_strings)
  if (typeof(value) == STR)
    result = "\"";
    while (q = index(value, "\"") || index(value, "\\"))
      if (value[q] == "\"")
        q = min(q, index(value + "\\", "\\"));
      endif
      result = result + value[1..q - 1] + "\\" + value[q];
      value = value[q + 1..$];
    endwhile
    return result + value + "\"";
  elseif (typeof(value) == ERR)
    return $code_utils:error_name(value);
  else
    return tostr(value);
  endif
else
  return tostr(value);
endif
```

## Verb: to_value

```yaml
names: [to_value]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
result = this:_tolist(string = args[1] + "}");
if (result[1] && result[1] != $string_utils:space(result[1]))
  return {0, tostr("after char ", length(string) - result[1], ":  ", result[2])};
elseif (typeof(result[1]) == INT)
  return {0, "missing } or \""};
elseif (length(result[2]) > 1)
  return {0, "comma unexpected."};
elseif (result[2])
  return {1, result[2][1]};
else
  return {0, "missing expression"};
endif
```

## Verb: _tolist

```yaml
names: [_tolist]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
rest = this:triml(args[1]);
vlist = {};
if (!rest)
  return {0, {}};
elseif (rest[1] == "}")
  return {rest[2..$], {}};
endif
while (1)
  rlen = length(rest);
  if (w = index("{\"", rest[1]))
    result = this:({"_tolist", "_unquote"}[w])(rest[2..rlen]);
    if (typeof(result[1]) == INT)
      return result;
    endif
    vlist = {@vlist, result[2]};
    rest = result[1];
  else
    thing = rest[1..tlen = min(index(rest + ",", ","), index(rest + "}", "}")) - 1];
    if (typeof(s = this:_toscalar(thing)) == STR)
      return {rlen, s};
    endif
    vlist = {@vlist, s};
    rest = rest[tlen + 1..rlen];
  endif
  if (!rest)
    return {0, vlist};
  elseif (rest[1] == "}")
    return {rest[2..$], vlist};
  elseif (rest[1] == ",")
    rest = this:triml(rest[2..$]);
  else
    return {length(rest), ", or } expected"};
  endif
endwhile
```

## Verb: _toscalar

```yaml
names: [_toscalar]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
thing = args[1];
if (!thing)
  return "missing value";
elseif (match(thing, "^#?[-+]?[0-9]+ *$"))
  return thing[1] == "#" ? toobj(thing) | toint(thing);
elseif (match(thing, "^[-+]?%([0-9]+%.[0-9]*%|[0-9]*%.[0-9]+%)%(e[-+]?[0-9]+%)? *$"))
  "matches 2. .2 3.2 3.2e3 .2e-3 3.e3";
  return `tofloat(thing) ! E_INVARG => tostr("Bad floating point value: ", thing)';
elseif (match(thing, "^[-+]?[0-9]+e[-+]?[0-9]+ *$"))
  "matches 345e4. No decimal, but has an e so still a float";
  return `tofloat(thing) ! E_INVARG => tostr("Bad floating point value: ", thing)';
elseif (thing[1] == "E")
  return (e = $code_utils:toerr(thing)) ? tostr("unknown error code `", thing, "'") | e;
elseif (thing[1] == "#")
  return tostr("bogus objectid `", thing, "'");
else
  return tostr("`", thing[1], "' unexpected");
endif
```

## Verb: prefix_to_value

```yaml
names: [prefix_to_value]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
alen = length(args[1]);
slen = length(string = this:triml(args[1]));
if (!string)
  return {0, "empty string"};
elseif (w = index("{\"", string[1]))
  result = this:({"_tolist", "_unquote"}[w])(string[2..slen]);
  if (typeof(result[1]) != INT)
    return result;
  elseif (result[1] == 0)
    return {0, "missing } or \""};
  else
    return {0, result[2], alen - result[1] + 1};
  endif
else
  thing = string[1..tlen = index(string + " ", " ") - 1];
  if (typeof(s = this:_toscalar(thing)) != STR)
    return {string[tlen + 1..slen], s};
  else
    return {0, s, alen - slen + 1};
  endif
endif
```

## Verb: _unquote

```yaml
names: [_unquote]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
rest = args[1];
result = "";
while (m = match(rest, "\\.?%|\""))
  "Find the next special character";
  if (rest[pos = m[1]] == "\"")
    return {rest[pos + 1..$], result + rest[1..pos - 1]};
  endif
  result = result + rest[1..pos - 1] + rest[pos + 1..m[2]];
  rest = rest[m[2] + 1..$];
endwhile
return {0, result + rest};
```

## Verb: words

```yaml
names: [words]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
rest = args[1];
"...trim leading blanks...";
if (0)
  rest[1..match(rest, "^ *")[2]] = "";
endif
rest = $string_utils:triml(rest);
if (!rest)
  return {};
endif
quote = 0;
toklist = {};
token = "";
pattern = " +%|\\.?%|\"";
while (m = match(rest, pattern))
  "... find the next occurence of a special character, either";
  "... a block of spaces, a quote or a backslash escape sequence...";
  char = rest[m[1]];
  token = token + rest[1..m[1] - 1];
  if (char == " ")
    toklist = {@toklist, token};
    token = "";
  elseif (char == "\"")
    "... beginning or end of quoted string...";
    "... within a quoted string spaces aren't special...";
    pattern = (quote = !quote) ? "\\.?%|\"" | " +%|\\.?%|\"";
  elseif (m[1] < m[2])
    "... char has to be a backslash...";
    "... include next char literally if there is one";
    token = token + rest[m[2]];
  endif
  rest[1..m[2]] = "";
endwhile
return rest || char != " " ? {@toklist, token + rest} | toklist;
```

## Verb: word_start

```yaml
names: [word_start]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
rest = args[1];
wstart = match(rest, "[^ ]%|$")[1];
wbefore = wstart - 1;
rest[1..wbefore] = "";
if (!rest)
  return {};
endif
quote = 0;
wslist = {};
pattern = " +%|\\.?%|\"";
while (m = match(rest, pattern))
  "... find the next occurence of a special character, either";
  "... a block of spaces, a quote or a backslash escape sequence...";
  char = rest[m[1]];
  if (char == " ")
    wslist = {@wslist, {wstart, wbefore + m[1] - 1}};
    wstart = wbefore + m[2] + 1;
  elseif (char == "\"")
    "... beginning or end of quoted string...";
    "... within a quoted string spaces aren't special...";
    pattern = (quote = !quote) ? "\\.?%|\"" | " +%|\\.?%|\"";
  endif
  rest[1..m[2]] = "";
  wbefore = wbefore + m[2];
endwhile
return rest || char != " " ? {@wslist, {wstart, wbefore + length(rest)}} | wslist;
```

## Verb: char_list

```yaml
names: [char_list]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
if (30 < (len = length(string = args[1])))
  return {@this:char_list(string[1..$ / 2]), @this:char_list(string[$ / 2 + 1..$])};
else
  l = {};
  for c in [1..len]
    l = {@l, string[c]};
  endfor
  return l;
endif
```

## Verb: capitalize

```yaml
names: [capitalize, capitalise]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
string = args[1];
if (string && (i = index("abcdefghijklmnopqrstuvwxyz", string[1], 1)))
  string[1] = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[i];
endif
return string;
```

## Verb: print

```yaml
names: [print]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
return toliteral(args[1]);
value = args[1];
if (typeof(value) == LIST)
  if (value)
    result = "{" + this:print(value[1]);
    for val in (listdelete(value, 1))
      result = tostr(result, ", ", this:print(val));
    endfor
    return result + "}";
  else
    return "{}";
  endif
elseif (typeof(value) == STR)
  return tostr("\"", strsub(strsub(value, "\\", "\\\\"), "\"", "\\\""), "\"");
elseif (typeof(value) == ERR)
  return $code_utils:error_name(value);
else
  return tostr(value);
endif
```

## Verb: name_and_number

```yaml
names: [name_and_number, nn, name_and_number_list, nn_list]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
{objs, ?sepr = " ", @eng_args} = args;
if (typeof(objs) != LIST)
  objs = {objs};
endif
name_list = {};
for what in (objs)
  name = valid(what) ? what.name | {"<invalid>", "$nothing", "$ambiguous_match", "$failed_match"}[1 + (what in {#-1, #-2, #-3})];
  name = tostr(name, sepr, "(", what, ")");
  name_list = {@name_list, name};
endfor
return this:english_list(name_list, @eng_args);
```

## Verb: match_player

```yaml
names: [match_player]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
retstr = 0;
me = player;
if (length(args) < 2 || typeof(me = args[2]) == OBJ)
  me = valid(me) && is_player(me) ? me | $failed_match;
  if (typeof(args[1]) == STR)
    strings = {args[1]};
    retstr = 1;
    "return a string, not a list";
  else
    strings = args[1];
  endif
else
  strings = args;
  me = player;
endif
found = {};
for astr in (strings)
  if (!astr)
    aobj = $nothing;
  elseif (astr == "me")
    aobj = me;
  elseif (valid(aobj = $string_utils:literal_object(astr)) && is_player(aobj))
    "astr is a valid literal object number of some player, so we are done.";
  else
    aobj = $player_db:find(astr);
  endif
  found = {@found, aobj};
endfor
return retstr ? found[1] | found;
```

## Verb: match

```yaml
names: [match]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
subject = args[1];
if (subject == "")
  return $nothing;
endif
no_exact_match = no_partial_match = 1;
for i in [1..length(args) / 2]
  prop_name = args[2 * i + 1];
  for object in (typeof(olist = args[2 * i]) == LIST ? olist | {olist})
    if (valid(object))
      if (typeof(str_list = `object.(prop_name) ! E_PERM, E_PROPNF => {}') != LIST)
        str_list = {str_list};
      endif
      if (subject in str_list)
        if (no_exact_match)
          no_exact_match = object;
        elseif (no_exact_match != object)
          return $ambiguous_match;
        endif
      else
        for string in (str_list)
          if (index(string, subject) != 1)
          elseif (no_partial_match)
            no_partial_match = object;
          elseif (no_partial_match != object)
            no_partial_match = $ambiguous_match;
          endif
        endfor
      endif
    endif
  endfor
endfor
return no_exact_match && (no_partial_match && $failed_match);
```

## Verb: match_object

```yaml
names: [match_object]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
{string, here, ?who = player} = args;
if ($failed_match != (object = this:literal_object(string)))
  return object;
elseif (string == "me")
  return who;
elseif (string == "here")
  return here;
elseif (valid(pobject = who:match(string)) && string in {@pobject.aliases, pobject.name} || !valid(here))
  "...exact match in player or room is bogus...";
  return pobject;
elseif (valid(hobject = here:match(string)) && string in {@hobject.aliases, hobject.name} || pobject == $failed_match)
  "...exact match in room or match in player failed completely...";
  return hobject;
else
  return pobject;
endif
```

## Verb: literal_object

```yaml
names: [literal_object]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
string = args[1];
if (!string)
  return $nothing;
elseif (string[1] == "#" && E_TYPE != (object = $code_utils:toobj(string)))
  return object;
elseif (string[1] == "~")
  return this:match_player(string[2..$], #0);
elseif (string[1] == "$")
  string[1..1] = "";
  object = #0;
  while (pn = string[1..(dot = index(string, ".")) ? dot - 1 | $])
    if (!$object_utils:has_property(object, pn) || typeof(object = object.(pn)) != OBJ)
      return $failed_match;
    endif
    string = string[length(pn) + 2..$];
  endwhile
  if (object == #0 || typeof(object) == ERR)
    return $failed_match;
  else
    return object;
  endif
else
  return $failed_match;
endif
```

## Verb: abbreviated_value

```yaml
names: [abbreviated_value]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
{value, ?max_reslen = $maxint, ?max_lstlev = $maxint, ?max_lstlen = $maxint, ?max_strlen = $maxint, ?max_toklen = $maxint} = args;
return this:_abbreviated_value(value, max_reslen, max_lstlev, max_lstlen, max_strlen, max_toklen);
```
## Verb: _abbreviated_value

```yaml
names: [_abbreviated_value]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
{value, max_reslen, max_lstlev, max_lstlen, max_strlen, max_toklen} = args;
if ((type = typeof(value)) == LIST)
  if (!value)
    return "{}";
  elseif (max_lstlev == 0)
    return "{...}";
  else
    n = length(value);
    result = "{";
    r = max_reslen - 2;
    i = 1;
    eltstr = "";
    while (i <= n && i <= max_lstlen && r > (x = i == 1 ? 0 | 2))
      eltlen = length(eltstr = this:(verb)(value[i], r, max_lstlev - 1, max_lstlen, max_strlen, max_toklen));
      lastpos = 1;
      if (r >= eltlen + x)
        comma = i == 1 ? "" | ", ";
        result = tostr(result, comma);
        if (r > 4)
          lastpos = length(result);
        endif
        result = tostr(result, eltstr);
        r = r - eltlen - x;
      elseif (i == 1)
        return "{...}";
      elseif (r > 4)
        return tostr(result, ", ...}");
      else
        return tostr(result[1..lastpos], "...}");
      endif
      i = i + 1;
    endwhile
    if (i <= n)
      if (i == 1)
        return "{...}";
      elseif (r > 4)
        return tostr(result, ", ...}");
      else
        return tostr(result[1..lastpos], "...}");
      endif
    else
      return tostr(result, "}");
    endif
  endif
elseif (type == STR)
  result = "\"";
  while ((q = index(value, "\"")) ? q = min(q, index(value, "\\")) | (q = index(value, "\\")))
    result = result + value[1..q - 1] + "\\" + value[q];
    value = value[q + 1..$];
  endwhile
  result = result + value;
  if (length(result) + 1 > (z = max(min(max_reslen, max(max_strlen, max_strlen + 2)), 6)))
    z = z - 5;
    k = 0;
    while (k < z && result[z - k] == "\\")
      k = k + 1;
    endwhile
    return tostr(result[1..z - k % 2], "\"+...");
  else
    return tostr(result, "\"");
  endif
else
  v = type == ERR ? $code_utils:error_name(value) | tostr(value);
  len = max(4, min(max_reslen, max_toklen));
  return length(v) > len ? v[1..len - 3] + "..." | v;
endif
```