\# Final Fantasy (1987) — Unity Prototype



One battle-focused vertical slice that mimics FF1 mechanics (party of 4, turn-based Fight/Run, round resolution, “Ineffective” behavior).



\## Unity

\- Version: see `ProjectSettings/ProjectVersion.txt`

\- Pipeline: Universal 2D

\- Editor: \*\*Visible Meta Files\*\*, \*\*Force Text\*\*



\## Scenes (play order)

1\. Title → Start

2\. Battle → choose Fight/Run per party member → resolve round → Victory (stub)



\## To run

\- Open with Unity Hub (folder containing `Assets/`, `Packages/`, `ProjectSettings/`).

\- Open \*\*Scenes/Battle.unity\*\* and press Play.

\- Build Settings: make sure `Title` (if added) and `Battle` are included.



\## Branch workflow

\- `main` (protected)

\- Feature branches: `feat/<task>`, PR to `main`, 1 approval, \*\*Squash \& merge\*\*.



