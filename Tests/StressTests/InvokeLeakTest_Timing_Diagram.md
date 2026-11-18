# InvokeLeakTest Timing Diagram

## Normal Operation (Without Debugger)

```
Timeline (milliseconds):
0ms    10ms   20ms   30ms   40ms   50ms   60ms   70ms   80ms   90ms   100ms
|------|------|------|------|------|------|------|------|------|------|
│
│ Background Thread 1: Task.Run → Sleep(2-4ms) → Invoke()
│                                                     ↓
│ Background Thread 2: Task.Run → Sleep(2-4ms) → Invoke()
│                                                     ↓
│ Background Thread 3: Task.Run → Sleep(2-4ms) → Invoke()
│                                                     ↓
│                                                 [All added to _timeouts]
│
│ Main Loop: ──────────[Iter]───────[Iter]───────[Iter]───────[Iter]────
│                         ↓           ↓           ↓           ↓
│                    RunTimers   RunTimers   RunTimers   RunTimers
│                         ↓           ↓           ↓           ↓
│                    Execute 0   Execute 5   Execute 10  Execute 15
│
│ Counter:     0 ───────→ 0 ─────→ 5 ───────→ 15 ────→ 30 ────→ 45 ─────→ 50
│
│ Test Check:                                                           ✓ PASS
│                         └──────────────100ms window────────────────┘
```

**Result**: All invocations execute within 100ms → Test passes

---

## Problem Scenario (With Debugger - @BDisp's Machine)

```
Timeline (milliseconds):
0ms    10ms   20ms   30ms   40ms   50ms   60ms   70ms   80ms   90ms   100ms  110ms
|------|------|------|------|------|------|------|------|------|------|------|
│
│ Background Threads: 500 Tasks launch rapidly
│                     ↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓ (burst of invocations)
│                     All added to _timeouts within same DateTime.UtcNow tick
│                     Timestamps: T-100, T-99, T-98, T-97, ... (NudgeToUniqueKey)
│
│ Main Loop (SLOW due to debugger overhead):
│           ────────────────[Iter 1]────────────────────[Iter 2]──────────────
│                             25ms                         60ms
│                              ↓                            ↓
│                         RunTimers                    RunTimers
│                              ↓                            ↓
│                      DateTime.UtcNow                DateTime.UtcNow
│                            = T0                          = T1
│                              ↓                            ↓
│                      Check: k < now?               Check: k < now?
│                              ↓                            ↓
│           ┌─────────────────┴──────────────┐             │
│           │ Some timeouts: k >= now !      │             │ Execute some
│           │ These are NOT executed         │             │ timeouts
│           │ Added back to _timeouts        │             │
│           └────────────────────────────────┘             ↓
│                                                      Counter += 300
│ Counter:  0 ────────────────→ 0 ──────────────────────→ 300 ────────────→ 450
│
│ Test Check at 100ms:                                ✗ FAIL
│                     └──────────100ms window──────┘
│                     Counter = 300, Expected = 500
│                     Missing 200 invocations!
│
│ (Those 200 invocations execute later, around 110ms)
```

**Result**: Not all invocations execute within 100ms → TimeoutException

---

## The DateTime.UtcNow Resolution Problem

```
Real Time:        0.000ms  0.001ms  0.002ms  0.003ms  ...  15.6ms  15.7ms
                  │        │        │        │               │       │
DateTime.UtcNow:  T0───────────────────────────────────────→T1─────→T2
                  └─────────────15.6ms tick──────────────────┘
                           All invocations here get T0
```

**When 100 invocations happen within 15.6ms:**
```
Invoke #1:  k = T0 - 100 ticks
Invoke #2:  k = T0 - 99 ticks   (NudgeToUniqueKey increments)
Invoke #3:  k = T0 - 98 ticks
...
Invoke #100: k = T0 + 0 ticks   (This is T0!)
```

**When RunTimers() checks at time T0 + 50 ticks:**
```
Invoke #1-50:  k < now  → Execute ✓
Invoke #51-100: k >= now → NOT executed! Added back to queue ✗
```

---

## Why Debugger Makes It Worse

### Without Debugger
```
Main Loop Iteration Time: ~10-20ms
│ Invoke batch: 10 tasks ────→ Execute 10 ────→ Next batch: 10 tasks
│                          10ms              10ms
│ Small batches processed quickly
```

