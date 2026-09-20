# Research Plan

Target: **Graveyard Keeper 1.407**.

## Stable product status

The runtime questions required for Bite Countdown 1.x are closed.

Verified:

- vanilla selects the fish and resolves `_waiting_for_bite_delay` before the actual wait;
- the wait does not decrement until `can_take_out == true`;
- `FishingThrowingAnim.OnStateExit` is the verified initial-cast transition that enables the wait;
- the native bite occurs when the wait reaches zero and the state changes to `WaitingForPulling`;
- after a missed hook, vanilla can return directly to `WaitingForBite`, resolve another fish/wait, and repeat without a new cast;
- the bobber transform owns cast-distance placement;
- the current waiting sprite tight mesh provides the data-derived visual anchor;
- accepted residual visual offset is X=+2, Y=-2 game pixels;
- accepted alpha profile is 0.34 at full wait rising to 0.58 near the bite.

## Production mechanism

- initial ring start: postfix on verified `FishingThrowingAnim.OnStateExit`;
- missed-hook re-arm: postfix on `FishingGUI.ChangeState` when the resulting state is `WaitingForBite` and `can_take_out == true`;
- visual progress: session-local coroutine reading the existing native `_waiting_for_bite_delay`;
- visual anchor: current bobber sprite tight-mesh center plus accepted pixel offset;
- stop: any native transition out of `WaitingForBite`.

Production does not write fishing state, wait values, catch data, bait data, energy, achievements, or save data.
