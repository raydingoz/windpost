# Windpost – Sail/Wind Physics v10

This version rebuilds (from scratch) the boat model and the sail/wind/rudder relationship using a single consistent axis convention:
- **heading = 0** points the bow to **+Z**
- boat frame: forward = +Z, right = +X

## Run
Use a static server (ES modules require http/https):

```bash
cd windpost_threejs_pixel_sail_demo_v10
python -m http.server 8000
```

Open http://localhost:8000

## Controls
- A / D: rudder
- Q / E: trim sail
- Z / X: furl / unfurl sail (open/close)
- M: engine on/off
- T / G: throttle up/down
- Mouse drag: orbit
- W / S: zoom
- R: reset
- H: toggle HUD
