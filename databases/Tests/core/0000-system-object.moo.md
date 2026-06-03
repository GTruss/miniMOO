---
id: "#0"
name: The System Object
owner: "#0"
parent:
location:
flags:
  - readable
aliases: []
created: 2026-06-02T00:00
updated: 2026-06-02T00:00
---

# The System Object

```yaml
name: maxint
type: integer
value: 9223372036854775807
```

```yaml
name: last_restart_time
type: integer
value: 0
flags:
  - readable
```

```yaml
name: shutdown_task
type: error
value: 0
```

```yaml
name: shutdown_time
type: integer
value: 0
```

```yaml
name: shutdown_message
type: string
value: ""
```

```yaml
name: root
type: object
value: "#1"
```

```yaml
name: root_class
type: object
value: "#1"
```

```yaml
name: room
type: object
value: "#3"
```

```yaml
name: builder
type: object
value: "#4"
```

```yaml
name: thing
type: object
value: "#5"
```

```yaml
name: player
type: object
value: "#6"
```

```yaml
name: exit
type: object
value: "#7"
```

```yaml
name: container
type: object
value: "#8"
```

```yaml
name: note
type: object
value: "#9"
```

```yaml
name: login
type: object
value: "#10"
```

```yaml
name: player_start
type: object
value: "#62"
```

```yaml
name: last_huh
type: object
value: "#11"
```

```yaml
name: string_utils
type: object
value: "#20"
```

```yaml
name: building_utils
type: object
value: "#21"
```

```yaml
name: seq_utils
type: object
value: "#33"
```

```yaml
name: mail_player
type: object
value: "#40"
```

```yaml
name: gender_utils
type: object
value: "#41"
```

```yaml
name: verb_editor
type: object
value: "#49"
```

```yaml
name: generic_editor
type: object
value: "#50"
```

```yaml
name: object_utils
type: object
value: "#52"
```

```yaml
name: list_utils
type: object
value: "#55"
```

```yaml
name: command_utils
type: object
value: "#56"
```

```yaml
name: wiz
type: object
value: "#57"
```

```yaml
name: prog
type: object
value: "#58"
```

```yaml
name: code_utils
type: object
value: "#59"
```

```yaml
name: display_options
type: object
value: "#67"
```

```yaml
name: generic_options
type: object
value: "#68"
```

```yaml
name: prog_options
type: object
value: "#76"
```

```yaml
name: build_options
type: object
value: "#77"
```

```yaml
name: generic_utils
type: object
value: "#78"
```

```yaml
name: quota_utils
type: object
value: "#79"
```

```yaml
name: byte_quota_utils
type: object
value: "#79"
```

```yaml
name: frands_player_class
type: object
value: "#88"
```

```yaml
name: nothing
type: object
value: "#-1"
```

```yaml
name: failed_match
type: object
value: "#-2"
```

```yaml
name: ambiguous_match
type: object
value: "#-3"
```

## Verb: do_login_command

```yaml
names: [do_login_command]
dobj: any
prep: none
iobj: any
owner: "#0"
flags: [readable, executable]
```

```csharp
args = $login:parse_command(@args);
return $login:(args[1])(@listdelete(args, 1));
```
