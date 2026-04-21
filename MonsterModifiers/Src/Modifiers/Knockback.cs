using HarmonyLib;
using UnityEngine;

namespace MonsterModifiers.Modifiers;

public class Knockback
{
    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    public class Knockback_Character_RPC_Damage_Patch
    {
        public static void Postfix(Character __instance, HitData hit)
        {
            if (hit == null || __instance == null || __instance.IsDead())
                return;

            Character attacker = hit.GetAttacker();
            if (attacker == null)
                return;

            var modifierComponent = attacker.GetComponent<Custom_Components.MonsterModifier>();
            if (modifierComponent == null)
                return;

            if (!modifierComponent.Modifiers.Contains(MonsterModifierTypes.Knockback))
                return;

            if (!__instance.IsPlayer())
                return;

            Player player = __instance as Player;
            if (player != null && player.IsBlocking())
                return;

            float staggerForce = MonsterModifiersPlugin.Cfg_Knockback_StaggerForce.Value;
            float pushForce = MonsterModifiersPlugin.Cfg_Knockback_PushForce.Value;

            Vector3 pushDir = hit.m_dir * pushForce + Vector3.up * (pushForce * 0.3f);
            __instance.Stagger(pushDir);
            __instance.GetComponent<Rigidbody>()?.AddForce(pushDir, ForceMode.Impulse);
        }
    }
}
