# Modlist Hash Checker
Generates a hash value from running modlist that can be used to compare to an expected hash, as well as when joining other lobbies with this mod.

## What does this mod do?
**The primary function of this mod is to generate a hash from the currently loaded modlist via the mod names and version numbers. This hash value can then be used to compare to an expected modpack hash, as well as between host player and those who join them.** 

### Warning Messages
- When the expected hash is set, if someone using your modpack uses an incorrect version of a mod or adds additional mods to their profile they can be given a warning message (if enabled) indicating that they may experience issues with unapproved mods/versions.
- When the expected hash is blank, a convenient warning message is shown in the menus so you can set it there when you are ready to publish your modpack. (you can also set this manually by copying the hash from the log file and setting the config option)

### Host/Client Hash Matching
- Another feature this mod provides is that a custom lobby key will be assigned that only this mod will read indicating what hash the host is playing on. 
- If another player joins the lobby with this mod it will compare the hash between the host and the joined player.
- If there is a mismatch between the two hash values, the joiner will receive a warning message (if enabled) that the host has a differing hash value and they may experience issues.

### Extensive Logging
- All of this is also documented in the logfile, to make troubleshooting issues between players a bit easier.

### Highly Configurable
- This mod is highly configurable to customize it to your modpack's experience. Every warning message, button, etc. can all be set to custom values.

### Warning
- Unless you are fully in control of the modpack this mod may be featured in, you generally should not change the configuration of this mod.


## Credits
- darmuh - Initial Mod Creation and Menu Message Handling
- andrey - Added lobby functionality to compare hash values between host and joiner.
- Electric131 - Initial post to thunderstore and code cleanup. Made hash read-only to other mods.