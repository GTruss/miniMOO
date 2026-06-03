---
created: 2026-05-24T12:40
updated: 2026-06-02T19:05
---
# miniMOO Test Database

This folder is the working source database for miniMOO test runs.

It is not the source of truth for the default live world, and it is no longer
the database that `--test` opens directly.

Instead:

- `databases/Tests/core` and `databases/Tests/world` are the editable source
  test world
- `--test clone` copies that source world into `databases/Tests/.testruns`
- plain `--test` opens the cloned `.testruns` database for manual testing
- `--test run` opens the cloned `.testruns` database, logs in as `Tester`,
  runs `@test-builtins` and `@test-scripts`, and exits
- commands like `@dump-db` during test runs write into `.testruns`, not into
  the source `Tests` world

Recommended workflow:

1. Make changes in the source test world under `databases/Tests`
2. Run `--test clone`
3. Either:
   - run plain `--test` for manual testing, or
   - run `--test run` for the scripted test suite

This keeps the source test world clean while still exercising real in-world
commands and persistence behavior.

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
