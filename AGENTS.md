# Bite Countdown — Project Working Contract

This repository follows the canonical global rules in `NikichMods/DevRules`.

Before substantive technical work, read the current `ENGINEERING_RULES.md`, `CI_POLICY.md`, `GIT_WORKFLOW.md`, `PROJECT_BOOTSTRAP.md`, and `RUNTIME_TEST_HARNESS.md` when runtime evidence is relevant.

## Project identity

- Project: **Bite Countdown**
- Repository: `NikichMods/BiteCountdown`
- Game: **Graveyard Keeper 1**
- Target version: **1.407**
- Mod stack: **BepInEx / Harmony**
- BepInEx GUID: `nikich.bitecountdown`
- Stable installed DLL: `BiteCountdown.dll`
- Current accepted stable runtime baseline: **1.0.3**

Do not reintroduce the earlier rod-balance product concept into runtime or release naming.

## Shared Graveyard Keeper research

Reusable Graveyard Keeper 1.407 host/runtime facts are shared through `NikichMods/GraveyardKeeperResearch`.

Before starting fresh host-internals research, check:
- `NikichMods/GraveyardKeeperResearch/docs/RESEARCH_INDEX.md`;
- the relevant canonical shared document, especially `docs/FISHING_RUNTIME.md` for fishing lifecycle/geometry questions;
- accepted local project evidence/history if the fact is Bite Countdown-specific.

Keep cross-project host/runtime facts in the shared research repository. Keep Bite Countdown product behavior, visual decisions, acceptance state, build identity, and release evidence in this repository.

## Product scope

Bite Countdown is a visual-only fishing quality-of-life mod.

It adds a subtle in-world ring around the bobber during the native bite-wait interval. The ring contracts across the already-resolved vanilla wait and reaches the bobber when the native bite occurs.

The mod must not change:

- fish selection, quality, rarity, spots, casting distance, time-of-day logic, bait/lure semantics, or rod-specific fish availability;
- bite wait duration or hook reaction duration;
- energy use;
- caught item amount;
- the reeling minigame;
- achievements, quest semantics, reward handling, or save data.

## Architecture constraints

Follow the global host-native-first gate.

- Vanilla owns fish selection and `_waiting_for_bite_delay`.
- Initial countdown starts after verified `FishingThrowingAnim.OnStateExit` enables `can_take_out`.
- After a missed hook, vanilla can return directly to `WaitingForBite`; re-arm only when the resulting state is `WaitingForBite` and `can_take_out == true`.
- The visual coroutine reads the native timer. It never writes or replaces the gameplay timer.
- Anchor to the current bobber sprite tight mesh and apply the residual correction in bobber-sprite local pixels so native left/right mirroring also mirrors that correction.
- No permanent Harmony `Update()` patch, broad polling, or parallel fishing state machine.
- Unsupported runtime states fail closed by disabling only the countdown.

## Accepted visual behavior

- start scale: `1.55`;
- end scale: `0.08`;
- alpha at full wait: `0.34`;
- alpha near bite: `0.58`;
- sprite-local visual correction: `X=-2, Y=-2` game pixels. This preserves the accepted right-facing placement and mirrors the horizontal correction for left-facing fishing;
- ring base RGB: `0.60, 0.82, 0.92`;
- alpha remains `0.34 -> 0.58`;
- once per native wait, sample final `RenderSettings.ambientLight` at end-of-frame and scale RGB by a bounded factor `0.62 -> 1.00` based on ambient grayscale;
- brightness adaptation must have no direct dependency on Keeper's Lantern or any other lighting mod.

## Diagnostics

Stable releases must not create a separate diagnostic log file. Production may emit one normal BepInEx error only when a fatal compatibility/runtime failure disables the countdown.

## Save / compatibility

The mod writes no custom save data. Removing `BiteCountdown.dll` restores vanilla behavior without migration.

## Git / acceptance

- `main`: stable accepted state.
- Research: `research/*`.
- Build-bearing development: `dev/X.Y.Z`.
- Numbered binaries are immutable once handed/published.
- Stable installed DLL: `BiteCountdown.dll`.
- GitHub Releases are the stable binary surface.

## Shared Graveyard Keeper research

Cross-project Graveyard Keeper 1.407 host/runtime research is centralized in `NikichMods/GraveyardKeeperResearch`.

Before starting a fresh investigation into vanilla/game-engine/UI/NGUI/data/lifecycle behavior:

1. read this repository's own canonical verified-data / architecture docs first;
2. consult `NikichMods/GraveyardKeeperResearch/docs/RESEARCH_INDEX.md` and the linked shared knowledge documents;
3. search accepted local/shared test evidence and relevant history if the result has not yet been promoted;
4. perform new static/runtime research or a probe only if the question remains open.

Project-specific mechanics, product/UX decisions, release state, and build acceptance remain canonical in this repository. Reusable host/runtime facts that can serve multiple Graveyard Keeper mods should be promoted back into the shared research repository after acceptance rather than left only in chat, commit history, or a test log.

