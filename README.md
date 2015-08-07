<h2>What is HDT-Voice?</h2>
HDT-Voice is a plugin for Hearthstone Deck Tracker that allows you to play the game and navigate the menus using simple voice commands.

<h2>Sounds good, how do I get it?</h2>
The latest version of HDT-Voice will always be available here:
https://github.com/topher-au/HDT-Voice/releases

Since HDT-Voice is a plugin for Hearthstone Deck Tracker, you'll also need to download and configure it before you are able to use HDT-Voice.

Hearthstone Deck Tracker is available from:
https://github.com/Epix37/Hearthstone-Deck-Tracker/releases

<h2>OK, I got the download, now what?</h2>

To install, simply copy HDT-Voice.dll into your Hearthstone Deck Tracker plugins folder, then right click and select Properties and check "Unblock" at the bottom of the panel.

Once you have installed the DLL file, open Hearthstone Deck Tracker and go to the Options menu. Select Plugins, then select HDT-Voice and click "Enable".

Click the "Configure" button to select the audio device that HDT-Voice will use to receive voice commands (NYI: default audio device only), as well as adjust the threshold for voice recognition.

Once you've completed the above steps, next time you load Hearthstone you should see "HDT-Voice: Listening..." in the upper left hand corner of the window. That means that everything has worked and you should now be able to use voice commands in Hearthstone!

<h2>I've done all that, now how do I attack face?!</h2>
Good thing you asked! The first thing you need to know is that the plugin operates in two different modes: <i>menu mode</i> and <i>game mode</i>.

<h3>Menu Mode</h3>

From the main menu you will probably want to say...
- <b>"Play"</b>
- <b>"Solo"</b> (NYI)
- <b>"Arena"</b> (NYI)
- <b>"Brawl"</b> (no deck editor)

If you're at the Play menu/deck selection screen, you might want to say...
- <b>"Basic"</b>, "Custom"</b> or"Deck 1-9"</b> to select deck
- <b>"Casual"</b> or "Ranked" for game type
- <b>"Start Game"</b> to start the game
- <b>"Cancel"</b> to cancel the opponent finder
- <b>"Back"</b> to go back

Or maybe the plugin thought you said "brawl", in which case say...
- <b>"Start Brawl"</b> to start brawl
- <b>"Cancel"</b> to cancel the opponent finder
- <b>"Back"</b> to go back

<h3>Game Mode</h3>
When referring to a card or minion, you may refer to it by it's name or by it's number.
- <i>&lt;card&gt;</i> - a card name or number ("1", "2", "Sludge Belcher", "Armorsmith 2")
- <i>&lt;friendly&gt;</i> - a friendly minion ("minion 3", "Sludge Belcher 2")
- <i>&lt;enemy&gt;</i> - an enemy minion
- <i>&lt;target&gt;</i> - a target, specify which side ("friendly minion 4", "enemy hero")

The first thing you do when you start a game of Hearthstone is mulligan a card (or all of them). To do so, say one of the following:
- <b>"click &lt;card&gt;"</b> ("click 2", "click Dr. Boom")
- <b>"click confirm"</b>

Once the game has started, you can perform a variety of commands as listed below.

- <b>"target &lt;target&gt;"</b> ("target friendly face", "target my minion 1", "target enemy hero")

- <b>"play &lt;card&gt;"</b> - play a card to the right of the board
- <b>"play &lt;card&gt; to &lt;friendly&gt;"</b> - play a minion to the left of the friendly minion specified
- <b>"cast &lt;card&gt; on &lt;target&gt;"</b> - play a card onto the minion specified

- <b>"use hero power"</b> - use hero power
- <b>"use hero power on &lt;target&gt;"</b> - use hero power on specified minion

- <b>"attack &lt;enemy&gt; with &lt;friendly&gt;"</b> ("Attack Sylvanas Windrunner with Dr. Boom")
- <b>"&lt;friendly&gt; go &lt;enemy&gt;"</b> ("Huffer go face")

- <b>"say &lt;emote&gt;"</b>
thanks/thank you, well played, greetings/hello, sorry, oops, threaten

- <b>"choose option &lt;x&gt; of &lt;y&gt;"</b>
when presented with options (such as certain Druid cards), x is the option you
want to select and y is the number of total options, up to 4.
Say "click" afterwards to confirm your selection.

- <b>"end turn"</b> - ends your turn

<h3>Synonyms!</h3>
You can use the following synonyms:
- hero, face
- enemy, opposing, opponent
- my, friendly

<h2>Troubleshooting</h2>
Please check that your default audio device is set to the microphone used for speech recognition and restart Hearthstone Deck Tracker. You may also want to adjust the threshold level in the settings window (available from within the HDT plugins panel)

You can also enable the debug log and send it to me, it might help!
