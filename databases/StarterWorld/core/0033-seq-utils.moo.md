---
id: "#33"
name: $seq_utils
owner: "#0"
parent: "#78"
location:
flags:
  - readable
aliases: []
created: 2026-05-24T15:10
updated: 2026-05-24T15:10
---

# $seq_utils

## Verb: from_string

```yaml
names: [from_string]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
":from_string(string) => corresponding finite sequence or E_INVARG";
text = strsub(args[1], " ", "");
if (!text)
  return {};
endif

parts = {};
for word in ($string_utils:explode(text, ","))
  sep = index(word, "...");
  seplen = 3;
  if (!sep)
    sep = index(word, "..");
    seplen = 2;
  endif

  if (!sep)
    if (!match(word, "^[-+]?[0-9]+$"))
      return E_INVARG;
    endif
    start = toint(word);
    part = {start, start + 1};
  else
    if (sep == 1)
      return E_INVARG;
    endif

    first = word[1..sep - 1];
    last = word[sep + seplen..$];

    if (!(match(first, "^[-+]?[0-9]+$") && match(last, "^[-+]?[0-9]+$")))
      return E_INVARG;
    endif

    start = toint(first);
    finish = toint(last);
    part = finish >= start ? {start, finish + 1} | {};
  endif

  parts = {@parts, part};
endfor

return this:union(@parts);
```

## Verb: union

```yaml
names: [union]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
":union(seq1,seq2,...) => union of all finite sequences";
result = {};
for seq in (args)
  if (length(seq) % 2)
    return E_INVARG;
  endif

  for r in [1..length(seq) / 2]
    result = this:_add_interval(result, seq[(2 * r) - 1], seq[2 * r]);
  endfor
endfor
return result;
```

## Verb: intersection

```yaml
names: [intersection]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
":intersection(seq1,seq2,...) => intersection of all finite sequences";
if (!args)
  return {};
endif

result = args[1];
if (length(result) % 2)
  return E_INVARG;
endif

for seq in (args[2..$])
  if (length(seq) % 2)
    return E_INVARG;
  endif
  result = this:_intersect_two(result, seq);
endfor

return result;
```

## Verb: _add_interval

```yaml
names: [_add_interval]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
{seq, first, after} = args;
if (after <= first)
  return seq;
endif

new = {};
inserted = 0;

for r in [1..length(seq) / 2]
  start = seq[(2 * r) - 1];
  finish = seq[2 * r];

  if (finish < first)
    new = {@new, start, finish};
  elseif (after < start)
    if (!inserted)
      new = {@new, first, after};
      inserted = 1;
    endif
    new = {@new, start, finish};
  else
    first = min(first, start);
    after = max(after, finish);
  endif
endfor

return inserted ? new | {@new, first, after};
```

## Verb: _intersect_two

```yaml
names: [_intersect_two]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
{left, right} = args;
result = {};
i = 1;
j = 1;

while (i < length(left) && j < length(right))
  start = max(left[i], right[j]);
  finish = min(left[i + 1], right[j + 1]);

  if (start < finish)
    result = {@result, start, finish};
  endif

  if (left[i + 1] < right[j + 1])
    i = i + 2;
  else
    j = j + 2;
  endif
endwhile

return result;
```
