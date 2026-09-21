# Bite Countdown Pixel Snap Prototype 0.3.1

Research-only visual A/B/C test for the residual 1-2 rendered-pixel horizontal alignment difference observed at the swamp.

Cross-spot probe 0.3.0 showed that the swamp and village lake use the same waiting bobber sprite, tight-mesh geometry, PPU, flip state, and left-facing production anchor formula. The notable remaining difference is the screen-space fractional phase of the calculated ring center.

## Modes

During an active bite wait:

- **F1 — Current**: exact Bite Countdown 1.0.1 positioning.
- **F2 — IntegerPixel**: snap the ring center's screen X to the nearest integer pixel.
- **F3 — HalfPixel**: snap the ring center's screen X to the nearest half-pixel.

Only horizontal position is changed. Timing, scale, opacity, fish selection, bait, energy, hook window, catch amount, and saves remain untouched.

A small research label in the upper-left shows the active mode and current candidate coordinates.

## Test

1. Temporarily remove/disable stable `BiteCountdown.dll` to avoid drawing two rings.
2. Install `Bite Countdown Pixel Snap Prototype 0.3.1.dll`.
3. At the swamp, cast and switch between F1/F2/F3 while the ring is visible. Note which mode is visually centered best.
4. Repeat at the village lake (the useful left-facing control case).
5. Report the best mode for each location. If no mode is clearly better, say so.

No save warning is required. Remove the prototype DLL after the test.
