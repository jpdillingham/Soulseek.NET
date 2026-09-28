---------------------------- MODULE TokenBucket ----------------------------
(***************************************************************************)
(* Model of src/Common/TokenBucket.cs.                                     *)
(*                                                                         *)
(* Code                                                                    *)
(*   GetAsync / GetInternalAsync   clamp the request, take SyncRoot, check *)
(*                                 Disposed, capture waitForReset, wait if *)
(*                                 empty, then take tokens with a CAS loop *)
(*   Return                        CAS loop, capped at 2x the capacity it  *)
(*                                 read                                    *)
(*   SetCapacity                   sets Capacity; applied on the next tick *)
(*   Clock.Elapsed                 currentCapacity := Capacity,            *)
(*                                 currentCount := currentCapacity, Reset()*)
(*   Dispose                       Clock.Dispose(), fault Disposal,        *)
(*                                 Disposed := true                        *)
(*                                                                         *)
(* Abstractions                                                            *)
(*   - Each Interlocked call, and each read or write of a field, is one    *)
(*     atomic step. A CAS loop is a read step and a compare-and-swap step. *)
(*   - Tick bodies are split into their separate writes. A tick that       *)
(*     started before Dispose() may finish after it, as System.Timers.Timer*)
(*     allows.                                                             *)
(*   - Reset signals are numbered; completing one is adding it to the set  *)
(*     of completed signals.                                               *)
(*   - Task.WhenAny returns any task that is complete; if more than one is,*)
(*     the model tries each.                                               *)
(*   - Dispose() always happens, so the liveness property can require      *)
(*     every request to finish.                                            *)
(***************************************************************************)
EXTENDS Naturals, FiniteSets

CONSTANTS
    InitialCapacity,   \* the constructor's capacity
    Capacities,        \* values SetCapacity may be called with
    Request,           \* the count each GetAsync asks for (> 0)
    ReturnAmount,      \* the count each Return gives back (> 0)
    Getters,           \* callers of GetAsync
    Cancellable,       \* getters whose cancellation token may be cancelled
    Returners,         \* callers of Return
    Setters,           \* callers of SetCapacity
    MaxTicks           \* how many times the timer may fire

None == "none"
ClockId    == <<"clock">>
DisposerId == <<"dispose">>
Min(a, b) == IF a < b THEN a ELSE b
MaxCapacity == CHOOSE c \in Capacities \cup {InitialCapacity} :
                   \A d \in Capacities \cup {InitialCapacity} : c >= d

