using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace MonsterModifiers.Modifiers;

public class ResistanceNotification
{
    private static readonly Dictionary<ZDOID, Dictionary<HitData.DamageType, int>> HitCounts =
        new Dictionary<ZDOID, Dictionary<HitData.DamageType, int>>();

    private const int MaxNotifications = 2;

    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    public class ResistanceNotification_Character_RPC_Damage_Patch
    {
        public static void Prefix(Character __instance, HitData hit)
        {
            if (hit == null || __instance == null || __instance.IsPlayer())
                return;

            if (hit.m_hitType != HitData.HitType.PlayerHit)
                return;

            var modifierComponent = __instance.GetComponent<Custom_Components.MonsterModifier>();
            if (modifierComponent == null)
                return;

            if (!modifierComponent.Modifiers.Contains(MonsterModifierTypes.ResistanceNotification))
                return;

            if (__instance.m_nview == null)
                return;

            ZDOID id = __instance.m_nview.GetZDO().m_uid;
            if (!HitCounts.ContainsKey(id))
                HitCounts[id] = new Dictionary<HitData.DamageType, int>();

            CheckAndNotify(__instance, id, HitData.DamageType.Blunt, hit.m_damage.m_blunt, modifierComponent, MonsterModifierTypes.BluntImmunity, "$modifier_blunt_resistance");
            CheckAndNotify(__instance, id, HitData.DamageType.Slash, hit.m_damage.m_slash, modifierComponent, MonsterModifierTypes.SlashImmunity, "$modifier_slash_resistance");
            CheckAndNotify(__instance, id, HitData.DamageType.Pierce, hit.m_damage.m_pierce, modifierComponent, MonsterModifierTypes.PierceImmunity, "$modifier_pierce_resistance");
            CheckAndNotifyElemental(__instance, id, hit.m_damage, modifierComponent);
        }

        private static void CheckAndNotify(Character character, ZDOID id, HitData.DamageType type, float damage,
            Custom_Components.MonsterModifier modComp, MonsterModifierTypes immunity, string messageKey)
        {
            if (damage <= 0 || !modComp.Modifiers.Contains(immunity))
                return;

            var counts = HitCounts[id];
            if (!counts.ContainsKey(type)) counts[type] = 0;
            if (counts[type] >= MaxNotifications) return;

            counts[type]++;
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                Localization.instance.Localize(messageKey));
        }

        private static void CheckAndNotifyElemental(Character character, ZDOID id,
            HitData.DamageTypes dmg, Custom_Components.MonsterModifier modComp)
        {
            if (!modComp.Modifiers.Contains(MonsterModifierTypes.ElementalImmunity))
                return;

            float elemTotal = dmg.m_fire + dmg.m_frost + dmg.m_lightning + dmg.m_poison + dmg.m_spirit;
            if (elemTotal <= 0) return;

            var counts = HitCounts[id];
            var key = HitData.DamageType.Fire;
            if (!counts.ContainsKey(key)) counts[key] = 0;
            if (counts[key] >= MaxNotifications) return;

            counts[key]++;
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                Localization.instance.Localize("$modifier_elemental_resistance"));
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
    public class ResistanceNotification_Character_OnDeath_Patch
    {
        public static void Prefix(Character __instance)
        {
            if (__instance == null || __instance.m_nview == null)
                return;

            ZDOID id = __instance.m_nview.GetZDO().m_uid;
            HitCounts.Remove(id);
        }
    }
}
