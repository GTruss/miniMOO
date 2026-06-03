---
created: 2026-05-24T12:40
updated: 2026-06-02T19:05
---
# miniMOO Test Database

This folder is the working test database for miniMOO.

It is not the source of truth for the default world. On every run of the CLI
with `--test` or `--tests`, the host refreshes both `core/` and `world/` by
copying them from the live database configured by `appsettings.live.json`
into the test database configured by `appsettings.tests.json`.

That means:

- test runs always start from a fresh copy of the current live database
- any old files already in `Tests/core` or `Tests/world` are deleted first
- test-created objects and edits are written back into this folder during the test run
- commands like `@dump-db` checkpoint both `core/` and `world/` here

This is useful because the scripted test suite exercises real in-world commands
and persistence behavior, not just isolated engine code.

## File Format

Object files use the `.moo.md` extension.

Each file starts with YAML-lite frontmatter for object metadata. Properties use
`yaml` fenced blocks, and verbs use a `yaml` metadata block followed by a
`csharp` code block. The code is still MOO script; `csharp` is used only for
friendlier editor highlighting.

Example:

````markdown
---
id: "#1"
name: "$root"
owner: "#0"
parent:
location:
flags:
  - readable
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

## Validation

The loader validates the basic shape of object files:

- object files must contain frontmatter with an `id`
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