(* --algorithm TokenBucket
variables
    capacity     = InitialCapacity,     \* Capacity (pending)
    curCapacity  = InitialCapacity,     \* currentCapacity (active)
    count        = InitialCapacity,     \* currentCount
    signal       = 0,                   \* waitForReset
    nextSignal   = 1,
    completed    = {},                  \* reset signals that have been completed
    disposal     = FALSE,               \* Disposal has been faulted
    disposed     = FALSE,               \* Disposed
    clockStopped = FALSE,
    ticks        = 0,
    semaphore    = None,                \* SyncRoot: who holds it
    cancelled    = [g \in Getters |-> FALSE],
    requested    = [g \in Getters |-> 0],
    granted      = [g \in Getters |-> 0],
    outcome      = [g \in Getters |-> "pending"];

define
    CountNeverNegative == count >= 0

    \* the burst limit: never more than 2x the largest capacity the bucket has had
    CountAtMostTwiceMaxCapacity == count <= 2 * MaxCapacity

    \* stricter: never more than 2x the active capacity, outside the middle of a tick (between applying the new
    \* capacity and refilling). Return can break this until the next tick if a tick lowers the capacity between
    \* its read of the capacity and its read of the count.
    CountAtMostTwiceActiveCapacity == pc[ClockId] = "Refill" \/ count <= 2 * curCapacity

    \* every grant is between 0 and the (clamped) request
    GrantsWithinRequest == \A g \in Getters : 0 <= granted[g] /\ granted[g] <= requested[g]

    \* every GetAsync call eventually finishes: granted, cancelled or disposed
    EveryRequestFinishes == <>(\A g \in Getters : pc[g] = "Done")
end define;

\* GetAsync(Request) and GetInternalAsync
fair+ process Getter \in Getters
variables sig = 0, current = 0, available = 0;
begin
Clamp:
    requested[self] := Min(Request, curCapacity);
AcquireSemaphore:
    either
        await semaphore = None;
        semaphore := self;
    or
        await cancelled[self];                  \* SyncRoot.WaitAsync(cancellationToken) throws
        outcome[self] := "cancelled";
        goto Done;
    end either;
CheckDisposed:
    if disposed then
        outcome[self] := "disposed";
        goto ReleaseSemaphore;
    end if;
CaptureSignal:
    sig := signal;
CheckEmpty:
    if count # 0 then goto ReadCount; end if;
WaitForAny:
    await sig \in completed \/ cancelled[self] \/ disposal;
    with winner \in {w \in {"reset", "cancel", "disposal"} :
                        \/ (w = "reset" /\ sig \in completed)
                        \/ (w = "cancel" /\ cancelled[self])
                        \/ (w = "disposal" /\ disposal)} do
        if winner = "disposal" then
            outcome[self] := "disposed";        \* await winner throws ObjectDisposedException
            goto ReleaseSemaphore;
        elsif cancelled[self] then
            outcome[self] := "cancelled";       \* ThrowIfCancellationRequested
            goto ReleaseSemaphore;
        end if;
    end with;
ReadCount:
    current := count;
    available := Min(current, requested[self]);
CompareExchange:
    if count = current then
        count := current - available;
        granted[self] := available;
        outcome[self] := "granted";
    else
        goto ReadCount;
    end if;
ReleaseSemaphore:
    semaphore := None;
end process;

\* Return(ReturnAmount)
fair process Returner \in Returners
variables readCapacity = 0, readCount = 0;
begin
ReadCapacity:
    readCapacity := curCapacity;
ReadCountForReturn:
    readCount := count;
CompareExchangeReturn:
    if count = readCount then
        count := Min(readCount + Min(ReturnAmount, readCapacity), 2 * readCapacity);
    else
        goto ReadCapacity;
    end if;
end process;

\* SetCapacity(c)
fair process Setter \in Setters
begin
SetCapacity:
    with c \in Capacities do
        capacity := c;
    end with;
end process;

\* a request's cancellation token being cancelled
fair process Canceller \in {<<"cancel", g>> : g \in Cancellable}
begin
Cancel:
    either
        cancelled[self[2]] := TRUE;
    or
        skip;
    end either;
end process;

\* Clock.Elapsed; a tick that started before Dispose() stopped the clock can still finish
fair process Clock = ClockId
variables old = 0;
begin
Tick:
    while ticks < MaxTicks do
        if clockStopped then
            goto Done;
        else
            ticks := ticks + 1;
        end if;
ApplyCapacity:
        curCapacity := capacity;
Refill:
        count := curCapacity;
SwapSignal:
        old := signal;
        signal := nextSignal;
        nextSignal := nextSignal + 1;
CompleteSignal:
        completed := completed \cup {old};
    end while;
end process;

\* Dispose()
fair process Disposer = DisposerId
begin
StopClock:
    clockStopped := TRUE;
FaultDisposal:
    disposal := TRUE;
SetDisposed:
    disposed := TRUE;
end process;
end algorithm; *)
\* BEGIN TRANSLATION (chksum(pcal) = "c966da99" /\ chksum(tla) = "fb10fe3d")
VARIABLES pc, capacity, curCapacity, count, signal, nextSignal, completed, 
          disposal, disposed, clockStopped, ticks, semaphore, cancelled, 
          requested, granted, outcome

(* define statement *)
CountNeverNegative == count >= 0


CountAtMostTwiceMaxCapacity == count <= 2 * MaxCapacity




CountAtMostTwiceActiveCapacity == pc[ClockId] = "Refill" \/ count <= 2 * curCapacity


GrantsWithinRequest == \A g \in Getters : 0 <= granted[g] /\ granted[g] <= requested[g]


EveryRequestFinishes == <>(\A g \in Getters : pc[g] = "Done")

VARIABLES sig, current, available, readCapacity, readCount, old

vars == << pc, capacity, curCapacity, count, signal, nextSignal, completed, 
           disposal, disposed, clockStopped, ticks, semaphore, cancelled, 
           requested, granted, outcome, sig, current, available, readCapacity, 
           readCount, old >>

