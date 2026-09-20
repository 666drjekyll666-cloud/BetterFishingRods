# Better Fishing Rods Visual Prototype 0.2.0

Research-only visual prototype for Graveyard Keeper 1.407.

## Scope

This build changes **only the presentation of the waiting phase**:

- after the native cast-settling animation ends and `can_take_out` becomes true, an in-world elliptical ring appears at the bobber;
- the ring reads the existing `_waiting_for_bite_delay` every frame while the native `WaitingForBite` state is active;
- its radius contracts linearly with the remaining native wait;
- it disappears when vanilla leaves `WaitingForBite`.

It does **not** change:

- selected fish;
- RNG;
- native wait duration;
- hook reaction window;
- catch amount;
- bait/lure;
- energy;
- inventory/reward handling;
- saves.

## Architecture

The prototype uses:

- one verified event hook at `FishingThrowingAnim.OnStateExit` to start the ring;
- one `FishingGUI.ChangeState` hook for cleanup;
- one session-local coroutine only while the ring is visible;
- cached reflection fields for the native state and timer;
- one lazily created `SpriteRenderer` child under the current bobber.

There is no permanent Harmony `Update()` patch and no per-frame object search.

## Test

Install only this visual prototype (remove the old research probe DLL first).

Perform several normal casts and judge:

- position: is the ring centered on the bobber/water contact point?
- size: is the starting ring too large or too small?
- visibility: is it too bright, too faint, or visually noisy?
- timing: does it visibly collapse at the same moment as the bite?
- motion: does the linear countdown feel readable rather than distracting?

Return a screenshot/video if alignment is wrong; otherwise a short textual verdict is enough.

Diagnostic log:

`BepInEx/BetterFishingRodsVisualPrototype-0.2.0.log`
