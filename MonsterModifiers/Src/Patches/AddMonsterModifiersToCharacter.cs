using HarmonyLib;
using UnityEngine;

namespace MonsterModifiers.Custom_Components;

public class AddMonsterModifiersToCharacter
{
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(typeof(Character), nameof(Character.Awake))]
    public static class Character_Awake_Patch
    {
        private static void Postfix(Character __instance)
        {
            if (!__instance.IsPlayer())
            {
                __instance.gameObject.AddComponent<MonsterModifier>();
            }
        }
    }

    [HarmonyPatch(typeof(Character), "UpdateCachedAnimHashes")]
    public static class Character_UpdateCachedAnimHashes_Patch
    {
        private static bool Prefix(Character __instance)
        {
            if (__instance.m_animator == null || !__instance.m_animator.isInitialized)
                return false;
            return true;
        }
    }
}
