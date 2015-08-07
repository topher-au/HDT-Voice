HDT-Voice is a plugin for Hearthstone Deck Tracker that allows you to play
the game and navigate the menus using simple voice commands.

Hearthstone Deck Tracker is available from:
https://github.com/Epix37/Hearthstone-Deck-Tracker/releases

To install, simply copy HDT-Voice.dll into your Hearthstone Deck Tracker plugins
folder, then right click and select Properties and check "Unblock" at the bottom
of the panel.

Once you have installed the DLL file, open Hearthstone Deck Tracker and go to the
Options menu. Select Plugins, then select HDT-Voice and click "Enable".

Click the "Configure" button to select the audio device that HDT-Voice will use
to receive voice commands (NYI: default audio device only), as well as adjust the threshold for voice recognition.

Once you've completed the above steps, next time you load Hearthstone you should
see "HDT-Voice: Listening..." in the upper left hand corner of the window. That
means that everything has worked and you should now be able to use voice commands
in Hearthstone!

Menu Navigation
---
<h3>Main Menu</h3>
- Play
- Solo (NYI)
- Arena (NYI)
- Brawl (no deck editor)

<h3>Play Menu</h3>
- "Basic", "Custom" and "Deck 1-9" to select deck
- "Casual", "Ranked" for game type
- "Start Game" to start the game
- "Cancel" to cancel the opponent finder
- "Back" to go back

<h3>Brawl Menu</h3>
- "Start Brawl" to start brawl
- "Cancel" to cancel the opponent finder
- "Back" to go back

Playing the Game
---
You can refer to a card or minion as either "card x" or "minion x", or you
can use it's name. If there are multiple with the same name, they will be
numbered as they are ordered in the game, i.e. "Wisp 1", "Wisp 2" from left
to right.


<h2>In Game Commands</h2>
<h3>Mulligan</h3>
"click &lt;card&gt;"
"click confirm"

<h3>During Play</h3>
- "focus &lt;target&gt;"
eg "focus my hero", "focus enemy Mana Addict", "focus enemy face"

- "play &lt;card&gt;" - play a card to the right of the board
- "play &lt;card&gt; to &lt;target>" - play a card to the specified target location
e.g. "play 3", "play Pyroblast to opponent face"

- "attack &lt;enemy&gt; with &lt;friendly&gt;"
e.g. "attack face with huffer" or "attack minion 1 with hero"
NOTE: you do not need to specify who each minion belongs to

- "say &lt;emote&gt;"
thanks, well played, greetings, sorry, oops, threaten

- "choose option &lt;x&gt; of &lt;y&gt;"
when presented with options (such as certain Druid cards), x is the option you
want to select and y is the number of total options, up to 4.
Say "click" afterwards to confirm your selection.

- "end turn" - ends your turn
