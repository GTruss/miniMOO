---
created: 2026-05-24T12:40
updated: 2026-05-24T15:08
---
# miniMOO File World Data

This folder contains read-only bootstrap world data.

Runtime persistence will live somewhere else later. For now, files here are
copied to the executable output and loaded by the host during startup.

Object files use the `.moo.md` extension while the format is still settling.

Each file starts with YAML-lite frontmatter for object metadata. Properties use
```yaml fenced blocks, and verbs use a ```yaml metadata block followed
by a ```csharp code block. The code is still MOO script; `csharp` is used only
for friendlier editor highlighting.

The loader validates the basic shape of object files:

- object files must start with frontmatter
- property metadata uses `name`
- verb metadata uses `names`
- a metadata block cannot contain both `name` and `names`
- verb metadata must be followed by a `csharp` code block
- orphan `csharp` blocks are rejected
- unsupported fence languages are rejected
- object names, property names, and verb names cannot be empty
- duplicate properties on one object are rejected
- duplicate names inside one verb definition are rejected
- duplicate object ids are rejected when loading a directory

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
