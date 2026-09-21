# Bite Countdown Anchor Audit Probe 0.3.0

Research-only observation probe for Graveyard Keeper 1.407.

## Question

Bite Countdown 1.0.1 is visually centered at previously tested fishing spots, while the swamp appears about 1-2 rendered pixels too far to the right.

The open question is whether the difference follows:

- fishing spot identity;
- left/right fishing direction;
- bobber transform mirroring;
- SpriteRenderer flip state;
- a different waiting sprite / tight-mesh center;
- or screen-space rounding.

## What the probe does

The probe uses the same verified initial-cast lifecycle seam as production: `FishingThrowingAnim.OnStateExit`, followed by the same one-frame delay used before Bite Countdown reads the idle bobber sprite.

Once per cast it records:

- scene, fishing spot ID, cast distance and native `is_to_right`;
- player and bobber position/scale;
- current bobber sprite, `flipX` / `flipY`, PPU and tight-mesh extents;
- the exact mesh-center and +2/-2 pixel correction calculation used by Bite Countdown 1.0.1;
- resulting world and screen coordinates;
- renderer-bounds center and camera pixel/orthographic data for comparison.

It renders nothing, changes no fishing values, and writes no save data. It has a separate plugin GUID and may remain installed alongside Bite Countdown 1.0.1.

## Runtime test

1. Keep stable `BiteCountdown.dll` 1.0.1 installed.
2. Add `Bite Countdown Anchor Audit Probe 0.3.0.dll` to `BepInEx/plugins/`.
3. Visit every available fishing spot.
4. At each spot, make **one cast at the same distance**; middle distance is preferred.
5. Wait until the normal Bite Countdown ring appears. The catch itself does not need to be completed.
6. After all spots are sampled, exit the game and return only:

`BepInEx/BiteCountdownAnchorAuditProbe-0.3.0.log`

No save restriction or cleanup is required beyond removing the probe DLL when the test is complete.
