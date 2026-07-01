# Change Log
All notable changes to this project will be documented in this file.
 
The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).
 
 
## [Unreleased] - 2026-07-01

### Added (Client)
- Added a modern map rail with grouped NPC/player corpse toggles, target info toggle, legend toggle, follow control, grid/text toggles, and reset control.
- Added a bottom-left map legend showing NPC, player, corpse, ground spawn, and selected marker icons.
- Added a modern selected-info card for clicked spawns and ground spawns.
- Added softer client connect and disconnect chimes.
- Added persisted settings for showing the map legend and map rail.

### Changed (Client)
- Updated the client project to .NET 10 and PackageReference-based dependencies.
- Modernized the main window, toolbar, status bar, dock theme, options/about dialogs, map controls, list panels, and marker rendering.
- Improved map marker visibility, including corpse and ground spawn icons, selected rings, and selected-line coloring.
- Improved spawn, player, corpse, and ground-spawn selection behavior from both the map and lists.
- Improved map refresh responsiveness and packet polling behavior.
- Improved list repainting and selection highlighting after sorting, resizing, and fullscreen/windowed transitions.
- Removed the old floating map status box and obsolete quick-filter drawer.

### Added (Server)
- Added an offset diff finder workflow for comparing old/new EQ executables and offset sets.
- Added a Windows GUI front-end for the offset diff finder and wired the server Diff Finder button to launch it.

### Changed (Server)
- Updated the server project/toolset for current Visual Studio builds.
- Improved EQ process scanning and packet handling reliability.
- Removed old Windows connect/disconnect beeps from the server.

## [2.5.0] - 2021-02-19

### Added (Client)
- Added 5 more adhoc lookups to the map.
- Added ability to filter as well as lookup via the adhoc lookups. L is lookup, F is filter. Boxes are interpreted left to right. So if you lookup all goblins in slot 1, then filter all scouts in slot 2, it will show only the goblins who are not scouts.
- Added level searching to adhoc lookups. L:xx is the lookup/filter
- Added setting to control display of lookedup mobs. Bottom of the view menu. If Lookup text is checked, then all mob targeted by a lookup will display something next to the dot. If Lookup Name/Number isn't checked, then it will show the mob's name. If Lookup Name/Number is checked, then it will show 1 through 6 depending on which slot in the lookups the mob matched on.
- Added filters for primary and offhand. These are zone filters only, and go in the zone filter file. Mobs that are found with the item in the respective slot, will be treated as hunt mobs. That moves them to the top of the spawn list, and marks them with a flashing circle on the map. Filters need to look like this:
	<section name="Primary">
		<oldfilter><regex>NamearkBook</regex></oldfilter>
	</section>
	<section name="Offhand">
		<oldfilter><regex>Name:Shiverback</regex></oldfilter>
	</section>
- Added aways on top toggle. Bottom of the view menu.

### Changed (Client)
- Added special handling of a_tainted_egg in the Vish encounter. It now acts like a normal mob and can be added to lookups/hunt mobs etc.

### Changed (Server)
- Changed server args to be compatible with VS2019
- Changed the debug buffer from 2048 to 8192
