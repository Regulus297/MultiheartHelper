using System;
using System.Collections.Generic;
using Celeste.Mod.MultiheartHelper.Data;
using Celeste.Mod.MultiheartHelper.UI;

namespace Celeste.Mod.MultiheartHelper;

public class MultiheartHelperModuleSession : EverestModuleSession
{
    public List<string> collectedHearts = [];
    public List<Action> BeforeUpdateNextFrame = [];
    public List<string> collectedItems = [];
    public List<string> collectedLogs = [];

    public bool HasCateline = false;

    public Queue<TerminalTab.TerminalLine> terminalLines = [];
}