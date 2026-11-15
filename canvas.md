# Touch interaction planning canvas

Session-only working view; the task plan owns decisions. Open with Mermaid preview.
Gray: existing. Blue: proposed. Green: settled. Yellow: gated. Purple: built/verified. Red: reopened.

```mermaid
flowchart LR
  subgraph Policy
    old[Crosshair currently selects touch navigation]:::existing
    environment{GATE-05: Engineer-run delayed browser probe}:::gated
    replace[STEP-01: Independent options; replace beta defaults]:::verified
    environment --> replace
    old --> replace
  end
  subgraph Gestures
    arb[STEP-02: Tap / 400 ms hold / pan arbitration]:::verified
    nav[Two-contact navigation; third contact cancels]:::verified
    replace --> arb
    arb --> nav
  end
  subgraph Feedback
    pin[Existing data-coordinate Inspection]:::existing
    pending[STEP-03: Provisional cursor and pending feedback]:::proposed
    hide[Hide stale ticks and grid until acknowledged]:::settled
    ack[Frame seam verified; inspection reconciliation built]:::gated
    arb --> pending --> ack --> pin
    nav --> ack
    nav --> hide --> ack
  end
  subgraph Verification
    checks[STEP-04: Demo, migration and local gates]:::settled
    auth{STEP-05: Audit launch authorization}:::gated
    audit[Final independent Claude audit]:::settled
    challenge[STEP-06: Challenge and affected checks]:::settled
    ack --> checks --> auth --> audit --> challenge
  end
  classDef verified fill:#ede9fe,stroke:#7c3aed,color:#4c1d95;
  classDef existing fill:#eee,stroke:#777,color:#222;
  classDef proposed fill:#dbeafe,stroke:#2563eb,color:#172554;
  classDef settled fill:#dcfce7,stroke:#16a34a,color:#14532d;
  classDef gated fill:#fef3c7,stroke:#ca8a04,color:#713f12;
```
