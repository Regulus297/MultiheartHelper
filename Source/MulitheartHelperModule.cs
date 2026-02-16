using System;
using System.Collections.Generic;
using Celeste.Mod.MultiheartHelper.Data;
using Celeste.Mod.MultiheartHelper.Entities;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Celeste.Mod.MultiheartHelper;

public class MultiheartHelperModule : EverestModule {
    public static MultiheartHelperModule Instance { get; private set; }

    public override Type SettingsType => typeof(MultiheartHelperModuleSettings);
    public static MultiheartHelperModuleSettings Settings => (MultiheartHelperModuleSettings) Instance._Settings;

    public override Type SessionType => typeof(MultiheartHelperModuleSession);
    public static MultiheartHelperModuleSession Session => (MultiheartHelperModuleSession) Instance._Session;

    public override Type SaveDataType => typeof(MultiheartHelperModuleSaveData);
    public static MultiheartHelperModuleSaveData SaveData => (MultiheartHelperModuleSaveData) Instance._SaveData;

    public static Dictionary<AreaData, MultiheartMetadata> multiheartData = [];
    public static Dictionary<string, AreaItemMetadata> itemData = [];

    public MultiheartHelperModule()
    {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(MultiheartHelperModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(MultiheartHelperModule), LogLevel.Info);
#endif
    }

    public override void Load()
    {
        On.Celeste.AreaData.Load += PostAreaLoad;
        IL.Celeste.OuiJournalProgress.ctor += Hook_OuiJournalProgress_ctor;
        On.Monocle.Scene.BeforeUpdate += Hook_BeforeUpdate;
        MultiheartModifier.Hook();
        SemipermanentCrumbleBlock.Hook();
        LineMirror.Hook();
        SpinnerBreakController.Load();
        CatelineRefill.Load();
    }

    private void Hook_BeforeUpdate(On.Monocle.Scene.orig_BeforeUpdate orig, Monocle.Scene self)
    {
        if (Session != null)
        {
            foreach (var action in Session.BeforeUpdateNextFrame)
            {
                action?.Invoke();
            }
            Session.BeforeUpdateNextFrame.Clear();
        }
        orig(self);
    }

    private static void Hook_OuiJournalProgress_ctor(ILContext il)
    {
        ILCursor c = new(il);
        c.GotoNext(i => i.MatchLdstr("heartgem"));
        c.GotoNext(i => i.MatchLdstr("dot"));
        c.Index -= 2;
        c.Emit(OpCodes.Ldloc_2);
        c.Emit(OpCodes.Ldloc, 4);
        c.EmitDelegate<Action<AreaData, List<string>>>((data, list) => {
            if(multiheartData.TryGetValue(data, out MultiheartMetadata meta) && SaveData.TryGetData(data.ID, out var savedData)) {
                list.Clear();
                foreach(var heart in meta.Hearts) {
                    if (MTN.Journal.Has(heart.Texture))
                    {
                        if (savedData.unlockedHearts.Contains(heart.Name))
                            list.Add(heart.Texture);
                    }
                }
            }
        });

        c.GotoNext(i => i.MatchLdcR4(-32));
        c.Index++;
        c.Emit(OpCodes.Ldloc_2);
        c.Emit(OpCodes.Ldloc, 8);
        c.EmitDelegate(AddHeartIconsHook);

    }

    private static float AddHeartIconsHook(float value, AreaData data, OuiJournalPage.Row row) {
        if(!multiheartData.TryGetValue(data, out var meta))
            return value;


        return meta.Spacing;
    }

    private void PostAreaLoad(On.Celeste.AreaData.orig_Load orig)
    {
        orig();
        foreach (var map in AreaData.Areas)
        {
            MultiheartMetadata meta;
            if (Everest.Content.TryGet("Maps/" + map.Mode[0].Path + ".multiheart.meta", out ModAsset metaFile) && metaFile.TryDeserialize(out meta))
            {
                multiheartData[map] = meta;
            }

            AreaItemMetadata itemMeta;
            if (Everest.Content.TryGet("Maps/" + map.Mode[0].Path + ".items.meta", out ModAsset itemFile) && itemFile.TryDeserialize(out itemMeta))
            {
                itemData[map.Mode[0].Path] = itemMeta;
            }
        }
    }


    public override void Unload()
    {
        multiheartData.Clear();
        SemipermanentCrumbleBlock.Unhook();
        LineMirror.Unhook();
        SpinnerBreakController.Unload();
        CatelineRefill.Unload();
    }
}