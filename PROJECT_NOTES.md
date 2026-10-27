# Project notes

These are some notes on how I structured BlackHoleSim 2D and what the simulation leaves out.

## Representing a ray

Each `Photon` keeps its current `X` and `Y` coordinates and a `Path` of earlier positions for drawing. The physics calculation works with polar coordinates:

- `U = 1 / radius`
- `W = dU / dAngle`
- `Angle` is the current direction around the center

Every ray initially travels to the right. Starting rays at different heights makes it easy to compare trajectories.

## Updating the path

`Physics.Step()` uses the light-ray equation for a Schwarzschild black hole:

```text
d²u/dφ² + u = (3/2) r_s u²
```

The code rewrites it as two first-order equations, then uses RK4 to estimate the next values of `U` and `W`. RK4 combines four estimates, weighted `1, 2, 2, 1`, before converting the result back to `X` and `Y`.

A ray moving directly along a radial line is handled separately so the program doesn't divide by zero when the angular movement is zero.

`MainForm.UpdateSimulation()` runs several small physics steps per timer tick. Rays stop moving when they cross the event horizon, leave the simulation area, or exceed the stored path limit.

## Drawing

The program uses Windows Forms drawing methods instead of a custom graphics engine. It draws saved paths as blue lines, active rays as small white circles, and the horizon as a black disk with an orange border.

This was a deliberate tradeoff: the 2D view is easier to follow and modify, even though the original project has more visual detail.

## Things I'd like to explore next

- Show the number of active rays in the window
- Add a way to change the starting direction of a ray
- Compare numerical trajectories with known reference cases
- Experiment with different integration step sizes

## Scope

The model uses a non-spinning black hole and an orbital plane. It does not reproduce what an observer's camera would see, and I haven't independently validated it against high-precision general-relativity calculations.
