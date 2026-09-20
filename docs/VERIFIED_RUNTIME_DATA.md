# Verified Runtime Data

This document stores verified Graveyard Keeper runtime facts relevant to Better Fishing Rods.

Target: **Graveyard Keeper 1.407**.

Evidence below comes from the user's current Steam installation/runtime diagnostics captured in August–September 2026, using `Assembly-CSharp 11.0.0.0`, plus direct IL inspection captured from that runtime. Historical decompilations and third-party mods remain research clues only.

## Rod identities and vanilla data

Current runtime GameBalance data confirms:

| Rod | Item ID | Tier | Base price (raw data) | Efficiency | Energy on use |
|---|---|---:|---:|---:|---:|
| Simple Fishing Rod | `fishing_rod_0` | 1 | 2 | 0.75 | -1.0 |
| Good Fishing Rod | `fishing_rod_1` | 2 | 3 | 0.90 | -0.5 |
| Excellent Fishing Rod | `fishing_rod_2` | 3 | 5 | 1.00 | -0.3 |

Implication: vanilla rod progression already improves energy efficiency. Better Fishing Rods should not casually introduce proportional extra energy costs for x2/x3 catches because that would work against an existing vanilla upgrade axis.

## Fish-selection data and waiting time

Current runtime GameBalance contains 26 `FishDefinition` rows.

Each fish definition contains:

- `item_id`;
- `fish_preset`;
- rod modifiers;
- spot/distance/time-of-day modifiers;
- `no_bait_mod`;
- per-bait modifiers;
- a `wait_time` inside each bait-data entry.

Verified examples among actually eligible combinations include different waiting values such as 5, 6, 7, 8 and 9. Waiting time is therefore not one universal fixed delay; it is part of the fish/bait data.

This matters for visual design: an exact cast-to-bite countdown would expose information derived from the already-selected fish/bait combination. A full-duration countdown is therefore not the default design for Better Fishing Rods.

## Fishing state / reaction-window data

Current runtime reflection confirms that `FishingGUI` contains:

- state `WaitingForBite`;
- state `WaitingForPulling`;
- state `Pulling`;
- state `TakingOut`;
- field `_waiting_for_pulling_time`;
- string field `FISH_CATCH_TIME_MLTPLR`;
- method `ChangeState(FishingGUI.FishingState)`.

Current runtime reflection also confirms `Fishing.FishPreset.catch_time`.

The exact 1.407 expression that combines `catch_time`, player modifiers and `_waiting_for_pulling_time` still requires direct method-body verification before production code relies on it.

## Vanilla fishing reaction buff

Current GameBalance data contains:

- buff ID `buff_fishing`;
- icon `b_fishing_1`;
- additive player resource `buff_fish_catch_time_mltplr = 2`.

The runtime also exposes `FISH_CATCH_TIME_MLTPLR` in `FishingGUI`.

This is strong evidence that the game already has a native player-parameter path for extending bite reaction time. The exact consumer expression remains to be verified before choosing the production seam.

A separate vanilla fishing buff `buff_fishing2` adds `buff_pulling_fish_mltplr = 1.5`; this affects a different part of fishing and should not be conflated with the bite reaction window.

## Successful catch / reward path

Direct current-runtime IL for `FishingGUI.UpdateTakingOut()` confirms that on successful fishing the game:

1. calls `AchievementsSystem.CheckKeyQuests("fishing_success", 1)`;
2. passes the existing `FishingGUI._fish` object to `MainGame.player.AddToInventory`;
3. if inventory insertion fails, passes that same `_fish` object to `MainGame.player.DropItem`;
4. records a fishing design event using reservoir/distance/fish identity;
5. updates known-fish discovery state and related one-time unlocks.

Important consequences:

- vanilla achievement/key-quest semantics are event-based here: one successful fishing cycle sends one `fishing_success`, independent of item quantity;
- changing only the final `_fish` amount should not multiply this success event;
- normal inventory/drop fallback already owns reward placement;
- a narrow amount change before this native reward path is preferable to custom inventory/drop handling.

Still to verify before production acceptance:

- the exact current 1.407 point where `_fish` is constructed and its quantity initialized;
- that an amount greater than one remains intact through both inventory and world-drop paths in live runtime;
- bait/lure consumption timing and count.

## Fishing visual objects

Current installed-runtime hierarchy inspection confirms dedicated player fishing render objects including:

- `fishing FX`;
- `bobber`;
- `water_fx`;
- fish/fish-shadow render objects.

The `bobber` uses a `SpriteRenderer`.

This establishes that an in-world visual cue can potentially live near the existing fishing presentation rather than in a permanent HUD.

Not yet verified:

- what animation/resource owns the current bite ripple;
- whether `water_fx` is the existing bite ripple or another effect;
- whether an existing vanilla sprite/animation can be reused for the proposed warning ring;
- the cheapest reliable event seam for starting and cancelling the cue.

## Localization / rod descriptions

Pending research:

- exact localization keys for the three rod names/descriptions;
- whether the current game already has useful rod descriptions that can be extended;
- least invasive localization/data path.

## Open runtime evidence

The remaining highest-value research questions are:

1. current IL for fish creation / `GetRandomFish` / waiting-state transition;
2. current IL for the hook reaction-window calculation;
3. bait/lure consumption path;
4. active fishing presentation at the bite moment, especially `bobber` and `water_fx`;
5. live amount > 1 inventory/drop behavior.

Do not promote historical decompilation or third-party implementation details into this file unless current runtime evidence confirms them.
