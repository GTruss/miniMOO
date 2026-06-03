---
id: "#10"
name: $login
owner: "#0"
parent: "#1"
location:
flags:
  - readable
aliases: []
created: 2026-06-03T12:00
updated: 2026-06-03T12:00
---

# $login

```yaml
name: welcome_message
type: list
value:
  - "Welcome to miniMOO!"
  - "The server is running %v."
  - "Use: connect <player> <password>"
flags:
  - readable
  - writable
  - chown
```

```yaml
name: registration_string
type: string
value: "Character creation is not yet available from the login screen."
flags:
  - readable
  - writable
  - chown
```

```yaml
name: newt_registration_string
type: string
value: "Your character is unavailable."
flags:
  - readable
  - writable
  - chown
```

```yaml
name: registration_address
type: string
value: ""
flags:
  - readable
  - writable
  - chown
```

```yaml
name: create_enabled
type: integer
value: 0
flags:
  - readable
  - writable
  - chown
```

```yaml
name: bogus_command
type: string
value: "?"
flags:
  - readable
```

```yaml
name: blank_command
type: string
value: "welcome"
flags:
  - readable
```

```yaml
name: help_message
type: list
value:
  - "Available commands from the login screen:"
  - "  connect <player> <password>"
  - "  welcome"
  - "  who"
  - "  uptime"
  - "  version"
  - "  quit"
flags:
  - readable
  - writable
  - chown
```

## Verb: ?

```yaml
names: ["?", "help"]
dobj: any
prep: none
iobj: any
owner: "#0"
flags: [readable, executable]
```

```csharp
if ((caller != #0) && (caller != this))
  return E_PERM;
endif

for line in (this.help_message)
  notify(player, line);
endfor
return 0;
```

## Verb: welcome

```yaml
names: ["wel*come", "@wel*come"]
dobj: any
prep: none
iobj: any
owner: "#0"
flags: [readable, executable]
```

```csharp
if ((caller != #0) && (caller != this))
  return E_PERM;
endif

for line in (this.welcome_message)
  notify(player, strsub(line, "%v", server_version()));
endfor
return 0;
```

## Verb: who

```yaml
names: ["w*ho", "@w*ho"]
dobj: any
prep: none
iobj: any
owner: "#0"
flags: [readable, executable]
```

```csharp
if ((caller != #0) && (caller != this))
  return E_PERM;
endif

plyrs = connected_players();
if (!length(plyrs))
  notify(player, "No one logged in.");
else
  for p in (plyrs)
    notify(player, tostr(p.name, " (", p, ")"));
  endfor
endif
return 0;
```

## Verb: connect

```yaml
names: ["co*nnect", "@co*nnect"]
dobj: any
prep: none
iobj: any
owner: "#0"
flags: [readable, executable]
```

```csharp
((caller == #0) || (caller == this)) || raise(E_PERM);

try
  {name, ?password = ""} = args;
except (E_ARGS)
  notify(player, tostr("Usage:  ", verb, " <existing-player-name> <password>"));
  return 0;
endtry

candidate = this:_match_player(name);
if (!valid(candidate) || !is_player(candidate))
  notify(player, "No such player.");
  return 0;
endif

if (`candidate.password ! E_PROPNF' == E_PROPNF)
  notify(player, "That player cannot be used for console login.");
  return 0;
elseif (candidate.password != password)
  notify(player, "Incorrect password.");
  return 0;
endif

return candidate;
```

## Verb: create

```yaml
names: ["cr*eate", "@cr*eate"]
dobj: any
prep: none
iobj: any
owner: "#0"
flags: [readable, executable]
```

```csharp
if ((caller != #0) && (caller != this))
  return E_PERM;
elseif (!this.create_enabled)
  notify(player, this.registration_string);
else
  notify(player, "Login-time player creation is not implemented in miniMOO yet.");
endif
return 0;
```

## Verb: quit

```yaml
names: ["q*uit", "@q*uit"]
dobj: any
prep: none
iobj: any
owner: "#0"
flags: [readable, executable]
```

```csharp
if ((caller != #0) && (caller != this))
  return E_PERM;
endif

boot_player(player);
return 0;
```

## Verb: uptime

```yaml
names: ["up*time", "@up*time"]
dobj: any
prep: none
iobj: any
owner: "#0"
flags: [readable, executable]
```

```csharp
if ((caller != #0) && (caller != this))
  return E_PERM;
endif

notify(player, tostr("The server has been up for ", time() - $last_restart_time, " seconds."));
return 0;
```

## Verb: version

```yaml
names: ["v*ersion", "@v*ersion"]
dobj: any
prep: none
iobj: any
owner: "#0"
flags: [readable, executable]
```

```csharp
if ((caller != #0) && (caller != this))
  return E_PERM;
endif

notify(player, tostr("The MOO is currently running version ", server_version(), "."));
return 0;
```

## Verb: parse_command

```yaml
names: [parse_command]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
if ((caller != #0) && (caller != this))
  return E_PERM;
endif

if (!args)
  return {this.blank_command, @args};
endif

verb = args[1];
for i in ({this, @$object_utils:ancestors(this)})
  if ((`verb_args(i, verb) ! E_VERBNF => 0' == {"any", "none", "any"}) && `index(verb_info(i, verb)[2], "x") ! ANY => 0')
    return args;
  endif
endfor

return {this.bogus_command, @args};
```

## Verb: _match_player

```yaml
names: [_match_player]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
target = $string_utils:trim(args[1]);
if (!target)
  return $failed_match;
elseif (valid(literal = $string_utils:literal_object(target)) && is_player(literal))
  return literal;
endif

matches = {};
for obj in ($object_utils:descendants($player))
  if (is_player(obj) && ((obj.name == target) || (target in obj.aliases)))
    matches = setadd(matches, obj);
  endif
endfor

if (length(matches) == 1)
  return matches[1];
elseif (length(matches))
  return $ambiguous_match;
else
  return $failed_match;
endif
```
