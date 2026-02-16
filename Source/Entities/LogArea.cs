using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Entities;
using Celeste.Mod.MultiheartHelper.UI;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.MultiheartHelper.Entities
{
    [CustomEntity("MultiheartHelper/LogArea")]
    public class LogArea : Entity
    {
        TalkComponent talk;
        List<string> logs;
        string terminalDialog;
        List<string> terminalParameters;
        public LogArea(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            logs = data.Attr("log").Split(',').ToList();
            terminalDialog = data.Attr("terminalDialog");
            terminalParameters = data.Attr("terminalParameters").Split(',').ToList();
            Add(talk = new TalkComponent(new Rectangle(0, 0, data.Width, data.Height), new Vector2(data.Width / 2, data.Height / 2), Interact));
            talk.PlayerMustBeFacing = false;
        }

        private void Interact(Player player)
        {
            if (terminalDialog == "")
            {
                GiveLog();
                return;
            }
            Add(new Coroutine(GiveLogRoutine(), true));
        }

        private void GiveLog()
        {
            foreach (string logToGive in logs)
            {
                if (logToGive != "" && !MultiheartHelperModule.Session.collectedLogs.Contains(logToGive))
                    MultiheartHelperModule.Session.collectedLogs.Add(logToGive);
            }
        }

        private IEnumerator GiveLogRoutine()
        {
            InventoryUI ui;
            (Scene as Level).Add(ui = new InventoryUI(new Color(135, 227, 229), "Terminal"));
            ui.AcceptInput = false;
            if (ui.currentTab is TerminalTab tab)
            {
                yield return tab.PrintDialog(terminalDialog, [GetLogString(), ..terminalParameters]);
            }
            ui.Close();
            talk.RemoveSelf();
            GiveLog();
        }

        public string GetLogString() {
            if (logs.Count == 1)
            {
                return Dialog.Clean($"{logs[0]}_name");
            }
            string output = "";
            int i = 0;
            foreach (string log in logs)
            {
                output += Dialog.Clean($"{log}_name");
                if (i == logs.Count - 2)
                {
                    output += " & ";
                }
                else if (i < logs.Count - 2)
                {
                    output += ", ";
                }
                i++;
            }
            return output;
        }
    }
}