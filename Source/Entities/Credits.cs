using System.Collections.Generic;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.MultiheartHelper.Entities {
    // Copied from SJ
    [CustomEntity("MultiheartHelper/Credits")]
    public class Credits: Entity {
        public abstract class CreditsNode {
            public float LineHeight => ActiveFont.LineHeight;

            public abstract void Render(Vector2 position, float alignment = 0.5f, float scale = 1f);

            public abstract float Height(float scale = 1f);
        }

        public class Heading : CreditsNode
        {
            public const float HeadingScale = 2.5f;
            public readonly Color HeadingColor;

            public string HeadingString;

            public override void Render(Vector2 position, float alignment = 0.5f, float scale = 1f)
            {
                ActiveFont.DrawEdgeOutline(HeadingString, position.Floor(), new Vector2(alignment, 0f), Vector2.One * 2.5f * scale, Color.White, 4f, Color.DarkSlateBlue, 2f, Color.Black);
            }

            public override float Height(float scale = 1f)
            {
                return base.LineHeight * 2.5f * scale;
            }
        }

        public class Subtitle : CreditsNode
        {
            public const float SubtitleScale = 0.9f;
            public readonly Color HeadingColor;

            public string HeadingString;
            public override float Height(float scale = 1)
            {
                return base.LineHeight * SubtitleScale * scale;
            }

            public override void Render(Vector2 position, float alignment = 0.5F, float scale = 1)
            {
                ActiveFont.DrawEdgeOutline(HeadingString, position.Floor(), new Vector2(alignment, 0f), Vector2.One * SubtitleScale * scale, Color.Gray, 4f, Color.DarkSlateBlue, 2f, Color.Black);
            }
        }

        public class RegularText : CreditsNode
        {
            public const float RegularScale = 1.4f;
            public readonly Color HeadingColor;

            public string HeadingString;
            public override float Height(float scale = 1)
            {
                return base.LineHeight * RegularScale * scale;
            }

            public override void Render(Vector2 position, float alignment = 0.5F, float scale = 1)
            {
                ActiveFont.DrawEdgeOutline(HeadingString, position.Floor(), new Vector2(alignment, 0f), Vector2.One * RegularScale * scale, Color.White, 4f, Color.DarkSlateBlue, 2f, Color.Black);
            }
        }


        private List<CreditsNode> credits = [];
        private float scrollSpeed;
        private float scroll, height;
        private float alignment, scale;
        private float x;
        private bool scrollOffScreen;

        public Credits(EntityData data, Vector2 offset): base(data.Position + offset) {
            x = data.Float("xPos", 0f);
            scrollOffScreen = data.Bool("scrollOffScreen", true);
            alignment = data.Float("alignment", 0.5f);
            scale = data.Float("scale", 1);
            height = 0;
            credits = ParseDialog(data.Attr("dialogKey"));
            foreach(CreditsNode node in credits) {
                height += node.Height(scale) + 10f * scale;
            }
            scrollSpeed = height / data.Float("scrollTime", 60);
            base.Depth = -2000000;
            base.Tag = TagsExt.SubHUD;
        }

        public override void Update()
        {
            base.Update();
            scroll += scrollSpeed * Engine.DeltaTime;
            if (scroll < 0f || (scroll > height && !scrollOffScreen))
            {
                scrollSpeed = 0f;
            }
            scroll = scrollOffScreen ? Calc.Max(scroll, 0f) : Calc.Clamp(scroll, 0f, height);
        }

        public override void Render()
        {
            base.Render();
            Vector2 val = Calc.Floor(new Vector2(x * Engine.Width, Engine.Height - scroll));
            foreach (CreditsNode credit in credits)
            {
                float num = credit.Height(scale);
                // if (val.Y > 0f - num && val.Y < 1080f)
                // {
                    credit.Render(val, alignment, scale);
                // }
                val.Y += num + 10f * scale;
            }
        }

        public static List<CreditsNode> ParseDialog(string dialogKey) {
            List<CreditsNode> nodes = [];

            string dialog = Dialog.Clean(dialogKey);
            foreach(string line in dialog.Split("\n")) {
                if(line.StartsWith("h1: ")) {
                    nodes.Add(new Heading() { HeadingString = line.Substring(4) });
                }
                else if(line.StartsWith("h2: ")) {
                    nodes.Add(new Subtitle() { HeadingString = line.Substring(4) });
                }
                else {
                    nodes.Add(new RegularText() { HeadingString = line });
                }
            }

            return nodes;
        }
    }
}