ProcSet == (Getters) \cup (Returners) \cup (Setters) \cup ({<<"cancel", g>> : g \in Cancellable}) \cup {ClockId} \cup {DisposerId}

Init == (* Global variables *)
        /\ capacity = InitialCapacity
        /\ curCapacity = InitialCapacity
        /\ count = InitialCapacity
        /\ signal = 0
        /\ nextSignal = 1
        /\ completed = {}
        /\ disposal = FALSE
        /\ disposed = FALSE
        /\ clockStopped = FALSE
        /\ ticks = 0
        /\ semaphore = None
        /\ cancelled = [g \in Getters |-> FALSE]
        /\ requested = [g \in Getters |-> 0]
        /\ granted = [g \in Getters |-> 0]
        /\ outcome = [g \in Getters |-> "pending"]
        (* Process Getter *)
        /\ sig = [self \in Getters |-> 0]
        /\ current = [self \in Getters |-> 0]
        /\ available = [self \in Getters |-> 0]
        (* Process Returner *)
        /\ readCapacity = [self \in Returners |-> 0]
        /\ readCount = [self \in Returners |-> 0]
        (* Process Clock *)
        /\ old = 0
        /\ pc = [self \in ProcSet |-> CASE self \in Getters -> "Clamp"
                                        [] self \in Returners -> "ReadCapacity"
                                        [] self \in Setters -> "SetCapacity"
                                        [] self \in {<<"cancel", g>> : g \in Cancellable} -> "Cancel"
                                        [] self = ClockId -> "Tick"
                                        [] self = DisposerId -> "StopClock"]

Clamp(self) == /\ pc[self] = "Clamp"
               /\ requested' = [requested EXCEPT ![self] = Min(Request, curCapacity)]
               /\ pc' = [pc EXCEPT ![self] = "AcquireSemaphore"]
               /\ UNCHANGED << capacity, curCapacity, count, signal, 
                               nextSignal, completed, disposal, disposed, 
                               clockStopped, ticks, semaphore, cancelled, 
                               granted, outcome, sig, current, available, 
                               readCapacity, readCount, old >>

AcquireSemaphore(self) == /\ pc[self] = "AcquireSemaphore"
                          /\ \/ /\ semaphore = None
                                /\ semaphore' = self
                                /\ pc' = [pc EXCEPT ![self] = "CheckDisposed"]
                                /\ UNCHANGED outcome
                             \/ /\ cancelled[self]
                                /\ outcome' = [outcome EXCEPT ![self] = "cancelled"]
                                /\ pc' = [pc EXCEPT ![self] = "Done"]
                                /\ UNCHANGED semaphore
                          /\ UNCHANGED << capacity, curCapacity, count, signal, 
                                          nextSignal, completed, disposal, 
                                          disposed, clockStopped, ticks, 
                                          cancelled, requested, granted, sig, 
                                          current, available, readCapacity, 
                                          readCount, old >>

CheckDisposed(self) == /\ pc[self] = "CheckDisposed"
                       /\ IF disposed
                             THEN /\ outcome' = [outcome EXCEPT ![self] = "disposed"]
                                  /\ pc' = [pc EXCEPT ![self] = "ReleaseSemaphore"]
                             ELSE /\ pc' = [pc EXCEPT ![self] = "CaptureSignal"]
                                  /\ UNCHANGED outcome
                       /\ UNCHANGED << capacity, curCapacity, count, signal, 
                                       nextSignal, completed, disposal, 
                                       disposed, clockStopped, ticks, 
                                       semaphore, cancelled, requested, 
                                       granted, sig, current, available, 
                                       readCapacity, readCount, old >>

CaptureSignal(self) == /\ pc[self] = "CaptureSignal"
                       /\ sig' = [sig EXCEPT ![self] = signal]
                       /\ pc' = [pc EXCEPT ![self] = "CheckEmpty"]
                       /\ UNCHANGED << capacity, curCapacity, count, signal, 
                                       nextSignal, completed, disposal, 
                                       disposed, clockStopped, ticks, 
                                       semaphore, cancelled, requested, 
                                       granted, outcome, current, available, 
                                       readCapacity, readCount, old >>

