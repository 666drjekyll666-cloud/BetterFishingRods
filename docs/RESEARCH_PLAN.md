# Research Plan

Research target: **Graveyard Keeper 1.407**.

Production implementation waits until the assumptions used by production code are verified at the cheapest reliable evidence level.

## Closed by current 1.407 IL/runtime evidence

The following are now directly established:

- rod IDs: `fishing_rod_0`, `fishing_rod_1`, `fishing_rod_2`;
- vanilla rod efficiency: 0.75 / 0.90 / 1.00;
- vanilla energy-use data: -1.0 / -0.5 / -0.3;
- one weighted fish selection occurs before waiting;
- selected fish/bait data supplies base wait;
- resolved wait gets exactly `Random.Range(0.9, 1.1)` variance;
- actual wait countdown is paused until `can_take_out == true`;
- wait expiry constructs `new Item(selected item_id, 1)`;
- vanilla hook window is `FishPreset.catch_time`, multiplied by `buff_fish_catch_time_mltplr` when that player parameter is nonzero;
- selected bait/lure is consumed/degraded once through vanilla `RemoveBait` when entering `TakingOut`;
- successful reward handling passes the same `_fish` through vanilla inventory/drop handling;
- `fishing_success` is emitted once per successful fishing cycle.

## Production seam candidates supported by evidence

### Catch amount

At native bite expiry, `_fish` already exists with quantity 1 before `ChangeState(WaitingForPulling)`.

Leading seam:

`vanilla creates selected fish Item -> rod-tier amount adjustment once -> vanilla continues`

A `ChangeState` hook filtered to `WaitingForPulling` is a strong event-driven candidate.

### Faster bite

`ChangeState(WaitingForBite)` resolves and stores the per-attempt native wait.

Leading seam: scale the resolved `_waiting_for_bite_delay` once after vanilla selection, based on rod tier. Do not alter global fish definitions or reroll selection.

### Longer hook window

Vanilla fully resolves `_waiting_for_pulling_time` when entering `WaitingForPulling`, including its own fishing-time buff.

Leading seam: scale that already-resolved timer by rod tier, preserving the vanilla buff path.

## Accepted full-countdown visual

The ring tracks the complete **effective** fish-wait interval.

Sequence:

1. cast lands;
2. native settling phase runs;
3. at the native `can_take_out=true` transition, start the ring;
4. ring contracts across the current effective `_waiting_for_bite_delay`;
5. ring reaches center when vanilla changes to `WaitingForPulling`.

Fish-dependent timing information is intentionally visible and may become learnable player knowledge.

## Remaining visual research

Probe 0.1.0 captured useful IL but accidentally zeroed the by-ref wait after `GetRandomFish`, invalidating its live mid-wait visual timing.

Probe 0.1.1 removes that patch and should answer in one normal cast:

- does current `FishingThrowingAnim.OnStateExit` coincide with `can_take_out: false -> true`;
- what `bobber`, `fishing FX`, and `water_fx` display at countdown start;
- how those objects/sprites change at 75%, 50%, 25%, 10%, and 3% remaining;
- whether the existing visual language can be reused.

Prefer an event-started one-shot animation/tween/coroutine. Do not use a permanent Harmony `Update()` patch or global scene polling.

## Edge semantics before production acceptance

Verify:

- Simple / Good / Excellent rods;
- common, quality and rare fish;
- no bait, consumable bait and durability lure;
- exact x1/x2/x3 quantity;
- no extra RNG;
- full/nearly full inventory and world drop;
- one achievement/quest success event per fishing cycle;
- vanilla fishing buffs still stack correctly;
- save/load and DLL removal.

## CI policy

Do not run hosted CI for docs/bookkeeping.

Research Probe 0.1.1 is a new immutable binary and therefore gets one deliberate clean Release build gate before handoff.
