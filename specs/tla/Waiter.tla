------------------------------- MODULE Waiter -------------------------------
(***************************************************************************)
(* Model of src/Common/Waiter.cs for a single WaitKey.                     *)
(*                                                                         *)
(* Code                                                                    *)
(*   Wait<T>()          GetOrAdd lock, EnterReadLock, AddOrUpdate queue,   *)
(*                      ExitReadLock, then PendingWait.Register()          *)
(*   Disposition()      shared by Complete, Cancel, Throw and Timeout      *)
(*   CancelAll()        one Cancel(key) per key, used by Dispose()         *)
(*   PendingWait        a wait's timeout and cancellation callbacks call   *)
(*                      Timeout(key) and Cancel(key)                       *)
(*                                                                         *)
(* State                                                                   *)
(*   lockSlot    Locks[key]: the id of a ReaderWriterLockSlim, or None     *)
(*   waitSlot    Waits[key]: the id of a ConcurrentQueue, or None          *)
(*   queues      the contents of every queue ever created                  *)
(*                                                                         *)
(* Abstractions                                                            *)
(*   - Each ConcurrentDictionary and ConcurrentQueue call is one atomic    *)
(*     step.                                                               *)
(*   - Dequeuing a wait, finishing its task and disposing it is one atomic *)
(*     step: Disposition does them back to back on one thread.             *)
(*   - Timeout and cancellation are the same path (Disposition by key),    *)
(*     so one "callback" process per wait stands for both.                 *)
(*   - ReaderWriterLockSlim: readers share, one upgradeable holder, and a  *)
(*     writer needs no readers. Writer preference is not modeled, which    *)
(*     only allows more interleavings.                                     *)
(***************************************************************************)
EXTENDS Naturals, Sequences, FiniteSets

CONSTANTS
    Waits,          \* callers of Wait(); each adds one wait
    Completers,     \* callers of Complete(key)
    CallbackWaits,  \* waits whose timeout or cancellation callback may fire
    CancelAlls      \* callers of CancelAll() (Dispose)

None == 0
Ids  == 1..(2 * Cardinality(Waits))    \* each Wait() allocates at most one lock and one queue
Callback(w) == <<"callback", w>>

(* --algorithm Waiter
variables
    lockSlot    = None,
    waitSlot    = None,
    nextId      = 1,
    queues      = [i \in Ids |-> << >>],
    readers     = [i \in Ids |-> 0],
    upgrader    = [i \in Ids |-> FALSE],
    writer      = [i \in Ids |-> FALSE],
    enqueued    = {},                             \* waits that Wait() added
    finished    = [w \in Waits |-> "no"],         \* how each wait's task was completed
    registered  = [w \in Waits |-> FALSE],        \* Register() has run
    armed       = [w \in Waits |-> FALSE],        \* its callbacks are live (registered and not disposed since)
    misdirected = {},                             \* <<owner, finished>>: a callback finished another wait
    snapshot    = [a \in CancelAlls |-> {}];      \* waits pending when CancelAll() started

define
    Reachable(w) == waitSlot # None /\ \E i \in 1..Len(queues[waitSlot]) : queues[waitSlot][i] = w
    Pending      == {w \in enqueued : finished[w] = "no"}

    \* a pending wait can always be found through Waits[key]; otherwise nothing can complete it
    NoLostWaits == \A w \in Pending : Reachable(w)

    \* a wait's timeout or cancellation only ever finishes that wait
    CallbacksFinishOwnWait == misdirected = {}

    \* when CancelAll() returns, every wait that was pending when it started is finished
    CancelAllFinishesEveryWait ==
        \A a \in CancelAlls : pc[a] = "Done" => \A w \in snapshot[a] : finished[w] # "no"
end define;

\* Waiter.Disposition(key, action); origin is the wait whose callback called it, or None
procedure Disposition(origin, kind)
variables q = None, l = None;
begin
TryGetQueue:
    if waitSlot = None then
        return;
    else
        q := waitSlot;
    end if;
TryDequeue:
    if queues[q] = << >> then
        return;
    else
        with w = Head(queues[q]) do
            queues[q] := Tail(queues[q]);
            finished[w] := kind;                  \* action(wait)
            armed[w] := FALSE;                    \* wait.Dispose() disposes its registrations
            if origin # None /\ origin # w then
                misdirected := misdirected \cup {<<origin, w>>};
            end if;
        end with;
    end if;
TryGetLock:
    if lockSlot = None then
        return;
    else
        l := lockSlot;
    end if;
EnterUpgradeable:
    await ~writer[l] /\ ~upgrader[l];
    upgrader[l] := TRUE;
CheckEmpty:
    if queues[q] # << >> then goto ExitUpgradeable; end if;
EnterWrite:
    await readers[l] = 0;
    writer[l] := TRUE;
CheckEmptyAgain:
    if queues[q] = << >> then
RemoveQueue:
        waitSlot := None;                         \* Waits.TryRemove(key): removes whatever queue is there now
RemoveLock:
        lockSlot := None;                         \* Locks.TryRemove(key)
    end if;
ExitWrite:
    writer[l] := FALSE;
ExitUpgradeable:
    upgrader[l] := FALSE;
    return;
end procedure;

\* Waiter.Wait<T>(key)
process Wait \in Waits
variables lk = None;
begin
GetOrAddLock:
    if lockSlot = None then
        lk := nextId;
        lockSlot := nextId;
        nextId := nextId + 1;
    else
        lk := lockSlot;
    end if;
EnterRead:
    await ~writer[lk];
    readers[lk] := readers[lk] + 1;
AddOrUpdate:
    if waitSlot = None then
        queues[nextId] := << self >>;
        waitSlot := nextId;
        nextId := nextId + 1;
    else
        queues[waitSlot] := Append(queues[waitSlot], self);
    end if;
    enqueued := enqueued \cup {self};
ExitRead:
    readers[lk] := readers[lk] - 1;
Register:
    \* runs even if the wait was already completed and disposed
    registered[self] := TRUE;
    armed[self] := TRUE;
end process;

\* the timeout or cancellation callback of one wait: calls Timeout(key) or Cancel(key)
process WaitCallback \in {Callback(w) : w \in CallbackWaits}
begin
Fire:
    either
        await armed[self[2]];
        armed[self[2]] := FALSE;                  \* a callback fires at most once
        call Disposition(self[2], "timed out or cancelled");
    or
        skip;                                     \* it may never fire
    end either;
end process;

\* Waiter.Complete(key)
process Complete \in Completers
begin
CompleteKey:
    call Disposition(None, "completed");
end process;

\* Waiter.CancelAll(): one Cancel(key) for each key present
process CancelAll \in CancelAlls
begin
SnapshotKeys:
    snapshot[self] := Pending;
    if waitSlot = None then goto Done; end if;
CancelKey:
    call Disposition(None, "cancelled");
end process;
end algorithm; *)
\* BEGIN TRANSLATION (chksum(pcal) = "42fcac85" /\ chksum(tla) = "2bad9025")
CONSTANT defaultInitValue
VARIABLES pc, lockSlot, waitSlot, nextId, queues, readers, upgrader, writer, 
          enqueued, finished, registered, armed, misdirected, snapshot, stack

(* define statement *)
Reachable(w) == waitSlot # None /\ \E i \in 1..Len(queues[waitSlot]) : queues[waitSlot][i] = w
Pending      == {w \in enqueued : finished[w] = "no"}


NoLostWaits == \A w \in Pending : Reachable(w)


CallbacksFinishOwnWait == misdirected = {}


CancelAllFinishesEveryWait ==
    \A a \in CancelAlls : pc[a] = "Done" => \A w \in snapshot[a] : finished[w] # "no"

VARIABLES origin, kind, q, l, lk

vars == << pc, lockSlot, waitSlot, nextId, queues, readers, upgrader, writer, 
           enqueued, finished, registered, armed, misdirected, snapshot, 
           stack, origin, kind, q, l, lk >>

ProcSet == (Waits) \cup ({Callback(w) : w \in CallbackWaits}) \cup (Completers) \cup (CancelAlls)

Init == (* Global variables *)
        /\ lockSlot = None
        /\ waitSlot = None
        /\ nextId = 1
        /\ queues = [i \in Ids |-> << >>]
        /\ readers = [i \in Ids |-> 0]
        /\ upgrader = [i \in Ids |-> FALSE]
        /\ writer = [i \in Ids |-> FALSE]
        /\ enqueued = {}
        /\ finished = [w \in Waits |-> "no"]
        /\ registered = [w \in Waits |-> FALSE]
        /\ armed = [w \in Waits |-> FALSE]
        /\ misdirected = {}
        /\ snapshot = [a \in CancelAlls |-> {}]
        (* Procedure Disposition *)
        /\ origin = [ self \in ProcSet |-> defaultInitValue]
        /\ kind = [ self \in ProcSet |-> defaultInitValue]
        /\ q = [ self \in ProcSet |-> None]
        /\ l = [ self \in ProcSet |-> None]
        (* Process Wait *)
        /\ lk = [self \in Waits |-> None]
        /\ stack = [self \in ProcSet |-> << >>]
        /\ pc = [self \in ProcSet |-> CASE self \in Waits -> "GetOrAddLock"
                                        [] self \in {Callback(w) : w \in CallbackWaits} -> "Fire"
                                        [] self \in Completers -> "CompleteKey"
                                        [] self \in CancelAlls -> "SnapshotKeys"]

TryGetQueue(self) == /\ pc[self] = "TryGetQueue"
                     /\ IF waitSlot = None
                           THEN /\ pc' = [pc EXCEPT ![self] = Head(stack[self]).pc]
                                /\ q' = [q EXCEPT ![self] = Head(stack[self]).q]
                                /\ l' = [l EXCEPT ![self] = Head(stack[self]).l]
                                /\ origin' = [origin EXCEPT ![self] = Head(stack[self]).origin]
                                /\ kind' = [kind EXCEPT ![self] = Head(stack[self]).kind]
                                /\ stack' = [stack EXCEPT ![self] = Tail(stack[self])]
                           ELSE /\ q' = [q EXCEPT ![self] = waitSlot]
                                /\ pc' = [pc EXCEPT ![self] = "TryDequeue"]
                                /\ UNCHANGED << stack, origin, kind, l >>
                     /\ UNCHANGED << lockSlot, waitSlot, nextId, queues, 
                                     readers, upgrader, writer, enqueued, 
                                     finished, registered, armed, misdirected, 
                                     snapshot, lk >>

TryDequeue(self) == /\ pc[self] = "TryDequeue"
                    /\ IF queues[q[self]] = << >>
                          THEN /\ pc' = [pc EXCEPT ![self] = Head(stack[self]).pc]
                               /\ q' = [q EXCEPT ![self] = Head(stack[self]).q]
                               /\ l' = [l EXCEPT ![self] = Head(stack[self]).l]
                               /\ origin' = [origin EXCEPT ![self] = Head(stack[self]).origin]
                               /\ kind' = [kind EXCEPT ![self] = Head(stack[self]).kind]
                               /\ stack' = [stack EXCEPT ![self] = Tail(stack[self])]
                               /\ UNCHANGED << queues, finished, armed, 
                                               misdirected >>
                          ELSE /\ LET w == Head(queues[q[self]]) IN
                                    /\ queues' = [queues EXCEPT ![q[self]] = Tail(queues[q[self]])]
                                    /\ finished' = [finished EXCEPT ![w] = kind[self]]
                                    /\ armed' = [armed EXCEPT ![w] = FALSE]
                                    /\ IF origin[self] # None /\ origin[self] # w
                                          THEN /\ misdirected' = (misdirected \cup {<<origin[self], w>>})
                                          ELSE /\ TRUE
                                               /\ UNCHANGED misdirected
                               /\ pc' = [pc EXCEPT ![self] = "TryGetLock"]
                               /\ UNCHANGED << stack, origin, kind, q, l >>
                    /\ UNCHANGED << lockSlot, waitSlot, nextId, readers, 
                                    upgrader, writer, enqueued, registered, 
                                    snapshot, lk >>

TryGetLock(self) == /\ pc[self] = "TryGetLock"
                    /\ IF lockSlot = None
                          THEN /\ pc' = [pc EXCEPT ![self] = Head(stack[self]).pc]
                               /\ q' = [q EXCEPT ![self] = Head(stack[self]).q]
                               /\ l' = [l EXCEPT ![self] = Head(stack[self]).l]
                               /\ origin' = [origin EXCEPT ![self] = Head(stack[self]).origin]
                               /\ kind' = [kind EXCEPT ![self] = Head(stack[self]).kind]
                               /\ stack' = [stack EXCEPT ![self] = Tail(stack[self])]
                          ELSE /\ l' = [l EXCEPT ![self] = lockSlot]
                               /\ pc' = [pc EXCEPT ![self] = "EnterUpgradeable"]
                               /\ UNCHANGED << stack, origin, kind, q >>
                    /\ UNCHANGED << lockSlot, waitSlot, nextId, queues, 
                                    readers, upgrader, writer, enqueued, 
                                    finished, registered, armed, misdirected, 
                                    snapshot, lk >>

EnterUpgradeable(self) == /\ pc[self] = "EnterUpgradeable"
                          /\ ~writer[l[self]] /\ ~upgrader[l[self]]
                          /\ upgrader' = [upgrader EXCEPT ![l[self]] = TRUE]
                          /\ pc' = [pc EXCEPT ![self] = "CheckEmpty"]
                          /\ UNCHANGED << lockSlot, waitSlot, nextId, queues, 
                                          readers, writer, enqueued, finished, 
                                          registered, armed, misdirected, 
                                          snapshot, stack, origin, kind, q, l, 
                                          lk >>

CheckEmpty(self) == /\ pc[self] = "CheckEmpty"
                    /\ IF queues[q[self]] # << >>
                          THEN /\ pc' = [pc EXCEPT ![self] = "ExitUpgradeable"]
                          ELSE /\ pc' = [pc EXCEPT ![self] = "EnterWrite"]
                    /\ UNCHANGED << lockSlot, waitSlot, nextId, queues, 
                                    readers, upgrader, writer, enqueued, 
                                    finished, registered, armed, misdirected, 
                                    snapshot, stack, origin, kind, q, l, lk >>

EnterWrite(self) == /\ pc[self] = "EnterWrite"
                    /\ readers[l[self]] = 0
                    /\ writer' = [writer EXCEPT ![l[self]] = TRUE]
                    /\ pc' = [pc EXCEPT ![self] = "CheckEmptyAgain"]
                    /\ UNCHANGED << lockSlot, waitSlot, nextId, queues, 
                                    readers, upgrader, enqueued, finished, 
                                    registered, armed, misdirected, snapshot, 
                                    stack, origin, kind, q, l, lk >>

CheckEmptyAgain(self) == /\ pc[self] = "CheckEmptyAgain"
                         /\ IF queues[q[self]] = << >>
                               THEN /\ pc' = [pc EXCEPT ![self] = "RemoveQueue"]
                               ELSE /\ pc' = [pc EXCEPT ![self] = "ExitWrite"]
                         /\ UNCHANGED << lockSlot, waitSlot, nextId, queues, 
                                         readers, upgrader, writer, enqueued, 
                                         finished, registered, armed, 
                                         misdirected, snapshot, stack, origin, 
                                         kind, q, l, lk >>

RemoveQueue(self) == /\ pc[self] = "RemoveQueue"
                     /\ waitSlot' = None
                     /\ pc' = [pc EXCEPT ![self] = "RemoveLock"]
                     /\ UNCHANGED << lockSlot, nextId, queues, readers, 
                                     upgrader, writer, enqueued, finished, 
                                     registered, armed, misdirected, snapshot, 
                                     stack, origin, kind, q, l, lk >>

RemoveLock(self) == /\ pc[self] = "RemoveLock"
                    /\ lockSlot' = None
                    /\ pc' = [pc EXCEPT ![self] = "ExitWrite"]
                    /\ UNCHANGED << waitSlot, nextId, queues, readers, 
                                    upgrader, writer, enqueued, finished, 
                                    registered, armed, misdirected, snapshot, 
                                    stack, origin, kind, q, l, lk >>

ExitWrite(self) == /\ pc[self] = "ExitWrite"
                   /\ writer' = [writer EXCEPT ![l[self]] = FALSE]
                   /\ pc' = [pc EXCEPT ![self] = "ExitUpgradeable"]
                   /\ UNCHANGED << lockSlot, waitSlot, nextId, queues, readers, 
                                   upgrader, enqueued, finished, registered, 
                                   armed, misdirected, snapshot, stack, origin, 
                                   kind, q, l, lk >>

ExitUpgradeable(self) == /\ pc[self] = "ExitUpgradeable"
                         /\ upgrader' = [upgrader EXCEPT ![l[self]] = FALSE]
                         /\ pc' = [pc EXCEPT ![self] = Head(stack[self]).pc]
                         /\ q' = [q EXCEPT ![self] = Head(stack[self]).q]
                         /\ l' = [l EXCEPT ![self] = Head(stack[self]).l]
                         /\ origin' = [origin EXCEPT ![self] = Head(stack[self]).origin]
                         /\ kind' = [kind EXCEPT ![self] = Head(stack[self]).kind]
                         /\ stack' = [stack EXCEPT ![self] = Tail(stack[self])]
                         /\ UNCHANGED << lockSlot, waitSlot, nextId, queues, 
                                         readers, writer, enqueued, finished, 
                                         registered, armed, misdirected, 
                                         snapshot, lk >>

Disposition(self) == TryGetQueue(self) \/ TryDequeue(self)
                        \/ TryGetLock(self) \/ EnterUpgradeable(self)
                        \/ CheckEmpty(self) \/ EnterWrite(self)
                        \/ CheckEmptyAgain(self) \/ RemoveQueue(self)
                        \/ RemoveLock(self) \/ ExitWrite(self)
                        \/ ExitUpgradeable(self)

GetOrAddLock(self) == /\ pc[self] = "GetOrAddLock"
                      /\ IF lockSlot = None
                            THEN /\ lk' = [lk EXCEPT ![self] = nextId]
                                 /\ lockSlot' = nextId
                                 /\ nextId' = nextId + 1
                            ELSE /\ lk' = [lk EXCEPT ![self] = lockSlot]
                                 /\ UNCHANGED << lockSlot, nextId >>
                      /\ pc' = [pc EXCEPT ![self] = "EnterRead"]
                      /\ UNCHANGED << waitSlot, queues, readers, upgrader, 
                                      writer, enqueued, finished, registered, 
                                      armed, misdirected, snapshot, stack, 
                                      origin, kind, q, l >>

EnterRead(self) == /\ pc[self] = "EnterRead"
                   /\ ~writer[lk[self]]
                   /\ readers' = [readers EXCEPT ![lk[self]] = readers[lk[self]] + 1]
                   /\ pc' = [pc EXCEPT ![self] = "AddOrUpdate"]
                   /\ UNCHANGED << lockSlot, waitSlot, nextId, queues, 
                                   upgrader, writer, enqueued, finished, 
                                   registered, armed, misdirected, snapshot, 
                                   stack, origin, kind, q, l, lk >>

AddOrUpdate(self) == /\ pc[self] = "AddOrUpdate"
                     /\ IF waitSlot = None
                           THEN /\ queues' = [queues EXCEPT ![nextId] = << self >>]
                                /\ waitSlot' = nextId
                                /\ nextId' = nextId + 1
                           ELSE /\ queues' = [queues EXCEPT ![waitSlot] = Append(queues[waitSlot], self)]
                                /\ UNCHANGED << waitSlot, nextId >>
                     /\ enqueued' = (enqueued \cup {self})
                     /\ pc' = [pc EXCEPT ![self] = "ExitRead"]
                     /\ UNCHANGED << lockSlot, readers, upgrader, writer, 
                                     finished, registered, armed, misdirected, 
                                     snapshot, stack, origin, kind, q, l, lk >>

ExitRead(self) == /\ pc[self] = "ExitRead"
                  /\ readers' = [readers EXCEPT ![lk[self]] = readers[lk[self]] - 1]
                  /\ pc' = [pc EXCEPT ![self] = "Register"]
                  /\ UNCHANGED << lockSlot, waitSlot, nextId, queues, upgrader, 
                                  writer, enqueued, finished, registered, 
                                  armed, misdirected, snapshot, stack, origin, 
                                  kind, q, l, lk >>

Register(self) == /\ pc[self] = "Register"
                  /\ registered' = [registered EXCEPT ![self] = TRUE]
                  /\ armed' = [armed EXCEPT ![self] = TRUE]
                  /\ pc' = [pc EXCEPT ![self] = "Done"]
                  /\ UNCHANGED << lockSlot, waitSlot, nextId, queues, readers, 
                                  upgrader, writer, enqueued, finished, 
                                  misdirected, snapshot, stack, origin, kind, 
                                  q, l, lk >>

Wait(self) == GetOrAddLock(self) \/ EnterRead(self) \/ AddOrUpdate(self)
                 \/ ExitRead(self) \/ Register(self)

Fire(self) == /\ pc[self] = "Fire"
              /\ \/ /\ armed[self[2]]
                    /\ armed' = [armed EXCEPT ![self[2]] = FALSE]
                    /\ /\ kind' = [kind EXCEPT ![self] = "timed out or cancelled"]
                       /\ origin' = [origin EXCEPT ![self] = self[2]]
                       /\ stack' = [stack EXCEPT ![self] = << [ procedure |->  "Disposition",
                                                                pc        |->  "Done",
                                                                q         |->  q[self],
                                                                l         |->  l[self],
                                                                origin    |->  origin[self],
                                                                kind      |->  kind[self] ] >>
                                                            \o stack[self]]
                    /\ q' = [q EXCEPT ![self] = None]
                    /\ l' = [l EXCEPT ![self] = None]
                    /\ pc' = [pc EXCEPT ![self] = "TryGetQueue"]
                 \/ /\ TRUE
                    /\ pc' = [pc EXCEPT ![self] = "Done"]
                    /\ UNCHANGED <<armed, stack, origin, kind, q, l>>
              /\ UNCHANGED << lockSlot, waitSlot, nextId, queues, readers, 
                              upgrader, writer, enqueued, finished, registered, 
                              misdirected, snapshot, lk >>

