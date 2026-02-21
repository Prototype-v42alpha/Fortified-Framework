using RimWorld;
using UnityEngine;
using Verse;

namespace Fortified;

public static class FleckMakerEx
{
    // 建立可控制方向的 FleckCreationData
    public static FleckCreationData GetDataStaticDirected(
        Vector3 loc,
        Map map,
        FleckDef fleckDef,
        float floatAngle,          // 方向角度（度數）
        float speed = 0.25f,       // 移動速度
        float scale = 1f,
        float rotationRate = 0f)   // 自身旋轉速率
    {
        return new FleckCreationData
        {
            def = fleckDef,
            spawnPosition = loc,
            scale = scale,
            ageTicksOverride = -1,

            // 方向控制關鍵
            velocityAngle = floatAngle,
            velocitySpeed = speed,

            // 視覺旋轉（非移動方向）
            rotationRate = rotationRate
        };
    }

    // 直接生成（Vector3 版）
    public static void StaticDirected(
        Vector3 loc,
        Map map,
        FleckDef fleckDef,
        float floatAngle,
        float speed = 0.25f,
        float scale = 1f,
        float rotationRate = 0f)
    {
        if (map == null) return;
        if (!loc.ShouldSpawnMotesAt(map)) return;

        var data = GetDataStaticDirected(loc, map, fleckDef, floatAngle, speed, scale, rotationRate);
        map.flecks.CreateFleck(data);
    }

    public static void StaticDirected(
        IntVec3 cell,
        Map map,
        FleckDef fleckDef,
        float floatAngle,
        float speed = 0.25f,
        float scale = 1f,
        float rotationRate = 0f)
    {
        StaticDirected(cell.ToVector3Shifted(), map, fleckDef, floatAngle, speed, scale, rotationRate);
    }

    public static void ThrowAfterburnerLaunchSmoke(Vector3 drawPos, float angle, Map map, FleckDef smokeFleck, FleckDef exhaustFleck)
    {
        if (map == null) return;
        if (!drawPos.ShouldSpawnMotesAt(map)) return;

        float rawAngle = angle;
        for (int i = 0; i < 6; i++)
        {
            float variation = Rand.Range(-30, 30);
            if (smokeFleck != null)
            {
                FleckCreationData dataStatic = FleckMaker.GetDataStatic(drawPos, map, smokeFleck);
                dataStatic.spawnPosition = drawPos + CircleConst.GetAngle(rawAngle + variation) * Rand.Range(1.2f, 2.5f);
                dataStatic.orbitSnapStrength = 0.08f;
                dataStatic.scale = Rand.Range(0.6f, 3f);
                dataStatic.rotationRate = Rand.Range(-8, 8);
                dataStatic.velocityAngle = rawAngle + variation;
                dataStatic.velocitySpeed = Mathf.Clamp((3.5f - dataStatic.scale) * 1.2f, 0.5f, 4f);
                dataStatic.targetSize = 4f;
                map.flecks.CreateFleck(dataStatic);
            }
            if (exhaustFleck != null)
            {
                FleckCreationData dataStatic = FleckMaker.GetDataStatic(drawPos, map, exhaustFleck);
                dataStatic.spawnPosition = drawPos + CircleConst.GetAngle(rawAngle) * 1.8f;
                dataStatic.scale = Rand.Range(0.8f, 2f);
                dataStatic.rotationRate = Rand.Range(-8, 8);
                dataStatic.velocityAngle = rawAngle + Rand.Range(-6, 6);
                dataStatic.velocitySpeed = (4.5f - dataStatic.scale) * 2.5f;
                dataStatic.targetSize = 0f;
                map.flecks.CreateFleck(dataStatic);
            }
        }
    }

