using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PuckStopperDrag : MonoBehaviour
{
    [Header("Basics")]
    public Camera cam;                 // If null, uses Camera.main
    public float planeY = 0f;          // Table height
    public bool smooth = true;         // Lerp smoothing (same feel as before)
    [Range(1f, 60f)] public float followSpeed = 25f; // Higher = snappier

    [Header("Optional Clamp (table bounds)")]
    public bool clamp = false;
    public Vector2 minXZ = new Vector2(-5f, -9f);
    public Vector2 maxXZ = new Vector2(5f, 0f);

    private float minX = -2f, maxX = 0f;
    private float minZ = -1f, maxZ = 1f;

    Rigidbody rb;
    Plane plane;
    bool dragging;
    Vector3 targetPos;

    private float restitution = 0.9f;      // e
    private float carry = 0.25f;           // k
    private float minHitSpeed = 0.15f;     // ignore tiny touches
    private float hitCooldown = 0.06f;

    float nextAllowedHitTime = 0f;

    private GameLiftClient gameLiftClient;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));

        rb = GetComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;

        gameLiftClient = FindObjectOfType<GameLiftClient>();

    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) dragging = true;
        if (Input.GetMouseButtonUp(0)) dragging = false;

        var ray = cam.ScreenPointToRay(Input.mousePosition);
        if (plane.Raycast(ray, out float enter))
        {
            var p = ray.GetPoint(enter);

            p.y = planeY;

            targetPos = p;
        }
    }

    void FixedUpdate()
    {
        if (!dragging) return;

        // Same smoothing feel as your transform-based version, but physics-friendly

        targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
        targetPos.z = Mathf.Clamp(targetPos.z, minZ, maxZ);



        Vector3 next = smooth
            ? Vector3.Lerp(rb.position, targetPos, 1f - Mathf.Exp(-followSpeed * Time.fixedDeltaTime))
            : targetPos;

        next.y = planeY;

        // IMPORTANT: never transform.position — always MovePosition

        rb.MovePosition(next);

        // (Optional) Per-step distance clamp if you still see rare tunneling:
        // float maxStep = 2.0f; // units per physics step
        // Vector3 delta = next - rb.position;
        // if (delta.sqrMagnitude > maxStep * maxStep)
        //     rb.MovePosition(rb.position + delta.normalized * maxStep);
    }

    public void SetStopperBoundaries(bool homeFlag)
    {
        //Debug.LogError();

        if (homeFlag)
        {
            minX = -2f;
            minZ = -1f;
            maxX = 0f;
            maxZ = 1f;
        }
        else
        {
            minX = 0f;
            minZ = -1f;
            maxX = 2f;
            maxZ = 1f;
        }
        
    }

    void OnTriggerEnter(Collider other)
    {
        if (Time.time < nextAllowedHitTime) return;
        if (!GameManager.Instance.puckRb) return;

        // Only respond to local stopper hits
        if (other != GameManager.Instance.puckCollider) return;

        // Compute 2D normal from stopper -> puck (table plane)
        Vector3 puckPos = GameManager.Instance.puckRb.position;
        Vector3 stopPos = rb.position;

        Vector3 n3 = puckPos - stopPos;
        n3.y = 0f;
        if (n3.sqrMagnitude < 1e-6f) return;
        n3.Normalize();

        Vector3 vS = rb.velocity; vS.y = 0f;

        // If stopper barely moving, ignore
        //if (vS.magnitude < minHitSpeed) return;

        nextAllowedHitTime = Time.time + hitCooldown;

        GameManager.Instance.OnLocalStopperHit(n3, vS, puckPos);
    }
}
