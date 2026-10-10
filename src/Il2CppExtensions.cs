using System;
using System.Collections.Generic;
using EFT.UI;
using Il2CppInterop.Runtime;

namespace UIFixes;

public static class Il2CppExtensions
{
    public static Il2CppSystem.Enum ToIl2CppEnum<T>(this T value) where T : Enum
    {
        var boxed = Il2CppSystem.Enum.ToObject(Il2CppType.Of<T>(), Convert.ToInt64(value));
        return boxed.Cast<Il2CppSystem.Enum>();
    }

    public static T ToEnum<T>(this Il2CppSystem.Enum value) where T : Enum
    {
        var numeric = Il2CppSystem.Convert.ToInt64(value.Cast<Il2CppSystem.Object>());
        return (T)Enum.ToObject(typeof(T), numeric);
    }

    public static Il2CppSystem.Collections.Generic.List<T> ToIl2CppList<T>(this IEnumerable<T> source)
    {
        var list = new Il2CppSystem.Collections.Generic.List<T>();
        foreach (var item in source)
        {
            list.Add(item);
        }

        return list;
    }

    // This is just to avoid footgun - if you're not careful, you end up calling AddDisposable<T> where T is Action,
    // instead of the overload that takes Il2CppSystem.Action
    public static void AddOnDispose(this UIParent ui, Action action)
    {
        ui.AddDisposable((Il2CppSystem.Action)action);
    }
}