CheckEmpty(self) == /\ pc[self] = "CheckEmpty"
                    /\ IF count # 0
                          THEN /\ pc' = [pc EXCEPT ![self] = "ReadCount"]
                          ELSE /\ pc' = [pc EXCEPT ![self] = "WaitForAny"]
                    /\ UNCHANGED << capacity, curCapacity, count, signal, 
                                    nextSignal, completed, disposal, disposed, 
                                    clockStopped, ticks, semaphore, cancelled, 
                                    requested, granted, outcome, sig, current, 
                                    available, readCapacity, readCount, old >>

WaitForAny(self) == /\ pc[self] = "WaitForAny"
                    /\ sig[self] \in completed \/ cancelled[self] \/ disposal
                    /\ \E winner \in {w \in {"reset", "cancel", "disposal"} :
                                         \/ (w = "reset" /\ sig[self] \in completed)
                                         \/ (w = "cancel" /\ cancelled[self])
                                         \/ (w = "disposal" /\ disposal)}:
                         IF winner = "disposal"
                            THEN /\ outcome' = [outcome EXCEPT ![self] = "disposed"]
                                 /\ pc' = [pc EXCEPT ![self] = "ReleaseSemaphore"]
                            ELSE /\ IF cancelled[self]
                                       THEN /\ outcome' = [outcome EXCEPT ![self] = "cancelled"]
                                            /\ pc' = [pc EXCEPT ![self] = "ReleaseSemaphore"]
                                       ELSE /\ pc' = [pc EXCEPT ![self] = "ReadCount"]
                                            /\ UNCHANGED outcome
                    /\ UNCHANGED << capacity, curCapacity, count, signal, 
                                    nextSignal, completed, disposal, disposed, 
                                    clockStopped, ticks, semaphore, cancelled, 
                                    requested, granted, sig, current, 
                                    available, readCapacity, readCount, old >>

ReadCount(self) == /\ pc[self] = "ReadCount"
                   /\ current' = [current EXCEPT ![self] = count]
                   /\ available' = [available EXCEPT ![self] = Min(current'[self], requested[self])]
                   /\ pc' = [pc EXCEPT ![self] = "CompareExchange"]
                   /\ UNCHANGED << capacity, curCapacity, count, signal, 
                                   nextSignal, completed, disposal, disposed, 
                                   clockStopped, ticks, semaphore, cancelled, 
                                   requested, granted, outcome, sig, 
                                   readCapacity, readCount, old >>

CompareExchange(self) == /\ pc[self] = "CompareExchange"
                         /\ IF count = current[self]
                               THEN /\ count' = current[self] - available[self]
                                    /\ granted' = [granted EXCEPT ![self] = available[self]]
                                    /\ outcome' = [outcome EXCEPT ![self] = "granted"]
                                    /\ pc' = [pc EXCEPT ![self] = "ReleaseSemaphore"]
                               ELSE /\ pc' = [pc EXCEPT ![self] = "ReadCount"]
                                    /\ UNCHANGED << count, granted, outcome >>
                         /\ UNCHANGED << capacity, curCapacity, signal, 
                                         nextSignal, completed, disposal, 
                                         disposed, clockStopped, ticks, 
                                         semaphore, cancelled, requested, sig, 
                                         current, available, readCapacity, 
                                         readCount, old >>

ReleaseSemaphore(self) == /\ pc[self] = "ReleaseSemaphore"
                          /\ semaphore' = None
                          /\ pc' = [pc EXCEPT ![self] = "Done"]
                          /\ UNCHANGED << capacity, curCapacity, count, signal, 
                                          nextSignal, completed, disposal, 
                                          disposed, clockStopped, ticks, 
                                          cancelled, requested, granted, 
                                          outcome, sig, current, available, 
                                          readCapacity, readCount, old >>

Getter(self) == Clamp(self) \/ AcquireSemaphore(self)
                   \/ CheckDisposed(self) \/ CaptureSignal(self)
                   \/ CheckEmpty(self) \/ WaitForAny(self)
                   \/ ReadCount(self) \/ CompareExchange(self)
                   \/ ReleaseSemaphore(self)