    public static Vector3 ThrowAfterburnerExhaust(
        Vector3 drawPos,
        float evaluate,
        Map map,
        FleckDef exhaustFleck,
        FleckDef smokeFleck,
        SimpleCurve exhaustCurve,
        SimpleCurve smokeCurve,
        float projectileSpeedTilesPerTick,
        Vector3 postPosition)
    {
        if (map == null) return postPosition;
        if (!drawPos.ShouldSpawnMotesAt(map)) return postPosition;

        // 使用前一幀位置計算方向角（更準確的方向控制）
        Vector3 dir = drawPos - postPosition;
        float baseAngle = 0f;
        if (dir != Vector3.zero)
            baseAngle = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;

        for (int i = 0; i < 3; i++)
        {
            if (exhaustFleck != null)
            {
                FleckCreationData dataStatic = FleckMaker.GetDataStatic(drawPos, map, exhaustFleck);
                float curveVal = (exhaustCurve != null) ? exhaustCurve.Evaluate(evaluate) : 0f;
                dataStatic.scale = Rand.Range(Mathf.Min(0.1f, curveVal), curveVal);
                dataStatic.rotationRate = Rand.Range(-40, 40);
                dataStatic.velocityAngle = baseAngle + Rand.Range(-12, 12);
                dataStatic.velocitySpeed = Mathf.Max(0.2f, dataStatic.scale * projectileSpeedTilesPerTick * Rand.Range(0.8f, 1.6f));
                dataStatic.solidTimeOverride = 0.18f * (1f - (evaluate + 0.1f));
                map.flecks.CreateFleck(dataStatic);
            }
            if (smokeFleck != null)
            {
                FleckCreationData dataStatic = FleckMaker.GetDataStatic(drawPos, map, smokeFleck);
                float curveVal = (smokeCurve != null) ? smokeCurve.Evaluate(evaluate) : 0f;
                dataStatic.scale = Rand.Range(0f, curveVal) * Rand.Range(0.85f, 1.15f);
                dataStatic.rotationRate = Rand.Range(-25, 25);
                dataStatic.velocityAngle = baseAngle + Rand.Range(-35, 35);
                dataStatic.velocitySpeed = Mathf.Clamp01(1 - dataStatic.scale) * Rand.Range(0.2f, 1.2f);
                map.flecks.CreateFleck(dataStatic);
            }
        }
        postPosition = drawPos;
        return postPosition;
    }

    // Variant_Original: 保留原本的行為（作為參考實作）
    public static void ThrowAfterburnerLaunchSmoke_Variant_Original(Vector3 drawPos, float angle, Map map, FleckDef smokeFleck, FleckDef exhaustFleck)
    {
        // 直接復刻原先的 launch-smoke 行為
        ThrowAfterburnerLaunchSmoke(drawPos, angle, map, smokeFleck, exhaustFleck);
    }

    // Variant_Directional: 更強調朝向性與尾跡感（長速率、較小散佈）
    public static void ThrowAfterburnerLaunchSmoke_Variant_Directional(Vector3 drawPos, float angle, Map map, FleckDef smokeFleck, FleckDef exhaustFleck)
    {
        if (map == null) return;
        if (!drawPos.ShouldSpawnMotesAt(map)) return;

        float rawAngle = angle;
        for (int i = 0; i < 6; i++)
        {
            float variation = Rand.Range(-20, 20);
            if (exhaustFleck != null)
            {
                FleckCreationData data = FleckMaker.GetDataStatic(drawPos, map, exhaustFleck);
                data.spawnPosition = drawPos + CircleConst.GetAngle(rawAngle) * 1.5f + CircleConst.GetAngle(rawAngle + variation) * 0.5f;
                data.scale = Rand.Range(1f, 2f);
                data.rotationRate = Rand.Range(-10, 10);
                data.velocityAngle = rawAngle + variation * 0.25f;
                data.velocitySpeed = Rand.Range(4f, 8f); // 更快，形成尾跡
                data.targetSize = 0f;
                map.flecks.CreateFleck(data);
            }
            if (smokeFleck != null)
            {
                FleckCreationData data = FleckMaker.GetDataStatic(drawPos, map, smokeFleck);
                data.spawnPosition = drawPos + CircleConst.GetAngle(rawAngle + variation) * 2f;
                data.scale = Rand.Range(0.6f, 2.5f);
                data.rotationRate = Rand.Range(-6, 6);
                data.velocityAngle = rawAngle + variation;
                data.velocitySpeed = Rand.Range(1f, 3f);
                data.targetSize = 3f;
                map.flecks.CreateFleck(data);
            }
        }
    }

