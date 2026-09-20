# Balance Spec

## Accepted product direction

Better Fishing Rods should reduce repeated fishing actions while preserving the vanilla fish-selection and reeling systems.

### Catch amount

- Simple Fishing Rod: x1
- Good Fishing Rod: x2
- Excellent Fishing Rod: x3

x2/x3 means multiple copies of the **same vanilla-selected catch**, not additional fish-table rolls.

### Rod progression

Upgraded rods should also:

- reduce bite waiting;
- provide a more forgiving hook reaction window.

Exact values remain deliberately undecided until the current timing calculations are verified.

### Bite readability

Bite readability should improve **globally for all rods**.

Keep the existing vanilla bite sound unchanged unless later evidence establishes a concrete problem with it.

The cue should be in-world around the bobber, not a permanent HUD prompt.

## Leading bite-cue design

Current preferred direction:

- shortly before the native bite, a subtle ring/ripple appears around the bobber;
- it contracts toward the bobber;
- reaching the center coincides with the actual bite;
- the normal vanilla splash/bobber motion/sound remain the actual bite event;
- the cue is global and purely informational; it does not change fish selection or create new input.

### Why not a full cast-to-bite countdown by default

Current fish data proves that waiting time varies by selected fish and bait.

A ring that begins immediately after the cast and exactly represents the full native wait would therefore expose hidden information about the already-selected catch. It could become an unintended fish detector.

Preferred solution: a **short fixed pre-bite telegraph**. It still gives the player a clear “get ready now” signal and readable timing while preserving most of the uncertainty of the vanilla wait.

The exact lead time is still open. A rough research range such as 0.7–1.0 seconds may be tested, but it is not yet a balance decision.

### Visual hierarchy

Prefer mechanisms in this order:

1. reuse/drive a verified existing vanilla ripple/water effect if it can express the cue cleanly;
2. reuse an existing compatible sprite/animation near `bobber`/`water_fx`;
3. only if those are unsuitable, create one tiny in-world ring asset/object with session-local lifecycle.

Do not add:

- a large `!` marker;
- screen flash;
- permanent UI;
- a continuously searching/polling visual system.

## Energy and bait compensation

Current rod data already makes higher-tier rods more energy-efficient:

- Simple: 1.0 energy unit in raw use data;
- Good: 0.5;
- Excellent: 0.3.

Therefore Better Fishing Rods should **not** introduce proportional extra energy costs merely to offset x2/x3 catches unless later progression evidence shows a concrete need.

Likewise, do not automatically consume x2/x3 bait. The product goal is fewer repeated actions, not mathematical preservation of vanilla output per click.

## Achievement semantics

Current successful-catch IL sends one `fishing_success` event per successful fishing cycle before reward placement.

Default requirement: x2/x3 item quantity must not turn one cast into multiple fishing-success/quest events.

## Balance principles

- No extra fish RNG.
- Preserve vanilla fish rarity and availability.
- Preserve one successful fishing cycle as one gameplay/achievement event.
- Prefer one simple economy adjustment, if one is actually needed, over several compensating penalties.
- Measure the practical value of x2/x3 before changing rod prices.

## Open decisions

- Good/Excellent bite-wait reduction.
- Good/Excellent reaction-window improvement.
- Exact fixed pre-bite telegraph duration.
- Reuse of native ripple vs minimal custom ring.
- Rod prices after economy analysis.
- Final player-facing rod descriptions.
