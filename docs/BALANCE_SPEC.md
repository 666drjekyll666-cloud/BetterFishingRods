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

Exact values remain deliberately undecided until balance testing.

### Bite readability

Bite readability should improve **globally for all rods**. Keep the existing vanilla bite sound unchanged unless later evidence establishes a concrete problem with it. The cue should be in-world around the bobber, not a permanent HUD prompt.

## Accepted full-countdown concept

The visual ring represents the **full effective fish-wait interval**, not only the final fraction before the bite.

Desired sequence:

1. the cast lands;
2. the bobber/water presentation gets a short natural settling beat;
3. once vanilla considers the cast settled and actual bite waiting begins, a ring around the bobber starts its countdown;
4. the ring contracts continuously over the full effective wait;
5. it reaches the center exactly when the native bite occurs.

The ring follows the **effective wait after any Better Fishing Rods wait reduction**, so visual timing and gameplay timing cannot drift.

### Learnable fish timing is intentional

Fish/bait combinations have different native waiting times. The countdown therefore exposes a subtle timing signature of the already-selected catch. This is intentional:

- different contraction speeds/durations add variety;
- experienced players may learn recurring timing patterns;
- correctly anticipating a likely fish from timing is a small skill/knowledge reward;
- the ring never changes RNG or displays explicit fish identity.

Do not normalize every fish to one countdown duration merely to hide this information.

### Settling delay

Do not begin contraction the instant the cast touches the water.

Current 1.407 IL shows that `UpdateWaitingForBite()` returns while `can_take_out == false`; only afterward does it decrement `_waiting_for_bite_delay`.

Preferred design: start the ring from that native transition rather than adding a second arbitrary delay.

### Visual hierarchy

Prefer:

1. reuse/drive a verified existing vanilla ripple/water/fishing-FX asset;
2. reuse an existing compatible sprite near `bobber`, `fishing FX`, or `water_fx`;
3. only if unsuitable, create one small in-world ring object with session-local lifecycle.

Do not add a large `!`, screen flash, permanent UI, unrelated extra bite sound, or global scene-search/polling system.

## Energy and bait compensation

Current rod data already makes higher-tier rods more energy-efficient:

- Simple: 1.0 energy unit in raw use data;
- Good: 0.5;
- Excellent: 0.3.

Do not introduce proportional extra energy or bait costs merely to offset x2/x3 catches unless later progression evidence shows a concrete need.

## Achievement semantics

Current successful-catch IL sends one `fishing_success` event per successful fishing cycle before reward placement.

Default requirement: x2/x3 item quantity must not turn one cast into multiple fishing-success/quest events.

## Open decisions

- Good/Excellent bite-wait reduction.
- Good/Excellent reaction-window improvement.
- Ring size, opacity, thickness, and easing.
- Reuse of native ripple/fishing FX vs minimal custom ring.
- Rod prices after economy analysis.
- Final rod descriptions.
