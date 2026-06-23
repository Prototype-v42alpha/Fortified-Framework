using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Fortified
{
    // 滑动时动态重绘损坏覆盖层
    // 复刻SectionLayer_BuildingsDamage几何 加滑动偏移 用DrawMesh实时画
    internal static class DamageOverlayRenderer
    {
        // 按偏移重绘建筑损坏贴图
        public static void DrawShifted(Building b, Vector3 offset)
        {
            if (b.def.useHitPoints == false || b.HitPoints >= b.MaxHitPoints) return;
            if (!b.def.drawDamagedOverlay) return;
            DamageGraphicData dmg = b.def.graphicData?.damageData;
            if (dmg != null && !dmg.enabled) return;

            DrawScratches(b, offset);
            DrawCornersAndEdges(b, offset);
        }

        // 重绘划痕
        private static void DrawScratches(Building b, Vector3 offset)
        {
            int count = CountOverlay(b, DamageOverlay.Scratch);
            if (count == 0) return;

            Rect rect = BuildingsDamageSectionLayerUtility.GetDamageRect(b);
            float margin = Mathf.Min(0.5f * Mathf.Min(rect.width, rect.height), 1f);
            rect = rect.ContractedBy(margin / 2f);
            if (rect.width <= 0f || rect.height <= 0f) return;

            float minDist = Mathf.Max(rect.width, rect.height) * 0.7f;
            List<Vector2> pts = new List<Vector2>();
            Rand.PushState();
            Rand.Seed = b.thingIDNumber * 3697;
            for (int i = 0; i < count; i++) AddScratch(pts, rect.width, rect.height, ref minDist);
            Rand.PopState();

            float alt = DamageAltitude(b);
            IList<Material> mats = BuildingsDamageSectionLayerUtility.GetScratchMats(b);
            Rand.PushState();
            Rand.Seed = b.thingIDNumber * 7;
            for (int k = 0; k < pts.Count; k++)
            {
                float rot = Rand.Range(0f, 360f);
                float size = margin;
                if (rect.width > 0.95f && rect.height > 0.95f) size *= Rand.Range(0.85f, 1f);
                Vector3 center = new Vector3(rect.xMin + pts[k].x, alt, rect.yMin + pts[k].y) + offset;
                DrawPlane(center, new Vector2(size, size), mats.RandomElement(), rot);
            }
            Rand.PopState();
        }

        // 复刻划痕散布算法
        private static void AddScratch(List<Vector2> pts, float w, float h, ref float minDist)
        {
            bool ok = false;
            float x = 0f, y = 0f;
            while (!ok)
            {
                for (int i = 0; i < 5; i++)
                {
                    x = Rand.Value * w;
                    y = Rand.Value * h;
                    float best = float.MaxValue;
                    for (int j = 0; j < pts.Count; j++)
                    {
                        float d = (x - pts[j].x) * (x - pts[j].x) + (y - pts[j].y) * (y - pts[j].y);
                        if (d < best) best = d;
                    }
                    if (best >= minDist * minDist) { ok = true; break; }
                }
                if (!ok) { minDist *= 0.85f; if (minDist < 0.001f) break; }
            }
            if (ok) pts.Add(new Vector2(x, y));
        }

        // 重绘边角
        private static void DrawCornersAndEdges(Building b, Vector3 offset)
        {
            DamageGraphicData dmg = b.def.graphicData?.damageData;
            if (dmg == null) return;
            Rand.PushState();
            Rand.Seed = b.thingIDNumber * 3;
            if (BuildingsDamageSectionLayerUtility.UsesLinkableCornersAndEdges(b))
                DrawLinkable(b, dmg, offset);
            else
                DrawFullCorners(b, offset);
            Rand.PopState();
        }

        // 复刻可链接边角
        private static void DrawLinkable(Building b, DamageGraphicData dmg, Vector3 offset)
        {
            float alt = DamageAltitude(b);
            List<DamageOverlay> overlays = BuildingsDamageSectionLayerUtility.GetOverlays(b);
            IntVec3 pos = b.Position;
            Vector3 baseV = new Vector3(pos.x + 0.5f, alt, pos.z + 0.5f) + offset;
            float ex = Rand.Range(0.4f, 0.6f), ez = Rand.Range(0.4f, 0.6f);
            float ex2 = Rand.Range(0.4f, 0.6f), ez2 = Rand.Range(0.4f, 0.6f);

            for (int i = 0; i < overlays.Count; i++)
            {
                switch (overlays[i])
                {
                    case DamageOverlay.TopEdge: DrawPlane(baseV + new Vector3(ex, 0f, 0f), Vector2.one, dmg.edgeTopMat, 0f); break;
                    case DamageOverlay.RightEdge: DrawPlane(baseV + new Vector3(0f, 0f, ez), Vector2.one, dmg.edgeRightMat, 90f); break;
                    case DamageOverlay.BotEdge: DrawPlane(baseV + new Vector3(ex2, 0f, 0f), Vector2.one, dmg.edgeBotMat, 180f); break;
                    case DamageOverlay.LeftEdge: DrawPlane(baseV + new Vector3(0f, 0f, ez2), Vector2.one, dmg.edgeLeftMat, 270f); break;
                    case DamageOverlay.TopLeftCorner: DrawPlane(baseV, Vector2.one, dmg.cornerTLMat, 0f); break;
                    case DamageOverlay.TopRightCorner: DrawPlane(baseV, Vector2.one, dmg.cornerTRMat, 90f); break;
                    case DamageOverlay.BotRightCorner: DrawPlane(baseV, Vector2.one, dmg.cornerBRMat, 180f); break;
                    case DamageOverlay.BotLeftCorner: DrawPlane(baseV, Vector2.one, dmg.cornerBLMat, 270f); break;
                }
            }
        }

        // 复刻整体边角
        private static void DrawFullCorners(Building b, Vector3 offset)
        {
            Rect rect = BuildingsDamageSectionLayerUtility.GetDamageRect(b);
            float alt = DamageAltitude(b);
            float size = Mathf.Min(Mathf.Min(rect.width, rect.height), 1.5f);
            BuildingsDamageSectionLayerUtility.GetCornerMats(out Material tl, out Material tr, out Material br, out Material bl, b);
            float s1 = size * Rand.Range(0.9f, 1f), s2 = size * Rand.Range(0.9f, 1f);
            float s3 = size * Rand.Range(0.9f, 1f), s4 = size * Rand.Range(0.9f, 1f);
            List<DamageOverlay> overlays = BuildingsDamageSectionLayerUtility.GetOverlays(b);

            for (int i = 0; i < overlays.Count; i++)
            {
                switch (overlays[i])
                {
                    case DamageOverlay.TopLeftCorner:
                    {
                        Rect r = new Rect(rect.xMin, rect.yMax - s1, s1, s1);
                        DrawPlane(new Vector3(r.center.x, alt, r.center.y) + offset, r.size, tl, 0f); break;
                    }
                    case DamageOverlay.TopRightCorner:
                    {
                        Rect r = new Rect(rect.xMax - s2, rect.yMax - s2, s2, s2);
                        DrawPlane(new Vector3(r.center.x, alt, r.center.y) + offset, r.size, tr, 90f); break;
                    }
                    case DamageOverlay.BotRightCorner:
                    {
                        Rect r = new Rect(rect.xMax - s3, rect.yMin, s3, s3);
                        DrawPlane(new Vector3(r.center.x, alt, r.center.y) + offset, r.size, br, 180f); break;
                    }
                    case DamageOverlay.BotLeftCorner:
                    {
                        Rect r = new Rect(rect.xMin, rect.yMin, s4, s4);
                        DrawPlane(new Vector3(r.center.x, alt, r.center.y) + offset, r.size, bl, 270f); break;
                    }
                }
            }
        }

        // 统计指定类型覆盖数
        private static int CountOverlay(Building b, DamageOverlay type)
        {
            int n = 0;
            List<DamageOverlay> overlays = BuildingsDamageSectionLayerUtility.GetOverlays(b);
            for (int i = 0; i < overlays.Count; i++) if (overlays[i] == type) n++;
            return n;
        }

        // 损坏贴图高度
        private static float DamageAltitude(Building b) => b.def.Altitude + 15f / 82f;

        // 实时画一个平面
        private static void DrawPlane(Vector3 center, Vector2 size, Material mat, float rot)
        {
            if (mat == null) return;
            Quaternion q = Quaternion.AngleAxis(rot, Vector3.up);
            Matrix4x4 m = Matrix4x4.TRS(center, q, new Vector3(size.x, 1f, size.y));
            Graphics.DrawMesh(MeshPool.plane10, m, mat, 0);
        }
    }
}
