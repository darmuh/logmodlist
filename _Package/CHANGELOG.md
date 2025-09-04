# Modlist Hash Checker CHANGELOG

## 0.2.1
- Some more project cleanup, stopped using gamelibs since it's a few versions behind
- Changed ``DisplayHashOnLevelLoad`` behavior to now have two different options for displaying player hash.
	- This is set via ``ChatHashMessageToAll`` which has the following results:
		- When enabled, your hash along with your username will be sent as a server message to all players in the lobby.
		- When disabled, your hash will be displayed in chat locally and not sent to any other players.

## 0.2.0
- Project cleanup
- Fixed some issues caused by hijacking the main menu notification game object
	- Now creates a separate game object specifically for our warning.
- Added chat message on load of level to display current modlist hash (per request)
	- Can be disabled via config item ``DisplayHashOnLevelLoad``

## 0.1.2
- Added new warning message for blank ExpectedModListHash configuration option.
- Refactored message handling code for re-use.
- Cleaned up code further, split different major classes into separate files.
- Removed AutoSetHash configuration option in favor of new warning message.
- Updated Icon, Manifest, README, and added CHANGELOG.
- Removed most mentions of "logmodlist" in favor of ModListHashChecker

## 0.1.1
- Fixed version number in thunderstore upload. (Electric131)

## 0.1.0
- Initial release by Electric131