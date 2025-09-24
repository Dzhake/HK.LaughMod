using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Modding;
using Modding.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LaughMod
{
    public class LaughMod : Mod, IGlobalSettings<Settings>, IMenuMod
    {
        public static Settings GS { get; set; } = new();

        public static LaughMod Instance;

        public override string GetVersion() => "1.0.0";

        public void OnLoadGlobal(Settings s)
        {
            GS = s;
            if (GS.Enabled is null) GS = new();
        }

        public Settings OnSaveGlobal() => GS;

        public static AudioClip LaughClip;
        public static AudioSource Source;

        public LaughMod() : base("LaughMod")
        {
            Instance = this;
        }

        public static bool TriggerEnabled(LaughTrigger trigger) => GS.Enabled[(int)trigger];

        public IMenuMod.MenuEntry BoolMenuEntry(int id, string name, [CanBeNull] string desc = null) => new()
        {
            Name = name,
            Description = desc,
            Values = new[] { "Off", "On" },
            Saver = opt => GS.Enabled[id] = opt switch
            {
                0 => false,
                1 => true,
                _ => throw new InvalidOperationException()
            },
            Loader = () => GS.Enabled[id] switch
            {
                false => 0,
                true => 1,
            }
        };

        public static readonly string[] Descs = [ "Player dies", "Player takes damage from environment", "Player takes damage while focusing", "Player tries to cast a spell with not enough SOUL", "Delicate flower breaks", "Grub reveals itself to be a mimic" ];

        public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? toggleButtonEntry)
        {
            List<IMenuMod.MenuEntry> result = new();
            if (GS is null) LogWarn("GS IS NULL");
            if (GS.Enabled is null) LogWarn("GS.ENABLED IS NULL");
            for (int i = 0; i <= (int)LaughTrigger.GrubMimic; i++)
                result.Add(BoolMenuEntry(i, ((LaughTrigger)i).ToString(), Descs[i]));

            return result;
        }

        public bool ToggleButtonInsideMenu { get; } = true;


        public static void Play(LaughTrigger trigger)
        {
            if (!TriggerEnabled(trigger)) return;
            Play();
        }

        public static void Play()
        {
            HeroController.instance.gameObject.GetOrAddComponent<AudioSource>().PlayOneShot(LaughClip);
        }

        public override void Initialize()
        {
            LaughClip = AudioUtils.LoadAudioClip(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "laugh.wav"));
            ModHooks.BeforePlayerDeadHook += () => Play(LaughTrigger.PlayerDeath);
            On.HeroController.TakeMP += OnTakeMP;
            ModHooks.HeroUpdateHook += HeroUpdate;
        }

        public static void HeroUpdate()
        {
            if (Input.GetKeyDown(KeyCode.O))
            {
                Play(LaughTrigger.FlowerBreak);
            }
        }

        public static void OnTakeMP(On.HeroController.orig_TakeMP orig, HeroController self, int amount)
        {
            Instance.Log(self.playerData.GetInt("MPCharge"));
            if (self.playerData.GetInt("MPCharge") <= 0)
                Play(LaughTrigger.NoSoulCast);
            orig(self, amount);
        }
    }
}