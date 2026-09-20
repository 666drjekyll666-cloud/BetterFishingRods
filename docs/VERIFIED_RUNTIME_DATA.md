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

Accepted runtime visual calibration:

- offset X: **+2 game pixels**;
- offset Y: **-2 game pixels**;
- start scale: **1.55**;
- end scale: **0.08**;
- alpha at full wait: **0.34**;
- alpha near bite: **0.58**.

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
