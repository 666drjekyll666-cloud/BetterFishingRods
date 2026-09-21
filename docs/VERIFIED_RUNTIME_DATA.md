# Verified Runtime Data

Target: **Graveyard Keeper 1.407**.

Verified Assembly-CSharp identity used during research:

- version: `11.0.0.0`;
- MVID: `6f50b8e7-156b-49ac-bbe8-7505894b2364`.

## Native countdown ownership

Current `FishingGUI.ChangeState(WaitingForBite)` resolves the selected fish and stores the native wait in `_waiting_for_bite_delay`.

`FishingGUI.UpdateWaitingForBite()` does not consume that timer while `can_take_out == false`. Once `can_take_out == true`, vanilla subtracts `Time.deltaTime` until the timer reaches zero and transitions to `WaitingForPulling`.

Runtime research verified that `FishingThrowingAnim.OnStateExit` is the initial-cast transition that flips `can_take_out` from false to true without replacing the resolved wait.

## Missed-hook lifecycle

Runtime testing verified that a missed hook can make vanilla transition directly from `WaitingForPulling` back to `WaitingForBite` without another throwing animation. Vanilla resolves another fish/wait and may repeat this cycle until the player ends fishing.

Bite Countdown therefore re-arms from the resulting native `WaitingForBite` state only when `can_take_out == true`.

## Bobber anchor

Runtime probing at all three tested cast distances verified that cast distance is already represented by the native bobber transform.

The visual float is offset inside that transform by the current bobber sprite geometry. Production therefore derives the anchor from the current waiting sprite's tight mesh rather than using a location/distance table.

Accepted initial runtime visual calibration (right-facing reference):

- screen-relative offset X: **+2 game pixels**;
- screen-relative offset Y: **-2 game pixels**;
- start scale: **1.55**;
- end scale: **0.08**;
- alpha at full wait: **0.34**;
- alpha near bite: **0.58**.

### Left/right orientation correction research

Cross-spot Anchor Audit Probe 0.3.0 sampled all four fishing spots. Sea and waterfall were right-facing; village lake and swamp were left-facing. All sampled waits used the same bobber waiting sprite, PPU, tight-mesh geometry and no SpriteRenderer flip. The left-facing spots shared the same calculated 1.0.1 anchor formula as each other.

Pixel Snap Prototype 0.3.1 compared the existing position against nearest integer- and half-screen-pixel X snapping. The candidate shifts were all below half of one physical screen pixel and produced no visible correction. Runtime feedback instead showed a consistent pattern: right-facing fishing remained correctly centered, while both left-facing spots were visibly shifted to the right.

Source inspection explains that pattern. Version 1.0.1 converts the calibrated X correction through `bobber.lossyScale`, which forces the residual +2-pixel correction to remain screen-right on both facings. The calibration was performed on a right-facing reference, where that screen-right +2 correction corresponds to **-2 pixels in bobber-sprite local X**. The native player/bobber transform should own the left/right mirroring.

The 1.0.2 candidate therefore stores the residual correction as sprite-local **X=-2, Y=-2 game pixels** (respecting SpriteRenderer flip if it is ever used) and lets the native transform mirror it. This preserves the already-correct right-facing placement while mirroring the correction for left-facing fishing.

## Gameplay isolation

Accepted runtime/source evidence confirms the countdown implementation reads native state/timing and manages only its own visual object.

It does not write:

- fishing state;
- wait duration;
- hook duration;
- fish selection or quality;
- bait/lure data;
- energy;
- catch amount;
- achievement/quest state;
- save data.

Stable production builds create no separate Bite Countdown diagnostic log file.


## Stable release 1.0.1

Stable runtime source is tagged `v1.0.1` at commit `d7d63dc6c69109e2b0011f5eb77c2a5d206e9a67`.

Build identity:

- clean Release run: `35543919057`;
- Actions artifact: `10616015471`;
- installed DLL: `BiteCountdown.dll`;
- plugin GUID: `nikich.bitecountdown`;
- SHA-256: `a84b47e72e8f5f6dd3f7028b45c390a57bed5a236ead0951fc5b4c94f52d581c`;
- GitHub Release: `v1.0.1`.

The runtime behavior is the accepted countdown implementation. The 1.0.1 production delta is product identity only: Bite Countdown naming, fresh GUID/assembly/DLL identity, release metadata, and removal of the temporary 1.0.0 stable release surface.


## 1.0.2 candidate orientation fix

The 1.0.2 candidate changes only residual visual-anchor coordinate semantics. Fishing lifecycle, timer ownership, ring scale/alpha, missed-hook re-arm behavior and gameplay isolation are unchanged from 1.0.1.

Acceptance requires one right-facing spot and one left-facing spot to confirm:
- right-facing alignment remains correct;
- left-facing alignment is corrected;
- ring timing and missed-hook re-arm remain unchanged.
