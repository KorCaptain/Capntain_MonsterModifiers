using System.Collections.Generic;
using HarmonyLib;
using MonsterModifiers.Modifiers;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterModifiers.Patches
{
	[HarmonyPatch(typeof(EnemyHud), "ShowHud")]
	public static class EnemyHud_ShowHud_Patch
	{
		private static void Postfix(EnemyHud __instance, Character c, bool isMount)
		{
			if (c.IsPlayer() || isMount)
				return;

			Custom_Components.MonsterModifier monsterModifier = c.GetComponent<Custom_Components.MonsterModifier>();
			if (monsterModifier == null)
				return;

			ChangeEnemyStars(c, monsterModifier.Modifiers, monsterModifier.IsBossCharacter, monsterModifier.OverflowStars);
		}

		public static void ChangeEnemyStars(Character character, List<MonsterModifierTypes> modifiers,
			bool isBoss = false, int overflowStars = 0)
		{
			if ((character.GetLevel() <= 1 && !isBoss) ||
			    !EnemyHud.instance.m_huds.TryGetValue(character, out var value))
			{
				return;
			}

			GameObject creatureGUI = value.m_gui;

			int startPosition = 0;
			int starLevelsExpandedMaxStarOnHud = 7;

			if (CompatibilityUtils.isStarLevelsExpandedInstalled && character.GetLevel() > starLevelsExpandedMaxStarOnHud)
			{
				startPosition = 2;
			}

			// 보스: minCount 이후 속성들을 별 슬롯에 표시
			// 일반 몬스터: 속성 전체를 별 슬롯에 표시
			int minCount = isBoss ? MonsterModifiersPlugin.Configurations_Boss_Min_Modifiers.Value : 0;
			int iconSlots = modifiers.Count - minCount; // 별 슬롯에 표시할 속성 수

			for (int i = startPosition; i < creatureGUI.transform.childCount; i++)
			{
				Transform child = creatureGUI.transform.GetChild(i);
				if (child.name.StartsWith("level_" + (modifiers.Count - minCount + overflowStars + 1)) ||
				    child.name.StartsWith("level_n") && child.gameObject.activeSelf)
				{
					for (int j = 0; j < child.transform.childCount; j++)
					{
						Transform child2 = child.transform.GetChild(j);
						if (child2.name.StartsWith("star") && child2.gameObject.activeSelf)
						{
							if (j < iconSlots)
							{
								// 아이콘으로 교체
								int modIndex = minCount + j;
								child2.GetComponent<Image>().sprite =
									ModifierUtils.GetModifierIcon(modifiers[modIndex]);
								child2.GetComponent<Image>().color =
									ModifierUtils.GetModifierColor(modifiers[modIndex]);
								child2.GetChild(0).gameObject.SetActive(false);
							}
							// else: overflow ★ → 아이콘 교체 안 함, 별 표시 유지
						}
					}
				}
			}
		}
	}
}
