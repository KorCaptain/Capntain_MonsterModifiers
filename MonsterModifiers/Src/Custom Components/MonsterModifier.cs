using System;
using System.Collections.Generic;
using System.Linq;
using MonsterModifiers.Modifiers;
using UnityEngine;
using MonsterModifiers.Visuals;

namespace MonsterModifiers.Custom_Components;

public class MonsterModifier : MonoBehaviour
{
   public List<MonsterModifierTypes> Modifiers = new List<MonsterModifierTypes>();

   public Character character;

   public int level;

   public bool IsBossCharacter = false;
   public int OverflowStars = 0;

   private void Start()
   {
      character = GetComponent<Character>();
      level = character.GetLevel();

      // Check if the character is an Epic Loot bounty target
      if (character.m_nview != null && character.m_nview.GetZDO().GetString("BountyID") != string.Empty)
      {
         Destroy(this);
         return;
      }

      if (character.IsBoss())
      {
         if (MonsterModifiersPlugin.Configurations_Boss_Modifiers.Value == MonsterModifiersPlugin.Toggle.Off)
            return;

         IsBossCharacter = true;
         int minCount = MonsterModifiersPlugin.Configurations_Boss_Min_Modifiers.Value;
         int starCount = Mathf.Max(0, level - 1);
         int totalWanted = minCount + starCount;
         int availableCount = ModifierUtils.GetAvailableModifierCount(ModifierUtils.BossExcludedModifiers);
         int actualCount = Mathf.Min(totalWanted, availableCount);
         int iconSlots = Mathf.Min(starCount, availableCount - minCount);
         OverflowStars = Mathf.Max(0, starCount - iconSlots);

         string modifiersString = character.m_nview.GetZDO().GetString("modifiers", string.Empty);
         if (string.IsNullOrEmpty(modifiersString))
         {
            foreach (var modifier in ModifierUtils.RollRandomModifiers(actualCount, ModifierUtils.BossExcludedModifiers))
               Modifiers.Add(modifier);

            if (character.m_nview.GetZDO().IsOwner())
               character.m_nview.GetZDO().Set("modifiers", string.Join(",", Modifiers));
         }
         else
         {
            Modifiers = new List<MonsterModifierTypes>(Array.ConvertAll(modifiersString.Split(','),
               str => (MonsterModifierTypes)Enum.Parse(typeof(MonsterModifierTypes), str)));

            int reloadedAvail = ModifierUtils.GetAvailableModifierCount(ModifierUtils.BossExcludedModifiers);
            int reloadedIconSlots = Mathf.Min(starCount, reloadedAvail - minCount);
            OverflowStars = Mathf.Max(0, starCount - reloadedIconSlots);
         }

         ApplyStartModifiers();
         return;
      }

      if (level > 1)
      {
         string modifiersString = character.m_nview.GetZDO().GetString("modifiers", string.Empty);
         if (string.IsNullOrEmpty(modifiersString))
         {
            int numModifiers = Mathf.Min(level - 1, MonsterModifiersPlugin.Configurations_MaxModifiers.Value);

            foreach (var modifier in ModifierUtils.RollRandomModifiers(numModifiers))
            {
               Modifiers.Add(modifier);
            }

            if (character.m_nview.GetZDO().IsOwner())
            {
               string serializedModifiers = string.Join(",", Modifiers);
               character.m_nview.GetZDO().Set("modifiers", serializedModifiers);
            }
         }
         else
         {
            Modifiers = new List<MonsterModifierTypes>(Array.ConvertAll(modifiersString.Split(','),
               str => (MonsterModifierTypes)Enum.Parse(typeof(MonsterModifierTypes), str)));
         }

         ApplyStartModifiers();
      }
   }

   public void ChangeModifiers(List<MonsterModifierTypes> modifierTypesList, int numModifiers)
   {
      if (level > 1)
      {
         foreach (var modifier in modifierTypesList)
         {
            Modifiers.Add(modifier);
            Debug.Log("Monster with name " + character.name + " has has changed modifiers. New modifier: " + modifier);
         }

         if (character.m_nview.GetZDO().IsOwner())
         {
            string serializedModifiers = string.Join(",", Modifiers);
            character.m_nview.GetZDO().Set("modifiers", serializedModifiers);
         }
      }
   }

   public void ApplyStartModifiers()
   {
      if (Modifiers.Contains(MonsterModifierTypes.PersonalShield))
      {
         PersonalShield.AddPersonalShield(character);
      }

      if (Modifiers.Contains(MonsterModifierTypes.ShieldDome))
      {
         var shieldDome = character.gameObject.AddComponent<ShieldDome>();
         shieldDome.AddShieldDome(character);
      }

      if (Modifiers.Contains(MonsterModifierTypes.StaggerImmune))
      {
         StaggerImmune.AddStaggerImmune(character);
      }

      if (Modifiers.Contains(MonsterModifierTypes.FastMovement))
      {
         FastMovement.AddFastMovement(character);
      }

      if (Modifiers.Contains(MonsterModifierTypes.DistantDetection))
      {
         DistantDetection.AddDistantDetection(character);
      }

      if (Modifiers.Contains(MonsterModifierTypes.Quiet))
      {
         Quiet.AddQuiet(character);
      }

      // ArmorCharge 시너지: PierceImmunity + SlashImmunity + BluntImmunity + FastMovement
      if (Modifiers.Contains(MonsterModifierTypes.PierceImmunity) &&
          Modifiers.Contains(MonsterModifierTypes.SlashImmunity) &&
          Modifiers.Contains(MonsterModifierTypes.BluntImmunity) &&
          Modifiers.Contains(MonsterModifierTypes.FastMovement))
      {
         Modifiers.Add(MonsterModifierTypes.ArmorCharge);
         ArmorCharge.Apply(character);
      }

      // FireTrail 시너지: FireInfused + FastAttackSpeed
      if (Modifiers.Contains(MonsterModifierTypes.FireInfused) &&
          Modifiers.Contains(MonsterModifierTypes.FastAttackSpeed))
      {
         Modifiers.Add(MonsterModifierTypes.FireTrail);
         var ft = character.gameObject.AddComponent<FireTrail>();
         ft.Init(character);
      }
   }
}
