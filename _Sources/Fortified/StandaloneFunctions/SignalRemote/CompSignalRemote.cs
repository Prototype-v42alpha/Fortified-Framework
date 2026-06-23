using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Multiplayer.API;

namespace Fortified;

// 遥控器按钮项
public class SignalRemoteButton
{
    // 按下发出的信号tag
    public string signalTag;
    // 按钮文本
    public string label = "Send signal";
    // 按钮提示
    public string desc;
    // 按钮图标路径
    public string iconPath;
}

public class CompProperties_SignalRemote : CompProperties
{
    // 按钮列表
    public List<SignalRemoteButton> buttons;

    public CompProperties_SignalRemote()
    {
        compClass = typeof(CompSignalRemote);
    }
}

// 遥控器 每个按钮发一个配置信号
public class CompSignalRemote : ThingComp
{
    public CompProperties_SignalRemote Props => (CompProperties_SignalRemote)props;

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo g in base.CompGetGizmosExtra()) yield return g;
        if (!parent.Spawned || Props.buttons == null) yield break;

        foreach (SignalRemoteButton btn in Props.buttons)
        {
            if (btn == null || btn.signalTag.NullOrEmpty()) continue;
            yield return MakeButton(btn);
        }
    }

    // 构造按钮gizmo
    private Command_Action MakeButton(SignalRemoteButton btn)
    {
        Command_Action cmd = new Command_Action
        {
            defaultLabel = btn.label,
            defaultDesc = btn.desc,
            action = () => SyncedSend(this, btn.signalTag)
        };
        if (!btn.iconPath.NullOrEmpty())
            cmd.icon = ContentFinder<Texture2D>.Get(btn.iconPath, false);
        return cmd;
    }

    // 同步发信号 多人安全
    [SyncMethod]
    private static void SyncedSend(CompSignalRemote comp, string tag)
    {
        if (comp?.parent == null || tag.NullOrEmpty()) return;
        Find.SignalManager.SendSignal(new Signal(tag,
            comp.parent.Named("SUBJECT"),
            comp.parent.Position.Named("POSITION")));
    }
}
