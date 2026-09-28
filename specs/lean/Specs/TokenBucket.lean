/-!
# TokenBucket arithmetic (src/Common/TokenBucket.cs)

The TLA+ model (specs/tla/TokenBucket.tla) checks the concurrency with small numbers. This file
proves the arithmetic for every value, and shows where 64-bit (`long`) arithmetic overflows.

Each definition is one expression from the C# code, written over mathematical integers:

| Lean              | C#                                                                  |
|-------------------|---------------------------------------------------------------------|
| `clampRequest`    | `GetAsync`: `Math.Min(count, (int)Math.Min(int.MaxValue, currentCapacity))` |
| `take`            | `GetInternalAsync`: `availableCount = Math.Min(current, count)`, then `current - availableCount` |
| `returnUpdate`    | `Return`: `Math.Min(current + Math.Min(count, capacity), capacity * 2)` |
-/

namespace Specs.TokenBucket

def intMax : Int := 2147483647
def longMin : Int := -9223372036854775808
def longMax : Int := 9223372036854775807

/-- `GetAsync`, for `count > 0`: the request passed to `GetInternalAsync`. -/
def clampRequest (count capacity : Int) : Int := min count (min intMax capacity)

/-- `GetInternalAsync`: tokens granted from `current` for a request of `request`. -/
def granted (current request : Int) : Int := min current request

/-- `GetInternalAsync`: the count left after the grant. -/
def remaining (current request : Int) : Int := current - granted current request

/-- `Return`, for `count > 0`: the new count, given the capacity and count it read. -/
def returnUpdate (current capacity count : Int) : Int := min (current + min count capacity) (2 * capacity)

/-! ## GetAsync -/

/-- A positive request is clamped to between 1 and the capacity, and fits in an `int`. -/
theorem clampRequest_bounds (count capacity : Int) (hc : 0 < count) (hcap : 1 ≤ capacity) :
    1 ≤ clampRequest count capacity ∧
    clampRequest count capacity ≤ count ∧
    clampRequest count capacity ≤ capacity ∧
    clampRequest count capacity ≤ intMax := by
  unfold clampRequest intMax; simp only [Int.min_def]; split <;> split <;> omega

/-! ## GetInternalAsync -/

/-- A grant is never negative, never more than requested, and never more than the bucket holds. -/
theorem granted_bounds (current request : Int) (hcur : 0 ≤ current) (hreq : 0 ≤ request) :
    0 ≤ granted current request ∧ granted current request ≤ request ∧ granted current request ≤ current := by
  unfold granted; simp only [Int.min_def]; split <;> omega

/-- Taking tokens never leaves the count negative. -/
theorem remaining_nonneg (current request : Int) :
    0 ≤ remaining current request := by
  unfold remaining granted; simp only [Int.min_def]; split <;> omega

/-! ## Return -/

/-- `Return` never makes the count negative. -/
theorem returnUpdate_nonneg (current capacity count : Int)
    (hcur : 0 ≤ current) (hcap : 1 ≤ capacity) (hcount : 0 < count) :
    0 ≤ returnUpdate current capacity count := by
  unfold returnUpdate; simp only [Int.min_def]; split <;> split <;> omega

/-- `Return` never leaves more than 2x the capacity it read. -/
theorem returnUpdate_le_twice (current capacity count : Int) :
    returnUpdate current capacity count ≤ 2 * capacity := by
  unfold returnUpdate; simp only [Int.min_def]; split <;> split <;> omega

/-- `Return` adds at most one capacity's worth of tokens. -/
theorem returnUpdate_adds_at_most_capacity (current capacity count : Int) :
    returnUpdate current capacity count ≤ current + capacity := by
  unfold returnUpdate; simp only [Int.min_def]; split <;> split <;> omega

/-- While the count is within 2x the capacity, `Return` never removes tokens. -/
theorem returnUpdate_never_removes (current capacity count : Int)
    (hcap : 1 ≤ capacity) (hcount : 0 < count) (hcur : current ≤ 2 * capacity) :
    current ≤ returnUpdate current capacity count := by
  unfold returnUpdate; simp only [Int.min_def]; split <;> split <;> omega

