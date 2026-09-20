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

Do not reintroduce the earlier rod-balance product concept into runtime or release naming.

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
- Anchor to the current bobber sprite tight mesh and apply accepted visual offset X=+2, Y=-2 game pixels.
- No permanent Harmony `Update()` patch, broad polling, or parallel fishing state machine.
- Unsupported runtime states fail closed by disabling only the countdown.

## Visual constants accepted by runtime test

- start scale: `1.55`;
- end scale: `0.08`;
- alpha at full wait: `0.34`;
- alpha near bite: `0.58`;
- visual offset: `X=+2, Y=-2` game pixels.

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
