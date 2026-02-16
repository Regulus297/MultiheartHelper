using System;
using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.MultiheartHelper.Data;
using Celeste.Mod.MultiheartHelper.UI.LogTypes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;


namespace Celeste.Mod.MultiheartHelper.UI {
    public class LogsTab : InventoryTab {
        public override string Name => "Logs";
        public const int ITEM_PADDING = 10;
        public static int ItemAreaWidth => Engine.Width / 2 - InventoryUI.MARGIN;
        float maxHeight = 0;
        int selectedY = 0;
        Wiggler selectionWiggler;

        ScrollUI logList;
        ScrollUI textScroll;
        bool logListFocused = true;
        Dictionary<string, LogUI> cache = [];


        public override void Render()
        {
            RenderLogsArea();
            RenderTextArea();
        }

        private void RenderLogsArea()
        {
            Draw.HollowRect(Engine.Width / 2, InventoryUI.MARGIN, ItemAreaWidth, Engine.Height - InventoryUI.MARGIN * 2, ui.PrimaryColor);
            logList.Render();
        }

        private void RenderTextArea()
        {
            textScroll.Render();
        }

        public LogsTab(InventoryUI ui) : base(ui)
        {

        }

        public override void Setup()
        {
            ui.Add(selectionWiggler = Wiggler.Create(0.4f, 4f, v =>
            {
                ui.selectionWobble = v * 5;
            }));
            logList = new(ui, InventoryUI.MARGIN, InventoryUI.MARGIN, ItemAreaWidth, Engine.Height - InventoryUI.MARGIN * 2, RenderLogList);
            textScroll = new(ui, Engine.Width / 2 + ITEM_PADDING, InventoryUI.MARGIN + ITEM_PADDING, ItemAreaWidth, Engine.Height - 2 * (InventoryUI.MARGIN + ITEM_PADDING), RenderText);
        }

        private void RenderText()
        {
            if (!ui.TabFocused)
                return;
            if (selectedY >= MultiheartHelperModule.Session.collectedLogs.Count || selectedY < 0)
                return;

            string selectedItem = MultiheartHelperModule.Session.collectedLogs[selectedY];
            LogUI log;
            if (!cache.TryGetValue(selectedItem, out log))
            {
                string header = Dialog.Clean($"{selectedItem}_text").Split('\n')[0];
                log = LogUI.Create(ui, $"{selectedItem}_text", header.StartsWith("$logType ") ? header.Substring(9) : "text");
                cache[selectedItem] = log;
            }
            float height = log?.Render(textScroll.Translated(0, 0), new Vector2(Engine.Width / 2 - InventoryUI.MARGIN - ITEM_PADDING * 2, Engine.Height - 2 * (InventoryUI.MARGIN + ITEM_PADDING)), ui.PrimaryColor) ?? 0;
            textScroll.TotalSize = new(ItemAreaWidth, height);
            textScroll.Clamped = true;
        }

        public void RenderLogList()
        {
            maxHeight = 0;
            foreach (string logID in MultiheartHelperModule.Session.collectedLogs)
            {
                maxHeight = MathF.Max(maxHeight, ActiveFont.HeightOf(Dialog.Clean($"{logID}_name")));
            }
            logList.TotalSize = new(ItemAreaWidth, (maxHeight + 2 * ITEM_PADDING) * MultiheartHelperModule.Session.collectedLogs.Count);
            logList.Clamped = true;
            int i = 0;
            foreach (string logID in MultiheartHelperModule.Session.collectedLogs)
            {
                ActiveFont.DrawEdgeOutline(Dialog.Clean($"{logID}_name"), logList.Translated(ITEM_PADDING, ITEM_PADDING + i * (ITEM_PADDING * 2 + maxHeight)), Vector2.Zero, Vector2.One, ui.PrimaryColor, 4f, Color.Black);
                i++;
            }
        }

        public override void Focus() {
            ui.TargetRect = GetSelectionRect();
        }

        public override bool RegisterDirection(int x, int y)
        {
            if (logListFocused)
            {
                if (x > 0)
                {
                    logListFocused = false;
                    ui.TargetRect = GetSelectionRect();
                    return true;
                }
                if (selectedY + y < 0 || selectedY + y >= MultiheartHelperModule.Session.collectedLogs.Count)
                {
                    selectionWiggler?.Start();
                    return false;
                }
                selectedY += y;

                Vector2 selectionVector = logList.Translated(new Vector2(0, (int)(selectedY * (maxHeight + ITEM_PADDING * 2))));
                selectionVector += new Vector2(logList.bounds.X, logList.bounds.Y);
                if (y > 0 && selectionVector.Y > Engine.Height / 2)
                {
                    logList.ScrollByY(maxHeight);
                }
                else if (y < 0 && selectionVector.Y < Engine.Height / 2)
                {
                    logList.ScrollByY(-maxHeight);
                }

                ui.TargetRect = GetSelectionRect();
                textScroll.ScrollToY(0);

                return true;
            }
            if (y != 0)
            {
                bool atTop = textScroll.Translated(0, 0).Y >= 0;
                textScroll.ScrollByY(y * (Engine.Width/2 - InventoryUI.MARGIN));
                if (atTop && textScroll.Translated(0, 0).Y >= 0)
                {
                    return false;
                }
                return true;
            }
            else if (x < 0)
            {
                logListFocused = true;
                ui.TargetRect = GetSelectionRect();
                return true;
            }
            return false;
        }

        private Rectangle GetSelectionRect()
        {
            if (logListFocused)
                return new(InventoryUI.MARGIN, InventoryUI.MARGIN + (int)(selectedY * (maxHeight + ITEM_PADDING * 2)), ItemAreaWidth, (int)(maxHeight + ITEM_PADDING * 2));
            return new(Engine.Width / 2, InventoryUI.MARGIN, ItemAreaWidth, Engine.Height - 2 * InventoryUI.MARGIN);
        }
    }
}