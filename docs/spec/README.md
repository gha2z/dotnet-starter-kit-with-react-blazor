# docs/spec — the requirements home

> **One file per app (or cross-cutting workstream).** This is the only place you author
> requirements for the platform. Sessions read specs here; plans map them to `FR-###` feature ids.

## Why this directory exists

The agentic workflow drives phases off a stable, human-authored requirements source. Rather than
pasting requirements into each session prompt, write them once here in plain language, then tell a
session: *"plan the next phase of the <track> against `docs/spec/<file>.md`."* The architect turns
the spec into `Phase-NN-*/plan.md` with tracked `FR-###` ids.

## File naming convention

```
docs/spec/
├── README.md                 ← this file
├── 010-{app-or-stream}.md     ← numbered for stable ordering (leading zeroes)
├── 020-{app-or-stream}.md
└── ...
```

Numbers stay stable. Renaming is allowed while a file is still draft; once a session has planned
off it, renumber only with the same wave of changes.

## What a spec file looks like

```markdown
# {App / stream} requirements

**Track:** opencode/<track-name>   <!-- which track owns this work -->
**Status:** draft | active | frozen
**Owner:** {who decides scope changes}
**Last updated:** {date}

## Goal

One paragraph — what this app/stream must be able to do when done.

## Out of scope (explicit)

- Things deliberately not included (so sessions don't invent them).

## Requirements

### FR-001 — {title}
Must: {one plain-language sentence the software must guarantee}.
Given {context}, when {action}, then {observable outcome}.

### FR-002 — {title}
Must: {…}. Given …, when …, then ….
```

## The FR-### contract

- **`Must:`** = a hard requirement. Not done until met and verified.
- **`Given / When / Then`** = the acceptance shape. A session's verify step must be able to check it.
- **No FR-### is "planned by size"** — each one is a bounded, verifiable behavior. Big behaviors get
  split into FR-001a/b by the architect, never by the spec author.
- Numbering is global per app file (`FR-001`, `FR-002`, …). Do not renumber on re-plan; archive dead
  ids with `~~struck~~` + date instead so plans remain traceable.

## Workflow hooks

| You want to… | Do this |
|---|---|
| Start a new app/stream | Copy the skeleton below into `docs/spec/NNN-{name}.md`, set Track + Owner |
| Plan the next phase | `opencode/<track>/00-Index.md` roadmap → architect reads the spec → `plan.md` |
| Check a phase's coverage | The plan's `FR-###` refs should map 1:1 to Musts in this file |
| Retire a requirement | Strike it with a date, keep the id for traceability |

## Skeleton

```markdown
# {App / stream} requirements

**Track:** opencode/
**Status:** draft
**Owner:**
**Last updated:** {date}

## Goal
{…}

## Out of scope (explicit)
- {…}

## Requirements
{FR-### entries}
```

## Parity note (this repo today)

The current main goal is **React → Blazor parity to zero gaps** — the spec for that lives in
`opencode/addBlazorFrontends/00-Index.md` (phase/feature index) and the React clients at
`clients/admin` + `clients/dashboard` are the read-only reference. When the platform apps beyond
parity (ERP back-office, standalone POS, consumer mobile) are specified, they get files here.