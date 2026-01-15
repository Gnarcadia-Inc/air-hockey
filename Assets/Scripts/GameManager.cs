using UnityEngine;
using TMPro;
using System;
using System.Collections;

/// Runs after default scripts so clamping happens *after* your drag/mouse script.
[DefaultExecutionOrder(1000)]
public class GameManager : MonoBehaviour
{

    public static GameManager Instance { get; private set; }

    [Header("Scene References")]
    public Transform stopper;               // Stopper Transform (has/hasn't RB)
    public Transform opponentStopper;
    public Transform puck;                  // Puck Transform (should have a non-kinematic RB)
    public TextMeshProUGUI minTensText;  // M tens
    public TextMeshProUGUI minOnesText;  // M ones
    public TextMeshProUGUI secTensText;  // S tens
    public TextMeshProUGUI secOnesText;      // UI mm:ss
    public TextMeshProUGUI scoreHomeText;
    public TextMeshProUGUI scoreAwayText;
    public GameObject endGameUI;            // UI shown when time is up

    [Header("A) Stopper Clamp (world X/Z)")]
    private bool clampStopper = false;
    private float minX = -2f, maxX = 0f;
    private float minZ = -1f, maxZ = 1f;
    [Tooltip("Keeps stopper slightly inside the bounds to avoid edge jitter.")]
    public float clampInset = 0.01f;
    [Tooltip("When clamped, zero out X/Z velocity to avoid oscillation.")]
    public bool zeroVelocityOnClamp = true;

    [Header("B) Goal Detection (Z)")]
    [Tooltip("If puck.z >= positiveGoalLineZ → + team scores")]
    private float positiveGoalLineZ = 2f;
    [Tooltip("If puck.z <= negativeGoalLineZ → - team scores")]
    private float negativeGoalLineZ = -2f;
    [Tooltip("Prevents double-scoring while the puck stays behind the line")]
    public float goalCooldownSeconds = 1f;

    [Header("B) Puck Reset After Goal")]
    [Tooltip("Y height is preserved; X and Z reset to 0.")]
    public bool resetPuckToCenter = true;

    [Header("C) Match Timer")]
    [Tooltip("Match duration in seconds (2 minutes = 120)")]
    public float matchSeconds = 120f;
    public bool freezeOnTimeUp = true;

    [Header("Scores (read-only at runtime)")]
    public int scorePositive;               // + team
    public int scoreNegative;               // - team

    private int lastYouScore = 0;
    private int lastOppScore = 0;

    public Rigidbody stopperRb;
    public Rigidbody opponentStopperRb;
    public Collider opponentStopperCollider;
    public Rigidbody puckRb;
    public Collider puckCollider;

    private float puckHeight = 0.0685f;
    private float stopperHeight = 0.06f;

    float timeRemaining;
    bool gameEnded;

    private bool gameFlag = false;

    private bool tieFlag = true;
    private float tieBuffer = 0f;

    private bool lastMessageFlag = false;

    public TextMeshProUGUI promptText;

    private GameLiftClient gameLiftClient;

    private bool homeFlag;
    public bool isAuthority;

    private Vector3 homeStopperStartPosition = new Vector3(-1.5f, 0.06f, 0f);
    private Vector3 awayStopperStartPosition = new Vector3(1.5f, 0.06f, 0f);

    public Transform mainCamera;
    private Vector3 homeCameraPosition = new Vector3(-3.3f, 1.32f, 0f);
    private Vector3 homeCameraRotation = new Vector3(25f, 90f, 0f);
    private Vector3 awayCameraPosition = new Vector3(3.3f, 1.32f, 0f);
    private Vector3 awayCameraRotation = new Vector3(25f, -90f, 0f);

    public Transform table;

    private bool hasRemotePuck = false;
    private Vector3 remotePos;
    private Vector3 remoteVel;
    private float remoteLastRecvTime;

    // Tune these
    [SerializeField] private float remotePosLerp = 18f;   // higher = snappier
    [SerializeField] private float remoteSnapDist = 0.35f; // snap if too far off

    private Vector3 stopperPos;
    private Vector3 stopperVel;
    private float stopperLastRecvTime;

    // Tune these
    [SerializeField] private float stopperPosLerp = 18f;   // higher = snappier
    [SerializeField] private float stopperSnapDist = 0.35f; // snap if too far off

