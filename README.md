### Requirements
- MacGruber's Life >= 12.
- ClockwiseSilver's scripts are bundled in this var, Kiss is modified

### Getting started
- Load the scene in `via5.Cue.1`.
- When nothing is possessed, atom "Player" is considered the player.
- Must be in play mode to make things work.

### Things to do
- Go in Play Mode.
- Click a person to select it, shows a menu on top.
- Right-click on the ground to move selected person.
- Right-click on slot in front of chair to sit.
- Kiss auto detection when heads get close
- Click Call on menu, then HJ.
- Click Crouch on menu, then BJ.

### Menu
- Call: Moves in front of player
- Straddle: Sits on top of player, only works if player is sitting.
- Crouch: crouches down
- HJ: mostly only works while standing, hand must be close enough
- BJ: head must be close enough, crouch in front of stand works okay

### VR support
- Only tested on Valve Index, probably won't work on anything else, some of controllers are hardcoded for now. Touching A or B while pointing on a person should show the menu on the hand, other hand can be used to click buttons. While the menu is visible for a person, it's considered selected, and the trigger on the other hand will move the person when pointing somewhere on the floor.

### Things that have initial support
- Animations: There's `res/animations.json` that contains the list of animations for different states. They're all BVH files right now.
- Clothing: There's `res/clothing.json`, meta information on clothing items to be able to do minimum undressing.
- Moods and personalities for facial expressions, as well as detecting triggers for excitement. The excitement stuff changes too slowly right now, but it can be manually set in the script UI, AI tab, Force Excitement slider. Integrates with MG's Breathing.

### Slots
- The sit slot in front of the chair is just an Empty atom with a name that starts with "cue!sit", I usually name them "cue!sit#1", "cue!sit#2", etc. More can be added.
- Characters are moved to spawn points "cue!spawn#N" when the script load, in undefined order.

### AI
- Disabled for now, but basic support for randomly moving between slots in the scene.

### Broken things
- Nav sometimes gets stuck when moving close to another person, should stop after 5s.
- Navmesh doesn't update automatically when the scene is changed. There's a button in the script UI, Stuff tab.
- Characters have an ugly up/down animation when they stop, that's for heels, it gets feet unstuck. Working on it.