WaitCallback(self) == Fire(self)

CompleteKey(self) == /\ pc[self] = "CompleteKey"
                     /\ /\ kind' = [kind EXCEPT ![self] = "completed"]
                        /\ origin' = [origin EXCEPT ![self] = None]
                        /\ stack' = [stack EXCEPT ![self] = << [ procedure |->  "Disposition",
                                                                 pc        |->  "Done",
                                                                 q         |->  q[self],
                                                                 l         |->  l[self],
                                                                 origin    |->  origin[self],
                                                                 kind      |->  kind[self] ] >>
                                                             \o stack[self]]
                     /\ q' = [q EXCEPT ![self] = None]
                     /\ l' = [l EXCEPT ![self] = None]
                     /\ pc' = [pc EXCEPT ![self] = "TryGetQueue"]
                     /\ UNCHANGED << lockSlot, waitSlot, nextId, queues, 
                                     readers, upgrader, writer, enqueued, 
                                     finished, registered, armed, misdirected, 
                                     snapshot, lk >>

Complete(self) == CompleteKey(self)

SnapshotKeys(self) == /\ pc[self] = "SnapshotKeys"
                      /\ snapshot' = [snapshot EXCEPT ![self] = Pending]
                      /\ IF waitSlot = None
                            THEN /\ pc' = [pc EXCEPT ![self] = "Done"]
                            ELSE /\ pc' = [pc EXCEPT ![self] = "CancelKey"]
                      /\ UNCHANGED << lockSlot, waitSlot, nextId, queues, 
                                      readers, upgrader, writer, enqueued, 
                                      finished, registered, armed, misdirected, 
                                      stack, origin, kind, q, l, lk >>