ReadCapacity(self) == /\ pc[self] = "ReadCapacity"
                      /\ readCapacity' = [readCapacity EXCEPT ![self] = curCapacity]
                      /\ pc' = [pc EXCEPT ![self] = "ReadCountForReturn"]
                      /\ UNCHANGED << capacity, curCapacity, count, signal, 
                                      nextSignal, completed, disposal, 
                                      disposed, clockStopped, ticks, semaphore, 
                                      cancelled, requested, granted, outcome, 
                                      sig, current, available, readCount, old >>

ReadCountForReturn(self) == /\ pc[self] = "ReadCountForReturn"
                            /\ readCount' = [readCount EXCEPT ![self] = count]
                            /\ pc' = [pc EXCEPT ![self] = "CompareExchangeReturn"]
                            /\ UNCHANGED << capacity, curCapacity, count, 
                                            signal, nextSignal, completed, 
                                            disposal, disposed, clockStopped, 
                                            ticks, semaphore, cancelled, 
                                            requested, granted, outcome, sig, 
                                            current, available, readCapacity, 
                                            old >>

CompareExchangeReturn(self) == /\ pc[self] = "CompareExchangeReturn"
                               /\ IF count = readCount[self]
                                     THEN /\ count' = Min(readCount[self] + Min(ReturnAmount, readCapacity[self]), 2 * readCapacity[self])
                                          /\ pc' = [pc EXCEPT ![self] = "Done"]
                                     ELSE /\ pc' = [pc EXCEPT ![self] = "ReadCapacity"]
                                          /\ count' = count
                               /\ UNCHANGED << capacity, curCapacity, signal, 
                                               nextSignal, completed, disposal, 
                                               disposed, clockStopped, ticks, 
                                               semaphore, cancelled, requested, 
                                               granted, outcome, sig, current, 
                                               available, readCapacity, 
                                               readCount, old >>

Returner(self) == ReadCapacity(self) \/ ReadCountForReturn(self)
                     \/ CompareExchangeReturn(self)

SetCapacity(self) == /\ pc[self] = "SetCapacity"
                     /\ \E c \in Capacities:
                          capacity' = c
                     /\ pc' = [pc EXCEPT ![self] = "Done"]
                     /\ UNCHANGED << curCapacity, count, signal, nextSignal, 
                                     completed, disposal, disposed, 
                                     clockStopped, ticks, semaphore, cancelled, 
                                     requested, granted, outcome, sig, current, 
                                     available, readCapacity, readCount, old >>

Setter(self) == SetCapacity(self)

Cancel(self) == /\ pc[self] = "Cancel"
                /\ \/ /\ cancelled' = [cancelled EXCEPT ![self[2]] = TRUE]
                   \/ /\ TRUE
                      /\ UNCHANGED cancelled
                /\ pc' = [pc EXCEPT ![self] = "Done"]
                /\ UNCHANGED << capacity, curCapacity, count, signal, 
                                nextSignal, completed, disposal, disposed, 
                                clockStopped, ticks, semaphore, requested, 
                                granted, outcome, sig, current, available, 
                                readCapacity, readCount, old >>

Canceller(self) == Cancel(self)

Tick == /\ pc[ClockId] = "Tick"
        /\ IF ticks < MaxTicks
              THEN /\ IF clockStopped
                         THEN /\ pc' = [pc EXCEPT ![ClockId] = "Done"]
                              /\ ticks' = ticks
                         ELSE /\ ticks' = ticks + 1
                              /\ pc' = [pc EXCEPT ![ClockId] = "ApplyCapacity"]
              ELSE /\ pc' = [pc EXCEPT ![ClockId] = "Done"]
                   /\ ticks' = ticks
        /\ UNCHANGED << capacity, curCapacity, count, signal, nextSignal, 
                        completed, disposal, disposed, clockStopped, semaphore, 
                        cancelled, requested, granted, outcome, sig, current, 
                        available, readCapacity, readCount, old >>

ApplyCapacity == /\ pc[ClockId] = "ApplyCapacity"
                 /\ curCapacity' = capacity
                 /\ pc' = [pc EXCEPT ![ClockId] = "Refill"]
                 /\ UNCHANGED << capacity, count, signal, nextSignal, 
                                 completed, disposal, disposed, clockStopped, 
                                 ticks, semaphore, cancelled, requested, 
                                 granted, outcome, sig, current, available, 
                                 readCapacity, readCount, old >>

