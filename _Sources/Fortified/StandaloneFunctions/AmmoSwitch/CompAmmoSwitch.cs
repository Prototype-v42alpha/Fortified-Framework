using RimWorld;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Fortified
{
    // CompProperties
    public class CompProperties_AmmoSwitch : CompProperties
    {
        public List<AmmoOption> ammos = new List<AmmoOption>();
        public int defaultIndex = 0;
        public float switchCooldownSeconds = 1.5f;

        public CompProperties_AmmoSwitch()
        {
            compClass = typeof(CompAmmoSwitch);
        }
    }
    public class CompAmmoSwitch : ThingComp
    {
        private int selectedIndex;
        private int cooldownUntilTick;

        public CompProperties_AmmoSwitch Props => (CompProperties_AmmoSwitch)props;

        public bool HasAnyAmmoOption => Props?.ammos != null && Props.ammos.Count > 0;
        public int OptionCount => HasAnyAmmoOption ? Props.ammos.Count : 0;
        public int SelectedIndex => selectedIndex;

        public AmmoOption CurrentAmmo
        {
            get
            {
                if (!HasAnyAmmoOption) return null;
                int idx = Mathf.Clamp(selectedIndex, 0, Props.ammos.Count - 1);
                return Props.ammos[idx];
            }
        }

        public ThingDef CurrentProjectile => CurrentAmmo?.projectileDef;
        public string CurrentLabel => CurrentAmmo?.ResolveLabel() ?? "N/A";
        public Texture2D CurrentIcon => CurrentAmmo?.ResolveIcon() ?? BaseContent.BadTex;

        public bool IsOnSwitchCooldown
        {
            get
            {
                if (Find.TickManager == null) return false;
                return Find.TickManager.TicksGame < cooldownUntilTick;
            }
        }

        public AmmoOption GetAmmoAt(int index)
        {
            if (!HasAnyAmmoOption) return null;
            if (index < 0 || index >= Props.ammos.Count) return null;
            return Props.ammos[index];
        }

        public void SetAmmo(int index, bool startCooldown = true)
        {
            if (!HasAnyAmmoOption) return;

            int clamped = Mathf.Clamp(index, 0, Props.ammos.Count - 1);
            bool changed = clamped != selectedIndex;
            selectedIndex = clamped;

            if (changed && startCooldown)
            {
                int cdTicks = Mathf.Max(0, Props.switchCooldownSeconds.SecondsToTicks());
                if (cdTicks > 0 && Find.TickManager != null)
                    cooldownUntilTick = Find.TickManager.TicksGame + cdTicks;
            }
        }

        public string GetAmmoTooltip(int index)
        {
            AmmoOption ammo = GetAmmoAt(index);
            if (ammo == null) return "無效彈種";

            string projText = ammo.projectileDef != null
                ? $"{ammo.projectileDef.LabelCap} ({ammo.projectileDef.defName})"
                : "未設定";

            return $"彈種：{ammo.ResolveLabel()}\n投射物：{projText}";
        }

        public string GetGizmoDesc()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"當前彈種：{CurrentLabel}");

            if (CurrentAmmo?.projectileDef != null)
                sb.AppendLine(CurrentAmmo.description);

            if (HasAnyAmmoOption)
            {
                for (int i = 0; i < Props.ammos.Count; i++)
                {
                    AmmoOption a = Props.ammos[i];
                    string mark = (i == selectedIndex) ? "✓ " : "  ";
                    string proj = a?.projectileDef != null ? a.projectileDef.LabelCap : "未設定";
                    sb.AppendLine($"{mark}{a?.ResolveLabel() ?? "N/A"} -> {proj}");
                }
            }

            return sb.ToString().TrimEnd();
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            if (HasAnyAmmoOption)
                selectedIndex = Mathf.Clamp(Props.defaultIndex, 0, Props.ammos.Count - 1);
            else
                selectedIndex = 0;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            int defaultIdx = Props?.defaultIndex ?? 0;
            Scribe_Values.Look(ref selectedIndex, "selectedIndex", defaultIdx);
            Scribe_Values.Look(ref cooldownUntilTick, "cooldownUntilTick", 0);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && HasAnyAmmoOption)
                selectedIndex = Mathf.Clamp(selectedIndex, 0, Props.ammos.Count - 1);
        }
    }

    public class Command_AmmoSwitch : Command_Action
    {
        public CompAmmoSwitch comp;
        public LocalTargetInfo messageTarget;

        public void OpenCurrentProjectileInfoCard()
        {
            if (comp?.CurrentProjectile == null)
            {
                Messages.Message("目前彈種未設定投射物。", MessageTypeDefOf.RejectInput, false);
                return;
            }

            Find.WindowStack.Add(new Dialog_InfoCard(comp.CurrentProjectile));
        }

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                if (comp == null || !comp.HasAnyAmmoOption) yield break;

                for (int i = 0; i < comp.OptionCount; i++)
                {
                    int idx = i;
                    AmmoOption ammo = comp.GetAmmoAt(idx);

                    if (ammo == null) continue;
                    if (idx == comp.SelectedIndex) continue;

                    string label = ammo.ResolveLabel();

                    FloatMenuOption opt;
                    Texture2D icon = ammo.ResolveIcon();
                    if (icon != null)
                    {
                        opt = new FloatMenuOption(label, () => SelectAmmo(idx), icon, Color.white);
                    }
                    else
                    {
                        opt = new FloatMenuOption(label, () => SelectAmmo(idx));
                    }

                    opt.tooltip = new TipSignal(comp.GetAmmoTooltip(idx));
                    yield return opt;
                }
            }
        }

        private void SelectAmmo(int index)
        {
            comp.SetAmmo(index, startCooldown: true);
        }
    }
}