using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using Modding;
using Modding.Utils;
using UnityEngine;
using Satchel;
using Satchel.Futils;
using GlobalEnums;

namespace LaughMod
{
    public class LaughMod : Mod, IGlobalSettings<Settings>, IMenuMod
    {
        public static Settings GS { get; set; } = new();

        public static LaughMod Instance;

        public static System.Random RNG;

        public override string GetVersion() => "1.0.0";

        public void OnLoadGlobal(Settings s)
        {
            if (s.Enabled is null || s.Enabled.Length != GS.Enabled.Length)
            {
                for (int i = 0; i < GS.Enabled.Length; i++) GS.Enabled[i] = true; //enable all by default
                return;
            }
            GS = s;
        }

        public Settings OnSaveGlobal() => GS;

        public static AudioClip[] LaughClips;
        public static float clipStopTime;

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

        public static readonly string[] Descs = [ "Player takes damage from environment", "Player takes damage while focusing", "Player tries to cast a spell with not enough SOUL", "Delicate flower breaks"];

        public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? toggleButtonEntry)
        {
            List<IMenuMod.MenuEntry> result = new();
            if (GS is null) LogWarn("GS IS NULL");
            if (GS.Enabled is null) LogWarn("GS.ENABLED IS NULL");
            for (int i = 0; i <= (int)LaughTrigger.FlowerBreak; i++)
                result.Add(BoolMenuEntry(i, ((LaughTrigger)i).ToString(), Descs[i]));

            return result;
        }

        public bool ToggleButtonInsideMenu => true;


        public static void Play(LaughTrigger trigger)
        {
            if (!TriggerEnabled(trigger)) return;
            Play();
        }

        public static void Play()
        {
            if (Time.time < clipStopTime) return; //Prevent audio overlap.
            AudioClip laughClip = LaughClips[RNG.Next(0, LaughClips.Length)];
            clipStopTime = Time.time + laughClip.length;
            HeroController.instance.gameObject.GetOrAddComponent<AudioSource>().PlayOneShot(laughClip);
        }

        public override void Initialize()
        {
            if (ModHooks.GetMod("Satchel") == null)
            {
                LogError("Mod 'Satchel' is required, and is not enabled!");
                throw new Exception("Mod 'Satchel' is required, and is not enabled!");
            }

            RNG = new();
            string dir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "LaughClips");
            if (!Directory.Exists(dir))
            {
                LogError($"Directory {dir} not found!");
                return;
            }
            LaughClips = AudioUtils.LoadAudioClips(dir);
            //ModHooks.HeroUpdateHook += HeroUpdate;
            On.PlayMakerFSM.Awake += FSMAwake;
            On.HeroController.TakeDamage += TakeDamage;
        }

        /*public static void HeroUpdate()
        {
            if (Input.GetKeyDown(KeyCode.O))
                Play(LaughTrigger.FlowerBreak);
        }*/

        public static void FSMAwake(On.PlayMakerFSM.orig_Awake orig, PlayMakerFSM fsm)
        {
            if (fsm.name == "Knight" && fsm.FsmName == "Spell Control")
            {
                var noSoulState = fsm.AddState("LAUGH_MOD_NO_SOUL");
                fsm.ChangeTransition("Can Cast? QC", "CANCEL", "LAUGH_MOD_NO_SOUL");
                fsm.ChangeTransition("Can Cast?", "CANCEL", "LAUGH_MOD_NO_SOUL");
                noSoulState.AddAction(new CustomFsmAction(() => Play(LaughTrigger.NoSoulCast)));
                noSoulState.AddTransition("FINISHED", "Inactive");
                
                var damagedState = fsm.AddState("LAUGH_MOD_DAMAGED");
                fsm.ChangeTransition("Reset Cam Zoom", "FINISHED", "LAUGH_MOD_DAMAGED");
                damagedState.AddAction(new CustomFsmAction(() =>
                {
                    if (HeroController.instance.cState.focusing) Play(LaughTrigger.DamageWhileFocusing);
                }));
                damagedState.AddTransition("FINISHED", "Cancel All");
            }
            orig(fsm);
        }


        public static void TakeDamage(On.HeroController.orig_TakeDamage orig, HeroController self, GameObject go, CollisionSide damageSide, int damageAmount, int hazardType)
        {
            if (hazardType != 0 && hazardType != 1) Play(LaughTrigger.EnvironmentalDamage); //HKM https://discord.com/channels/879125729936298015/879129307157528576/1321501476568432733
            orig(self, go, damageSide, damageAmount, hazardType);
        }
    }
}