Refill == /\ pc[ClockId] = "Refill"
          /\ count' = curCapacity
          /\ pc' = [pc EXCEPT ![ClockId] = "SwapSignal"]
          /\ UNCHANGED << capacity, curCapacity, signal, nextSignal, completed, 
                          disposal, disposed, clockStopped, ticks, semaphore, 
                          cancelled, requested, granted, outcome, sig, current, 
                          available, readCapacity, readCount, old >>

SwapSignal == /\ pc[ClockId] = "SwapSignal"
              /\ old' = signal
              /\ signal' = nextSignal
              /\ nextSignal' = nextSignal + 1
              /\ pc' = [pc EXCEPT ![ClockId] = "CompleteSignal"]
              /\ UNCHANGED << capacity, curCapacity, count, completed, 
                              disposal, disposed, clockStopped, ticks, 
                              semaphore, cancelled, requested, granted, 
                              outcome, sig, current, available, readCapacity, 
                              readCount >>

CompleteSignal == /\ pc[ClockId] = "CompleteSignal"
                  /\ completed' = (completed \cup {old})
                  /\ pc' = [pc EXCEPT ![ClockId] = "Tick"]
                  /\ UNCHANGED << capacity, curCapacity, count, signal, 
                                  nextSignal, disposal, disposed, clockStopped, 
                                  ticks, semaphore, cancelled, requested, 
                                  granted, outcome, sig, current, available, 
                                  readCapacity, readCount, old >>

Clock == Tick \/ ApplyCapacity \/ Refill \/ SwapSignal \/ CompleteSignal

StopClock == /\ pc[DisposerId] = "StopClock"
             /\ clockStopped' = TRUE
             /\ pc' = [pc EXCEPT ![DisposerId] = "FaultDisposal"]
             /\ UNCHANGED << capacity, curCapacity, count, signal, nextSignal, 
                             completed, disposal, disposed, ticks, semaphore, 
                             cancelled, requested, granted, outcome, sig, 
                             current, available, readCapacity, readCount, old >>

FaultDisposal == /\ pc[DisposerId] = "FaultDisposal"
                 /\ disposal' = TRUE
                 /\ pc' = [pc EXCEPT ![DisposerId] = "SetDisposed"]
                 /\ UNCHANGED << capacity, curCapacity, count, signal, 
                                 nextSignal, completed, disposed, clockStopped, 
                                 ticks, semaphore, cancelled, requested, 
                                 granted, outcome, sig, current, available, 
                                 readCapacity, readCount, old >>

SetDisposed == /\ pc[DisposerId] = "SetDisposed"
               /\ disposed' = TRUE
               /\ pc' = [pc EXCEPT ![DisposerId] = "Done"]
               /\ UNCHANGED << capacity, curCapacity, count, signal, 
                               nextSignal, completed, disposal, clockStopped, 
                               ticks, semaphore, cancelled, requested, granted, 
                               outcome, sig, current, available, readCapacity, 
                               readCount, old >>

Disposer == StopClock \/ FaultDisposal \/ SetDisposed

(* Allow infinite stuttering to prevent deadlock on termination. *)
Terminating == /\ \A self \in ProcSet: pc[self] = "Done"
               /\ UNCHANGED vars

Next == Clock \/ Disposer
           \/ (\E self \in Getters: Getter(self))
           \/ (\E self \in Returners: Returner(self))
           \/ (\E self \in Setters: Setter(self))
           \/ (\E self \in {<<"cancel", g>> : g \in Cancellable}: Canceller(self))
           \/ Terminating

Spec == /\ Init /\ [][Next]_vars
        /\ \A self \in Getters : SF_vars(Getter(self))
        /\ \A self \in Returners : WF_vars(Returner(self))
        /\ \A self \in Setters : WF_vars(Setter(self))
        /\ \A self \in {<<"cancel", g>> : g \in Cancellable} : WF_vars(Canceller(self))
        /\ WF_vars(Clock)
        /\ WF_vars(Disposer)

Termination == <>(\A self \in ProcSet: pc[self] = "Done")

\* END TRANSLATION 
=============================================================================
