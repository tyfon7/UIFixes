using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace UIFixes;

public class NoRandomGrenadesPatch : ModulePatch
{
    private static NoRandomGrenadesPatch Patch;

    public static void Init()
    {
        Settings.DeterministicGrenades.Bind(enabled =>
        {
            if (enabled)
            {
                Patch ??= new NoRandomGrenadesPatch();
                Patch.Enable();
            }
            else
            {
                Patch?.Disable();
            }
        });
    }

    // Make ctor private so I don't forget to call Init() instead
    private NoRandomGrenadesPatch() { }

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Class1472), nameof(Class1472.vmethod_1));
    }

    [PatchTranspiler]
    public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ble_S || instruction.opcode == OpCodes.Ble) // DnSpy is lying about which one this is!?
            {
                // This is the line 
                // if (count > 0)
                // which in IL does "if count is less than or equal to 1, jump over"
                // So switch the IL to bge, so it jumps over if count is greater or equal to 1, thus skipping the randomizer
                yield return new CodeInstruction(instruction)
                {
                    opcode = instruction.opcode == OpCodes.Ble_S ? OpCodes.Bge_S : OpCodes.Bge,
                };
            }
            else
            {
                yield return instruction;
            }
        }
    }
}
