# Better Fishing Rods — Project Working Contract

This repository follows the canonical global rules in `666drjekyll666-cloud/DevRules`.

Before substantive technical work, read the current `ENGINEERING_RULES.md`, `CI_POLICY.md`, `GIT_WORKFLOW.md`, `PROJECT_BOOTSTRAP.md`, and `RUNTIME_TEST_HARNESS.md` when runtime evidence is relevant. This file contains only Better Fishing Rods-specific additions.

## Project identity

- Project: **Better Fishing Rods**
- Repository: `666drjekyll666-cloud/BetterFishingRods`
- Game: **Graveyard Keeper 1**
- Target version: **1.407**
- Mod stack: **BepInEx / Harmony**

## Product scope

The mod should reduce fishing repetition without replacing the vanilla fishing system.

Current accepted direction:

- Simple Fishing Rod: catch amount x1;
- Good Fishing Rod: x2 of the same catch already selected by vanilla;
- Excellent Fishing Rod: x3 of the same catch already selected by vanilla;
- upgraded rods should reduce bite waiting and increase the hook reaction window, with exact values chosen only after runtime research;
- bite readability should be improved globally for all rods;
- the existing bite sound should remain vanilla unless later evidence establishes a concrete problem with it.

Do not silently change fish species/quality selection, rarity, fishing spots, casting distances, time-of-day logic, bait/lure semantics, vanilla rod-specific fish availability, the reeling minigame, fishing buffs, achievements, or quest semantics.

## Architecture constraints

Follow the global host-native-first gate.

Preferred catch path:

`vanilla determines catch -> mod adjusts final amount -> vanilla continues`

Prefer changing verified host-owned timing inputs for bite wait and hook window rather than replacing the fishing state machine.

For bite readability, prefer a narrow in-world one-shot effect tied to the native fishing lifecycle. Reuse an existing bobber/water effect or host animation path when practical. Avoid a permanent HUD, broad polling, per-frame object searches, or a parallel timing system.

Unknown rods or unsupported states must fail safe to vanilla x1 behavior.

## Evidence requirements before production implementation

Verify against Graveyard Keeper 1.407:

- exact IDs and data for all three rods;
- where the fish is finally selected;
- where the caught `Item` and its amount are created/granted;
- full-inventory/drop behavior;
- bait consumption semantics;
- achievements/statistics interactions with amount;
- the source and consumer of bite waiting time;
- the source and consumer of the hook reaction window;
- how the vanilla Fast Reflexes effect changes hook timing;
- the native bite/bobber/ripple lifecycle and available visual assets/events;
- rod name/description/price data and the least invasive localization path.

Third-party mods, old decompilations, wikis, and player reports are research hints only unless confirmed against current runtime evidence.

## Bite cue design gate

The preferred UX direction is an immersive visual cue around the bobber, visually consistent with vanilla water effects.

Current leading candidate: a subtle ring/ripple that appears shortly before the bite and contracts toward the bobber, reaching the center at the bite moment.

Before implementation, verify whether an existing vanilla effect can be reused or adjusted. Avoid exposing more hidden fish-selection information than necessary; an exact full-duration countdown from cast is not the default design.

## Balance rules

The goal is less repetition, not preservation of vanilla output per real-world minute at all costs.

Do not automatically add proportional energy/bait penalties for x2/x3 catches. Evaluate economy impact first. If compensation is actually needed, prefer one simple lever over multiple coupled systems.

Do not add a config menu or arbitrary player multipliers without a concrete need.

## Save / compatibility

Avoid custom persistent save data unless necessary. Removing the DLL should restore vanilla behavior without migration.

Do not globally alter fish item definitions or affect fish obtained through trade, crafting, console commands, or unrelated mods.

## Git / acceptance

- `main`: stable/accepted state plus documentation/bookkeeping allowed by global policy.
- Research work: `research/*`.
- Build-bearing development: `dev/X.Y.Z` or a narrowly named development branch.
- Numbered binaries handed to the user are immutable.
- Stable installed DLL name: `BetterFishingRods.dll`.
- Record every handed build in `docs/TEST_BUILD_LOG.md`.
- User acceptance of a tested numbered build gates stable promotion under global `GIT_WORKFLOW.md`.

## CI

Do not run hosted CI for research, docs, bookkeeping, or intermediate commits unless a concrete executable property needs verification.

Before binary handoff, require the project's clean Release build gate from the exact source state. Do not assume a Windows runner is required until the toolchain proves it.

## Long-lived sources of truth

- `AGENTS.md`
- `docs/VERIFIED_RUNTIME_DATA.md`
- `docs/BALANCE_SPEC.md`
- `docs/RESEARCH_PLAN.md`
- `docs/TEST_BUILD_LOG.md`
- `README.md`
- `CHANGELOG.md`
