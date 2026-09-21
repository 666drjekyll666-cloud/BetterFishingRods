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

It was handed as an immutable candidate and then superseded before acceptance by 1.0.3, which carries the same orientation fix plus the requested brightness work.


## Ambient-aware ring brightness — accepted 1.0.3

Keeper's Lantern 1.0.12 source verifies that its accepted outdoor-night **Night Brightness** value is an ambient scale of **0.70**. Its world-lighting pass applies the final transformed colour to Unity's global `RenderSettings.ambientLight` in `LateUpdate`.

That gives Bite Countdown a generic host-level signal instead of a mod-specific integration:

- with vanilla lighting, `RenderSettings.ambientLight` contains the vanilla final ambient colour;
- with Keeper's Lantern, the same value already contains its final darker/tinted ambient result;
- Bite Countdown does not reference Keeper's Lantern assemblies, GUIDs, config, or state.

The accepted 1.0.3 implementation deliberately skipped a separate runtime-measurement research step at the user's request and uses a bounded visual rule:

- base ring RGB: **0.60 / 0.82 / 0.92** (reduced from 0.72 / 0.93 / 1.00);
- existing alpha curve remains **0.34 -> 0.58**;
- once per native bite wait, after one `WaitForEndOfFrame`, read `RenderSettings.ambientLight.grayscale`;
- compute RGB multiplier with `Lerp(0.62, 1.00, ambientGrayscale)`;
- apply that multiplier only to ring RGB.

The end-of-frame sample is intentional: it observes the final ambient value after ordinary `LateUpdate` lighting work such as Keeper's Lantern's ambient pass. The value is sampled once per wait rather than polled continuously; a missed-hook re-arm naturally samples again.

This is presentation-only. It does not write global lighting state, does not change alpha/timing/gameplay, and does not require a lighting mod.


### 1.0.3 runtime acceptance

The exact 1.0.3 candidate from source `2b5c3cfec638098328eaf5707d0e79effb9a1bf6` was runtime-tested on Graveyard Keeper 1.407 with Keeper's Lantern 1.0.12 active.

User acceptance on 2026-09-21 confirmed:
- daytime ring brightness is good;
- during the darker Keeper's Lantern night the ring is no longer excessively bright;
- ring centering is correct;
- the candidate is approved for stable promotion.

The fishing lifecycle, timer ownership, missed-hook re-arm path and gameplay isolation are unchanged from the previously accepted implementation.

Accepted build identity:
- clean Release run: `35623960905`;
- Actions artifact: `10651360220`;
- frozen candidate ref: `candidate/1.0.3`;
- installed DLL: `BiteCountdown.dll`;
- plugin GUID: `nikich.bitecountdown`;
- SHA-256: `77be2d8e0a03197df8e9b82ffb4b6f8971c137f585f44fba50cafd47dc575f02`.
