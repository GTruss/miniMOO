---
created: 2026-05-24T12:40
updated: 2026-05-24T13:55
---
# miniMOO File World Data

This folder contains read-only bootstrap world data.

Runtime persistence will live somewhere else later. For now, files here are
copied to the executable output and loaded by the host during startup.

Object files use the `.moo.md` extension while the format is still settling.

Each file starts with YAML-lite frontmatter for object metadata. Properties use
```yaml fenced blocks, and verbs use a ```yaml metadata block followed
by a ```moo code block.

Example:

````markdown
---
id: "#1"
name: "$root"
owner: "#0"
parent: null
location: null
flags: [readable]
aliases: []
---

# $root

```yaml
name: description
type: string
value: You see nothing special.
```

## Verb: tell

```yaml
names: [tell]
dobj: this
prep: none
iobj: this
owner: "#0"
flags: [readable, executable]
```

```csharp
this:notify(tostr(@args));
```
````
