# Jewelcrafting API

Jewelcrafting has an API, which can be used to add your very own gems with your very own effects.

### How to add the API to your mod

If your mod shouldn't work without Jewelcrafting, then simply reference the Jewelcrafting.dll in your project, set a *hard* dependency on Jewelcrafting and ignore the tutorial.

##### Download the API

In the release section on the right side, you can download the JewelcraftingAPI.dll. Download the file and add it to your mods project. Set the "Copy to output directory" setting to "Copy if newer" in the files properties.

##### Merge the DLL

Add the NuGet package ILRepack.Lib.MSBuild.Task to your project. Add a file with the name ILRepack.targets to your mod and paste the following content into this file.

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <Target Name="ILRepacker" AfterTargets="Build">
        <ItemGroup>
            <InputAssemblies Include="$(TargetPath)"/>
            <InputAssemblies Include="$(OutputPath)JewelcraftingAPI.dll"/>
        </ItemGroup>
        <ILRepack Parallel="true" DebugInfo="true" Internalize="true" InputAssemblies="@(InputAssemblies)" OutputFile="$(TargetPath)" TargetKind="SameAsPrimaryAssembly" LibraryPath="$(OutputPath)"/>
    </Target>
</Project>
```

##### Reference Jewelcrafting

Add a reference to the JewelcraftingAPI.dll. Then set a *soft* dependency on Jewelcrafting, to make sure your mod is loaded after Jewelcrafting, like this:

`[BepInDependency("org.bepinex.plugins.jewelcrafting", BepInDependency.DependencyFlags.SoftDependency)]`

### Use the API

#### Adding gems

Use `API.AddGems(Gem type, color name, color code)` to add your own gem, with your own name and color. This adds a Topaz, which color is light blue.
```csharp
API.AddGems("Topaz", "light blue", Color.cyan);
```


Use `API.AddGemEffect<Config type>(Effect name, generic description, detailed description)` to add an effect for your gem. This adds an effect called Whirlwind.
```csharp
API.AddGemEffect<Whirlwind.Config>("Whirlwind", "Increases movement speed.", "Movement speed increased by $1%.");
```


The whirlwind effect increases movement speed. So we add the following class, which has the Harmony patches for the effect and the config we've provided above.
```csharp
using HarmonyLib;
using JetBrains.Annotations;
using Jewelcrafting;

namespace JewelcraftingAPITest;

public static class Whirlwind
{
	[PublicAPI]
	public struct Config
	{
		[InverseMultiplicativePercentagePower] public float Power;
	}
	
	[HarmonyPatch(typeof(Player), nameof(Player.GetJogSpeedFactor))]
	private class IncreaseJogSpeed
	{
		private static void Postfix(Player __instance, ref float __result)
		{
			__result += __instance.GetEffectPower<Config>("Whirlwind").Power / 100f;
		}
	}
		
	[HarmonyPatch(typeof(Player), nameof(Player.GetRunSpeedFactor))]
	private class IncreaseRunSpeed
	{
		private static void Postfix(Player __instance, ref float __result)
		{
			__result += __instance.GetEffectPower<Config>("Whirlwind").Power / 100f;
		}
	}
}
```
Hints for the Config struct: All config fields have to be float. If there are multiple fields, the first one is used for the gem effect display. Each field must have a PowerAttribute.

Different PowerAttributes when equipping multiple gems with this effect:
- **AdditivePowerAttribute**: Will add them together. E.g. 5% and 10% result in 15%.
- **MultiplicativePercentagePowerAttribute**: Will multiply their effect above 100%. E.g. 5% and 10% result in 1.05 * 1.1 = 1.155 = 15.5%.
- **InverseMultiplicativePercentagePowerAttribute**: Will multiply their effect below 100%. E.g. 5% and 10% result in 0.95 * 0.9 = 0.855 = 14.5%
- **MinPowerAttribute**: Takes the lower one. E.g. 5% and 10% result in 5%.
- **MaxPowerAttribute**: Takes the higher one. E.g. 5% and 10% result in 10%.

And finally, use `API.AddGemConfig(yaml string)` to add the default config for your gem effect. You can also specify where your gem should spawn and how common it should be. Default is 0.04f across all biomes. This adds the whirlwind effect to our light blue gem, when it's socketed into pants and makes it increase your movement speed by 100% for a simple topaz, 150% for an advanced topaz and 200% for a perfect topaz. It also makes the gem spawn in the mountain biome only and increases the spawn from 0.04f to 0.3f.
```csharp
StringBuilder sb = new();
sb.AppendLine("whirlwind:");
sb.AppendLine("  slot: legs");
sb.AppendLine("  gem: light blue");
sb.AppendLine("  power: [100, 150, 200]");
sb.AppendLine("gems:");
sb.AppendLine("  mountain:");
sb.AppendLine("    light blue: 0.3");
API.AddGemConfig(sb.ToString());
```

#### Adding Jewelry

This example is adding a necklace to Jewelcrafting. It's using the [ItemManager](https://github.com/blaxxun-boop/ItemManager), for easier handling of the recipe.

```cs
public void Awake()
{
	Item necklace = new(API.CreateNecklaceFromTemplate("Cyan", Color.cyan));
	necklace.Name.English("Cyan Necklace of Domination");
	necklace.Description.English("A cyan necklace that increases your damage dealt by 1000%.");
	necklace.Crafting.Add(API.GetGemcuttersTable().name, 3);
	necklace.MaximumRequiredStationLevel = 3;
	necklace.RequiredItems.Add("Coins", 1000);
	necklace.RequiredUpgradeItems.Add("Coins", 1500);

	ItemDrop.ItemData.SharedData necklaceShared = necklace.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared;

	StatusEffect domination = ScriptableObject.CreateInstance<IncreaseDamageDone>();
	domination.name = "Domination";
	domination.m_name = "Domination";
	domination.m_icon = necklaceShared.m_icons[0];
	domination.m_tooltip = "You are dominating. Your damage dealt is increased by 1000%.";

	necklaceShared.m_equipStatusEffect = domination;
}

public class IncreaseDamageDone : StatusEffect
{
	public override void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
	{
		hitData.ApplyModifier(10);
	}
}
```

You can use `IsLoaded()` to check, if the user is using Jewelcrafting.