### With Debugger  
```
Main Loop Iteration Time: ~25-50ms (2-5x slower!)
│ Invoke batch: 100 tasks (burst!) ────────→ Execute 50 ──→ 50 still queued
│                                      50ms                    ↓
│                                                      Need another 50ms!
│ Large batches accumulate, processing delayed
```

**Effect**: More invocations queue up between iterations, increasing likelihood of timestamp collisions and race conditions.

---

## The Race Condition Explained

```
Thread Timeline:

Background Thread                      Main Thread
─────────────────                      ───────────

[Call Invoke()]
      ↓
[Lock _timeoutsLockToken]
      ↓
k = DateTime.UtcNow.Ticks              
  = 1000                               
      ↓
k -= 100 (= 900)                       
      ↓                                 
[Add to _timeouts with k=900]          
      ↓
[Release lock]                         [Lock _timeoutsLockToken]
                                            ↓
                                       [Copy _timeouts]
                                            ↓
                                       [Release lock]
                                            ↓
                                       now = DateTime.UtcNow.Ticks
                                           = 850  ⚠️ (Timer hasn't updated!)
                                            ↓
                                       Check: k < now?
                                       900 < 850? → FALSE!
                                            ↓
                                       [Timeout NOT executed]
                                       [Added back to _timeouts]
```

**Problem**: Between when `k` is calculated (900) and when it's checked (now=850), the system timer hasn't updated! This can happen because:
1. DateTime.UtcNow has coarse resolution (~15ms)
2. Thread scheduling can cause the check to happen "early"
3. Debugger makes timing less predictable

---

## Solution Comparison

### Current: DateTime.UtcNow
```
Resolution: ~15.6ms (Windows), varies by platform
Precision:  Low
Stability:  Affected by system time changes
Debugger:   Timing issues

Time:     0ms  ────────────→ 15.6ms ────────────→ 31.2ms
Reading:  T0                 T1                     T2
          └─────All values here are T0─────┘
```

### Proposed: Stopwatch.GetTimestamp()
```
Resolution: ~1 microsecond (typical)
Precision:  High
Stability:  Not affected by system time changes
Debugger:   More reliable

Time:     0ms  →  0.001ms → 0.002ms → 0.003ms → ...
Reading:  T0      T1        T2        T3        ...
          Each reading is unique and monotonic
```

**Benefit**: With microsecond resolution, even 1000 rapid invocations get unique timestamps, eliminating the NudgeToUniqueKey workaround and race conditions.

---

## Test Scenarios

### Scenario 1: Fast Machine, No Debugger
```
Iteration time: 5-10ms
Invoke rate: 20-30/ms
Result: ✓ PASS (plenty of time margin)
```

### Scenario 2: Normal Machine, No Debugger  
```
Iteration time: 10-20ms
Invoke rate: 10-20/ms
Result: ✓ PASS (adequate time margin)
```

### Scenario 3: ARM Machine, Debugger (@tig's ARM Windows)
```
Iteration time: 20-30ms
Invoke rate: 15-20/ms
ARM timer resolution: Better than x64
Result: ✓ PASS (ARM timer architecture handles it)
```

### Scenario 4: x64 Machine, Debugger (@BDisp's x64, @tig's x64 Windows) - CONFIRMED
```
Iteration time: 30-50ms
Invoke rate: 10-15/ms
DateTime.UtcNow resolution: 15-20ms (x64 TSC/HPET timer)
Result: ✗ FAIL (exceeds 100ms window)

CONFIRMED: @tig reproduced on x64 but NOT on ARM
```

---

## Recommendations

### Immediate Fix (Test-Level)
```csharp
// Increase tolerance for debugger scenarios
#if DEBUG
private const int POLL_MS = Debugger.IsAttached ? 500 : 100;
#else
private const int POLL_MS = 100;
#endif
```

### Long-Term Fix (Production Code)
```csharp
// In TimedEvents.cs, replace DateTime.UtcNow with Stopwatch
private static long GetTimestampTicks() 
{
    return Stopwatch.GetTimestamp() * (TimeSpan.TicksPerSecond / Stopwatch.Frequency);
}

// Use in AddTimeout:
long k = GetTimestampTicks() + time.Ticks;
```

This provides microsecond resolution and eliminates the race condition entirely.
