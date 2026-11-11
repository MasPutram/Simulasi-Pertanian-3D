using UnityEngine;

// Animasi sederhana karakter blocky:
// - jalan: lengan & kaki berayun
// - pegang benih: kedua tangan ke depan dada
// - pegang buah: kedua lengan terangkat ke atas kepala
public class PlayerAnimator : MonoBehaviour
{
    public float swingAngle = 32f;
    public float swingSpeed = 10f;
    [Tooltip("Sudut lengan ke depan saat pegang benih (90 = lurus horizontal)")]
    public float forwardAngle = 75f;
    [Tooltip("Sudut lengan menyerong ke dalam saat pegang benih")]
    public float inwardAngle = 14f;

    // Posisi pusat lengan/kaki saat diam (kubus, pivot di tengah)
    static readonly Vector3 ArmDownL = new Vector3(-0.55f, 0.5f, 0f);
    static readonly Vector3 ArmDownR = new Vector3(0.55f, 0.5f, 0f);
    static readonly Vector3 ArmUpL = new Vector3(-0.5f, 1.32f, 0f);
    static readonly Vector3 ArmUpR = new Vector3(0.5f, 1.32f, 0f);
    static readonly Vector3 LegL = new Vector3(-0.2f, -0.5f, 0f);
    static readonly Vector3 LegR = new Vector3(0.2f, -0.5f, 0f);
    const float ShoulderY = 1.0f; // titik putar lengan
    const float HipY = 0f;        // titik putar kaki
    const float ArmHalf = 0.5f;   // setengah panjang lengan

    // Diisi HandItem: posisi dasar barang yang dipegang (lokal player)
    public static Vector3 heldBasePos;

    Transform armL, armR, legL, legR, held;
    float raise;    // 0..1 pose angkat ke atas (buah)
    float forward;  // 0..1 pose tangan ke depan (benih)
    float walkPhase;
    float walkAmount;
    Vector3 lastPos;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoAttach()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null && p.GetComponent<PlayerAnimator>() == null) p.AddComponent<PlayerAnimator>();
    }

    void Start()
    {
        armL = transform.Find("ArmL");
        armR = transform.Find("ArmR");
        legL = transform.Find("LegL");
        legR = transform.Find("LegR");
        lastPos = transform.position;
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        Vector3 delta = transform.position - lastPos;
        delta.y = 0;
        lastPos = transform.position;
        bool moving = delta.magnitude / dt > 0.5f;

        walkAmount = Mathf.MoveTowards(walkAmount, moving ? 1f : 0f, dt * 6f);
        if (moving) walkPhase += dt * swingSpeed;
        float swing = Mathf.Sin(walkPhase) * swingAngle * walkAmount;

        GameManager gm = GameManager.Instance;
        bool holdingFruit = gm != null && gm.heldFruit != null;
        bool holdingSeed = gm != null && gm.selectedPlant != null;
        raise = Mathf.MoveTowards(raise, holdingFruit ? 1f : 0f, dt * 5f);
        forward = Mathf.MoveTowards(forward, holdingSeed ? 1f : 0f, dt * 6f);

        PoseArm(armL, ArmDownL, ArmUpL, swing, -1f);
        PoseArm(armR, ArmDownR, ArmUpR, -swing, 1f);

        PoseLimb(legL, LegL, HipY, -swing);
        PoseLimb(legR, LegR, HipY, swing);

        // Barang yang dipegang ikut naik-turun sedikit saat jalan
        if (held == null || held.parent != transform) held = transform.Find("HeldItem");
        if (held != null && held.parent == transform)
        {
            float bob = Mathf.Abs(Mathf.Sin(walkPhase)) * 0.05f * walkAmount;
            held.localPosition = heldBasePos + Vector3.up * bob;
        }
    }

    // side: -1 = kiri, 1 = kanan
    void PoseArm(Transform arm, Vector3 down, Vector3 up, float swing, float side)
    {
        if (arm == null) return;
        float r = Smooth(raise), f = Smooth(forward);
        Vector3 shoulder = new Vector3(down.x, ShoulderY, down.z);
        Vector3 hang = Vector3.down * ArmHalf;

        // 1) turun + ayunan jalan
        Quaternion swingRot = Quaternion.Euler(swing, 0, 0);
        Vector3 pos = shoulder + swingRot * hang;
        Quaternion rot = swingRot;

        // 2) tangan ke depan, menyerong ke dalam (pegang benih)
        if (f > 0f)
        {
            Vector3 fShoulder = new Vector3(down.x * 0.9f, ShoulderY, 0f);
            Quaternion fRot = Quaternion.Euler(0, -side * inwardAngle, 0) * Quaternion.Euler(-forwardAngle + swing * 0.1f, 0, 0);
            pos = Vector3.Lerp(pos, fShoulder + fRot * hang, f);
            rot = Quaternion.Slerp(rot, fRot, f);
        }

        // 3) lurus ke atas kepala (pegang buah)
        if (r > 0f)
        {
            pos = Vector3.Lerp(pos, up, r);
            rot = Quaternion.Slerp(rot, Quaternion.Euler(swing * 0.15f, 0, 0), r);
        }

        arm.localPosition = pos;
        arm.localRotation = rot;
    }

    static float Smooth(float t) => t * t * (3f - 2f * t);

    static void PoseLimb(Transform limb, Vector3 rest, float pivotY, float angle)
    {
        if (limb == null) return;
        Vector3 pivot = new Vector3(rest.x, pivotY, rest.z);
        Quaternion rot = Quaternion.Euler(angle, 0, 0);
        limb.localPosition = pivot + rot * (rest - pivot);
        limb.localRotation = rot;
    }
}