/-- If the count is above 2x the capacity `Return` read (the capacity was just lowered), `Return` lowers
the count to 2x that capacity. -/
example : returnUpdate 4 1 1 = 2 := by decide

/-! ## The count stays within bounds, over any sequence of operations -/

/-- The bucket's state, for a fixed largest capacity `maxCapacity`. -/
def Inv (maxCapacity count : Int) : Prop := 0 ≤ count ∧ count ≤ 2 * maxCapacity

/-- Every way the count can change. Capacities are any value between 1 and `maxCapacity`, which
covers `Return` reading a capacity that a tick has since replaced. -/
inductive Step (maxCapacity : Int) : Int → Int → Prop
  | refill (count capacity : Int) :
      1 ≤ capacity → capacity ≤ maxCapacity → Step maxCapacity count capacity
  | take (count request : Int) :
      0 ≤ request → Step maxCapacity count (remaining count request)
  | give (count capacity amount : Int) :
      1 ≤ capacity → capacity ≤ maxCapacity → 0 < amount →
      Step maxCapacity count (returnUpdate count capacity amount)

theorem step_preserves_inv {maxCapacity count count' : Int}
    (hinv : Inv maxCapacity count) (hstep : Step maxCapacity count count') : Inv maxCapacity count' := by
  unfold Inv at *
  cases hstep with
  | refill capacity h1 h2 => omega
  | take request hreq =>
    exact ⟨remaining_nonneg count request, by
      have := (granted_bounds count request hinv.1 hreq).1
      unfold remaining; omega⟩
  | give capacity amount h1 h2 h3 =>
    exact ⟨returnUpdate_nonneg count capacity amount hinv.1 h1 h3, by
      have := returnUpdate_le_twice count capacity amount
      omega⟩

/-- Any number of steps, in any order. -/
inductive Reachable (maxCapacity : Int) : Int → Int → Prop
  | refl (count : Int) : Reachable maxCapacity count count
  | tail {a b c : Int} : Reachable maxCapacity a b → Step maxCapacity b c → Reachable maxCapacity a c

/-- Starting full, the count stays between 0 and 2x the largest capacity forever. -/
theorem count_always_in_bounds {maxCapacity initial count : Int}
    (hmax : 1 ≤ maxCapacity) (hinit : 1 ≤ initial ∧ initial ≤ maxCapacity)
    (hreach : Reachable maxCapacity initial count) : Inv maxCapacity count := by
  induction hreach with
  | refl => unfold Inv; omega
  | tail _ hstep ih => exact step_preserves_inv ih hstep

/-! ## 64-bit overflow -/

/-- With a capacity up to `long.MaxValue / 3`, every intermediate value in `Return` fits in a `long`. The sum
`current + Math.Min(count, capacity)` can reach 3x the capacity, because `current` can be 2x the capacity. -/
theorem returnUpdate_no_overflow (current capacity count : Int)
    (hcap : 1 ≤ capacity ∧ capacity ≤ 3074457345618258602)
    (hcur : 0 ≤ current ∧ current ≤ 2 * capacity)
    (hcount : 0 < count ∧ count ≤ intMax) :
    let sum := current + min count capacity
    longMin ≤ sum ∧ sum ≤ longMax ∧
    longMin ≤ 2 * capacity ∧ 2 * capacity ≤ longMax := by
  unfold longMin longMax intMax at *; simp only [Int.min_def]; split <;> omega

/-- `Return` as the C# code computes it, with `long` (wrapping) arithmetic. -/
def returnUpdateLong (current capacity count : Int64) : Int64 :=
  min (current + min count capacity) (capacity * 2)

/-- The constructor and `SetCapacity` accept any capacity. With `long.MaxValue`, `capacity * 2` wraps to
-2, and a `Return` on a full bucket leaves the count negative. -/
example : returnUpdateLong 9223372036854775807 9223372036854775807 1 < 0 := by decide

end Specs.TokenBucket
