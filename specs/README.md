> [!WARNING]
> This is pure AI slop, but it is helpful feedback about the correctness of critical pieces of infrastructure in the library.  Any steering that comes out of this verification is done with human hands and eyes.

# Formal specifications

This directory holds formal models of two concurrency-heavy classes, and a script that checks them.

| Class | Model | Tool |
|---|---|---|
| `src/Common/Waiter.cs` | `tla/Waiter.tla` | TLA+ (TLC model checker) |
| `src/Common/TokenBucket.cs` | `tla/TokenBucket.tla` | TLA+ (TLC model checker) |
| `src/Common/TokenBucket.cs` (arithmetic) | `lean/Specs/TokenBucket.lean` | Lean 4 (proofs) |

The models describe the code as of commit `3985b070`.

## What these are

- **A TLA+ model** describes what the class does, step by step, including every point where another thread
  can run. TLC tries *every* interleaving of the modeled threads, within the sizes set in a `.cfg` file,
  and checks a property in each state. If the property can fail, TLC prints the exact sequence of steps.
- **A Lean proof** shows that a formula always has a property, for every possible input. Lean checks the
  proof when the project builds.

A model is a description of the code, not the code itself. When a modeled class changes, update its model;
nothing detects the drift automatically.

## Running

Requirements:
- Java 11 or later, and [`tla2tools.jar`](https://github.com/tlaplus/tlaplus/releases/latest/download/tla2tools.jar)
- [elan](https://github.com/leanprover/elan) (installs the Lean version pinned in `lean/lean-toolchain`)

```sh
TLA2TOOLS=/path/to/tla2tools.jar ./specs/run.sh
```

`JAVA` and `LAKE` can point at specific executables. The script takes about 20 seconds. It exits non-zero if
any check gives a result other than the one expected.

## Checks

Each `.cfg` file checks one property. Its first line says what TLC is expected to report.

| Check | Expected | Meaning |
|---|---|---|
| `Waiter.LostWait` | violation | **Bug.** A new wait can be lost: `Disposition` checks its own queue is empty, then removes `Waits[key]` by key, which by then can hold a newer queue with a new wait in it. |
| `Waiter.CallbackTarget` | violation | **Bug.** A wait's timeout or cancellation finishes the *oldest* wait for its key, not the wait whose timer or token fired. |
| `Waiter.CancelAll` | violation | **Bug.** `CancelAll()`, and so `Dispose()`, cancels only one wait per key. |
| `TokenBucket` | pass | The count is never negative and never above 2x the largest capacity, and every grant is between 0 and the request. |
| `TokenBucket.Liveness` | pass | Every `GetAsync` call finishes, including calls waiting for a refill or queued behind one when `Dispose()` runs, and when a timer tick lands after `Dispose()`. |
| `TokenBucket.ActiveCapacity` | violation | **Accepted behavior.** After a capacity decrease, the count can exceed 2x the *new* capacity until the next tick. See below. |
| `lean` (build) | pass | See [Lean proofs](#lean-proofs). |

When a bug is fixed, update the model to match the fixed code and change the config's `EXPECT` line to `pass`.

## Waiter model

One `WaitKey`, with processes for `Wait()`, `Complete()`, `CancelAll()`, and each wait's timeout or
cancellation callback. `Disposition()` is a procedure that all of them call.

Abstractions:
- Each `ConcurrentDictionary` and `ConcurrentQueue` call is one atomic step.
- Dequeuing a wait, completing its task and disposing it is one step; `Disposition` does these back to back.
- A timeout and a cancellation take the same path (`Timeout(key)` and `Cancel(key)` both call
  `Disposition(key)`), so one callback process per wait stands for both.
- `ReaderWriterLockSlim` writer preference is not modeled. That only allows more interleavings.

## TokenBucket model

Processes for `GetAsync`, `Return`, `SetCapacity`, a cancellation token, the timer, and `Dispose()`.

Abstractions:
- Each `Interlocked` call and each field read or write is one step, so a compare-and-swap loop is a read
  step followed by a compare-and-swap step.
- The timer's `Elapsed` handler is split into its separate writes. A tick that started before `Dispose()`
  can finish after it, as `System.Timers.Timer` allows.
- `Task.WhenAny` may return any task that is complete; TLC tries each.
- `Dispose()` always happens eventually, so the liveness check can require every request to finish.

### The accepted overshoot (`TokenBucket.ActiveCapacity`)

`Return` reads the active capacity, then the count. If a tick lowers the capacity between those two reads:

1. `Return` reads a capacity of 2.
2. A tick applies a new capacity of 1 and refills the count to 1.
3. `Return` reads the count (1), and its compare-and-swap succeeds: min(1 + 2, 2 × 2) = **3**.

The count is 3, above 2x the new capacity. It is never above 2x the old capacity (the `TokenBucket` check),
and the next tick resets it.

## Lean proofs

`lean/Specs/TokenBucket.lean` proves, for all integers:

- `GetAsync` clamps a positive request to between 1 and the capacity.
- A grant is between 0 and the request, and never more than the count; the count left is never negative.
- `Return` never makes the count negative, never leaves more than 2x the capacity it read, adds at most one
  capacity's worth, and never removes tokens while the count is within 2x the capacity.
- Over any sequence of refills, grants and returns, the count stays between 0 and 2x the largest capacity.

And about 64-bit (`long`) arithmetic:

- With a capacity up to `long.MaxValue / 3`, nothing in `Return` overflows. The sum
  `current + Math.Min(count, capacity)` can reach 3x the capacity.
- With a capacity of `long.MaxValue`, `capacity * 2` wraps to -2, and a `Return` on a full bucket leaves the
  count negative. The constructor and `SetCapacity` accept any capacity. `SoulseekClient` passes
  `MaximumSpeed * 1024 / 10`, far below the limit.
