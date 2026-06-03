---
id: "#0"
name: The System Object
owner: "#0"
parent:
location:
flags:
  - readable
aliases: []
updated: 2026-06-03T07:55:16-05:00
---

# The System Object

```yaml
name: ambiguous_match
type: object
value: "#-3"
flags:
  - readable
```

```yaml
name: builder
type: object
value: "#4"
flags:
  - readable
```

```yaml
name: building_utils
type: object
value: "#21"
flags:
  - readable
```

```yaml
name: build_options
type: object
value: "#77"
flags:
  - readable
```

```yaml
name: byte_quota_utils
type: object
value: "#79"
flags:
  - readable
```

```yaml
name: code_utils
type: object
value: "#59"
flags:
  - readable
```

```yaml
name: command_utils
type: object
value: "#56"
flags:
  - readable
```

```yaml
name: container
type: object
value: "#8"
flags:
  - readable
```

```yaml
name: display_options
type: object
value: "#67"
flags:
  - readable
```

```yaml
name: exit
type: object
value: "#7"
flags:
  - readable
```

```yaml
name: failed_match
type: object
value: "#-2"
flags:
  - readable
```

```yaml
name: frands_player_class
type: object
value: "#88"
flags:
  - readable
```

```yaml
name: gender_utils
type: object
value: "#41"
flags:
  - readable
```

```yaml
name: generic_editor
type: object
value: "#50"
flags:
  - readable
```

```yaml
name: generic_options
type: object
value: "#68"
flags:
  - readable
```

```yaml
name: generic_utils
type: object
value: "#78"
flags:
  - readable
```

```yaml
name: last_huh
type: object
value: "#11"
flags:
  - readable
```

```yaml
name: last_restart_time
type: integer
value: 1780491141
flags:
  - readable
```

```yaml
name: list_utils
type: object
value: "#55"
flags:
  - readable
```

```yaml
name: login
type: object
value: "#10"
flags:
  - readable
```

```yaml
name: mail_player
type: object
value: "#40"
flags:
  - readable
```

```yaml
name: maxint
type: integer
value: 9223372036854775807
flags:
  - readable
```

```yaml
name: note
type: object
value: "#9"
flags:
  - readable
```

```yaml
name: nothing
type: object
value: "#-1"
flags:
  - readable
```

```yaml
name: object_utils
type: object
value: "#52"
flags:
  - readable
```

```yaml
name: player
type: object
value: "#6"
flags:
  - readable
```

```yaml
name: player_start
type: object
value: "#62"
flags:
  - readable
```

```yaml
name: prog
type: object
value: "#58"
flags:
  - readable
```

```yaml
name: prog_options
type: object
value: "#76"
flags:
  - readable
```

```yaml
name: quota_utils
type: object
value: "#79"
flags:
  - readable
```

```yaml
name: room
type: object
value: "#3"
flags:
  - readable
```

```yaml
name: root
type: object
value: "#1"
flags:
  - readable
```

```yaml
name: root_class
type: object
value: "#1"
flags:
  - readable
```

```yaml
name: seq_utils
type: object
value: "#33"
flags:
  - readable
```

```yaml
name: shutdown_message
type: string
value: "Wizard (#2): "
flags:
  - readable
```

```yaml
name: shutdown_task
type: error
value: 0
flags:
  - readable
```

```yaml
name: shutdown_time
type: integer
value: 1780491316
flags:
  - readable
```

```yaml
name: string_utils
type: object
value: "#20"
flags:
  - readable
```

```yaml
name: thing
type: object
value: "#5"
flags:
  - readable
```

```yaml
name: verb_editor
type: object
value: "#49"
flags:
  - readable
```

```yaml
name: wiz
type: object
value: "#57"
flags:
  - readable
```

## Verb: do_login_command

```yaml
names: ["do_login_command"]
dobj: any
prep: none
iobj: any
owner: "#0"
flags:
  - readable
  - executable
```

```csharp
args = $login:parse_command(@args);
return $login:(args[1])(@listdelete(args, 1));
```

