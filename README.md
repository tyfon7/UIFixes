# UI Fixes

Quality of life improvements and fixes for SPT

_Tarkov is full of annoyances, but we can fix them! Sometimes._

✨ Recently added

## Added features

New UI features enabled by this mod

-   Multiselect - select multiple items at once with shift-click or dragging a selection box
    -   Move items around as a group, drop them into containers, place them in grids
    -   Ctrl-click and Alt-click to quick move or equip them all. Compatible with Quick Move to Containers!
    -   Context menu to insure, equip, unequip, load/unload ammo, install/uninstall, pin, and lock all at once
-   Swap items in place - drag one item over another to swap their locations!
-   Flea market history - press the new back button to go back to the previous search
-   Linked flea search from empty slots - find mods that fit that specific slot
-   Keybinds for most context menu actions
-   Toggle/Hold input - tap a key for "Press" mechanics, hold the key for "Continuous" mechanics
    -   Can be set for aiming, sprinting, tactical devices, headlights, and goggles/faceshields

## Improved features

Existing SPT features made better

#### Inventory

-   Modify equipped weapons
-   Rebind Home/End, PageUp/PageDown to work like you would expect
-   Customizable mouse scrolling speed
-   Moving stacks into containers always moves entire stack
-   Items made stackable by other mods follow normal stacking behavior
-   Synchronize stash scroll position everywhere your stash is visible
-   Insure and repair items directly from the context menu
-   Load ammo via context menu _in raid_
-   Load ammo preset will pull ammo from inventory, not just stash
-   Multi-grid vest and backpack grids reordered to be left to right, top to bottom
-   Vests and backpacks are taggable
-   Sorting will stack and combine stacks of items
-   Open->All context flyout that will recursively open nested containers to get at that innermost bag
-   Improve modding/preset dropdown usability
-   Enable context menu while searching (off by default)

#### Inspect windows

-   Show the total stats (including sub-mods) when inspecting mods (optional, toggleable _in_ the inspect pane with a new button)
-   See stats change as you add/remove mods, with color-coded deltas
-   Highlight empty slots when you drag items that can be attached there
-   Remember last window size when you change it, with restore button to resize to default
-   Move left and move right buttons + keybinds to quickly snap inspect windows to the left or right half of the screen, for easy comparisons
-   Auto-expand descriptions when possible (great for showing extra text from mods like Item Info)
-   Quickbinds will not be removed from items you keep when you die
-   Compatible slots are now outlined in green, like the paper doll already does

#### Traders

-   Autoswitch between buy and sell when you click trader items or control-click your own items
-   Autoselect quest items when turning in
-   Repair window remembers last used repairer, and actually updates the repair amount when you switch repairers
-   More icons on trader avatars - when their only new quests are operational (dailies/weeklies) and when you have items you could turn in
-   Trader window remembers which tab you had open, and can optionally remember which trader you last looked at even when you completely close the trading screen

#### Flea market

-   Auto-expand categories when there's space to do so
-   Locked trader item tooltip reveals which specific quest will unlock it
-   Option to keep the Add Offer window open after placing your offer
-   Set prices in the Add Offer window by clicking the min/avg/max market prices (multiplies for bulk orders)
-   Autoselect Similar checkbox is remembered across sessions and application restarts
-   Replace barter offers icons with actual item images, plus owned/required counts on expansion, ✨ color-coded if you own the items needed
-   Clears filters for you when you type in search bar and there's no match

#### Weapon modding/presets

-   Weapons can grow left or up, not just right and down
-   Enable zooming with mousewheel
-   Skip needless unsaved changes warnings when not actually closing the screen
-   Saving the current preset is now one-click; you don't have to confirm the name. That's what Save As... is for.
-   Open build window can filter out stock presets so you only see yours

#### Hideout

-   Remember window state when you leave hideout without closing (e.g. when searching for a recipe item on flea)
-   Tools used in crafting will be returned to the container they were in

#### In raid

-   Quickbind tactical devices to control them individually
-   Option to make unequipped weapons moddable in raid, optionally with multitool
-   Reloading will swap magazines in-place, instead of dropping them on the ground when there's no room
-   Grenade quickbinds will transfer to the next grenade of the same type after throwing
-   Warning on character select screen when your PMC is carrying quest items

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
-   Fix modding/preset UI breaking when you click not allowed (red outlined) items
-   Fix trader items not showing "Compatible with Available" with your items
-   Fix selected barter items not being highlighted correctly

#### In raid

-   Remove the unimplemented door actions like "Bang & clear" that are never going to happen
-   Fix the keybind for weapons always showing up as 1, 2, and 3. Now shows your actual keybind like every other slot
-   Fix issue where clicking the eye at the wrong time during raid load would break your equipment UI for the rest of the raid
-   Fix not being able to pay the BTR man with money from your backpack or secure container if you happened to be carrying grenades

#### Mail

-   Skips "You can return to this later" warnings when not transferring all items
-   "Receive All" button no longer shows up when there is nothing to receive

## Interop

UI Fixes offers interop with other mods that want to use the multi-select functionality, _without_ taking a hard dependency on `Tyfon.UIFixes.dll`.

To do this, simply download and add [MultiSelectInterop.cs](src/Multiselect/MultiSelectInterop.cs) to your client project. It will take care of testing if UI Fixes is present and, using reflection, interoping with the mod.

MultiSelectInterop exposes a small static surface to give you access to the multi-selection.

```cs
public static class MultiSelect
{
    // Returns the number of items in the current selection
    public static int Count { get; }

    // Returns the items in the current selection
    public static IEnumerable<Item> Items { get; }

    // Executes an operation on each item in the selection, sequentially
    // Passing an ItemUiContext is optional as it will use ItemUiContext.Instance if needed
    // The second overload takes an async operation and returns a task representing the aggregate.
    public static void Apply(Action<Item> action, ItemUiContext context = null);
    public static Task Apply(Func<Item, Task> func, ItemUiContext context = null);
}
```
