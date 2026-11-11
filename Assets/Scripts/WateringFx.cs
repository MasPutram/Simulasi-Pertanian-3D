using UnityEngine;
using System.Collections;

// Animasi menyiram: gembor muncul di depan player, dimiringkan, air menetes dari corong
public class WateringFx : MonoBehaviour
{
    public const string ModelName = "tool_wateringcan";
    const float TargetSize = 1.0f;

    static WateringFx runner;
    static Material waterMat;
    bool busy;

    public static void Play(Vector3 plotPos)
    {
        if (!Application.isPlaying) return; // jangan sampai tersimpan ke scene saat di Editor
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        if (runner == null) runner = new GameObject("WateringFx").AddComponent<WateringFx>();
        if (runner.busy) return;
        runner.StartCoroutine(runner.Run(player.transform, plotPos));
    }

    public static GameObject FindModel() => HandItem.FindSceneObject(ModelName);

    IEnumerator Run(Transform player, Vector3 plotPos)
    {
        busy = true;
        GameObject model = FindModel();

        GameObject can;
        if (model != null)
        {
            can = Instantiate(model);
            can.SetActive(true);
            foreach (var col in can.GetComponentsInChildren<Collider>()) Destroy(col);
        }
        else
        {
            can = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(can.GetComponent<Collider>());
            can.GetComponent<Renderer>().material.color = new Color(0.24f, 0.62f, 0.56f);
        }
        can.name = "WateringCan_Active";

        // Skala supaya ukurannya pas dengan karakter
        Renderer[] rs = can.GetComponentsInChildren<Renderer>();
        if (rs.Length > 0)
        {
            Bounds b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            float max = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (max > 0) can.transform.localScale *= TargetSize / max;
        }

        // Gembor di samping tanaman (sisi kanan dari arah player) dengan corong mengarah ke tanaman,
        // jadi dari kamera di belakang player bentuknya terlihat dari samping
        Vector3 toPlot = plotPos - player.position;
        toPlot.y = 0;
        if (toPlot.sqrMagnitude < 0.01f) toPlot = player.forward;
        Vector3 dir = toPlot.normalized;
        Vector3 right = Vector3.Cross(Vector3.up, dir);
        Quaternion face = Quaternion.LookRotation(-right) * Quaternion.Euler(0, -90, 0);
        Vector3 holdPos = new Vector3(plotPos.x, 0f, plotPos.z) + right * 1.0f + Vector3.up * 1.35f;

        Transform held = FindHeld(player);
        if (held != null) held.gameObject.SetActive(false);
        Transform tip = can.transform.Find("SpoutTip");
        ParticleSystem water = CreateWater(tip != null ? tip : can.transform);

        AudioManager.Instance?.PlayWater();

        float t = 0f;
        const float raise = 0.2f, pour = 1.1f, lower = 0.2f;
        while (t < raise + pour + lower)
        {
            t += Time.deltaTime;
            float tilt;
            if (t < raise) tilt = Mathf.Lerp(0f, 45f, t / raise);
            else if (t < raise + pour) tilt = 45f + Mathf.Sin((t - raise) * 9f) * 4f;
            else tilt = Mathf.Lerp(45f, 0f, (t - raise - pour) / lower);

            if (t >= raise && !water.isPlaying && t < raise + pour) water.Play();
            if (t >= raise + pour && water.isPlaying) water.Stop();

            can.transform.position = holdPos + Vector3.up * Mathf.Sin(Mathf.Clamp01(t / raise) * Mathf.PI * 0.5f) * 0.15f;
            can.transform.rotation = face * Quaternion.Euler(0, 0, -tilt);
            yield return null;
        }

        // Biarkan tetesan terakhir jatuh sebelum dihapus
        water.transform.SetParent(null, true);
        Destroy(water.gameObject, 1f);
        Destroy(can);
        if (held != null) held.gameObject.SetActive(true);
        busy = false;
    }

    static Transform FindHeld(Transform root)
    {
        foreach (Transform c in root)
        {
            if (c.name == "HeldItem") return c;
            Transform f = FindHeld(c);
            if (f != null) return f;
        }
        return null;
    }

    static ParticleSystem CreateWater(Transform parent)
    {
        GameObject go = new GameObject("WaterDrops");
        go.transform.SetParent(parent, false);
        // Semprotan mengikuti arah corong (+X lokal)
        go.transform.localRotation = Quaternion.Euler(0, 90, 0);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.startLifetime = 0.7f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.09f, 0.15f);
        main.startColor = new Color(0.35f, 0.7f, 1f, 1f);
        main.gravityModifier = 1.4f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.maxParticles = 300;

        var emission = ps.emission;
        emission.rateOverTime = 120f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.radius = 0.03f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 2.5f;
        if (waterMat == null)
        {
            Shader sh = Shader.Find("Particles/Standard Unlit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            waterMat = new Material(sh);
            waterMat.color = new Color(0.45f, 0.75f, 1f, 1f);
        }
        renderer.material = waterMat;
        return ps;
    }
}
