using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace Fortified
{
    // 涂装颜色配置
    public class Dialog_PaintConfig : Window
    {
        private CompPaintable comp;
        private Pawn pawn;
        private Color baseColor;
        private Color baseColor2;
        private Color baseColor3;
        private FFF_CamoDef camoDef;
        private float brightness;
        private FFF_OverlayDef overlayDef;
        private float previewZoom = 1.0f;
        private int previewRot;

        private Vector2 camoScrollPos;
        private Color initialColor;
        private Color initialColor2;
        private Color initialColor3;
        private FFF_CamoDef initialCamo;
        private float initialBrightness;
        private FFF_OverlayDef initialOverlay;
        private Vector2 overlayScrollPos;


        private float totalHeight = 500f;

        // 迷彩预览缓存
        private Dictionary<FFF_CamoDef, Texture2D> camoPreviewCache = new();
        private Texture2D nonePreviewTex;
        private Color cachedPreviewColor;
        private Color cachedPreviewColor2;
        private Color cachedPreviewColor3;
        private float cachedPreviewBrightness;
        private bool bakeDirty;
        private bool bakeImmediate;
        private float bakeRequestTime;

        // 叠加层预览缓存
        private Dictionary<FFF_OverlayDef, Texture2D> overlayPreviewCache = new();
        private Texture2D overlayNonePreviewTex;

        // 渲染副本
        private Pawn previewPawn;
        private CompPaintable previewComp;

        // 迷彩/叠加层显示尺度：0=2列, 1=4列, 2=6列
        private static int savedCamoScale = 0;
        private static int savedOverlayScale = 0;

        // 文本输入缓冲区
        private Dictionary<string, string> textBuffers = new();

        public override Vector2 InitialSize => new Vector2(1000f, totalHeight);

        protected override void SetInitialSizeAndPosition()
        {
            float height = 500f;
            height += 30f;
            height += 28f * 3;
            if (comp.Props.enableCamoSwitch) height += (34f + 28f * 3) * 2;
            height += 36f;
            height += 50f;
            totalHeight = Mathf.Max(height, 780f);
            base.SetInitialSizeAndPosition();
        }

        public Dialog_PaintConfig(CompPaintable comp)
        {
            this.comp = comp;
            this.pawn = comp.parent as Pawn;
            this.baseColor = comp.color1;
            this.baseColor2 = comp.color2;
            this.baseColor3 = comp.color3;
            this.camoDef = comp.camoDef;
            this.brightness = comp.brightness;
            this.overlayDef = comp.overlayDef;

            // 记录初始状态
            this.initialColor = comp.color1;
            this.initialColor2 = comp.color2;
            this.initialColor3 = comp.color3;
            this.initialCamo = comp.camoDef;
            this.initialBrightness = comp.brightness;
            this.initialOverlay = comp.overlayDef;

            this.previewRot = 2;
            this.previewZoom = GetDefaultZoom();
            this.forcePause = true;
            this.doCloseX = true;
            this.closeOnClickedOutside = false;

            // 生成渲染副本并烘焙预览
            try
            {
                previewPawn = PawnGenerator.GeneratePawn(pawn.kindDef);
                previewComp = previewPawn.TryGetComp<CompPaintable>();
                if (previewComp != null)
                {
                    previewComp.color1 = baseColor;
                    previewComp.color2 = baseColor2;
                    previewComp.color3 = baseColor3;
                    previewComp.brightness = brightness;
                    previewComp.overlayDef = overlayDef;
                    BakeAllPreviews();
                }
            }
            catch (System.Exception e)
            {
                Log.Warning($"[Fortified] 生成预览副本失败: {e.Message}");
                previewPawn = null;
                previewComp = null;
            }
        }

        // 获取初始缩放
        private float GetDefaultZoom()
        {
            if (pawn == null) return 1f;
            Vector2 size = Vector2.one;
            if (pawn.Drawer?.renderer?.BodyGraphic != null) size = pawn.Drawer.renderer.BodyGraphic.drawSize;
            else if (pawn.ageTracker?.CurKindLifeStage?.bodyGraphicData != null) size = pawn.ageTracker.CurKindLifeStage.bodyGraphicData.drawSize;
            float maxDim = Mathf.Max(size.x, size.y);
            return maxDim > 0.1f ? 1.28f / maxDim : 1.28f;
        }

        // 烘焙所有预览
        private void BakeAllPreviews()
        {
            if (previewPawn == null || previewComp == null) return;
            const int renderSize = 256;
            var renderer = Find.PawnCacheRenderer;
            if (renderer == null) return;

            // 设置副本参数
            previewComp.color1 = baseColor;
            previewComp.color2 = baseColor2;
            previewComp.color3 = baseColor3;
            previewComp.brightness = brightness;
            previewComp.overlayDef = overlayDef;

            // 烘焙无迷彩
            previewComp.camoDef = null;
            previewPawn.Drawer.renderer.SetAllGraphicsDirty();
            nonePreviewTex = RenderToTexture2D(renderer, renderSize);

            // 烘焙每个迷彩
            foreach (var def in DefDatabase<FFF_CamoDef>.AllDefsListForReading)
            {
                previewComp.camoDef = def;
                previewPawn.Drawer.renderer.SetAllGraphicsDirty();
                camoPreviewCache[def] = RenderToTexture2D(renderer, renderSize);
            }

            // 恢复副本状态
            previewComp.camoDef = camoDef;
            previewComp.overlayDef = overlayDef;

            cachedPreviewBrightness = brightness;

            // 烘焙无叠加层预览
            previewComp.camoDef = camoDef;
            previewComp.overlayDef = null;
            previewPawn.Drawer.renderer.SetAllGraphicsDirty();
            overlayNonePreviewTex = RenderToTexture2D(renderer, renderSize);

            // 烘焙各叠加层预览
            foreach (var od in comp.Props.availableOverlays)
            {
                previewComp.camoDef = camoDef;
                previewComp.overlayDef = od;
                previewPawn.Drawer.renderer.SetAllGraphicsDirty();
                overlayPreviewCache[od] = RenderToTexture2D(renderer, renderSize);
            }

            // 恢复副本状态
            previewComp.camoDef = camoDef;
            previewComp.overlayDef = overlayDef;
        }

        // 同步渲染到 Texture2D
        private Texture2D RenderToTexture2D(PawnCacheRenderer renderer, int size)
        {
            var rt = new RenderTexture(size, size, 16, RenderTextureFormat.ARGB32);
            rt.Create();
            // 调整缩放
            float zoom = previewZoom * 1.5f;
            renderer.RenderPawn(previewPawn, rt, default, zoom, 0f, new Rot4(previewRot));

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            rt.Release();
            UnityEngine.Object.Destroy(rt);
            return tex;
        }

        // 获取最终颜色
        private Color FinalColor() => baseColor;

        public override void DoWindowContents(Rect inRect)
        {
            SyncPendingData();
            float y = inRect.y;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 30f), "FFF_PaintSystemTitle".Translate());
            Text.Font = GameFont.Small;
            y += 36f;

            // 左列：预览 + 滑条 + 按钮，宽 440px
            float leftW = 440f;
            DrawPreview(ref y, inRect, leftW);
            y += 10f;

            Rect leftHalf = new Rect(inRect.x, y, leftW, inRect.height - y);
            DrawColorSliders(ref y, leftHalf);
            DrawMiscSliders(ref y, leftHalf);

            y += 10f;
            if (Widgets.ButtonText(new Rect(inRect.x, y, leftW - 30f, 36f), "FFF_ConfirmPaint".Translate()))
            {
                ApplyPaint();
                Close();
            }

            // 右列：迷彩面板
            Rect rightHalf = new Rect(inRect.x + leftW + 10f, 36f, inRect.width - leftW - 20f, inRect.height - 46f);
            DrawCamoSelection(rightHalf);
        }

        // 同步副本预览数据（不碰真实 pawn）
        private void SyncPendingData()
        {
            if (previewComp == null) return;

            if (previewComp == null) return;

            // 检测变化
            bool colorChanged = previewComp.color1 != baseColor || previewComp.color2 != baseColor2
                || previewComp.color3 != baseColor3 || previewComp.brightness != brightness;
            bool camoChanged = previewComp.camoDef != camoDef;
            bool overlayChanged = previewComp.overlayDef != overlayDef;

            previewComp.color1 = baseColor;
            previewComp.color2 = baseColor2;
            previewComp.color3 = baseColor3;
            previewComp.camoDef = camoDef;
            previewComp.brightness = brightness;
            previewComp.overlayDef = overlayDef;
            PortraitsCache.SetDirty(previewPawn);
            previewPawn.Drawer.renderer.SetAllGraphicsDirty();

            if (colorChanged || camoChanged || overlayChanged)
            {
                bakeDirty = true;
                bakeRequestTime = UnityEngine.Time.realtimeSinceStartup;
                // 立即更新标记
                if (camoChanged || overlayChanged) bakeImmediate = true;
            }
        }

        // 绘制迷彩选择
        private void DrawCamoSelection(Rect rect)
        {
            var availOverlays = comp.Props.availableOverlays;
            bool hasOverlays = availOverlays != null && availOverlays.Count > 0;

            // 分割迷彩和叠加层
            float splitRatio = hasOverlays ? 0.6f : 1f;
            Rect camoRect = new Rect(rect.x, rect.y, rect.width, rect.height * splitRatio - (hasOverlays ? 4f : 0f));
            Rect overlayRect = hasOverlays
                ? new Rect(rect.x, rect.y + rect.height * splitRatio + 4f, rect.width, rect.height * (1f - splitRatio) - 4f)
                : Rect.zero;

            DrawCamoPanel(camoRect);
            if (hasOverlays)
                DrawOverlayPanel(overlayRect);
        }

        // 绘制迷彩选择面板
        private void DrawCamoPanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(4f);
            Text.Font = GameFont.Small;

            // 标题行
            Rect headerRow = new Rect(innerRect.x, innerRect.y, innerRect.width, 24f);
            Widgets.Label(new Rect(headerRow.x, headerRow.y, headerRow.width - 150f, 24f), "FFF_CamoScheme".Translate());
            float btnW = 45f;
            float btnX = headerRow.xMax - 145f;
            if (Widgets.ButtonText(new Rect(btnX, headerRow.y, btnW, 22f), "FFF_2Columns".Translate())) savedCamoScale = 0;
            if (Widgets.ButtonText(new Rect(btnX + 48f, headerRow.y, btnW, 22f), "FFF_4Columns".Translate())) savedCamoScale = 1;
            if (Widgets.ButtonText(new Rect(btnX + 96f, headerRow.y, btnW, 22f), "FFF_6Columns".Translate())) savedCamoScale = 2;
            // 选中高亮
            GUI.color = new Color(0.4f, 0.9f, 0.4f);
            Widgets.DrawBox(new Rect(btnX + savedCamoScale * 48f, headerRow.y, btnW, 22f), 2);
            GUI.color = Color.white;

            // 计算网格
            int targetCols = savedCamoScale == 0 ? 2 : savedCamoScale == 1 ? 4 : 6;
            float padding = 4f;
            float availW = innerRect.width - 16f; // 预留滚条宽
            float cellSize = (availW - padding * (targetCols + 1)) / targetCols;
            int cols = targetCols;

            float startY = innerRect.y + 32f;

            var allDefs = DefDatabase<FFF_CamoDef>.AllDefsListForReading;
            int totalItems = 1 + allDefs.Count;
            int rows = Mathf.CeilToInt((float)totalItems / cols);
            float viewHeight = rows * (cellSize + padding) + padding;

            // 检测颜色变化
            if (cachedPreviewColor != baseColor || cachedPreviewColor2 != baseColor2 ||
                cachedPreviewColor3 != baseColor3 || cachedPreviewBrightness != brightness)
            {
                bakeDirty = true;
                bakeRequestTime = UnityEngine.Time.realtimeSinceStartup;
                cachedPreviewColor = baseColor;
                cachedPreviewColor2 = baseColor2;
                cachedPreviewColor3 = baseColor3;
                cachedPreviewBrightness = brightness;
            }

            // 处理烘焙请求
            bool shouldBake = bakeImmediate || (bakeDirty && UnityEngine.Time.realtimeSinceStartup - bakeRequestTime >= 0.2f);
            if (shouldBake)
            {
                foreach (var t in camoPreviewCache.Values) if (t) UnityEngine.Object.Destroy(t);
                foreach (var t in overlayPreviewCache.Values) if (t) UnityEngine.Object.Destroy(t);
                if (nonePreviewTex) UnityEngine.Object.Destroy(nonePreviewTex);
                if (overlayNonePreviewTex) UnityEngine.Object.Destroy(overlayNonePreviewTex);

                bakeDirty = false;
                bakeImmediate = false;
                camoPreviewCache.Clear();
                overlayPreviewCache.Clear();
                nonePreviewTex = null;
                overlayNonePreviewTex = null;
                BakeAllPreviews();
            }

            Rect scrollRect = new Rect(innerRect.x, startY, innerRect.width, innerRect.height - 28f);
            Rect viewRect = new Rect(0, 0, innerRect.width - 16f, viewHeight);
            Widgets.BeginScrollView(scrollRect, ref camoScrollPos, viewRect);


            for (int i = 0; i < totalItems; i++)
            {
                bool isNone = i == 0;
                FFF_CamoDef def = isNone ? null : allDefs[i - 1];
                bool selected = isNone ? camoDef == null : camoDef == def;

                int col = i % cols;
                int row = i / cols;
                float cx = col * (cellSize + padding) + padding;
                float cy = row * (cellSize + padding) + padding;
                Rect cell = new Rect(cx, cy, cellSize, cellSize);

                Widgets.DrawBoxSolid(cell, new Color(0.12f, 0.12f, 0.12f));

                // 绘制预览
                Texture2D tex = isNone ? nonePreviewTex
                    : (camoPreviewCache.TryGetValue(def, out var t) ? t : null);
                if (tex != null)
                    GUI.DrawTexture(cell, tex, ScaleMode.ScaleToFit);

                // 选中高亮
                if (selected)
                {
                    GUI.color = new Color(0.4f, 0.9f, 0.4f);
                    Widgets.DrawBox(cell, 2);
                    GUI.color = Color.white;
                }
                else
                {
                    Widgets.DrawBox(cell, 1);
                }

                // 标签
                Text.Font = GameFont.Tiny;
                string lbl = isNone ? "FFF_CamoStandard".Translate() : (def.label ?? def.defName);
                Widgets.Label(new Rect(cell.x, cell.yMax - 18f, cell.width, 18f), lbl);
                Text.Font = GameFont.Small;

                // 点击选择
                if (Widgets.ButtonInvisible(cell))
                {
                    camoDef = def;
                    bakeImmediate = true;
                    bakeRequestTime = 0f;
                }

                if (Mouse.IsOver(cell))
                {
                    Widgets.DrawHighlight(cell);
                    TooltipHandler.TipRegion(cell, isNone ? "FFF_CamoStandardDesc".Translate() : (def.label ?? def.defName));
                }
            }
            Widgets.EndScrollView();
        }

        // 绘制叠加层选择面板
        private void DrawOverlayPanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(4f);
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width - 150f, 24f), "FFF_OverlayTitle".Translate());

            // 叠加层列切换
            float btnW = 45f;
            float btnX = inner.xMax - 145f;
            if (Widgets.ButtonText(new Rect(btnX, inner.y, btnW, 22f), "FFF_2Columns".Translate())) savedOverlayScale = 0;
            if (Widgets.ButtonText(new Rect(btnX + 48f, inner.y, btnW, 22f), "FFF_4Columns".Translate())) savedOverlayScale = 1;
            if (Widgets.ButtonText(new Rect(btnX + 96f, inner.y, btnW, 22f), "FFF_6Columns".Translate())) savedOverlayScale = 2;

            GUI.color = new Color(0.4f, 0.9f, 0.4f);
            Widgets.DrawBox(new Rect(btnX + savedOverlayScale * 48f, inner.y, btnW, 22f), 2);
            GUI.color = Color.white;

            var overlays = comp.Props.availableOverlays;
            int totalItems = 1 + overlays.Count;
            // 计算列数
            int cols = savedOverlayScale == 0 ? 2 : savedOverlayScale == 1 ? 4 : 6;
            float padding = 4f;
            float availW = inner.width - 16f;
            float cellSize = (availW - padding * (cols + 1)) / cols;
            float viewHeight = Mathf.CeilToInt((float)totalItems / cols) * (cellSize + padding) + padding;

            Rect scrollRect = new Rect(inner.x, inner.y + 28f, inner.width, inner.height - 28f);
            Rect viewRect = new Rect(0, 0, inner.width - 16f, viewHeight);
            Widgets.BeginScrollView(scrollRect, ref overlayScrollPos, viewRect);

            for (int i = 0; i < totalItems; i++)
            {
                bool isNone = i == 0;
                FFF_OverlayDef def = isNone ? null : overlays[i - 1];
                bool selected = isNone ? overlayDef == null : overlayDef == def;

                int col = i % cols;
                int row = i / cols;
                float cx = col * (cellSize + padding) + padding;
                float cy = row * (cellSize + padding) + padding;
                Rect cell = new Rect(cx, cy, cellSize, cellSize);

                Widgets.DrawBoxSolid(cell, new Color(0.12f, 0.12f, 0.12f));

                // 绘制预览
                Texture2D tex = isNone ? overlayNonePreviewTex
                    : (overlayPreviewCache.TryGetValue(def, out var t) ? t : null);
                if (tex != null)
                    GUI.DrawTexture(cell, tex, ScaleMode.ScaleToFit);

                if (selected)
                {
                    GUI.color = new Color(0.4f, 0.9f, 0.4f);
                    Widgets.DrawBox(cell, 2);
                    GUI.color = Color.white;
                }
                else
                    Widgets.DrawBox(cell, 1);

                Text.Font = GameFont.Tiny;
                string lbl = isNone ? "FFF_OverlayNone".Translate() : (def.label ?? def.defName);
                Widgets.Label(new Rect(cell.x, cell.yMax - 18f, cell.width, 18f), lbl);
                Text.Font = GameFont.Small;

                if (Widgets.ButtonInvisible(cell))
                {
                    overlayDef = def;
                    // 标记刷新
                    bakeImmediate = true;
                    bakeRequestTime = 0f;
                }

                if (Mouse.IsOver(cell))
                {
                    Widgets.DrawHighlight(cell);
                    string tip = isNone ? "FFF_OverlayNone".Translate()
                        : (def.label ?? def.defName) + (def.multiplyBase ? "FFF_OverlayMultiply".Translate() : "FFF_OverlayDirect".Translate());
                    TooltipHandler.TipRegion(cell, tip);
                }
            }

            Widgets.EndScrollView();
        }

        // 绘制预览区域
        private void DrawPreview(ref float y, Rect inRect, float leftW)
        {
            float previewSize = 400f;
            Rect previewArea = new Rect(inRect.x, y, previewSize, previewSize);
            Widgets.DrawBoxSolid(previewArea, new Color(0.1f, 0.1f, 0.1f));
            Widgets.DrawBox(previewArea);

            // 绘制副本
            if (previewPawn != null)
            {
                var img = PortraitsCache.Get(previewPawn, new Vector2(previewSize, previewSize),
                    new Rot4(previewRot), default, previewZoom, true, true);
                GUI.DrawTexture(previewArea, img);
            }

            DrawPreviewControls(previewArea);

            // 缩放滑条
            Rect scrollRect = new Rect(previewArea.xMax + 6f, y, 20f, previewSize);
            previewZoom = GUI.VerticalSlider(scrollRect, previewZoom, 3.0f, 0.2f);
            y = previewArea.yMax;
        }

        // 绘制旋转按钮
        private void DrawPreviewControls(Rect area)
        {
            if (Widgets.ButtonText(new Rect(area.x + 4, area.yMax - 28, 40, 24), "◀"))
            {
                previewRot = (previewRot + 3) % 4;
                PortraitsCache.SetDirty(previewPawn);
            }
            if (Widgets.ButtonText(new Rect(area.xMax - 44, area.yMax - 28, 40, 24), "▶"))
            {
                previewRot = (previewRot + 1) % 4;
                PortraitsCache.SetDirty(previewPawn);
            }
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawColorSliders(ref float y, Rect inRect)
        {
            DrawColorGroupHeader(ref y, inRect, "FFF_Color1".Translate(), baseColor);
            DrawSlider(ref y, inRect, "FFF_Red".Translate(), ref baseColor.r, "c1r");
            DrawSlider(ref y, inRect, "FFF_Green".Translate(), ref baseColor.g, "c1g");
            DrawSlider(ref y, inRect, "FFF_Blue".Translate(), ref baseColor.b, "c1b");
            y += 10f;

            if (camoDef != null)
            {
                DrawColorGroupHeader(ref y, inRect, "FFF_Color2".Translate(), baseColor2);
                DrawSlider(ref y, inRect, "FFF_Red".Translate(), ref baseColor2.r, "c2r");
                DrawSlider(ref y, inRect, "FFF_Green".Translate(), ref baseColor2.g, "c2g");
                DrawSlider(ref y, inRect, "FFF_Blue".Translate(), ref baseColor2.b, "c2b");
                y += 10f;

                DrawColorGroupHeader(ref y, inRect, "FFF_Color3".Translate(), baseColor3);
                DrawSlider(ref y, inRect, "FFF_Red".Translate(), ref baseColor3.r, "c3r");
                DrawSlider(ref y, inRect, "FFF_Green".Translate(), ref baseColor3.g, "c3g");
                DrawSlider(ref y, inRect, "FFF_Blue".Translate(), ref baseColor3.b, "c3b");
                y += 10f;
            }
        }

        // 绘制颜色组标题
        private void DrawColorGroupHeader(ref float y, Rect inRect, string label, Color color)
        {
            float swatch = 20f;
            Widgets.Label(new Rect(inRect.x, y, inRect.width - swatch - 6f, 24f), label);
            Rect swatchRect = new Rect(inRect.xMax - swatch, y + 2f, swatch, 20f);
            Widgets.DrawBoxSolid(swatchRect, color);
            Widgets.DrawBox(swatchRect);
            y += 24f;
        }

        // 绘制杂项滑条
        private void DrawMiscSliders(ref float y, Rect inRect)
        {
            Widgets.Label(new Rect(inRect.x, y, 60f, 24f), "FFF_BaseBrightness".Translate());
            brightness = Widgets.HorizontalSlider(new Rect(inRect.x + 64f, y, inRect.width - 140f, 24f), brightness, 0f, 1f);
            Widgets.Label(new Rect(inRect.width - 70f, y, 60f, 24f), (brightness * 100f).ToString("F0") + "%");
            y += 36f;
        }


        // 绘制列表滑条
        private void DrawSlider(ref float y, Rect inRect, string label, ref float val, string id)
        {
            Rect lblRect = new Rect(inRect.x, y, 30f, 24f);
            Widgets.Label(lblRect, label);

            // 预留右侧输入框空间
            float inputW = 50f;
            Rect sliderRect = new Rect(inRect.x + 34f, y, inRect.width - 100f - inputW, 24f);

            float oldVal = val;
            val = Widgets.HorizontalSlider(sliderRect, val, 0f, 1f);

            // 255 进制处理
            int intVal = Mathf.RoundToInt(val * 255f);
            if (!textBuffers.TryGetValue(id, out string buffer) || oldVal != val)
            {
                buffer = intVal.ToString();
                textBuffers[id] = buffer;
            }

            Rect inputRect = new Rect(inRect.xMax - inputW, y, inputW, 24f);
            string oldBuffer = buffer;
            Widgets.TextFieldNumeric(inputRect, ref intVal, ref buffer, 0, 255);
            textBuffers[id] = buffer;

            // 如果文本输入有变，同步回 val
            if (buffer != oldBuffer)
            {
                val = intVal / 255f;
            }

            y += 28f;
        }

        // 发送涂装请求
        private void ApplyPaint()
        {
            confirmed = true;

            // 设置请求状态
            comp.activePaintRequest = true;
            comp.requestColor = baseColor;
            comp.requestColor2 = baseColor2;
            comp.requestColor3 = baseColor3;
            comp.requestCamo = camoDef;
            comp.requestBrightness = brightness;
            comp.requestOverlay = overlayDef;


            // 通知刷新
            comp.Notify_ColorChanged();

            // 弹出提示
            Messages.Message("FFF_PaintRequestSent".Translate(), comp.parent, MessageTypeDefOf.NeutralEvent, false);
        }

        private bool confirmed = false;

        public override void Close(bool doCloseSound = true)
        {
            base.Close(doCloseSound);

            if (!confirmed)
            {
                // 无需回滚 pending
            }

            // 刷新渲染
            pawn.Drawer.renderer.SetAllGraphicsDirty();

            // 销毁副本
            if (previewPawn != null)
            {
                try { previewPawn.Destroy(); } catch { }
                previewPawn = null;
                previewComp = null;
            }
            foreach (var t in camoPreviewCache.Values)
                if (t != null) UnityEngine.Object.Destroy(t);
            camoPreviewCache.Clear();

            foreach (var t in overlayPreviewCache.Values)
                if (t != null) UnityEngine.Object.Destroy(t);
            overlayPreviewCache.Clear();

            if (nonePreviewTex != null) UnityEngine.Object.Destroy(nonePreviewTex);
            nonePreviewTex = null;
            if (overlayNonePreviewTex != null && overlayNonePreviewTex != nonePreviewTex)
                UnityEngine.Object.Destroy(overlayNonePreviewTex);
            overlayNonePreviewTex = null;
        }
    }
}
