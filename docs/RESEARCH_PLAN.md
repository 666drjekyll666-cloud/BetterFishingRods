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
- current 1.407 `FishingThrowingAnim.OnStateExit` is the verified transition that flips `can_take_out` from false to true without changing the resolved wait;
- the native bite transition to `WaitingForPulling` occurs when the wait crosses zero;
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

Probe 0.1.1 closed the countdown lifecycle. No further runtime test is required to discover when the ring should start or end.

The remaining visual question is narrower: choose the ring asset/mechanism. During the actual wait, `water_fx` and `fishing FX` were inactive at every sampled point while the `bobber` sprite advanced through `hero_fishing_idle_bobber_frm_*` frames. This does not prove whether the subtle vanilla ripple is baked into the bobber frames or owned by another renderer.

Next preference order:

1. inspect/reuse the bobber sprite/animation only if the ripple can be isolated without scaling or distorting the float itself;
2. otherwise use one small dedicated in-world ring sprite/object anchored to the bobber, with lifecycle started by `FishingThrowingAnim.OnStateExit` and stopped by `WaitingForPulling`;
3. keep the animation driven by the already-resolved effective wait; no independent gameplay timer, permanent `Update()` Harmony patch, or global scene polling.

A visual prototype may use a session-local coroutine/tween because the ring itself must animate continuously; that recurring work exists only while one fishing wait is active and does not own gameplay state.

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

Research Probe 0.1.1 completed successfully and is now closed. Do not rebuild or replace it. Further documentation-only updates do not require hosted CI.