CancelKey(self) == /\ pc[self] = "CancelKey"
                   /\ /\ kind' = [kind EXCEPT ![self] = "cancelled"]
                      /\ origin' = [origin EXCEPT ![self] = None]
                      /\ stack' = [stack EXCEPT ![self] = << [ procedure |->  "Disposition",
                                                               pc        |->  "Done",
                                                               q         |->  q[self],
                                                               l         |->  l[self],
                                                               origin    |->  origin[self],
                                                               kind      |->  kind[self] ] >>
                                                           \o stack[self]]
                   /\ q' = [q EXCEPT ![self] = None]
                   /\ l' = [l EXCEPT ![self] = None]
                   /\ pc' = [pc EXCEPT ![self] = "TryGetQueue"]
                   /\ UNCHANGED << lockSlot, waitSlot, nextId, queues, readers, 
                                   upgrader, writer, enqueued, finished, 
                                   registered, armed, misdirected, snapshot, 
                                   lk >>

CancelAll(self) == SnapshotKeys(self) \/ CancelKey(self)

(* Allow infinite stuttering to prevent deadlock on termination. *)
Terminating == /\ \A self \in ProcSet: pc[self] = "Done"
               /\ UNCHANGED vars

Next == (\E self \in ProcSet: Disposition(self))
           \/ (\E self \in Waits: Wait(self))
           \/ (\E self \in {Callback(w) : w \in CallbackWaits}: WaitCallback(self))
           \/ (\E self \in Completers: Complete(self))
           \/ (\E self \in CancelAlls: CancelAll(self))
           \/ Terminating

Spec == Init /\ [][Next]_vars

Termination == <>(\A self \in ProcSet: pc[self] = "Done")

\* END TRANSLATION 
=============================================================================
