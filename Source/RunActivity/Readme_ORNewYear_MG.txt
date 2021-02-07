Open Rails, Monogame version (unofficial) README - Release NewYear - Rev.87
January 9th, 2021

Please note that the installation and use of Open Rails software, even of its unofficial versions, is governed by the Open Rails End User License Agreement. 

INSTALLATION
- the requirements for installation of the official Open Rails version apply, with the precisions of next lines
- XNA 3.1 Redistributable is not needed
- you must have at least a Windows Vista computer. Windows XP is not supported
- start openrails simply by clicking on Openrails.exe
- don't try to update the pack by using the link on the upper right side of the main menu window: 
you would return to the official OR version.

RELEASE NOTES
This unofficial version has been derived from the latest official OpenRails testing revision X1.3.1-279. 

No Unstable releases are used to preserve the Monogame version of OR NewYear MG. 
This version includes some features not (yet) available in the Open Rails testing official version, that is:
- Monogame, OR NewYear MG version
- addition of track sounds in the sound debug window (by dennisat)
- F5 HUD scrolling (by mbm_or)
- checkbox in General Options tab to enable or disable watchdog
- increase of remote horn sound volume level
- when car is selected through the F9 window, the car's brake line in the extended brake HUD is highlighted in yellow (by mbm_or)
- improved distance management in roadcar camera
- signal script parser (by perpetualKid): reduces CPU time needed for signal management
- true 64-bit management, allowing to use more than 4 GB of memory, if available, in Win64 systems (mainly by perpetualKid)
- general options checkbox for optional run at 32 bit on Win64 (consumes less memory, recommended for computers with 4 GB RAM)
- translatable Train Driving Info window (see http://www.elvastower.com/forums/index.php?/topic/33401-f5-hud-scrolling/page__view__findpost__p__251671 and following posts), by mbm_OR
- extended Raildriver setup (by perpetualKid)
- analog clocks showing game time, see http://www.elvastower.com/forums/index.php?/topic/29546-station-clocks/ , by jonas
- bug fix for bad mouse pointing in full screen mode and for issues with Fast full screen-Alt-Tab mode
- Webserver operating also with IPv4 addresses, that is on external devices, by mbm_OR, cjakeman and BillC
- panto commands and animations now swapped when in rear cab
- correction to reduce transfer flickering at short distance
- allow passing a red signal for shunting through dispatcher window, see https://trello.com/c/I19VVKlz/450-manual-callon-from-dispatcher-window , by césarbl
- Fix memory usage indication that was clamped to 4 GB, see http://www.elvastower.com/forums/index.php?/topic/33907-64-bit-openrails-consumes-more-memory/page__view__findpost__p__257699
- option to skip saving commands (reduces save time on long activities), see http://www.elvastower.com/forums/index.php?/topic/33907-64-bit-openrails-consumes-more-memory/page__view__findpost__p__257687
- skip warning messages related to ruler token, that is introduced by TSRE5
- General Option to reduce memory usage
- Bug fix for https://bugs.launchpad.net/or/+bug/1877644 SPEED signals not being updated in multiplayer clients. By cesarbl
- Avoid 3D cab stuttering by a mutable shape primitive, by YoRyan
- bug fix for memory leak, see http://www.elvastower.com/forums/index.php?/topic/34069-potential-memory-leak/
- track gauge can be changed over the whole route, see http://www.elvastower.com/forums/index.php?/topic/34022-adjusting-track-gauge/
- first edition of management of ORTS_POWERKEY and ORTS_BATTERY cab controls, by Paolo
- re-introduced advanced coupling, by steamer_ctn
- bug fix for https://bugs.launchpad.net/or/+bug/1895391 Calculation of reversal point distance failing
- support for inclined gauges in 2Dcabs, see http://www.elvastower.com/forums/index.php?/topic/34517-enable-inclined-gauges-in-2d-cabs/
- bug fix for http://www.elvastower.com/forums/index.php?/topic/34572-new-player-trains-dont-show-train-brakes-in-hud/
- bug fix for http://www.elvastower.com/forums/index.php?/topic/34565-tt-crash-when-forming-new-train-player/ 
- support for animation of wipers in 2D cabs, see http://www.elvastower.com/forums/index.php?/topic/34604-2d-cabs-wipers-animation/page__view__findpost__p__265101 ,
with the contribution of Paolo.
- bug fix for http://www.elvastower.com/forums/index.php?/topic/34633-unhandled-exception-overflow-in-win7/page__view__findpost__p__265463 , by mbm_OR
- extended support for ETCS L2 planning area, for braking distance and text area, and initial support for menus by césarBL, see  http://www.elvastower.com/forums/index.php?/topic/34577-planning-area-of-the-etcs-dmi/ 
- removed trace of wait processes when enable watchdog option deselected
- tentative support for RAIN textures for objects, terrain and transfers
- dynamic terrain and scenery loading strategy (useful for slower computers)
- American horn blow sequence for AI trains, see http://www.elvastower.com/forums/index.php?/topic/34721-ai-train-horn-honk-american-style/ 
- NEW: bug fix for https://bugs.launchpad.net/or/+bug/1910684 Crash selecting height entry in altitude file
- NEW: first thread hang is now logged even if watchdog disabled

Moreover some features present in the Unstable Monogame version by James Ross have been inserted.

CREDITS
This unofficial version couldn't have been created without following contributions:
- the whole Open Rails Development Team and Open Rails Management Team, that have generated the official Open Rails version
- the Monogame Development Team
- Peter Gulyas, who created the first Monogame version of Open Rails
- perpetualKid
- Dennis A T (dennisat)
- Mauricio (mbm_OR)
- cjakeman
- BillC
- Peter Newell (steamer_ctn)
- Rob Roeterdink (roeter)
- jonas
- YoRyan
- césarbl
- Paolo
- Weter
- Carlo Santucci

- all those who contributed with ideas and provided contents for testing and pointed to malfunctions.

DISCLAIMER
No testing on a broad base of computer configurations and of contents has been done. Therefore, in addition
to the disclaimers valid also for the official Open Rails version, 
the above named persons keep no responsibility, including on malfunctions, damages, losses of data or time.
It is reminded that Open Rails is distributed WITHOUT ANY WARRANTY, and without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.

