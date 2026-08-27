# NeuralEvent Transport and Authority Contract

## Purpose

The Python decoder and Unity game are separate processes. Transport therefore needs its own authority rules rather than assuming every UDP datagram can immediately mutate gameplay.

The competition path remains:

```text
EEG source
  ↓
Python acquisition / FBCCA / quality / dwell
  ↓
NeuralEvent v1
  ↓
UDP 127.0.0.1:19742
  ↓
Unity UdpNeuralReceiver
  ↓
DualAuraCombatDirector
```

Raw EEG never crosses this boundary.

---

## Background-thread receive

`UdpNeuralReceiver` owns a dedicated socket thread.

The socket thread:

- receives datagrams;
- captures a Unity-process `Stopwatch` timestamp immediately on receipt;
- pushes raw JSON into a thread-safe concurrent queue;
- never mutates Unity gameplay objects.

JSON parsing and callbacks remain on Unity's main thread.

---

## Why Python `monotonic_ns` is not a Unity latency clock

Python and Unity each have a process-local monotonic clock with an arbitrary epoch.

Therefore this is invalid:

```text
UnityNow - PythonMonotonicTimestamp
```

unless an explicit clock-mapping protocol has been established.

Mindforge retains `NeuralEvent.monotonic_ns` for source provenance, ordering, replay, and offline timing analysis. Unity measures **queue residence age** using the receive timestamp captured in the Unity process.

End-to-end physical latency must be measured with a synchronized experiment rather than by subtracting unrelated process clocks.

---

## Bounded backlog

The receiver has three protections:

```text
maxQueuedPackets
maxDrainPerFrame
maxPacketQueueAgeSeconds
```

If the render thread stalls during a large VFX event, the OS/network thread may continue receiving events. The queue is bounded so a long stall cannot create an arbitrarily large delayed command train.

Old non-critical datagrams are discarded based on Unity receive age.

Counters are exposed for:

- current queue depth;
- backpressure drops;
- stale/aged drops.

These may be shown on the spectator evidence HUD during qualification.

---

## Evidence vs gameplay authority

The receiver exposes two streams.

### `EvidenceReceived`

Newest valid decoder evidence observed in the current Unity frame.

Use for:

- `NeuralEvidenceHud`;
- non-coded aura feedback shell;
- telemetry.

### `EventReceived`

Bounded gameplay/governance authority.

At most one non-stop authority event is emitted per Unity frame.

If a stalled frame accumulated:

```text
AURA_SELECTED sight
ABSTAIN HELD
ABSTAIN HELD
```

Mindforge may show the newest `HELD` evidence to the spectator while still granting the valid Sight selection once. It does not process all three as separate gameplay commands in one frame.

---

## Control precedence

`PARTICIPANT_STOP` dominates every other event.

On participant stop:

- pending queue is discarded;
- buffs are cleared;
- BCI authority is disabled for the session path;
- combat remains safe/controller-owned.

`BCI_LOST` and `BCI_RECOVERED` are explicit control states.

An ordinary `ABSTAIN` is **not** equivalent to connection loss. It means the decoder observed a window but declined to infer a target.

---

## UDP semantics

UDP is intentionally used for the local derived-event boundary because:

- events are small;
- late evidence is usually less valuable than current evidence;
- the game must never block waiting for the BCI;
- controller combat remains authoritative.

Sequence IDs let Unity reject duplicate/out-of-order datagrams.

The system should not attempt to "reliably replay" a stale neural command merely because it was delayed. Reliability for scientific analysis belongs in the recorded/replay evidence layer, not in live gameplay command buffering.

---

## Qualification faults

The neurOS Phantom Unicorn can inject:

- delayed LSL delivery;
- dropped chunks;
- source silence;
- recovery.

The Unity receiver should be tested while simultaneously triggering:

- 20 ms Counter Pulse hit-stop;
- 55 ms Rift Cleave hit-stop;
- 80 ms Signal Break hit-stop;
- 120 ms Twin Eclipse hit-stop;
- high particle load;
- frame spikes.

Expected behavior:

```text
no conflicting multi-selection burst
no main-thread socket blocking
no unbounded queue growth
stale evidence discarded
PARTICIPANT_STOP preserved
BCI loss visible
combat remains playable
```
