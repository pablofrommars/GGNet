# GGNet.Demo

Blazor Web App (Interactive Server) that exercises GGNet's interactive surface. It is the QA
harness for interactivity work and the host `tests/GGNet.E2ETests` drives.

## Run locally

```
dotnet run --project src/GGNet.Demo
```

`wwwroot/css/app.css` is the committed Tailwind build. After adding a utility class, regenerate it
with `npm ci && npm run tailwind:build` from this directory.

## Touch pages

| Page | Exercises |
|---|---|
| `/touch` | Charts with the touch policy off (`Touch = new()`): taps as ordinary clicks on marks and an ancestor, a tap standing in for hover on a cursor-glued tooltip, mouse pans. Charts with it on: immediate one-finger pan and two-finger time pan/pinch with `ScrollY` (x) and `Owned` (both), no touch navigation for a y-only chart (the page keeps swipes and pinches), and no double-tap reset. A mark-anchored tooltip without `Interactivity`. |
| `/touch-layout` | A `ScrollY` chart in a two-way scroll container, an `Owned` chart in a modal `<dialog>` (mouse tooltip above the dialog, dismissed by a touch pan), a crosshair chart at three container widths, and a touch-off chart whose taps stay clicks across unmount, remount and navigation. |

Both pages carry the **contact diagnostic**, a bar fixed to the bottom of the viewport. It reads
input on the window, ahead of GGNet's gesture module, at two layers:

- `touches` / `touch max` come from touch events and list every finger the browser receives,
  including fingers it has claimed for a native pinch or scroll.
- `pointers` / `pointer max` come from pointer events, which is what GGNet sees. When the browser
  claims a native gesture it sends `pointercancel` and stops reporting those fingers, so this count
  can drop while the fingers stay down; `cancel` rises by the number dropped.

The bar also shows down/up/cancel totals, each pointer's id, type and position (`*` marks the
primary), the page scale and the scroll offset. Reading it: if `touches` does not count a finger,
the hardware or compositor never delivered it; if `touches` counts it and `pointers` does not, the
browser claimed it; if both count it and the chart does not react, the defect is GGNet's.
`page scale` above 1.00 means a pinch zoomed the browser page, not the chart domain.

The header prints the build identity (`Version+SourceRevisionId`) under the navigation.

## Overview and detail page

`/overview-detail` shows the host-owned overview/detail pattern on generated telemetry.

| Section | Exercises |
|---|---|
| Overview and detail | Three stacked sub-panels with `onActivate` and a key each, and a separately composed detail chart that adds an equipment-activity panel; both plots stay mounted and overlap, toggled by opacity, scale and `pointer-events`. **Expand** and **Close detail** switch between them and carry the x window and the shared selection; **All** resets the viewport only and **Clear** removes the selection only. The default inspection policy pins on tap. **Native touch actions** remounts with tap-to-activate in overview, tap-to-pin in detail, double-tap reset in both, an in-chart ×, and one measured cursor label at 24 SVG units. **Owned inspection**, **ScrollY inspection** and **Navigation only** remount both plots under another touch policy. The readouts show the selected time, each plot's window (Unix seconds), its change count and the last cause. |
| Cost of the hidden chart | **Measure refresh** changes the last reading twenty times refreshing only the overview, then twenty times refreshing the hidden detail too, and prints the mean and maximum time per refresh, request to painted frame, and the difference. |
| Activation without gestures | A plot with no `Interactivity`: a tap on a mark, a grid line or the background activates at once. |
| Activation against inspection | A plot with crosshair and cursor tooltip: taps inspect and the activation counter stays at zero; the button is the explicit control. |

Crosshair inspection keeps mouse clicks from activating the overview. Native touch actions delay
single taps 300 ms for double-tap arbitration; hold, drag and multiple contacts take precedence.
The default compatibility mode retains the earlier controls and no touch double-tap reset.

## Time cursor page

`/time-cursor` exercises the shared crosshair and its readout on 24 generated hours in which no two
series are sampled at the same instant.

| Section | Exercises |
|---|---|
| Time cursor | Four time-aligned panels with `Crosshair` and `Pan`: temperature (nearest within 10 minutes) with a stepped target (value in effect) and an ambient probe that stops reporting after nine hours (missing), gravity sampled irregularly (previous within 3 hours), pressure, and cooling intervals drawn as bars (the bar containing the time, `off` between bars). The readout above the chart has a fixed height. **Clear** ends the inspection, **Reset view** restores the window, **Append readings** adds samples at the end of temperature and pressure, **New context** rebuilds the plot context. The counters under the readout show position changes, readout changes, view changes, the window and its length, and clicks that reached a gravity mark or the chart's container. |
| Crosshair without navigation | The crosshair with no readout and no `Pan`: its inferred `Owned` policy reserves the plot area for inspection, so neither one nor two fingers move the page from inside it. |
| Snapped axes | A discrete x axis: the line snaps to the category under the pointer. |
| Independent x axes | `freeX` facets: the line stays in the panel under the pointer and does not pin. |

On a touchscreen: tap to pin, drag sideways to pan at once, hold for 400 ms and then drag to scrub,
and use two fingers on the first chart to pan and pinch the time window; a third finger cancels. A
vertical drag inside the plot does nothing under the default `Owned` policy. A tap on a gravity mark
must leave both click counters at zero.

## Standalone deployment to a touch device

The demo runs as an ordinary user process from its own directory; it needs no SDK on the device,
no service and no system change. Chromium on the device reaches it over loopback.

1. Publish self-contained, stamping the working tree's identity:

   ```
   REV=$(git rev-parse --short HEAD)
   DIRTY=$( (git diff HEAD; git ls-files --others --exclude-standard | sort | xargs cat) | shasum -a 256 | cut -c1-8)
   dotnet publish src/GGNet.Demo -c Release -r linux-arm64 --self-contained true \
     -o "$OUT" -p:SourceRevisionId=$REV.dirty.$DIRTY
   ```

   Drop `.dirty.$DIRTY` for a clean tree. Publishing for a runtime identifier re-restores the
   projects; rebuild the solution afterwards before running the gates.

2. Check the device first: a free loopback port, and `libicu`, `libssl`, `libstdc++` present
   (`/sbin/ldconfig -p`). Without `libicu`, start with `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`.

3. Copy and start:

   ```
   rsync -a --delete "$OUT/" "$DEVICE:ggnet-touch-demo/app/"
   ssh "$DEVICE" 'cd ~/ggnet-touch-demo/app && chmod +x GGNet.Demo &&
     (ASPNETCORE_URLS=http://127.0.0.1:15236 ASPNETCORE_ENVIRONMENT=Production \
      setsid nohup ./GGNet.Demo > ../demo.log 2>&1 < /dev/null & echo $! > ../demo.pid)'
   ```

4. Verify before touching the screen: `curl` `/touch` on the device and compare the header's build
   identity with the one published.

5. Stop: `ssh "$DEVICE" 'kill $(cat ~/ggnet-touch-demo/demo.pid)'`. Remove `~/ggnet-touch-demo`
   to leave no trace.

Emulated touch in the browser suite is supporting evidence only; a physical touchscreen is the
acceptance surface for touch behaviour.
