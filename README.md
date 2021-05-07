### Requirements
- MacGruber's Life >= 12.
- ClockwiseSilver's scripts are bundled in this var, they're all modified

### Getting started
- Load the scene in `via5.Cue.1`.
- When nothing is possessed, atom "Player" is considered the player.
- **Must be in play mode to make things work.**
- Click a person to select it, menu appears on top.
- Middle-click shows all slots in scene.

### Things to do
- In Play Mode, click the female to select it.
- Clicking HJ or BJ in the menu automatically moves the female in front of the player then starts. Click Stand or Make Idle to stop.
- Right-click on the ground to move selected person.
- Right-click on slot in front of chair to sit.
- Kiss auto detection when heads get close (stopping is iffy, there's Stop Kiss in menu, but might start again after 10s)
- Select male, right-click on sit slot in front of sofa to sit, select female, click Straddle.

### Menu
- Call: Moves in front of player
- Straddle: Sits on top of player, only works if player is sitting
- HJ: Mostly only works while standing, hand must be close enough
- BJ: Head must be close enough, crouch in front of stand works okay
- Stand: Cancels current event, stands up

- Stop kiss: Stops kissing, might restart after 10s if still close enough
- Make idle: Stops current event
- Crouch: Crouches at current position
- Genitals: Toggles showing genitals.
- Breasts: Toggles showing breasts.
- test: Don't.

- Reload: Reloads plugin

### VR support
- Only tested on Valve Index, probably won't work on anything else, some of controllers are hardcoded for now. Touching A or B while pointing on a person should show the menu on the hand, other hand can be used to click buttons. While the menu is visible for a person, it's considered selected, and the trigger on the other hand will move the person when pointing somewhere on the floor.

### Things that have initial support
- Animations: There's `res/animations.json` that contains the list of animations for different states. They're all BVH files right now.
- Clothing: There's `res/clothing.json`, it's meta information on clothing items to be able to do minimum undressing. Note that the genitals/breasts buttons in the menu can switch between item states (normal, up, etc.) but also between different items (pants and pants down for male).
- Moods and personalities for facial expressions, as well as detecting triggers for excitement. The excitement stuff changes too slowly right now, but it can be manually set in the script UI, AI tab, Force Excitement slider. Integrates with MG's Breathing.

### Slots
- The sit slot in front of the chair is just an Empty atom with a name that starts with `cue!sit`. It ignores everything after `#`, so something like `cut!sit#sofa` works. More can be added. Reload plugin when changing. The person will rotate to look in the same direction as the blue axis.
- Characters are moved to spawn points "cue!spawn#N" when the script load, in undefined order.

### AI
- Disabled for now, but basic support for randomly moving between slots in the scene.

### Broken things
- Nav sometimes gets stuck when moving close to another person, should stop after 5s.
- Navmesh doesn't update automatically when the scene is changed. There's a button in the script UI, Stuff tab.
- Characters have an ugly up/down animation when they stop, that's for heels, it gets feet unstuck. Working on it.
- Wait for animations to finish before changing states (if crouching, wait for crouch to finish before standing)
