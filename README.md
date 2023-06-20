# SAVE OUR SHIP 2 EXPERIMENTAL

## **READ CAREFULLY AND IN FULL BEFORE DOING ANYTHING!**

### **INTRODUCTION AND DISCLAIMER:**

**SOS2 EXPERIMENTAL** (SOS2EXP) is the maintained version of [Save Our Ship 2](https://steamcommunity.com/sharedfiles/filedetails/?id=1909914131) for RimWorld 1.4. It includes over two years worth of bugfixes, performance optimizations, mechanics changes and some new features that might need more testing. An updated version of the Creation kit is also [available](https://github.com/SonicTHI/SaveOurShip2CreationKit). Only use it with SOS2EXP.

**While this version is officially endorsed, it is not made by RHX media and you use it at your own risk.** It is backwards compatible with the STEAM release however it adds features that might not get adopted into the STEAM version and might cause issues when trying to move from SOS2EXP back to STEAM. While most of these issues can be solved with the dev mode or some minor save editing, no support will be provided in this case.

___
### **INSTALLATION AND UPDATING:**

**Make a separate save while NOT IN A SHIP BATTLE before switching to this version and unsubscribe from SOS2 on STEAM!**

**To download the latest build click above: Code -> Download ZIP.** With major changes a stable build might also be available under Releases.

Installation procedure for non Steam mods: [RW wiki](https://rimworldwiki.com/wiki/Installing_mods)

When updating to a new SOS2EXP version: Delete the old folder then extract the new one.

___
### **TROUBLESHOOTING AND ISSUE REPORTING:**

Check the [SOS2EXP dev sheet](https://docs.google.com/spreadsheets/d/1XSeMCsOtBsbAOLYFbgYUpxyV4ot8L2pSeWMTwzAUCiM/edit#gid=0) for known issues and [the official SOS2 sheet](https://docs.google.com/spreadsheets/d/e/2PACX-1vT1tWMpG9R7bU6asg5ICf6AmQGzUHmxnL8OPOWFzV1o4L_Dsli6OQbHfFXY4CTxX6vpCEvJjycMPniB/pubhtml) for general SOS2 info, FAQs and mod compatibility. Note that some mods that do work with the STEAM build might not with SOS2EXP - particularly mods that alter, extend or otherwise interact with SOS2.

* **Before reporting make sure you are on the latest SOS2EXP version.** Version info is shown on startup in the debug log (*Enable development mode!*).
* Make sure your log is clear of any other red errors from other mods and you are not using mods listed as incompatible with SOS2EXP!
* If possible try replicating the bug with only the required mods installed (*Harmony, Wall light*).
* When making a report provide a **FULL LOG**! (*Open debug log - tilde or top left icon -> Files -> Open Save Folder -> Player.log*) A good description and screenshots of the bug are appreciated.
* You can report issues or give feedback here or in **#save-our-ship** on [Radian Helix Media discord](https://discord.gg/GK7nqgu) - **read the server rules!**

**FAILURE TO FOLLOW THE ABOVE WILL RESULT IN YOUR REPORT BEING CLOSED/IGNORED!**
___
### **MAJOR FEATURE LIST:**

* **Continue playing or start fresh:** the new release is backwards compatible but your ship might require some maintenance such as replacing large bridges with consoles, adding coolant tanks for more capacity or making more room for a larger crew. If however you plan to start on a ship you can pick from a varied selection of rebuilt options by customizing your starting scenario. Making a save before switching to this version out of combat is highly recommended. If however you want to be extra safe, land your ship on the planet and make sure no space maps are open.

* **[Rebuilt ship roster](https://imgur.com/a/YXXBs9p):** every ship in the mod has been redesigned and built from the ground up utilizing new systems and mechanics making for a more interesting and varied experience. New ships have also been added. Many thanks to the ship wrights that helped with this: HG, Oninnaise, UrbanMonkey, VVither_Skeleton, Rage Nova.

* **Ship blueprints:** sold by weapon merchants. These items allow you to build entire ships and are fully reusable and customizable. You will still need the required technologies and resources to do so (these are listed on each blueprint).

* **Ground defense:** all small ship weapons are now capable of firing on enemy combatants while your ship is landed. Decimate hordes of enemies with the push of a button so long as they are not already too close or your weapons on cooldown.

* **New boarding system and defense (this can be disabled in options):** shuttles can only reach enemy ships once most of their engines have been disabled. Pods however can be launched at any time and will land in the outer rooms. Once aboard enemies will breach doors and attempt to murder your crew. You can escape this fate by utilizing PD weapons to shoot down enemy pods in flight but beware - enemy ships can do the same to you.

* **Redone heat system:** simplified, with less cheese and far better performance. Radiators have been removed while heatsinks now vent heat through the roof even when shielded. Coolant tanks have been added that offer a much larger heat capacity but without the ability to vent it to space. A new *vent coolant* function is also available from the bridge that sacrifices your maximum capacity for a quick removal of heat.

* **New ending and planet travel system:** completing the Royalty ending now grants you an imperial destroyer instead of ending the game. Traveling to another planet now ends the game but saves your ship and everything on it to a file that can be loaded by editing a *Load ship* staring scenario and selecting your saved ship. Revisiting old planets is no longer possible. The ship start scenario has also been updated with similar code to ensure better compatibility and easier maintenance/updates in the future.

* **Hologram rework:** Biotech's new gene system and other changes caused too many issues with holograms so they have been reworked into formgel swarm like beings.

* **Random fleet system:** face off against randomly generated fleets of ships in various configurations and sizes.

* **Random wreck system:** generates wrecks from ships in the database for you to explore and loot. Other factions and ships might attempt to do the same.

* **Reworked salvage system:** gone are the days of pushing a button and getting a shipload of resources. Use salvage bays to drag defeated ships to your map where you can move and rotate them, split them apart and/or graft parts onto your existing ship. You can also repair or deconstruct them for resources or simply let them burn up in atmosphere. A larger crew or taking advantage of other mods or new mechs in Biotech will come in handy.

* **Bridge system overhaul:** old large bridges have become obsolete with two new consoles taking on and expanding the functionality. Some functions like scanning have been moved to advanced sensors or the new science console while the tactical variant lets you select various weapons types across your ship provided they are connected to it by the ships heat network.

* **Docking ports:** allow you to temporarily connect ships by using extenders with breathable air, power, heat and chemfuel pipes (if using Rimefeller) up to 3 tiles away and can also connect to each other.

* **New event:** watch out for debris field events that can damage your ship. These can be evaded on the bridge.

* **Performance optimizations:** from faster EVA checks to a completely rebuilt ship combat system - a lot has been done to improve the performance of this mod.

* **New UI and settings menu:** the ship battle UI has been moved bellow the pawn bar. It now shows all ships and can be moved or made persistent in the SOS2 settings menu. SOS2 settings have been reset to their defaults as we now use the native RW mod tools. Hugslib has been removed as a required mod.

* **Tons of other minor adjustments, balance changes and bugfixes.**

* **Extendability:** while not perfect it is now easier to mod in new types of ship buildings or adding EVA functionality to other mods. A navy system has also been added so modders can add factions with their own pawns, ships and colors.

* **Creation kit:** now includes new tools for fleet and blueprint creation as well as improvements to existing ones.

**For more details read the [SOS2EXP dev sheet](https://docs.google.com/spreadsheets/d/1XSeMCsOtBsbAOLYFbgYUpxyV4ot8L2pSeWMTwzAUCiM/edit#gid=0)** It also includes other info that you might find useful.

___
### **CONTRBUTION / CODE SUBMISSIONS / PULL REQUESTS:**

**First time submitters: do not make unsolicited pull requests. Contact me first.**