    private float remotePuckFreezeUntil = 0f;

    private bool inPuckHandoff = false;
    private float handoffStartTime = 0f;
    private float handoffBlendDuration = 0.5f; // 120ms blend into authoritative
    private Vector3 handoffBlendFromPos;
    private Vector3 handoffBlendFromVel;

    private bool gotFirstRemoteAfterSwitch = false;

    private Coroutine puckHandoffCoroutine;

    [Min(0.0001f)] private float duration = 0.5f;
    private AnimationCurve easing = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public TextMeshProUGUI awayPlus;
    public TextMeshProUGUI homePlus;

    private Vector2 awayPlusStartPos = new Vector2(-450f, -100f);
    private Vector2 awayPlusEndPos = new Vector2(-450f, 25f);
    private Vector2 homePlusStartPos = new Vector2(450f, -100f);
    private Vector2 homePlusEndPos = new Vector2(450f, 25f);

    public Transform youGoalLightTransform;
    public Transform oppGoalLightTransform;
    private Vector3 youGoalLightStartPos = new Vector3(-2.25f, -0.17f, 0f);
    private Vector3 youGoalLightEndPos = new Vector3(-2.25f, 0.162f, 0f);
    private Vector3 oppGoalLightStartPos = new Vector3(2.25f, -0.17f, 0f);
    private Vector3 oppGoalLightEndPos = new Vector3(2.25f, 0.162f, 0f);

    private bool goalFlag = false;

    public void OnRemotePuckState(float x, float z, float vx, float vz)
    {
        hasRemotePuck = true;
        remotePos = new Vector3(x, puckHeight, z);
        remoteVel = new Vector3(vx, 0f, vz);
        remoteLastRecvTime = Time.time;

        if (inPuckHandoff && !gotFirstRemoteAfterSwitch && puckRb != null)
        {
            gotFirstRemoteAfterSwitch = true;

            handoffBlendFromPos = puckRb.position;
            handoffBlendFromVel = puckRb.velocity;

            puckRb.isKinematic = true;

            handoffStartTime = Time.time;
        }
    }

    public void OnSwitchRemotePuckState(float vx, float vz)
    {
        hasRemotePuck = true;
        remotePos = new Vector3(puckRb.position.x, puckHeight, puckRb.position.z);
        remoteVel = new Vector3(vx, 0f, vz);
        remoteLastRecvTime = Time.time;

        if (inPuckHandoff && !gotFirstRemoteAfterSwitch && puckRb != null)
        {
            gotFirstRemoteAfterSwitch = true;

            handoffBlendFromPos = puckRb.position;
            handoffBlendFromVel = puckRb.velocity;

            puckRb.isKinematic = true;

            handoffStartTime = Time.time;
        }
    }

    public void OnRemoteStopperState(float x, float z, float vx, float vz)
    {
        stopperPos = new Vector3(x, stopperHeight, z);
        stopperVel = new Vector3(vx, 0f, vz);
        stopperLastRecvTime = Time.time;
    }

    void OnValidate()
    {
        if (maxX < minX) (minX, maxX) = (maxX, minX);
        if (maxZ < minZ) (minZ, maxZ) = (maxZ, minZ);
        clampInset = Mathf.Max(0f, clampInset);
    }

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        gameLiftClient = FindObjectOfType<GameLiftClient>();

        if (gameLiftClient == null)
        {
            Debug.LogError("NULLSHIT");
        }

        Debug.LogError("PAST NULLSHIT: " + gameLiftClient.GetHomeFlag());

        if (stopper) stopperRb = stopper.GetComponent<Rigidbody>();

        if (opponentStopper)
        {
            opponentStopperRb = opponentStopper.GetComponent<Rigidbody>();
            opponentStopperCollider = opponentStopper.GetComponent<MeshCollider>();
        }

        if (puck)
        {
            puckRb = puck.GetComponent<Rigidbody>();
            puckCollider = puck.GetComponent<MeshCollider>();
        }

        SetHomeFlag(gameLiftClient.GetHomeFlag());

        Debug.LogError("FURTHER PAST NULLSHIT");

        Debug.LogError("STARTCRO");

        if (endGameUI) endGameUI.SetActive(false);
        Time.timeScale = 1f;
        timeRemaining = Mathf.Max(0f, matchSeconds);
        gameEnded = false;

