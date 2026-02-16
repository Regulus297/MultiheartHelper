using System;
using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.MultiheartHelper.Entities
{
    [CustomEntity("MultiheartHelper/CatelineRefill")]
    public class CatelineRefill : Entity
    {
        public static ParticleType P_Shatter;

        public static ParticleType P_Regen;

        public static ParticleType P_Glow;  
        private Sprite sprite;

        private Wiggler wiggler;

        private BloomPoint bloom;

        private VertexLight light;

        private Level level;

        private SineWave sine;

        public static bool HasCateline
        {
            get
            {
                return MultiheartHelperModule.Session.HasCateline;
            }
            set
            {
                MultiheartHelperModule.Session.HasCateline = value;
            }
        }

        public static bool GainedCateline = false;
        public static bool Boosting = false;

        public static int TrailIndex = 0;
        public static readonly List<Color> TrailColors = [
            new Color(228, 121, 24),
            new Color(255, 255, 255),
            new Color(81, 81, 81),
            new Color(227, 188, 90)
        ];

        public CatelineRefill(EntityData data, Vector2 offset)
            : base(data.Position + offset)
        {
            Collider = new Hitbox(16f, 16f, -8f, -8f);
            Add(new PlayerCollider(OnPlayer));
            Add(sprite = new Sprite(GFX.Game, "objects/MultiheartHelper/catelineRefill/idle"));
            sprite.AddLoop("idle", "", 0.1f);
            sprite.Play("idle");
            sprite.CenterOrigin();
            Add(wiggler = Wiggler.Create(1f, 4f, delegate(float v)
            {
                sprite.Scale = Vector2.One * (1f + v * 0.2f);
            }));
            Add(new MirrorReflection());
            Add(bloom = new BloomPoint(0.8f, 16f));
            Add(light = new VertexLight(Color.White, 1f, 16, 48));
            Add(sine = new SineWave(0.6f));
            sine.Randomize();
            UpdateY();
            Depth = -100;
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            level = SceneAs<Level>();
        }

        public override void Update()
        {
            base.Update();
            UpdateY();
            light.Alpha = Calc.Approach(light.Alpha, sprite.Visible ? 1f : 0f, 4f * Engine.DeltaTime);
            bloom.Alpha = light.Alpha * 0.8f;
        }

        private void UpdateY()
        {
            Sprite obj2 = sprite;
            float num2 = bloom.Y = sine.Value * 2f;
            obj2.Y = num2;
        }

        public override void Render()
        {
            if (sprite.Visible)
            {
                sprite.DrawOutline();
            }
            base.Render();
        }

        private void OnPlayer(Player player)
        {
            if (CanUse(player))
            {
                player.RefillDash();
                player.RefillStamina();
                Audio.Play("event:/game/general/diamond_touch", Position);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                Collidable = false;
                HasCateline = true;
                if(player.StateMachine.state == Player.StDash)
                    GainedCateline = true;
                Add(new Coroutine(RefillRoutine(player)));
            }
        }

        private bool CanUse(Player player)
        {
            return player.Stamina < 20f || !HasCateline;
        }

        private IEnumerator RefillRoutine(Player player)
        {
            Celeste.Freeze(0.05f);
            yield return null;
            level.Shake();
            Sprite obj = sprite;
            bool visible = false;
            obj.Visible = visible;
            Depth = 8999;
            yield return 0.05f;
            float num = player.Speed.Angle();
            SlashFx.Burst(Position, num);
            RemoveSelf();
        }

        public static void Load()
        {
            On.Celeste.Player.Die += Hook_Die;
            On.Celeste.Player.DashEnd += Hook_DashEnd;
            On.Celeste.Player.BoostBegin += Hook_BoostBegin;
            On.Celeste.Player.Update += Hook_Update;
            On.Celeste.Player.CreateTrail += Hook_CreateTrail;
            On.Celeste.Spikes.OnCollide += Hook_OnCollideSpikes;
            On.Celeste.TriggerSpikes.OnCollide += Hook_OnCollideTrigger;
        }

        private static void Hook_OnCollideTrigger(On.Celeste.TriggerSpikes.orig_OnCollide orig, TriggerSpikes self, Player player)
        {
            if(!HasCateline)
                orig(self, player);
        }

        private static void Hook_BoostBegin(On.Celeste.Player.orig_BoostBegin orig, Player self)
        {
            if(HasCateline)
                Boosting = true;
            orig(self);
        }

        private static void Hook_Update(On.Celeste.Player.orig_Update orig, Player self)
        {
            orig(self);
            if(HasCateline && self.Scene.OnInterval(0.1f))
            {
                CreateTrail(self);
            }
        }

        private static void Hook_CreateTrail(On.Celeste.Player.orig_CreateTrail orig, Player self)
        {
            if(HasCateline)
            {
                CreateTrail(self);
            }
            else
            {
                orig(self);
            }
        }

        private static void CreateTrail(Player self)
        {
            // Copied from communalhelper's dream refill: https://github.com/CommunalHelper/CommunalHelper/blob/dev/src/DashStates/DreamTunnelDash/DreamTunnelDash.cs#L640
            Vector2 scale = new(Math.Abs(self.Sprite.Scale.X) * (float) self.Facing, self.Sprite.Scale.Y);
            TrailManager.Add(self, scale, TrailColors[TrailIndex]);
            TrailIndex++;
            TrailIndex %= TrailColors.Count;
        }

        private static void Hook_DashEnd(On.Celeste.Player.orig_DashEnd orig, Player self)
        {
            if(HasCateline && !GainedCateline && !Boosting)
                HasCateline = false;
            GainedCateline = false;
            Boosting = false;
            orig(self);
        }

        private static void Hook_OnCollideSpikes(On.Celeste.Spikes.orig_OnCollide orig, Spikes self, Player player)
        {
            if(!HasCateline)
                orig(self, player);
        }

        private static PlayerDeadBody Hook_Die(On.Celeste.Player.orig_Die orig, Player self, Vector2 direction, bool evenIfInvincible, bool registerDeathInStats)
        {
            if(HasCateline)
            {
                HasCateline = false;
            }
            GainedCateline = false;
            Boosting = false;
            return orig(self, direction, evenIfInvincible, registerDeathInStats);
        }

        public static void Unload()
        {
            On.Celeste.Player.DashEnd -= Hook_DashEnd;
            On.Celeste.Player.Die -= Hook_Die;
            On.Celeste.Player.BoostBegin -= Hook_BoostBegin;
            On.Celeste.Player.Update -= Hook_Update;
            On.Celeste.Player.CreateTrail -= Hook_CreateTrail;
            On.Celeste.Spikes.OnCollide -= Hook_OnCollideSpikes;
            On.Celeste.TriggerSpikes.OnCollide -= Hook_OnCollideTrigger;
        }
    }
}