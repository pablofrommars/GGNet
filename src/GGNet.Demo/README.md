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
| `/touch` | Tap versus drag, mark callbacks, an ancestor click handler, each pan axis, pinch ownership, double-tap reset, cursor-glued and mark-anchored tooltips, crosshair. |
| `/touch-layout` | A chart in a two-way scroll container, in a modal `<dialog>`, at three container widths, and across unmount, remount and navigation. |

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
