using RimWorld;
using UnityEngine;
using Verse;

namespace Fortified
{
    [StaticConstructorOnStartup]
    public class Gizmo_BulletproofPlateStatus : Gizmo
    {
        private static readonly Texture2D FullPlateBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.3f, 0.3f, 0.3f));

        private static readonly Texture2D EmptyPlateBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);

        public CompBulletproofPlate bulletproofPlate;

        public Gizmo_BulletproofPlateStatus()
        {
            Order = -120f;
        }

        public override bool Visible
        {
            get
            {
                return bulletproofPlate != null && bulletproofPlate.parent != null;
            }
        }

        public override float GetWidth(float maxWidth)
        {
            return 140f;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            if (bulletproofPlate == null || bulletproofPlate.parent == null)
            {
                return new GizmoResult(GizmoState.Clear);
            }

            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Rect rect2 = rect.ContractedBy(6f);
            Widgets.DrawWindowBackground(rect);
            
            Rect rect3 = rect2;
            rect3.height = rect.height / 2f;
            Text.Font = GameFont.Tiny;
            Widgets.Label(rect3, bulletproofPlate.parent.LabelCap);
            
            Rect rect4 = rect2;
            rect4.yMin = rect2.y + rect2.height / 2f;
            float fillPercent = Mathf.Min(1f, bulletproofPlate.DurabilityPercent);
            Widgets.FillableBar(rect4, fillPercent, FullPlateBarTex, EmptyPlateBarTex, doBorder: false);
            
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect4, (bulletproofPlate.GetCurrentDurability()).ToString("F1") + " / " + bulletproofPlate.GetMaxDurability());
            Text.Anchor = TextAnchor.UpperLeft;
            
            TooltipHandler.TipRegion(rect2, "FFF.BulletproofPlateTooltip".Translate());
            return new GizmoResult(GizmoState.Clear);
        }
    }
}
