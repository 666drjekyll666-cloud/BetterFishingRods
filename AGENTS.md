# Better Fishing Rods — Project Working Contract

This repository follows the canonical global rules in `666drjekyll666-cloud/DevRules`.

Before substantive technical work, read the current `ENGINEERING_RULES.md`, `CI_POLICY.md`, `GIT_WORKFLOW.md`, `PROJECT_BOOTSTRAP.md`, and `RUNTIME_TEST_HARNESS.md` when runtime evidence is relevant. This file contains only Better Fishing Rods-specific additions.

## Project identity

- Project: **Better Fishing Rods**
- Repository: `666drjekyll666-cloud/BetterFishingRods`
- Game: **Graveyard Keeper 1**
- Target version: **1.407**
- Mod stack: **BepInEx / Harmony**

## Current product scope

**1.x is a visual-only fishing quality-of-life mod.**

It adds a subtle in-world ring around the bobber during the native bite-wait interval. The ring contracts across the already-selected vanilla wait and reaches the bobber when the native bite occurs.

1.x must not change:

- fish selection, quality, rarity, spots, casting distance, time-of-day logic, bait/lure semantics, or rod-specific fish availability;
- wait duration or hook reaction duration;
- energy use;
- caught item amount;
- the reeling minigame;
- achievements, quest semantics, or reward handling.

Earlier x2/x3 catch and rod-balance ideas are **deferred and are not part of the current released product**. Do not add them to 1.x without an explicit new product decision.

## Architecture constraints

Follow the global host-native-first gate.

The countdown is presentation-only:

- vanilla owns fish selection and `_waiting_for_bite_delay`;
- initial countdown starts after the verified native `FishingThrowingAnim.OnStateExit` transition enables `can_take_out`;
- after a missed hook, vanilla can return directly from `WaitingForPulling` to `WaitingForBite`; re-arm only from that native state when `can_take_out == true`;
- visual progress reads the existing native timer; it must not own or write a parallel gameplay timer;
- anchor to the current bobber sprite's tight mesh and apply the accepted visual offset X=+2, Y=-2 game pixels;
- no permanent Harmony `Update()` patch, global polling, or per-frame scene search.

Unknown/unsupported runtime states must fail closed by disabling only the countdown.

## Diagnostics

Release builds must not create a separate Better Fishing Rods diagnostic log.

Research builds may use temporary diagnostics on research branches. Production may emit one BepInEx error when a required 1.407 seam is unavailable or a fatal runtime compatibility failure disables the countdown.

## Save / compatibility

The mod writes no custom save data. Removing the DLL restores vanilla fishing behavior without migration.

## Git / acceptance

- `main`: stable/accepted state plus documentation/bookkeeping allowed by global policy.
- Research work: `research/*`.
- Build-bearing development: `dev/X.Y.Z`.
- Numbered binaries handed to the user are immutable.
- Stable installed DLL name: `BetterFishingRods.dll`.
- Record handed builds in `docs/TEST_BUILD_LOG.md`.
- User acceptance of a tested numbered build gates stable promotion under global `GIT_WORKFLOW.md`.

## CI

Do not run hosted CI for research, docs, bookkeeping, or intermediate commits unless a concrete executable property needs verification.

Before stable binary publication, require one clean Release build from the exact production source state. A GitHub Release should reuse that exact accepted/hash-verified artifact rather than rebuilding it.

## Long-lived sources of truth

- `AGENTS.md`
- `docs/VERIFIED_RUNTIME_DATA.md`
- `docs/RESEARCH_PLAN.md`
- `docs/TEST_BUILD_LOG.md`
- `README.md`
- `CHANGELOG.md`