        UpdateTimerText();

        Debug.LogError("PASTSTARTCRO");
    }

    void Update()
    {
        if (gameEnded) return;

        UpdateTimerText();
    }

    void FixedUpdate()
    {
        if (gameEnded) return;

        if (gameLiftClient == null) return;


        // B) Goal detect + reset (do this in physics step)
        if (puckRb)
        {
            if (gameFlag)
            {
                float x = puckRb.position.x;

                float vx = puckRb.velocity.x;

                if (isAuthority)
                {
                    if (puckRb.isKinematic) puckRb.isKinematic = false;

                    puckCollider.enabled = true;

                    if (!goalFlag)
                    {
                        if (homeFlag)
                        {
                            if (puckRb.position.x >= 2.25f)
                            {
                                goalFlag = true;
                                GoalScored(false);
                            }
                            else if (puckRb.position.x <= -2.25f)
                            {
                                goalFlag = true;
                                GoalScored(true);
                            }
                        }
                        else
                        {
                            if (puckRb.position.x >= 2.25f)
                            {
                                goalFlag = true;
                                GoalScored(true);
                            }
                            else if (puckRb.position.x <= -2.25f)
                            {
                                goalFlag = true;
                                GoalScored(false);
                            }
                        }
                    }
                }
                else
                {
                    if (!puckRb.isKinematic) puckRb.isKinematic = true;

                    puckCollider.enabled = true;

                    float dt = Mathf.Clamp(Time.time - remoteLastRecvTime, 0f, 0.2f);
                    Vector3 predicted = remotePos + remoteVel * dt;
                    Vector3 current = puckRb.position;

                    puckRb.MovePosition(Vector3.Lerp(current, predicted, 1f - Mathf.Exp(-remotePosLerp * Time.fixedDeltaTime)));
                }
            }
        }

        if (opponentStopperRb && opponentStopperCollider)
        {
            if (!opponentStopperRb.isKinematic) opponentStopperRb.isKinematic = true;

            opponentStopperCollider.enabled = false;

            float dt = Mathf.Clamp(Time.time - stopperLastRecvTime, 0f, 0.2f);
            Vector3 predicted = stopperPos + stopperVel * dt;

            Vector3 current = opponentStopperRb.position;

            // Snap if error is large (prevents rubber-banding when packets delay)
            if ((predicted - current).sqrMagnitude > stopperSnapDist * stopperSnapDist)
            {
                opponentStopperRb.position = predicted;
            }
            else
            {
                // Smooth toward predicted
                Vector3 next = Vector3.Lerp(current, predicted, 1f - Mathf.Exp(-stopperPosLerp * Time.fixedDeltaTime));
                opponentStopperRb.MovePosition(next);
            }
        }
    }

    public void OnLocalStopperHit(Vector3 n, Vector3 vStopper, Vector3 reportedPuckPos)
    {
        var newVelo = CalculateImpulse(n, vStopper);
        if (newVelo == Vector3.zero) return;

        Debug.LogError("SEPH");

        if (isAuthority)
        {
            ApplyImpulseAuthoritative(newVelo);
        }
        else
        {
            Debug.LogError("JUMP");

            isAuthority = true;

            ApplyImpulseNonAuthoritative(newVelo);
        }
    }

    private Vector3 CalculateImpulse(Vector3 n, Vector3 vStopper)
    {
        var rb = puckRb;
        Vector3 vP = rb.velocity; vP.y = 0f;

        float vn = Vector3.Dot((vP - vStopper), n);
        if (vn > 0f) return Vector3.zero; // separating

        float e = 0.9f;   // tune
        float k = 0.25f;  // tune

        Vector3 vNew = vP - (1f + e) * vn * n + k * (vStopper - vP);
        return vNew;
    }

    public void ApplyImpulseAuthoritative(Vector3 velo)
    {
        puckRb.velocity = new Vector3(velo.x, 0f, velo.z);
    }

    public void ApplyImpulseNonAuthoritative(Vector3 velo)
    {
        puckRb.isKinematic = false;

        puckRb.velocity = new Vector3(velo.x, 0f, velo.z);

        gameLiftClient.SendPuckHit(velo);
    }

    public void ClearRemotePuck()
    {
        hasRemotePuck = false;
        remoteVel = Vector3.zero;
        // keep lastRemotePuckSeq as-is; or reset it if you also seq PUCK_SWITCH
    }

    public void GoalScored(bool selfFlag)
    {
        gameLiftClient.SendGoalUpdate(selfFlag);
    }

    // --- A) Clamp with sticky edge, using direct position set to avoid double-MovePosition jitter ---
    void ClampStopper_NoJitter()
    {
        float minXIn = minX + clampInset;
        float maxXIn = maxX - clampInset;
        float minZIn = minZ + clampInset;
        float maxZIn = maxZ - clampInset;

        Vector3 pos = stopperRb ? stopperRb.position : stopper.position;

        float clampedX = Mathf.Clamp(pos.x, minXIn, maxXIn);
        float clampedZ = Mathf.Clamp(pos.z, minZIn, maxZIn);
        bool outX = (clampedX != pos.x);
        bool outZ = (clampedZ != pos.z);

        if (outX || outZ)
        {
            Vector3 clamped = new Vector3(clampedX, pos.y, clampedZ);

            // IMPORTANT: set position directly so we don't queue another kinematic move this tick.
            if (stopperRb)
            {
                stopperRb.position = clamped; // <- direct set (authoritative final pose)

                if (zeroVelocityOnClamp)
                {
                    // Kill horizontal motion so you don't bounce back and forth
                    Vector3 v = stopperRb.velocity;
                    v.x = 0f; v.z = 0f; v.y = 0f;
                    stopperRb.velocity = v;
                    stopperRb.angularVelocity = Vector3.zero;
                }
            }
            else
            {
                stopper.position = clamped;
            }
        }
    }

    // --- B) Reset puck to center (X=0, Z=0), preserve Y ---
    void ResetPuckXZToCenter()
    {
        if (!puck) return;
        Vector3 p = puck.position;
        p.x = 0f; p.z = 0f;

        if (puckRb)
        {
            puckRb.velocity = Vector3.zero;
            puckRb.angularVelocity = Vector3.zero;
            puckRb.position = p; // direct set in physics step is fine here
        }
        else
        {
            puck.position = p;
        }

        if (homeFlag)
        {
            gameLiftClient.puckOnSide = true;
        }
        else
        {
            gameLiftClient.puckOnSide = false;
        }

        goalFlag = false;
    }

    public void FreezeRemotePuck(float seconds)
    {
        remotePuckFreezeUntil = Time.time + seconds;
    }

    void UpdateTimerText()
    {
        if (gameLiftClient != null && gameLiftClient.gameStartTime != default(DateTime) && gameLiftClient.gameDuration != 0f)
        {
            TimeSpan elapsedTime = DateTime.UtcNow - gameLiftClient.gameStartTime;
            float elapsedTimeFloat = (float)elapsedTime.TotalSeconds;


            if (tieFlag && (elapsedTimeFloat >= gameLiftClient.gameDuration))
            {
                tieBuffer = elapsedTimeFloat - gameLiftClient.gameDuration;
            }

            //Pregame & Endgame Sequence
            if (elapsedTimeFloat < 12f && elapsedTimeFloat >= 0f)
            {
                gameFlag = false;

                puckCollider.enabled = false;
                puckRb.isKinematic = true;

                promptText.gameObject.SetActive(true);
                promptText.text = "Opening Faceoff\n" + (int)(15f - elapsedTimeFloat);
            }
            else if (elapsedTimeFloat < 15f && elapsedTimeFloat >= 12f)
            {
                gameFlag = false;

                puckCollider.enabled = false;
                puckRb.isKinematic = true;

                promptText.gameObject.SetActive(true);
                promptText.text = "Puck drop in\n" + (int)(15f - elapsedTimeFloat);
            }
            else if (elapsedTimeFloat < 16f && elapsedTimeFloat >= 15f)
            {
                gameFlag = false;

                puckCollider.enabled = false;
                puckRb.isKinematic = true;

                promptText.gameObject.SetActive(true);
                promptText.text = "Puck drop";
            }
            else if (elapsedTimeFloat > (gameLiftClient.gameDuration + tieBuffer) && elapsedTimeFloat < (gameLiftClient.gameDuration + tieBuffer + 2f))
            {
                gameFlag = false;
                puckRb.isKinematic = true;

                puckCollider.enabled = false;

                promptText.gameObject.SetActive(true);
                promptText.text = "Game over";
            }
            else if (elapsedTimeFloat >= (gameLiftClient.gameDuration + tieBuffer + 2f) && elapsedTimeFloat < (gameLiftClient.gameDuration + tieBuffer + 15f))
            {
                gameFlag = false;
                promptText.gameObject.SetActive(false);

                EndGame();
            }
            else if (elapsedTimeFloat >= (gameLiftClient.gameDuration + tieBuffer + 15f))
            {
                if (!lastMessageFlag)
                {
                    lastMessageFlag = true;
                    gameLiftClient.SendMessageToServer("GAME_END");
                }
            }
            else
            {
                puckCollider.enabled = true;

                gameFlag = true;
                promptText.gameObject.SetActive(false);
            }

            float timeLeftFloat = Mathf.Max(gameLiftClient.gameDuration - elapsedTimeFloat, 0f);

            //string formattedTime = string.Format("{0}:{1:D2}", Mathf.FloorToInt(timeLeftFloat / 60), Mathf.FloorToInt(timeLeftFloat % 60));
            //timeText.text = formattedTime;

            // Compute mm ss (ceiling so 0.2s shows as 00 01, etc.)
            int t = Mathf.CeilToInt(timeLeftFloat);
            if (t < 0) t = 0;

            int mins = t / 60;
            int secs = t % 60;

            // Keep minutes to two digits (your UI has 2 boxes)
            mins = Mathf.Clamp(mins, 0, 99);

            int mTens = mins / 10;
            int mOnes = mins % 10;
            int sTens = secs / 10;
            int sOnes = secs % 10;

            if (minTensText) minTensText.text = mTens.ToString();
            if (minOnesText) minOnesText.text = mOnes.ToString();
            if (secTensText) secTensText.text = sTens.ToString();
            if (secOnesText) secOnesText.text = sOnes.ToString();
        }
    }


    public void UpdateScoreText(int youScore, int oppScore)
    {
        if (youScore != oppScore)
        {
            tieFlag = false;
        }
        else
        {
            tieFlag = true;
        }

        if (youScore != lastYouScore)
        {
            lastYouScore = youScore;

            StartCoroutine(ScorePlusAnimation(false));

            if (homeFlag)
            {
                StartCoroutine(GoalHornAnimation(true));
            }
            else
            {
                StartCoroutine(GoalHornAnimation(false));
            }
        }
        else
        {
            lastOppScore = oppScore;

            StartCoroutine(ScorePlusAnimation(true));

            if (homeFlag)
            {
                StartCoroutine(GoalHornAnimation(false));
            }
            else
            {
                StartCoroutine(GoalHornAnimation(true));
            }
        }


        scoreHomeText.text = youScore.ToString();
        scoreAwayText.text = oppScore.ToString();

        if (resetPuckToCenter) ResetPuckXZToCenter();
    }

    private IEnumerator ScorePlusAnimation(bool plusFlag)
    {
        if (plusFlag)
        {
            var plusRect = awayPlus.gameObject.GetComponent<RectTransform>();

            plusRect.anchoredPosition = awayPlusStartPos;

            Color color = awayPlus.color;
            color.a = 0f;
            awayPlus.color = color;

            awayPlus.gameObject.SetActive(true);

            float t = 0f;
            float d = Mathf.Max(0.0001f, duration);

            while (t < d)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / d);         // 0..1
                float w = easing.Evaluate(u);           // eased 0..1

                plusRect.anchoredPosition = Vector2.LerpUnclamped(awayPlusStartPos, awayPlusEndPos, w);


                float similarity;
                if (u < 0.25f)
                {
                    // 0 -> 1 over [0, 0.25] (quarter of the time)
                    float p = u / 0.25f;                 // 0..1
                    similarity = easing.Evaluate(p);     // eased up
                }
                else if (u < 0.75f)
                {
                    similarity = 1f; // hold
                }
                else
                {
                    // 1 -> 0 over [0.75, 1]
                    float p = (u - 0.75f) / 0.25f;       // 0..1
                    similarity = 1f - easing.Evaluate(p);// eased down
                }

                color.a = similarity;
                awayPlus.color = color;

                yield return null;
            }

            plusRect.anchoredPosition = awayPlusEndPos;

            awayPlus.gameObject.SetActive(false);
        }
        else
        {
            var plusRect = homePlus.gameObject.GetComponent<RectTransform>();

            plusRect.anchoredPosition = homePlusStartPos;

            Color color = homePlus.color;
            color.a = 0f;
            homePlus.color = color;

            homePlus.gameObject.SetActive(true);

            float t = 0f;
            float d = Mathf.Max(0.0001f, duration);

            while (t < d)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / d);         // 0..1
                float w = easing.Evaluate(u);           // eased 0..1

                plusRect.anchoredPosition = Vector2.LerpUnclamped(homePlusStartPos, homePlusEndPos, w);


                float similarity;
                if (u < 0.25f)
                {
                    // 0 -> 1 over [0, 0.25] (quarter of the time)
                    float p = u / 0.25f;                 // 0..1
                    similarity = easing.Evaluate(p);     // eased up
                }
                else if (u < 0.75f)
                {
                    similarity = 1f; // hold
                }
                else
                {
                    // 1 -> 0 over [0.75, 1]
                    float p = (u - 0.75f) / 0.25f;       // 0..1
                    similarity = 1f - easing.Evaluate(p);// eased down
                }

                color.a = similarity;
                homePlus.color = color;

                yield return null;
            }

            plusRect.anchoredPosition = homePlusEndPos;

            homePlus.gameObject.SetActive(false);
        }
    }

    private IEnumerator GoalHornAnimation(bool goalHornFlag)
    {
        if (goalHornFlag)
        {
            youGoalLightTransform.position = youGoalLightStartPos;

            youGoalLightTransform.gameObject.SetActive(true);

            float t = 0f;
            float d = Mathf.Max(0.0001f, duration);

            while (t < d)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / d);         // 0..1
                float w = easing.Evaluate(u);           // eased 0..1

                youGoalLightTransform.position = Vector3.LerpUnclamped(youGoalLightStartPos, youGoalLightEndPos, w);
                youGoalLightTransform.rotation *= Quaternion.Euler(0f, 0.1f, 0f);

                yield return null;
            }

            t = 0f;
            d = Mathf.Max(0.0001f, duration * 2f);

            while (t < d)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / d);         // 0..1
                float w = easing.Evaluate(u);           // eased 0..1

                youGoalLightTransform.rotation *= Quaternion.Euler(0f, 0.1f, 0f);

                yield return null;
            }

            t = 0f;
            d = Mathf.Max(0.0001f, duration);

            while (t < d)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / d);         // 0..1
                float w = easing.Evaluate(u);           // eased 0..1

                youGoalLightTransform.position = Vector3.LerpUnclamped(youGoalLightEndPos, youGoalLightStartPos, w);
                youGoalLightTransform.rotation *= Quaternion.Euler(0f, 0.1f, 0f);

                yield return null;
            }

            youGoalLightTransform.position = youGoalLightStartPos;

            youGoalLightTransform.gameObject.SetActive(false);
        }
        else
        {
            oppGoalLightTransform.position = oppGoalLightStartPos;

            oppGoalLightTransform.gameObject.SetActive(true);

            float t = 0f;
            float d = Mathf.Max(0.0001f, duration);

            while (t < d)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / d);         // 0..1
                float w = easing.Evaluate(u);           // eased 0..1

                oppGoalLightTransform.position = Vector3.LerpUnclamped(oppGoalLightStartPos, oppGoalLightEndPos, w);
                oppGoalLightTransform.rotation *= Quaternion.Euler(0f, 0.1f, 0f);

                yield return null;
            }

            t = 0f;
            d = Mathf.Max(0.0001f, duration * 2f);

            while (t < d)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / d);         // 0..1
                float w = easing.Evaluate(u);           // eased 0..1

                oppGoalLightTransform.rotation *= Quaternion.Euler(0f, 0.1f, 0f);

                yield return null;
            }

            t = 0f;
            d = Mathf.Max(0.0001f, duration);

            while (t < d)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / d);         // 0..1
                float w = easing.Evaluate(u);           // eased 0..1

                oppGoalLightTransform.position = Vector3.LerpUnclamped(oppGoalLightEndPos, oppGoalLightStartPos, w);
                oppGoalLightTransform.rotation *= Quaternion.Euler(0f, 0.1f, 0f);

                yield return null;
            }

            oppGoalLightTransform.position = oppGoalLightStartPos;

            oppGoalLightTransform.gameObject.SetActive(false);
        }
        
        
    }

    public FullPuckPhysics TurnOffPuckPhysics()
    {
        FullPuckPhysics fullPuck = new FullPuckPhysics();
        fullPuck.puckPos = puckRb.position;
        fullPuck.puckVelo = puckRb.velocity;

        puckRb.velocity = Vector3.zero;

        puckRb.isKinematic = true;

        return fullPuck;
    }

    public void TurnOnPuckPhysics(float posX, float posZ, float veloX, float veloZ)
    {
        if (!puckRb) return;

        if (puckHandoffCoroutine != null)
        {
            StopCoroutine(puckHandoffCoroutine);
            puckHandoffCoroutine = null;
        }

        puckRb.isKinematic = true; // ensure we can set safely
        puckRb.velocity = Vector3.zero;
        puckRb.angularVelocity = Vector3.zero;

        puckRb.position = new Vector3(posX, puckHeight, posZ);

        puckRb.isKinematic = false;
        puckRb.WakeUp();
        puckRb.velocity = new Vector3(veloX, 0f, veloZ);
    }


    public void MovePuck(float posX, float posZ)
    {
        puckRb.MovePosition(new Vector3(posX, puckHeight, posZ));
    }

    public void MoveOpponentStopper(float posX, float posZ)
    {
        opponentStopperRb.MovePosition(new Vector3(posX, stopperHeight, posZ));
    }

    public Vector3 GetPuckPosition()
    {
        return puckRb.position;
    }

    public FullPuckPhysics GetPuckState()
    {
        return new FullPuckPhysics
        {
            puckPos = puckRb.position,
            puckVelo = puckRb.velocity
        };
    }

    public Vector3 GetStopperPosition()
    {
        return stopperRb.position;
    }

    public void BeginHandoffBallistic(float carrySeconds)
    {
        if (!puckRb) return;

        // Start ballistic carry: keep physics running, but mark we're in handoff
        inPuckHandoff = true;
        gotFirstRemoteAfterSwitch = false;
        handoffStartTime = Time.time;

        puckRb.isKinematic = false;
        puckRb.WakeUp();

        // Safety: if remote never comes (packet loss), stop after carrySeconds
        if (puckHandoffCoroutine != null) StopCoroutine(puckHandoffCoroutine);
        puckHandoffCoroutine = StartCoroutine(HandoffTimeoutRoutine(carrySeconds));
    }

    private IEnumerator HandoffTimeoutRoutine(float carrySeconds)
    {
        yield return new WaitForSeconds(carrySeconds);

        // If we still haven't received remote authority, lock it to avoid drifting forever
        if (inPuckHandoff && !gotFirstRemoteAfterSwitch)
        {
            puckRb.velocity = Vector3.zero;
            puckRb.angularVelocity = Vector3.zero;
            puckRb.isKinematic = true;
            inPuckHandoff = false;
        }

        puckHandoffCoroutine = null;
    }


    public void SetHomeFlag(bool flag)
    {
        homeFlag = flag;

        if (homeFlag)
        {
            stopperRb.MovePosition(homeStopperStartPosition);
            opponentStopperRb.MovePosition(awayStopperStartPosition);

            mainCamera.position = homeCameraPosition;
            mainCamera.rotation = Quaternion.Euler(homeCameraRotation);

            table.rotation = Quaternion.Euler(Vector3.zero);

            gameLiftClient.puckOnSide = true;

            isAuthority = true;
        }
        else
        {
            stopperRb.MovePosition(awayStopperStartPosition);
            opponentStopperRb.MovePosition(homeStopperStartPosition);

            mainCamera.position = awayCameraPosition;
            mainCamera.rotation = Quaternion.Euler(awayCameraRotation);

            table.rotation = Quaternion.Euler(new Vector3(0f, 180f, 0f));

            gameLiftClient.puckOnSide = false;

            isAuthority = false;
        }

        FindObjectOfType<PuckStopperDrag>().SetStopperBoundaries(homeFlag);
    }

    void EndGame()
    {
        gameEnded = true;
        if (freezeOnTimeUp) Time.timeScale = 0f;
        if (endGameUI) endGameUI.SetActive(true);
    }
}
