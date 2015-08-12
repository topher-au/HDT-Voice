# What is HDT-Voice?
HDT-Voice is a plugin for Hearthstone Deck Tracker that allows you to play the game and navigate the menus using simple voice commands.

# What's new in this version
- Toggle the plugin on and off using the F12 key
- Lots of additions to the grammar table, see below.
- Bugfixes

# Requirements and Download

The requirements for HDT-Voice are as follows:
- [HDT-Voice plugin (download here)](https://github.com/topher-au/HDT-Voice/releases)
- [Hearthstone Deck Tracker](https://github.com/Epix37/Hearthstone-Deck-Tracker)
- [Microsoft Speech Recognition engine](https://www.google.com/?q=install+microsoft+speech+recognition)

# Installation Instructions

1. Download and extract the HDT-Voice plugin file from above
2. Copy it into the Hearthstone Deck Tracker\plugins folder
3. Right click HDT-Voice.dll and click properties
<p>![An image of the Windows Explorer context menu for HDT-Voice.dll](http://i.imgur.com/KBZMKog.png)</p>
4. At the bottom of the properties windows, check "Unblock" to allow Windows to run the plugin
<p>![An image of the Windows Explorer properties pane showing the Unblock checkbox](http://i.imgur.com/ZNtWyma.png)</p>
5. Load up Hearthstone Deck Tracker and select options at the top of the window
<p>![An image showing where to find the options button](http://i.imgur.com/cYJ6eF7.png)</p>
6. Choose plugins on the left and the select HDT-Voice and move the slider to enable the plugin
<p>![A view of the Hearthstone Deck Tracker settings window](http://i.imgur.com/Hl2vxBg.png)</p>
7. Click Settings to change some basic settings
<p>![The Settings window for HDT-Voice](http://i.imgur.com/FNVp9Lx.png)</p>
8. If all has gone successfully, you should now see "HDT-Voice: Listening... on your Hearthstone Deck Tracker overlay


# Commands

## Menu Mode

Since it is impossible to tell which menu the game is on, all of the following commands are available at any time.

### Main Menu
Command | Action
-------|--------
play | Open play mode menu
solo | Open solo adventures menu
arena | Open Arena menu
brawl | Open Tavern Brawl menu
open packs | Open card packs

### Play Menu
Command | Action
-------|--------
deck 1-9 | Select which deck to use
basic | Switch to basic decks
custom | Switch to custom decks
ranked | Switch to Ranked play
casual | Switch to Casual play
start game | Start game with current settings

### Arena Menu
Command | Action
-------|--------
cancel arena | Close the purchase arena screen
buy arena with gold | Purchase arena run with gold
hero 1-3 | Select hero
choose | Confirm hero selection
card 1-3 | Draft cards
start arena | Begin Arena run

### Brawl Menu
Command | Action
-------|--------
start brawl | Start brawl

### Opening card packs
Command | Action
-------|--------
open top pack | Open card pack
open bottom pack | Open card pack
open card 1-5 | Open card (clockwise from top)
done | Close pack

## Game Mode

Key | Replace with...
-------|--------
&lt;card&gt; | A card number or name (e.g. "2", "Sludge Belcher", "4")
&lt;friendly&gt; | A friendly minion name or number ("minion 4", "Kel'thuzad"), or "hero/face"
&lt;enemy&gt; | An enemy minion name or number ("minion 6", "Dr. Boom"), or "hero/face"
&lt;target&gt; | A target minion or hero, prefixed by whether they are friendly or enemy (i.e. "my face", "enemy hero")

### Other synonyms
Word | Also use...
-------|--------
friendly | my
enemy | opposing, opponent
hero | face

### Mulligan
Command | Action
-------|--------
click &lt;card&gt; | Toggle card for mulligan
click confirm | Confirm selection

### During Play
### Cursor and targeting
Command | Action
-------|--------
target &lt;target&gt; | Moves the cursor to the target ("target friendly minion 3")
target card &lt;card&gt; | Targets the specified card in your hand
click &lt;target&gt; | Clicks on the specified target ("click my face")
click | Sends a click to the current cursor location
cancel | Sends a right click

### Using your Hero Power
Command | Action
-------|--------
hero power (on &lt;target&gt;) | Use your hero power
&lt;hero power name&gt; (on &lt;target&gt;) | Use your hero power

### Playing minions from hand
Command | Action
-------|--------
play &lt;card&gt; | Plays a minion to the right side of the board
&lt;card&gt; on/to &lt;target&gt; | Plays a minion to the left of the specified friendly target

### Playing spells from hand
Command | Action
-------|--------
play &lt;card&gt; | Uses the spell card
&lt;card&gt; on/to &lt;target&gt; | Uses the spell card on the specified target

### Attacking with minions
Command | Action
-------|--------
attack &lt;enemy&gt; with &lt;friendly&gt; | ("Attack face with minion 3")
&lt;friendly&gt; go &lt;enemy&gt; | As above, but shorter ("Huffer go face!")

### Selecting card options
Command | Action
-------|--------
choose option &lt;x&gt; of &lt;y&gt; | When presented with *y* options, select option *x*

### Sending emotes
Command | Action
-------|--------
say &lt;emote&gt; | hello/greetings, oops, sorry, threaten, well played, thanks/thankyou


# Help!
Please check that your default audio device is set to the microphone used for speech recognition and restart Hearthstone Deck Tracker. You may also want to adjust the threshold level in the settings window (available from within the HDT plugins panel)

If you don't have a compatible speech recognition engine installed, you will need one. This can be enabled through your Windows settings.

You can also enable the debug log and send it to me, it might help!
