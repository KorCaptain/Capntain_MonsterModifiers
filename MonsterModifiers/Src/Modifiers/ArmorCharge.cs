using HarmonyLib;
using UnityEngine;

namespace MonsterModifiers.Modifiers;

public class ArmorCharge
{
    private const float SpeedMultiplier = 1.3f;
    private const float DamageMultiplier = 1.5f;

    public static void Apply(Character character)
    {
        character.m_walkSpeed *= SpeedMultiplier;
        character.m_runSpeed *= SpeedMultiplier;
        character.m_swimSpeed *= SpeedMultiplier;
    }

    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    public class ArmorCharge_Character_RPC_Damage_Patch
    {
        public static void Prefix(Character __instance, HitData hit)
        {
            if (hit == null || __instance == null)
                return;

            if (!__instance.IsPlayer())
                return;

            Character attacker = hit.GetAttacker();
            if (attacker == null)
                return;

            var modifierComponent = attacker.GetComponent<Custom_Components.MonsterModifier>();
            if (modifierComponent == null)
                return;

            if (!modifierComponent.Modifiers.Contains(MonsterModifierTypes.ArmorCharge))
                return;

            hit.m_damage.m_blunt *= DamageMultiplier;
            hit.m_damage.m_slash *= DamageMultiplier;
            hit.m_damage.m_pierce *= DamageMultiplier;
            hit.m_damage.m_fire *= DamageMultiplier;
            hit.m_damage.m_frost *= DamageMultiplier;
            hit.m_damage.m_lightning *= DamageMultiplier;
            hit.m_damage.m_poison *= DamageMultiplier;
            hit.m_damage.m_spirit *= DamageMultiplier;
        }
    }
}
