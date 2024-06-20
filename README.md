# UI Fixes

Quality of life improvements and fixes for SPT

Tarkov is full of annoyances but we can fix them! Sometimes.

✨ New in the latest version!

## Added features

New UI features enabled by this mod

-   ✨ Multiselect - select multiple items at once with shift-click or dragging a selection box
    -   Move items around as a group, drop them into containers, place them in grids
    -   Ctrl-click and Alt-click to quick move or equip them all. Compatible with Quick Move to Containers!
    -   Context menu to insure all, equip all, unequip all, unload ammo from all
-   Swap items in place - drag one item over another to swap their locations!
-   Flea market history - press the new back button to go back to the previous search
-   Keybinds for most context menu actions

## Improved features

Existing SPT features made better

#### Inventory

-   Rebind Home/End, PageUp/PageDown to work like you would expect
-   Customizable mouse scrolling speed
-   Allow found in raid money and ammo automatically stack with non-found-in-raid items
-   Synchronize stash scroll position everywhere your stash is visible
-   Insure and repair items directly from the context menu
-   Load ammo via context menu _in raid_
-   Load ammo preset will pull ammo from inventory, not just stash

#### Inspect windows

-   Show the total stats (including sub-mods) when inspecting mods (optional, toggleable _in_ the inspect pane with a new button)
-   See stats change as you add/remove mods, with color-coded deltas
-   Remember last window size when you change it, with restore button to resize to default
-   Move left and move right buttons + keybinds to quickly snap inspect windows to the left or right half of the screen, for easy comparisons.
-   Auto-expand descriptions when possible (great for showing extra text from mods like Item Info)
-   Quickbinds will not be removed from items you keep when you die

#### Traders

-   Autoswitch between buy and sell when you click trader items or control-click your own items
-   Autoselect quest items when turning in
-   Repair window remembers last used repairer, and actually updates the repair amount when you switch repairers

#### Flea market

-   Auto-expand categories when there's space to do so
-   Locked trader item tooltip reveals which specific quest will unlock it
-   Option to keep the Add Offer window open after placing your offer
-   Set prices in the Add Offer window by clicking the min/avg/max market prices (multiplies for bulk orders)
-   Autoselect Similar checkbox is remembered across sessions and application restarts

#### Weapon modding/presets

-   Enable zooming with mousewheel
-   Skip needless unsaved changes warnings when not actually closing the screen

#### Hideout

-   Remember window state when you leave hideout without closing (e.g. when searching for a recipe item on flea)

#### Mail

-   The messages window stays open when you return from transfering items

#### Misc

-   Confirm dialogs with Return/Enter/Space instead of just Y
-   Close modal dialogs by clicking outside of them
-   Sensible autofocus of textboxes in various dialogs
-   Many little UI tweaks to tighten up the graphics

## Fixes

Fixing bugs that BSG won't or can't

#### Inventory

-   Fix item tooltips disapparing if your mouse goes through the Quest/FoundInRaid icon (great for MoreCheckmarks)
-   Fix windows appearing partially offscreen

#### In raid

-   Remove the unimplemented door actions like "Bang & clear" that are never going to happen
-   Fix the keybind for weapons always showing up as 1, 2, and 3. Now shows your actual keybind like every other slot
-   ✨ Fix the quick item bar not respecting "Autohide" and "Never show" setting

#### Mail

-   Skips "You can return to this later" warnings when not transferring all items
-   "Receive All" button no longer shows up when there is nothing to receive
