using System;
using BepInEx.Configuration;

namespace UIFixes;

public abstract class UIFConfigEntryBase(ConfigEntryBase configEntry)
{
    protected readonly ConfigEntryBase configEntry = configEntry;

    public bool Readonly;

    protected abstract object BoxedOverrideValue { get; set; }

    public abstract void ClearOverride();

    public Type SettingType => configEntry.SettingType;

    public ConfigDescription Description => configEntry.Description;

    public ConfigDefinition Definition => configEntry.Definition;

    public string GetSerializedValue() => configEntry.GetSerializedValue();

    public void SetSerializedOverride(string value)
    {
        try
        {
            object boxedValue = TomlTypeConverter.ConvertToValue(value, SettingType);
            BoxedOverrideValue = boxedValue;
        }
        catch (Exception ex)
        {
            Plugin.Instance.Logger.LogWarning($"UIFixes: Config value of setting \"{Definition}\" could not be parsed: {ex.Message}; Value: {value}");
        }
    }
}

public class UIFConfigEntry<T> : UIFConfigEntryBase
{
    private readonly ConfigEntry<T> configEntryT;
    private T overrideValue;
    private bool overriden;

    public T Value
    {
        get => overriden ? overrideValue : configEntryT.Value;
        set
        {
            if (!Readonly)
            {
                configEntryT.Value = value;
            }
        }
    }

    protected override object BoxedOverrideValue
    {
        get
        {
            return overrideValue;
        }
        set
        {
            overrideValue = (T)value;
        }
    }

    public event EventHandler SettingChanged
    {
        add => configEntryT.SettingChanged += value;
        remove => configEntryT.SettingChanged -= value;
    }

    public UIFConfigEntry(ConfigEntry<T> configEntry) : base(configEntry)
    {
        configEntryT = configEntry;
    }

    public static implicit operator UIFConfigEntry<T>(ConfigEntry<T> configEntry) => new(configEntry);

    public override void ClearOverride()
    {
        overriden = false;
        overrideValue = default;
    }
}