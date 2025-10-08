How to collaborate on our Unity project:

1) Clone:
   git clone https://github.com/<your-username>/final-fantasy-prototype.git
   cd final-fantasy-prototype

2) Pick the right base branch for your area:
   - world-ui (map, movement, menus)
   - battle-system (combat logic)
   - integration (content + glue)

3) Create a feature branch off that base:
   git checkout world-ui         # or battle-system/integration
   git pull
   git checkout -b feat/<your-task>

4) Do your work → commit → push:
   git add -A
   git commit -m "feat: <short description>"
   git push -u origin feat/<your-task>

5) Open a Pull Request to main:
   - Fill out the PR template
   - Get 1 approval
   - Resolve all comments
   - Use "Squash and merge"

6) After merge:
   git checkout main
   git pull
   git branch -D feat/<your-task>   # optional cleanup
