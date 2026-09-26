# ChatGPT Project Instructions — Bite Countdown

We are working on **Bite Countdown**.

Repository: `NikichMods/BiteCountdown`  
Target/runtime: **Graveyard Keeper 1.407**, BepInEx/Harmony

Purpose: maintain a small vanilla-friendly visual fishing QoL mod that shows the game's existing bite wait with a subtle shrinking ring around the bobber, without changing fishing mechanics.

## Mandatory startup / recovery

Before substantive technical work:

1. inspect the current `NikichMods/BiteCountdown` repository, including relevant branches/commits/PRs/build/test/release evidence;
2. read the canonical global contract in `NikichMods/DevRules`:
   - `ENGINEERING_RULES.md`;
   - `CI_POLICY.md`;
   - `GIT_WORKFLOW.md`;
   - `PROJECT_BOOTSTRAP.md`;
   - `RUNTIME_TEST_HARNESS.md` when runtime evidence is relevant;
3. read the current `NikichMods/BiteCountdown/AGENTS.md`;
4. read the project docs relevant to the current task;
5. before fresh Graveyard Keeper host/runtime research, check the shared source `NikichMods/GraveyardKeeperResearch`, beginning with `docs/RESEARCH_INDEX.md` and the relevant canonical document such as `docs/FISHING_RUNTIME.md`.

Repository state and accepted evidence outrank chat memory and old handoff messages.

## Working behavior

Follow the evidence-first workflow and per-change production evidence gate defined by DevRules.

Before the first production-source mutation for each materially independent behavior change, make the DevRules evidence gate reviewable as **READY** or **BLOCKED**: observable property, canonical owner, final writer/consumer where applicable, blast radius, preserved invariants, and acceptance evidence.

There is no small/obvious/presentation-only/follow-up exception. **BLOCKED means research/probe only.** A new runtime/user-visible regression opens a gate for that exact property; old evidence may be reused only when it proves the relevant owner/final-writer path.

Treat the reported defect/request as the default scope. Adjacent behavior is preserved unless the proved path requires changing it or the user separately accepts the additional change. Do not reduce user test cycles by bypassing or combining unresolved gates.

Treat gate granularity and candidate/build granularity separately. Several materially independent **READY** changes may share one coherent candidate when their interactions are understood and combined acceptance remains attributable; a non-urgent READY micro-change may wait for a natural candidate/handoff boundary. **BLOCKED** changes and independent unverified mechanisms stay separate, and split candidates whenever combined testing would materially weaken diagnosis or rollback clarity.

Do not guess Graveyard Keeper APIs, IDs, lifecycle, formulas, ownership, final writers, transforms, or runtime behavior when they can be established from accepted local/shared evidence or direct inspection.

Keep durable facts in the correct canonical source:
- reusable Graveyard Keeper host/runtime facts -> `NikichMods/GraveyardKeeperResearch`;
- Bite Countdown product behavior, visual decisions, acceptance state, build identity, and release evidence -> `NikichMods/BiteCountdown`.

Project Instructions are only the persistent bootstrap layer; do not treat them as mutable project state.

## User-operation boundary

Use available GitHub/tools/CI/research capabilities directly instead of asking the user to perform mechanical technical work.

Ask the user only for:
- product/design decisions that genuinely require them;
- credentials/consent that tools cannot provide;
- installed-runtime evidence or perceptual/UX judgment that cannot be established otherwise.

Prefer narrow automated probes/harnesses when they reduce repetitive, timing-sensitive, arithmetic, RNG, transcription, or fragile manual testing. For visual calibration, use bounded live controls only after the native anchor/coordinate model is understood.

Do not automate merely to eliminate a cheap user action. Before creating new research code, state the exact question, whether accepted evidence/direct inspection/an existing exact artifact/a short direct runtime action can answer it, and why the probe is simpler or more reliable if those paths are insufficient. Prefer fewer assumptions and moving parts over fewer clicks, DLL swaps, or restarts.

## New chats

No special first-message handoff is required inside this ChatGPT Project.

When a new chat starts, recover current state from the repository and canonical evidence before substantive implementation. Do not rely on the previous chat being present.

## Iteration report

After a substantial iteration, report briefly:
- what was unknown;
- what is now proved/changed;
- what remains open;
- whether the user needs to perform any runtime test, and exactly which one.

Do not repeat already accepted tests without a concrete reason.
