using System;
using System.Reflection;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.MultiheartHelper.Entities
{
    [Tracked]
    [CustomEntity("MultiheartHelper/SpinnerBreakController")]
    public class SpinnerBreakController : Entity
    {
        public float minDebrisTime, maxDebrisTime;
        public bool affectFrostHelperSpinners;
        public static Type debris;
        public static FieldInfo duration;
        public SpinnerBreakController(EntityData entityData, Vector2 offset) : base()
        {
            minDebrisTime = entityData.Float(nameof(minDebrisTime));
            maxDebrisTime = entityData.Float(nameof(maxDebrisTime));
            affectFrostHelperSpinners = entityData.Bool(nameof(affectFrostHelperSpinners), true);
        }

        public static void Load()
        {
            On.Monocle.Entity.Added += InitDebris;

            try
            {
                EverestModuleMetadata frostHelper = new()
                {
                    Name = "FrostHelper",
                    Version = new Version(1, 74, 0)
                };
                if (Everest.Loader.TryGetDependency(frostHelper, out var module))
                {
                    debris = module.GetType().Assembly.GetType("FrostHelper.FastCrystalDebris");
                    MethodInfo init = debris?.GetMethod("Init", BindingFlags.NonPublic | BindingFlags.Instance);
                    duration = debris?.GetField("_duration", BindingFlags.NonPublic | BindingFlags.Instance);
                }
            }
            catch(Exception)
            {
                return;
            }
        }

        public static void Unload()
        {
            On.Monocle.Entity.Added -= InitDebris;
        }

        private static void InitDebris(On.Monocle.Entity.orig_Added orig, Entity self, Scene scene)
        {
            orig(self, scene);
            if (self is CrystalDebris crystalDebris)
            {
                SpinnerBreakController controller = scene.Tracker?.GetEntity<SpinnerBreakController>();
                if (controller == null)
                    return;

                crystalDebris.duration = Calc.Random.Range(controller.minDebrisTime, controller.maxDebrisTime);
            }
            else if(self.GetType() == debris)
            {
                SpinnerBreakController controller = scene.Tracker?.GetEntity<SpinnerBreakController>();
                if (controller == null || !controller.affectFrostHelperSpinners)
                    return;
                
                duration.SetValue(self, Calc.Random.Range(controller.minDebrisTime, controller.maxDebrisTime));
            }
        }
    }
}