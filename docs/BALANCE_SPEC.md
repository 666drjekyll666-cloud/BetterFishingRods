# Balance Spec

## Accepted product direction

Better Fishing Rods should reduce repeated fishing actions while keeping the vanilla fish-selection and reeling systems recognizable.

### Catch amount

- Simple Fishing Rod: x1
- Good Fishing Rod: x2
- Excellent Fishing Rod: x3

x2/x3 means multiple copies of the **same vanilla-selected catch**, not additional fish-table rolls.

### Rod progression

Upgraded rods should also:

- reduce bite waiting;
- provide a more forgiving hook reaction window.

The exact values are intentionally undecided until the 1.407 timing model is verified.

### Bite readability

Readability improvement should apply **globally to all rods**, rather than making the starter rod intentionally hard to read.

Keep the existing vanilla bite sound unless a later concrete requirement says otherwise.

Current preferred visual direction is an in-world cue around the bobber, not a HUD prompt.

## Leading bite-cue candidate

A subtle ring/ripple appears shortly before the bite and contracts toward the bobber. Reaching the center coincides with the bite.

Design goals:

- readable at normal play distance;
- visually compatible with Graveyard Keeper;
- no large icon, screen flash, or mobile-style exclamation marker;
- no new permanent UI;
- no additional gameplay input;
- one-shot/event-driven behavior where possible.

An exact full countdown from cast is **not** the default candidate because vanilla waiting time may contain information about the already-selected fish. Prefer a short fixed pre-bite telegraph unless current runtime evidence shows no meaningful information leak or a better native mechanism exists.

## Balance principles

- Do not add extra fish RNG.
- Do not automatically multiply bait or energy costs with catch amount.
- Measure economic/progression impact before changing rod prices.
- If compensation is needed, prefer one simple lever.
- Preserve vanilla rarity and fish availability unless separately approved.

## Open decisions

- Exact Good/Excellent bite-wait multipliers.
- Exact Good/Excellent hook-window values or multipliers.
- Exact pre-bite cue lead time.
- Whether the cue should reuse an existing ripple or require a minimal new asset.
- Whether rod purchase prices need adjustment after economy analysis.
- Exact player-facing rod descriptions.
