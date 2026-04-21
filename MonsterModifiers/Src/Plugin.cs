using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using Jotunn.Extensions;
using Jotunn.Managers;
using Jotunn.Utils;
using LocalizationManager;
using MonsterModifiers.Modifiers;
using UnityEngine;
using Paths = BepInEx.Paths;

namespace MonsterModifiers
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class MonsterModifiersPlugin : BaseUnityPlugin
    {
        internal const string ModName = "MonsterModifiers";
        internal const string ModVersion = "1.2.6";
        internal const string Author = "warpalicious";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGUID);

        public static readonly ManualLogSource MonsterModifiersLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);

        // Location Manager variables
        public Texture2D tex = null!;

        // Use only if you need them
        //private Sprite mySprite = null!;
        //private SpriteRenderer sr = null!;

        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        public void Awake()
        {
            // Uncomment the line below to use the LocalizationManager for localizing your mod.
            //Localizer.Load(); // Use this to initialize the LocalizationManager (for more information on LocalizationManager, see the LocalizationManager documentation https://github.com/blaxxun-boop/LocalizationManager#example-project).
            bool saveOnSet = Config.SaveOnConfigSet;
            Config.SaveOnConfigSet =
                false; // This and the variable above are used to prevent the config from saving on startup for each config entry. This is speeds up the startup process.

            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();

            if (saveOnSet)
            {
                Config.SaveOnConfigSet = saveOnSet;
                Config.Save();
            }

            YamlUtils.ParseDefaultYamls();
            TranslationUtils.AddLocalizations();
            ModifierAssetUtils.Setup();
            ModifierAssetUtils.LoadAllIcons();
            
            Configurations_MaxModifiers = ConfigFileExtensions.BindConfig(Config, "Balance", "Max Modifiers",5,"The maximum amount of modifiers a creature can have.", true);

            Configurations_Boss_Modifiers = ConfigFileExtensions.BindConfig(Config, "Boss Modifiers", "boss_Modifiers", Toggle.On, "Enable or disable modifier assignment for boss monsters.", true);
            int bossMaxAvail = Enum.GetValues(typeof(MonsterModifierTypes)).Length;
            Configurations_Boss_Min_Modifiers = Config.Bind("Boss Modifiers", "boss_Min_Modifiers", 5,
                new BepInEx.Configuration.ConfigDescription(
                    "Minimum modifiers for bosses regardless of star count. 0 = star-count only (no stars = no modifiers). N = always assign N + star-count modifiers.",
                    new BepInEx.Configuration.AcceptableValueRange<int>(0, bossMaxAvail)));

            Cfg_PierceImmunity_DamageReduction = Config.Bind("Modifier_Defense", "PierceImmunity Damage Reduction %", 70,
                new BepInEx.Configuration.ConfigDescription("Percentage of pierce damage reduced when PierceImmunity modifier is active. 100 = full immunity.", new BepInEx.Configuration.AcceptableValueRange<int>(0, 100)));
            Cfg_SlashImmunity_DamageReduction = Config.Bind("Modifier_Defense", "SlashImmunity Damage Reduction %", 70,
                new BepInEx.Configuration.ConfigDescription("Percentage of slash damage reduced when SlashImmunity modifier is active. 100 = full immunity.", new BepInEx.Configuration.AcceptableValueRange<int>(0, 100)));
            Cfg_BluntImmunity_DamageReduction = Config.Bind("Modifier_Defense", "BluntImmunity Damage Reduction %", 70,
                new BepInEx.Configuration.ConfigDescription("Percentage of blunt damage reduced when BluntImmunity modifier is active. 100 = full immunity.", new BepInEx.Configuration.AcceptableValueRange<int>(0, 100)));
            Cfg_ElementalImmunity_DamageReduction = Config.Bind("Modifier_Defense", "ElementalImmunity Damage Reduction %", 70,
                new BepInEx.Configuration.ConfigDescription("Percentage of elemental damage (fire/frost/lightning/poison/spirit) reduced when ElementalImmunity modifier is active. 100 = full immunity.", new BepInEx.Configuration.AcceptableValueRange<int>(0, 100)));

            Cfg_Knockback_StaggerForce = Config.Bind("Modifier_Offense", "Knockback Stagger Force", 500,
                new BepInEx.Configuration.ConfigDescription("Stagger force applied by the Knockback modifier.", new BepInEx.Configuration.AcceptableValueRange<int>(0, 2000)));
            Cfg_Knockback_PushForce = Config.Bind("Modifier_Offense", "Knockback Push Force", 45,
                new BepInEx.Configuration.ConfigDescription("Push force applied by the Knockback modifier.", new BepInEx.Configuration.AcceptableValueRange<int>(0, 200)));

            ShaderLogFilter.Install();

            // ShieldDome.LoadShieldDome();
            
            CompatibilityUtils.RunCompatibiltyChecks();
            
            StatusEffectUtils.CreateCustomStatusEffects();
            
            PrefabManager.OnVanillaPrefabsAvailable += PrefabUtils.CreateCustomPrefabs;
        }
        
        public static ConfigEntry<int> Configurations_MaxModifiers;
        public static ConfigEntry<Toggle> Configurations_Boss_Modifiers;
        public static ConfigEntry<int> Configurations_Boss_Min_Modifiers;
        public static ConfigEntry<int> Cfg_PierceImmunity_DamageReduction;
        public static ConfigEntry<int> Cfg_SlashImmunity_DamageReduction;
        public static ConfigEntry<int> Cfg_BluntImmunity_DamageReduction;
        public static ConfigEntry<int> Cfg_ElementalImmunity_DamageReduction;
        public static ConfigEntry<int> Cfg_Knockback_StaggerForce;
        public static ConfigEntry<int> Cfg_Knockback_PushForce;


        private void OnDestroy()
        {
            Config.Save();
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                MonsterModifiersLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                MonsterModifiersLogger.LogError($"There was an issue loading your {ConfigFileName}");
                MonsterModifiersLogger.LogError("Please check your config entries for spelling and format!");
            }
        }
    }

    public static class KeyboardExtensions
    {
        public static bool IsKeyDown(this KeyboardShortcut shortcut)
        {
            return shortcut.MainKey != KeyCode.None && Input.GetKeyDown(shortcut.MainKey) &&
                   shortcut.Modifiers.All(Input.GetKey);
        }

        public static bool IsKeyHeld(this KeyboardShortcut shortcut)
        {
            return shortcut.MainKey != KeyCode.None && Input.GetKey(shortcut.MainKey) &&
                   shortcut.Modifiers.All(Input.GetKey);
        }
    }
}