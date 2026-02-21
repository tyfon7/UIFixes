using System;
using System.ComponentModel;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace UIFixes;

public static class SettingExtensions
{
    public static void Subscribe<T>(this ConfigEntry<T> configEntry, Action<T> onChange)
    {
        configEntry.SettingChanged += (_, _) => onChange(configEntry.Value);
    }

    public static void Bind<T>(this ConfigEntry<T> configEntry, Action<T> onChange)
    {
        configEntry.Subscribe(onChange);
        onChange(configEntry.Value);
    }

    // KeyboardShortcut methods return false if any other key is down
    public static bool IsDownIgnoreOthers(this KeyboardShortcut shortcut)
    {
        return Input.GetKeyDown(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
    }

    public static bool IsPressedIgnoreOthers(this KeyboardShortcut shortcut)
    {
        return Input.GetKey(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
    }

    public static bool IsUpIgnoreOthers(this KeyboardShortcut shortcut)
    {
        return Input.GetKeyUp(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
    }

    internal static ConfigurationManagerAttributes GetAttributes(this ConfigEntryBase configEntry)
    {
        return configEntry.Description.Tags.OfType<ConfigurationManagerAttributes>().FirstOrDefault();
    }

    public static bool IsSynced(this ConfigEntryBase configEntry)
    {
        ConfigurationManagerAttributes attributes = configEntry.GetAttributes();
        return attributes != null && attributes.Synced.HasValue && attributes.Synced.Value;
    }

    public static void SetReadonly(this ConfigEntryBase configEntry, bool value, string explanation = "Readonly")
    {
        var attributes = configEntry.GetAttributes();
        if (attributes.ReadOnly != value)
        {
            attributes.ReadOnly = value;
            //attributes.DispName = value ? $"<color=grey>{configEntry.Definition.Key}</color>" : configEntry.Definition.Key;
            attributes.CustomDrawer = value ? MakeDisabledDrawer(explanation) : null;

            if (Settings.ConfigManager.DisplayingWindow)
            {
                Settings.ConfigManager.BuildSettingList();
            }
        }
    }

    public static void MakeExclusive(this ConfigEntry<bool> priorityConfig, ConfigEntry<bool> secondaryConfig, bool allowSecondaryToDisablePrimary = true)
    {
        priorityConfig.Bind(priorityValue =>
        {
            if (priorityValue)
            {
                secondaryConfig.Value = false;
            }

            secondaryConfig.SetReadonly(priorityValue && !allowSecondaryToDisablePrimary, $"by {priorityConfig.Definition.Key}");
        });

        if (allowSecondaryToDisablePrimary)
        {
            secondaryConfig.Subscribe(secondaryValue =>
            {
                priorityConfig.Value = false;
            });
        }
    }

    public static void DependOn(this ConfigEntry<bool> dependentConfig, ConfigEntry<bool> primaryConfig, bool primaryEnablesDependent = true)
    {
        primaryConfig.Bind(value =>
        {
            if (value)
            {
                if (primaryEnablesDependent)
                {
                    dependentConfig.Value = true;
                }
            }
            else
            {
                dependentConfig.Value = false;
            }

            dependentConfig.SetReadonly(!value, $"Requires {primaryConfig.Definition.Key}");
        });
    }

    public static void Require(this ConfigEntry<bool> config, bool requirement, string explanation)
    {
        if (!requirement)
        {
            Plugin.Instance.Logger.LogInfo($"Disabling '{config.Definition.Key}'; {explanation}");

            config.Value = false;
            config.SetReadonly(true, explanation);
        }
    }

    private static Action<ConfigEntryBase> MakeDisabledDrawer(string explanation)
    {
        // if (explanation.Length > 30)
        // {
        //     explanation = explanation[..30] + "...";
        // }

        return config =>
        {
            string value = config.BoxedValue switch
            {
                bool b => b ? "Enabled" : "Disabled",
                Enum e => GetEnumDisplayValue(e),
                _ => config.BoxedValue.ToString()
            };

            var reason = string.IsNullOrEmpty(explanation) ? "" : $" ({explanation})";
            GUILayout.Label($"<color=grey>{value}{reason}</color>", GUILayout.ExpandWidth(true));
        };
    }

    private static string GetEnumDisplayValue(Enum value)
    {
        var enumType = value.GetType();
        var enumMember = enumType.GetMember(value.ToString()).FirstOrDefault();
        var description = enumMember?.GetCustomAttributes(typeof(DescriptionAttribute), false).Cast<DescriptionAttribute>().FirstOrDefault();
        return description?.Description ?? value.ToString();
    }
}