    // Variant_Burst: 發生大量較大且慢速的煙霧彈（較強的瞬間視覺）
    public static void ThrowAfterburnerLaunchSmoke_Variant_Burst(Vector3 drawPos, float angle, Map map, FleckDef smokeFleck, FleckDef exhaustFleck)
    {
        if (map == null) return;
        if (!drawPos.ShouldSpawnMotesAt(map)) return;

        float rawAngle = angle;
        // 大爆發：更多粒子但速度較慢
        for (int i = 0; i < 12; i++)
        {
            float variation = Rand.Range(-120, 120);
            if (smokeFleck != null)
            {
                FleckCreationData data = FleckMaker.GetDataStatic(drawPos, map, smokeFleck);
                data.spawnPosition = drawPos + CircleConst.GetAngle(rawAngle + variation) * Rand.Range(0.5f, 3f);
                data.scale = Rand.Range(1f, 5f);
                data.rotationRate = Rand.Range(-20, 20);
                data.velocityAngle = rawAngle + variation;
                data.velocitySpeed = Rand.Range(0.2f, 1.5f);
                data.targetSize = 6f;
                map.flecks.CreateFleck(data);
            }
            if (exhaustFleck != null && Rand.Chance(0.3f))
            {
                FleckCreationData data = FleckMaker.GetDataStatic(drawPos, map, exhaustFleck);
                data.spawnPosition = drawPos + CircleConst.GetAngle(rawAngle) * Rand.Range(0.5f, 2f);
                data.scale = Rand.Range(2f, 4f);
                data.velocityAngle = rawAngle + Rand.Range(-10, 10);
                data.velocitySpeed = Rand.Range(0.5f, 2f);
                data.targetSize = 0f;
                map.flecks.CreateFleck(data);
            }
        }
    }

    // --- Exhaust variants ---

    // Variant_Original: 恢復原本的逐幀產生邏輯
    public static Vector3 ThrowAfterburnerExhaust_Variant_Original(
        Vector3 drawPos,
        float evaluate,
        Map map,
        FleckDef exhaustFleck,
        FleckDef smokeFleck,
        SimpleCurve exhaustCurve,
        SimpleCurve smokeCurve,
        float projectileSpeedTilesPerTick,
        Vector3 postPosition)
    {
        return ThrowAfterburnerExhaust(drawPos, evaluate, map, exhaustFleck, smokeFleck, exhaustCurve, smokeCurve, projectileSpeedTilesPerTick, postPosition);
    }

    // Variant_Directional: 依據前一幀位置強化方向性與長尾巴（適合高速子彈）
    public static Vector3 ThrowAfterburnerExhaust_Variant_Directional(
        Vector3 drawPos,
        float evaluate,
        Map map,
        FleckDef exhaustFleck,
        FleckDef smokeFleck,
        SimpleCurve exhaustCurve,
        SimpleCurve smokeCurve,
        float projectileSpeedTilesPerTick,
        Vector3 postPosition)
    {
        if (map == null) return postPosition;
        if (!drawPos.ShouldSpawnMotesAt(map)) return postPosition;

        Vector3 dirVec = drawPos - postPosition;
        float baseAngle = 0f;
        if (dirVec != Vector3.zero)
        {
            baseAngle = Mathf.Atan2(dirVec.z, dirVec.x) * Mathf.Rad2Deg;
        }

        for (int i = 0; i < 4; i++)
        {
            if (exhaustFleck != null)
            {
                FleckCreationData data = FleckMaker.GetDataStatic(drawPos, map, exhaustFleck);
                data.scale = Rand.Range(0, (exhaustCurve != null) ? exhaustCurve.Evaluate(evaluate) : 0f);
                data.rotationRate = Rand.Range(-30, 30);
                data.velocityAngle = baseAngle + Rand.Range(-8, 8);
                data.velocitySpeed = data.scale * (projectileSpeedTilesPerTick + Rand.Range(0.4f, 1.6f));
                data.solidTimeOverride = 0.09f * (1f - (evaluate + 0.1f));
                map.flecks.CreateFleck(data);
            }
            if (smokeFleck != null)
            {
                FleckCreationData data = FleckMaker.GetDataStatic(drawPos, map, smokeFleck);
                data.scale = Rand.Range(0, (smokeCurve != null) ? smokeCurve.Evaluate(evaluate) : 0f) * Rand.Range(0.85f, 1.15f);
                data.rotationRate = Rand.Range(-18, 18);
                data.velocityAngle = baseAngle + Rand.Range(-30, 30);
                data.velocitySpeed = Mathf.Clamp01(1 - data.scale) * Rand.Range(0.18f, 1.2f);
                map.flecks.CreateFleck(data);
            }
        }
        return drawPos;
    }

