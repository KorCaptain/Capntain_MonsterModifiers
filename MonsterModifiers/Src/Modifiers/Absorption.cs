using HarmonyLib;
using UnityEngine;

namespace MonsterModifiers.Modifiers;

public class Absorption
{
    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    public class Absorption_Character_RPC_Damage_Patch
    {
        public static void Prefix(Character __instance, HitData hit)
        {
            if (hit == null || __instance == null)
                return;

            var modiferComponent = __instance.GetComponent<Custom_Components.MonsterModifier>();
            if (modiferComponent == null)
                return;

            if (!modiferComponent.Modifiers.Contains(MonsterModifierTypes.Absorption))
                return;

            if (hit.m_damage.GetTotalDamage() == 0)
                return;

            var mods = modiferComponent.Modifiers;

            if (mods.Contains(MonsterModifierTypes.BluntImmunity) && hit.m_damage.m_blunt > 0)
            {
                float ratio = MonsterModifiersPlugin.Cfg_BluntImmunity_DamageReduction.Value / 100f;
                __instance.Heal(hit.m_damage.m_blunt * ratio);
            }

            if (mods.Contains(MonsterModifierTypes.SlashImmunity) && hit.m_damage.m_slash > 0)
            {
                float ratio = MonsterModifiersPlugin.Cfg_SlashImmunity_DamageReduction.Value / 100f;
                __instance.Heal(hit.m_damage.m_slash * ratio);
            }

            if (mods.Contains(MonsterModifierTypes.PierceImmunity) && hit.m_damage.m_pierce > 0)
            {
                float ratio = MonsterModifiersPlugin.Cfg_PierceImmunity_DamageReduction.Value / 100f;
                __instance.Heal(hit.m_damage.m_pierce * ratio);
            }

            if (mods.Contains(MonsterModifierTypes.ElementalImmunity))
            {
                float ratio = MonsterModifiersPlugin.Cfg_ElementalImmunity_DamageReduction.Value / 100f;
                if (hit.m_damage.m_fire > 0) __instance.Heal(hit.m_damage.m_fire * ratio);
                if (hit.m_damage.m_frost > 0) __instance.Heal(hit.m_damage.m_frost * ratio);
                if (hit.m_damage.m_lightning > 0) __instance.Heal(hit.m_damage.m_lightning * ratio);
                if (hit.m_damage.m_poison > 0) __instance.Heal(hit.m_damage.m_poison * ratio);
                if (hit.m_damage.m_spirit > 0) __instance.Heal(hit.m_damage.m_spirit * ratio);
            }
        }
    }
}
