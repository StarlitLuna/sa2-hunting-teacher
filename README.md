# SA2 Hunting Teacher

This project aims to help new SA2 runners learn hunting pieces efficiently.

## Description

Normally new runners to SA2 face a daunting task for learning all of the emerald pieces in the game required to effectively run either of the 2 main story modes that make up the 2 most popular categories.
This process generally involves spending countless hours grinding each individual hunting stage over and over until the player feels as though they've learned all of the possible pieces in that stage and there truly are no shortcuts.
Even then it's possible (and indeed very common) that one or two very rare pieces could have slipped through the cracks of this process.
So I developed this project to aid new players in learning their pieces by playing through a curated list of sets that guarantees the player will not only see every single possible piece in the stage, but also that they will do so as efficiently as possible
without any unnecessarily repeated sets.

## Getting Started

### Dependencies

* Windows
  * Linux running Wine should also work but this tool is not designed for or tested under Linux.
* [Sonic Adventure 2 - Steam Version](https://store.steampowered.com/app/213610/Sonic_Adventure_2/)
  * There are minor differences between the different platform releases but in general learning the pieces on 1 is enough to know them on all
  * That said, this tool is intended for the Steam version, and requires the Steam version of the game to be running alongside on the same machine.
* [.NET 8 Desktop Runtime](https://aka.ms/dotnet-core-applaunch?missing_runtime=true&arch=x64&apphost_version=8.0.0&gui=true)
  * Installed in your Wine environment if running Linux
* Optional but HIGHLY recommended:
  * [SA Mod Manager](https://github.com/X-Hax/SA-Mod-Manager/releases/latest)
	* This tool doesn't require the mod loader to be installed, but it's highly recommended you have this anyway for the general fixes the mod loader provides
	  as well as for the ability to install QoL speedrunning mods and practice mods.
  * [SA2 Story Style Upgrades](https://github.com/StarlitLuna/sa2-story-style-upgrades/releases/latest)
	* With `Include Current Hunting Level Upgrade` set to ON
	* Can also be 1-Click installed from [GameBanana](https://gamebanana.com/mods/478254)
  * A [fully completed save file](https://www.speedrun.com/sa2b/resources/acci5) to play on
	* Or at least one which has all the hunting levels unlocked for access through Stage Select

### Installing

The latest version can be found for download as an executable file on the [releases tab.](https://github.com/StarlitLuna/sa2-hunting-teacher/releases)

### Usage

To use this tool:

* Run it as an Administrator (you should get a prompt to run as admin anyway)
* Ensure SA2 is running before pressing start (it doesn't matter which order you open this or SA2 in as long as both are running)
* Configure the tool
  * Set the level you want to learn and number of repetitions
* Press start in Hunting Teacher app
* In game, load into the level you selected in the app
* Continue playing this level until your sequence is complete!

This tool will ensure that for each repetition of a sequence you see every single piece in the level at least once.
While running this tool you will have infinite lives so you don't need to worry about game overing.
The tool will also ensure that your set does NOT change if you die or restart the level.
Exiting the stage will also not progress the sequence so you get the same set you were already on until you collect all 3 pieces and "win" the level.
It's necessary to actually touch the third piece and trigger a win condition to progress the sequence.

#### Repetitions

When you configure your repetitions the default behavior is that you will play through all sets once before you go back to the first set and then it repeats.
The sequence completes when you repeat all sets the number of repetitions you configure. Additionally, there is a setting that changes the behavior of repetitions.
When enabled, each set will repeat the number of repetitions you configure before you move on to the next set. The sequence completes when you see the last set and play it
the number of repetitions you have configured. This option is to help re-enforce your learning in whichever way you feel would be more conducive towards memorizing the information.

e.g. with Repetitions in Place off (default) and 3 repetitions: A -> B -> C -> A -> B -> C -> A -> B -> C -> End

e.g. with Repetitions in Place on and 3 repetitions:            A -> A -> A -> B -> B -> B -> C -> C -> C -> End

Once a sequence is complete the tool will automatically reset itself to allow you to train in other levels.
You can also break out of a sequence early at any time by pressing the reset button.

#### Mad Space

Mad Space is a unique level in that the first hint of every piece (the only hint we care about) is reversed making just reading the hint itself
harder than it really needs to be in a stage that already presents other gimmicks and challenges. For that reason, this tool also provides
a setting that allows you to correct the order of the letters in each first hint so that it is readable from left to right like normal.
This feature is supported across all languages. By default, the hint reversal feature is turned on by default making reversed hints read naturally in-game.

When Mad Space is selected as the stage for learning in the level select drop down, a check box will appear asking if
you want to allow reversed hints. Leaving this off as it is by default will fix the hint text to make it readable. Turning it on will leave the hints
in their reversed ordering.

Whether you think learning the pieces by reading their hints ordered correctly and then practicing with the reversed words later will be
more beneficial or learning as the hints would actually appear in a real run from the start and not looking at standardized words at all
is more beneficial is entirely left up to the player to decide. Everyone is different and I'm aware of some people preferring one method
while others would prefer the other method. Give it a try for yourself and see which you prefer!

#### Set Editor

Aside from the predefined level options that allow you to play a curated sequence of sets for the purpose of learning the game's pieces,
you also have the option of creating your own entirely custom sequence(s) as you desire with any number of sets and any combination of pieces!
To do this, there's a set editor button next to the level selection dropdown that opens up a form that allows you to add sequences. In each
sequence you can define as many sets as you want via the intuitive UI. All possible pieces that exist in the game are selectable, even ones
that don't naturally spawn in the game due to the set generation logic.

Once you're done creating your custom sequence(s) you can save your work and close out the editor. For each sequence you add,
you'll see your new sequence appear in the level selection dropdown under the "Custom" group. Simply select that and start your sequence
as normal then enter the level you configured that sequence to be for in game and you'll be able to play out your custom practice sequence
with as many repetitions as you configure!

## License

This project is licensed under the [GPL v3.0](https://github.com/StarlitLuna/sa2-hunting-teacher/blob/master/LICENSE) License - see the LICENSE file for details

## Attributions

Finally, a special thanks to [Zeitthh](https://www.speedrun.com/users/Zeitthh) for helping me create the learning sets for all the levels 💜
