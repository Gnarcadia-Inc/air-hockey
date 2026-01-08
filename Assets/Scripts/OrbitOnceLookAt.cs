using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OrbitOnceLookAt : MonoBehaviour
{
    [Header("Start Pose")]
    public Vector3 startPosition = new Vector3(0f, 2f, -5f);
    public Vector3 startEulerAngles = Vector3.zero;

    [Header("Orbit Settings")]
    public Vector3 pivotPoint = Vector3.zero;
    [Tooltip("Degrees per second around the Y axis.")]
    private float degreesPerSecond = 15f;
    [Tooltip("If true, the camera starts orbiting immediately on Play.")]
    public bool autoPlayOnStart = true;

    [Header("Finish Settings")]
    [Tooltip("How close (in world units) the camera must be to the start position to snap at the end.")]
    public float snapPosThreshold = 0.01f;
    [Tooltip("How close (in degrees) the camera's rotation must be to the start rotation to snap at the end.")]
    public float snapRotThreshold = 0.1f;

    private bool _isPlaying;
    private float _accumulatedAngle;
    private Vector3 _startPosRuntime;
    private Quaternion _startRotRuntime;

    void Start()
    {
        // Initialize camera at the exact start pose.
        transform.position = startPosition;
        transform.eulerAngles = startEulerAngles;
        transform.LookAt(pivotPoint, Vector3.up); // optional: ensure initial look direction faces the pivot

        _startPosRuntime = transform.position;
        _startRotRuntime = transform.rotation;
        _accumulatedAngle = 0f;

        _isPlaying = autoPlayOnStart;
    }

    void Update()
    {
        if (!_isPlaying) return;

        float step = degreesPerSecond * Time.deltaTime;

        // Rotate around the pivot on the world Y axis.
        transform.RotateAround(pivotPoint, Vector3.up, step);

        // Keep looking at the pivot.
        transform.LookAt(pivotPoint, Vector3.up);

        _accumulatedAngle += step;

        // Stop after one full revolution, then snap precisely to the start pose.
        if (_accumulatedAngle >= 360f ||
            (Vector3.Distance(transform.position, _startPosRuntime) <= snapPosThreshold &&
             Quaternion.Angle(transform.rotation, _startRotRuntime) <= snapRotThreshold))
        {
            // Snap to the exact authored start pose and stop.
            //transform.position = _startPosRuntime;
            //transform.rotation = _startRotRuntime;

            //_isPlaying = false;
        }
    }

    // Optional: call this to start the move manually (e.g., from another script or UI).
    public void Play()
    {
        if (_isPlaying) return;
        transform.position = startPosition;
        transform.eulerAngles = startEulerAngles;
        transform.LookAt(pivotPoint, Vector3.up);
        _startPosRuntime = transform.position;
        _startRotRuntime = transform.rotation;
        _accumulatedAngle = 0f;
        _isPlaying = true;
    }
}
