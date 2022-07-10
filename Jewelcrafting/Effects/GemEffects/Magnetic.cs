using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Jewelcrafting.GemEffects;

public static class Magnetic
{
	static Magnetic()
	{
		EffectDef.ConfigTypes.Add(Effect.Magnetic, typeof(Config));
	}
	
	[PublicAPI]
	private struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}

	[HarmonyPatch(typeof(Projectile), nameof(Projectile.Setup))]
	private static class TagProjectile
	{
		private static void Postfix(Projectile __instance)
		{
			if (__instance.m_skill == Skills.SkillType.Spears && __instance.m_owner is Player player && Random.value < player.GetEffect(Effect.Magnetic) / 100f)
			{
				__instance.m_nview.GetZDO().Set("Jewelcrafting Magnet", true);
			}
		}
	}

	[HarmonyPatch(typeof(Projectile), nameof(Projectile.SpawnOnHit))]
	public class ReturnWeapon
	{
		private static ItemDrop IssueItemMagnet(ItemDrop item, Projectile projectile)
		{
			if (item && projectile.m_owner is Player player && projectile.m_nview.GetZDO().GetBool("Jewelcrafting Magnet"))
			{
				player.m_nview.InvokeRPC("Jewelcrafting Magnet", item.m_nview.GetZDO().m_uid);
			}

			return item;
		}

		private static readonly MethodInfo ItemDropper = AccessTools.DeclaredMethod(typeof(ItemDrop), nameof(ItemDrop.DropItem));
		private static readonly MethodInfo ItemRecaller = AccessTools.DeclaredMethod(typeof(ReturnWeapon), nameof(IssueItemMagnet));
		
		[UsedImplicitly]
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (CodeInstruction instruction in instructions)
			{
				yield return instruction;
				if (instruction.opcode == OpCodes.Call && instruction.OperandIs(ItemDropper))
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0); // this
					yield return new CodeInstruction(OpCodes.Call, ItemRecaller);
				}
			}
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.Awake))]
	public class RegisterRPC
	{
		[UsedImplicitly]
		private static void Postfix(Player __instance)
		{
			__instance.m_nview.Register<ZDOID>("Jewelcrafting Magnet", (s, item) => RPC_WeaponMagnet(__instance, item));
		}

		private static void RPC_WeaponMagnet(Player player, ZDOID item)
		{
			ZNetScene.instance.FindInstance(item)?.GetComponent<ItemDrop>()?.Pickup(player);
		}
	}
}