    // Variant_Burst: 以小量但較大且慢速的煙霧呈現（適合低速、厚重燃燒感）
    public static Vector3 ThrowAfterburnerExhaust_Variant_Burst(
        Vector3 drawPos,
        float evaluate,
        Map map,
        FleckDef exhaustFleck,
        FleckDef smokeFleck,
        SimpleCurve exhaustCurve,
        SimpleCurve smokeCurve,
        float projectileSpeedTilesPerTick,
        Vector3 postPosition)
    {
        if (map == null) return postPosition;
        if (!drawPos.ShouldSpawnMotesAt(map)) return postPosition;

        // 更少的迴圈但更顯著的粒子
        for (int i = 0; i < 2; i++)
        {
            if (exhaustFleck != null)
            {
                FleckCreationData data = FleckMaker.GetDataStatic(drawPos, map, exhaustFleck);
                data.scale = Rand.Range(0.8f, (exhaustCurve != null) ? exhaustCurve.Evaluate(evaluate) * 1.4f : 1f);
                data.rotationRate = Rand.Range(-24, 24);
                data.velocityAngle = Rand.Range(-45, 45);
                data.velocitySpeed = Rand.Range(0.08f, 0.6f) * projectileSpeedTilesPerTick;
                data.solidTimeOverride = 0.38f * (1f - evaluate);
                data.targetSize = Rand.Range(2f, 5f);
                map.flecks.CreateFleck(data);
            }
            if (smokeFleck != null)
            {
                float randAng = Rand.Range(-140f, 140f);
                float randDist = Rand.Range(0f, 0.25f);
                FleckCreationData data = FleckMaker.GetDataStatic(drawPos + CircleConst.GetAngle(randAng) * randDist, map, smokeFleck);
                data.scale = Rand.Range(0.6f, (smokeCurve != null) ? smokeCurve.Evaluate(evaluate) * 1.4f : 1f);
                data.rotationRate = Rand.Range(-8, 8);
                data.velocityAngle = Rand.Range(-160, 160);
                data.velocitySpeed = Mathf.Clamp01(1 - data.scale) * Rand.Range(0f, 0.55f);
                data.targetSize = Rand.Range(3f, 7f);
                map.flecks.CreateFleck(data);
            }
        }
        return drawPos;
    }
    public static float ThrowHitDeflectSpark(
    Vector3 pos,
    float angle,                 // 新增：來自彈頭/衝擊的基準角度（度數）
    Map map,
    FleckDef deflectFleck,
    FleckDef sparkFleck,
    int sparkCount = 10,
    float deflectAngleRange = 90f,      // 最大偏轉範圍（度數），實際偏轉為 ±deflectAngleRange/2
    float minSparkSpeed = 0.5f,
    float maxSparkSpeed = 3f,
    float minScale = 0.5f,
    float maxScale = 1.8f)
    {
        if (map == null) return 0f;
        if (!pos.ShouldSpawnMotesAt(map)) return 0f;
        if (sparkFleck == null && deflectFleck == null) return 0f;

        float halfRange = deflectAngleRange * 0.5f;
        // 基於傳入角度產生偏轉角
        float deflectAngle = angle + Rand.Range(-halfRange, halfRange);

        // 生成代表偏轉彈頭本體的 fleck（單一）
        if (deflectFleck != null)
        {
            Vector3 deflectSpawn = pos + CircleConst.GetAngle(deflectAngle) * 0.12f;
            FleckCreationData deflectData = FleckMaker.GetDataStatic(deflectSpawn, map, deflectFleck);
            deflectData.scale = (minScale + maxScale) * 0.5f;
            deflectData.rotationRate = Rand.Range(-180f, 180f);
            deflectData.velocityAngle = deflectAngle;
            deflectData.velocitySpeed = (minSparkSpeed + maxSparkSpeed) * 10f;
            deflectData.solidTimeOverride = 0.15f;
            deflectData.targetSize = 0f;
            map.flecks.CreateFleck(deflectData);
        }

        // 產生火花
        if (sparkFleck != null)
        {
            for (int i = 0; i < sparkCount; i++)
            {
                float ang = Rand.Range(-180f, 180f);
                float dist = Rand.Range(0.05f, 0.6f);
                Vector3 spawnPos = pos + CircleConst.GetAngle(ang) * dist;

                FleckCreationData data = FleckMaker.GetDataStatic(spawnPos, map, sparkFleck);
                data.scale = Rand.Range(minScale, maxScale);
                data.rotationRate = Rand.Range(-360f, 360f);
                data.velocityAngle = ang;
                data.velocitySpeed = Rand.Range(minSparkSpeed, maxSparkSpeed);
                data.solidTimeOverride = 0.05f;
                data.targetSize = 0f;
                map.flecks.CreateFleck(data);
            }
        }

        return deflectAngle;
    }
}