// Sets the Game view to a fixed 1920 x 1080 size (internal API, by reflection).
// Run: unity command script run --file Design/button-spike/unity/GameViewSize.cs --entry GameViewSize.Set1080p
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class GameViewSize
{
    public static string Set1080p()
    {
        var asm = typeof(Editor).Assembly;
        var sizesType = asm.GetType("UnityEditor.GameViewSizes");
        var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var instance = singleton.GetProperty("instance").GetValue(null);
        var group = sizesType.GetMethod("GetGroup").Invoke(instance, new object[] { (int)GameViewSizeGroupType.Standalone });
        var groupType = group.GetType();

        int count = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
        int index = -1;
        for (int i = 0; i < count; i++)
        {
            var size = groupType.GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
            var st = size.GetType();
            if ((int)st.GetProperty("width").GetValue(size) == 1920 && (int)st.GetProperty("height").GetValue(size) == 1080
                && st.GetProperty("sizeType").GetValue(size).ToString() == "FixedResolution") { index = i; break; }
        }
        if (index < 0)
        {
            var sizeType = asm.GetType("UnityEditor.GameViewSize");
            var enumType = asm.GetType("UnityEditor.GameViewSizeType");
            var ctor = sizeType.GetConstructor(new[] { enumType, typeof(int), typeof(int), typeof(string) });
            var newSize = ctor.Invoke(new object[] { Enum.Parse(enumType, "FixedResolution"), 1920, 1080, "Spike 1080p" });
            groupType.GetMethod("AddCustomSize").Invoke(group, new[] { newSize });
            index = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null) - 1;
        }

        var gvType = asm.GetType("UnityEditor.GameView");
        var gv = EditorWindow.GetWindow(gvType);
        gvType.GetMethod("SizeSelectionCallback", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
              .Invoke(gv, new object[] { index, null });
        return "game view size index " + index;
    }
}
