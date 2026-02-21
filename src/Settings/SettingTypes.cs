using System.ComponentModel;

namespace UIFixes;

public enum WeaponPresetConfirmationOption
{
    Never,
    [Description("On Close")]
    OnClose,
    Always
}

public enum TransferConfirmationOption
{
    Never,
    Always
}

public enum MultiSelectStrategy
{
    [Description("First Available Space")]
    FirstOpenSpace,
    [Description("Same Row or Below (Wrapping)")]
    SameRowOrLower,
    [Description("Keep Original Spacing (Best Effort)")]
    OriginalSpacing
}

public enum AutoFleaPrice
{
    None,
    Minimum,
    Average,
    Maximum
}

public enum ModifierKey
{
    None,
    Shift,
    Control,
    Alt
}

public enum ModRaidWeapon
{
    Never,
    [Description("With Multitool")]
    WithTool,
    Always
}

public enum AutoWishlistBehavior
{
    Normal,
    [Description("Visible Upgrades")]
    Visible,
    [Description("All Upgrades")]
    All
}