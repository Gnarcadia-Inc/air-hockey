using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuManager : MonoBehaviour
{
    public RectTransform titleImage;
    private Vector2 startAnchoredPosition = new Vector2(0f, 100f);
    private Vector2 endAnchoredPosition = new Vector2(0f, 350f);

    public GameObject overlayObject;           // Drag a UI GameObject here (e.g., another Image)
    private Vector2 overlayStartAnchoredPosition = new Vector2(0f, -200f);
    private Vector2 overlayEndAnchoredPosition = new Vector2(0f, 0f);

    public GameObject subtextObject;

    public int playerReadiness = 0;
    private bool exitedButtonFlag = false;

    [Min(0.0001f)] private float duration = 0.5f;
    public AnimationCurve easing = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool playFlag = false;
    private Coroutine _running;
    private Vector3 _primaryStartScale = new Vector3(1f, 1f, 1f);
    private Vector3 _primaryEndScale;
    private RectTransform _overlayRect;
    private CanvasGroup _overlayCanvasGroup;

    public Sprite findMatchSprite;
    public Sprite cancelFindSprite;
    public Sprite matchFoundSprite;
    public Sprite blackUserSprite;
    public Sprite pinkUserSprite;
    public Sprite greenUserSprite;

    public Image userImage;
    public TextMeshProUGUI usernameTextLeft;
    public TextMeshProUGUI titleTextLeft;
    public TextMeshProUGUI ratingTextLeft;
    public TextMeshProUGUI recordTextLeft;
    public TextMeshProUGUI lastTenTextLeft;
    public TextMeshProUGUI streakTextLeft;
    public Image gnightImageLeft;

    public Image matchImage;
    public TextMeshProUGUI usernameTextRight;
    public TextMeshProUGUI titleTextRight;
    public TextMeshProUGUI ratingTextRight;
    public TextMeshProUGUI recordTextRight;
    public TextMeshProUGUI lastTenTextRight;
    public TextMeshProUGUI streakTextRight;
    public Image gnightImageRight;

    public Image versusImage;

    public GameObject starPrefab;

    public List<Sprite> gnightSprites = new List<Sprite>();

    private List<GameObject> stars = new List<GameObject>();



    void Reset()
    {
        titleImage = GetComponent<RectTransform>();
        overlayObject.SetActive(false);
        subtextObject.SetActive(true);
        duration = 0.5f;
        easing = AnimationCurve.EaseInOut(0, 0, 1, 1);
    }

    void Awake()
    {
        if (!titleImage) titleImage = GetComponent<RectTransform>();

        //Set correct objects to active

        //Left
        gnightImageLeft.gameObject.SetActive(false);

        userImage.sprite = blackUserSprite;

        MatchBackZero();

        versusImage.gameObject.SetActive(false);
    }

    public void InitUserDetails()
    {
        usernameTextLeft.text = UserDetails.userName;

        //Ranking Divisions
        if (AirHockeyDetails.rating < 900)
        {
            titleTextLeft.text = "Beginner"; //Could change to bum
        }
        else if (AirHockeyDetails.rating >= 900 && AirHockeyDetails.rating < 1400)
        {
            titleTextLeft.text = "Novice"; //Could change to rookie
        }
        else if (AirHockeyDetails.rating >= 1400 && AirHockeyDetails.rating < 1700)
        {
            titleTextLeft.text = "Intermediate"; //Could change to pro
        }
        else if (AirHockeyDetails.rating >= 1700 && AirHockeyDetails.rating < 1900)
        {
            titleTextLeft.text = "Advanced"; //Could change to contender
        }
        else if (AirHockeyDetails.rating >= 1900 && AirHockeyDetails.rating < 2100)
        {
            titleTextLeft.text = "Expert"; //Could change to champion
        }
        else if (AirHockeyDetails.rating >= 2100 && AirHockeyDetails.rating < 2400)
        {
            titleTextLeft.text = "Master";
        }
        else if (AirHockeyDetails.rating >= 2400 && AirHockeyDetails.rating < 2600)
        {
            titleTextLeft.text = "Grandmaster";
        }
        else if (AirHockeyDetails.rating >= 2600)
        {
            titleTextLeft.text = "Ultramaster";
        }



        ratingTextLeft.text = AirHockeyDetails.rating.ToString();


        float spacer = 55f;
        float vert = 188f;

        //SWITCH TO MAKE MORE EFFICIENT
        switch (AirHockeyDetails.ranking)
        {
            case 1:
                Vector3 adj1 = userImage.transform.position - new Vector3(0f, vert, 0f);
                Instantiate(starPrefab, adj1, Quaternion.identity, userImage.transform);
                break;
            case 2:
                Vector3 adj2 = userImage.transform.position - new Vector3(0f, vert, 0f);
                Instantiate(starPrefab, adj2 + new Vector3(-0.5f * spacer, 0f, 0f), Quaternion.identity, userImage.transform);
                Instantiate(starPrefab, adj2 + new Vector3(0.5f * spacer, 0f, 0f), Quaternion.identity, userImage.transform);
                break;
            case 3:
                Vector3 adj3 = userImage.transform.position - new Vector3(0f, vert, 0f);
                Instantiate(starPrefab, adj3 + new Vector3(-1f * spacer, 0f, 0f), Quaternion.identity, userImage.transform);
                Instantiate(starPrefab, adj3, Quaternion.identity, userImage.transform);
                Instantiate(starPrefab, adj3 + new Vector3(1f * spacer, 0f, 0f), Quaternion.identity, userImage.transform);
                break;
            case 4:
                Vector3 adj4 = userImage.transform.position - new Vector3(0f, vert, 0f);
                Instantiate(starPrefab, adj4 + new Vector3(-1.5f * spacer, 0f, 0f), Quaternion.identity, userImage.transform);
                Instantiate(starPrefab, adj4 + new Vector3(-0.5f * spacer, 0f, 0f), Quaternion.identity, userImage.transform);
                Instantiate(starPrefab, adj4 + new Vector3(0.5f * spacer, 0f, 0f), Quaternion.identity, userImage.transform);
                Instantiate(starPrefab, adj4 + new Vector3(1.5f * spacer, 0f, 0f), Quaternion.identity, userImage.transform);
                break;
            case 5:
                Vector3 adj5 = userImage.transform.position - new Vector3(0f, vert, 0f);
                Instantiate(starPrefab, adj5 + new Vector3(-2f * spacer, 0f, 0f), Quaternion.identity, userImage.transform);
                Instantiate(starPrefab, adj5 + new Vector3(-1f * spacer, 0f, 0f), Quaternion.identity, userImage.transform);
                Instantiate(starPrefab, adj5, Quaternion.identity, userImage.transform);
                Instantiate(starPrefab, adj5 + new Vector3(1f * spacer, 0f, 0f), Quaternion.identity, userImage.transform);
                Instantiate(starPrefab, adj5 + new Vector3(2f * spacer, 0f, 0f), Quaternion.identity, userImage.transform);
                break;
        }




        recordTextLeft.text = AirHockeyDetails.numWins.ToString() + "-" + AirHockeyDetails.numLosses.ToString();

        int wins = 0;
        int losses = 0;
        foreach (bool res in AirHockeyDetails.lastTenResults)
        {
            if (res)
            {
                wins++;
            }
            else
            {
                losses++;
            }
        }
        lastTenTextLeft.text = wins.ToString() + "-" + losses.ToString();

        if (AirHockeyDetails.activeStreak >= 0)
        {
            streakTextLeft.text = "W" + AirHockeyDetails.activeStreak.ToString();
        }
        else
        {
            streakTextLeft.text = "L" + (-1 * AirHockeyDetails.activeStreak).ToString();
        }

        gnightImageLeft.sprite = gnightSprites[UserDetails.gnight - 1];
        gnightImageLeft.gameObject.SetActive(true);

        //NOW ON TO: SETUP UI ELEMENTS, ROLL FOR FIRST MOVE, MOVE SWITCHING AND CONTROL, PIECE AND DICE STATE TRANSMIT, WINNING
        //THEN GO BACK TO POLISHING UI, FIX VERSUS ANIMATION, FIX CANVAS SCALING
    }

    public void Play()
    {
        if (!playFlag)
        {
            playFlag = true;

            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(Animate());
        }
    }

    public void PlayFrom(Vector2 startPos, Vector2 endPos)
    {
        startAnchoredPosition = startPos;
        endAnchoredPosition = endPos;
        Play();
    }

    public void StopAndSnapToEnd()
    {
        if (_running != null) StopCoroutine(_running);
        if (!titleImage) return;
        titleImage.anchoredPosition = endAnchoredPosition;
        titleImage.localScale = _primaryEndScale == Vector3.zero ? titleImage.localScale * 0.5f : _primaryEndScale;
        _running = null;
    }

    public void StopAndResetToStart()
    {
        if (_running != null) StopCoroutine(_running);
        if (!titleImage) return;
        titleImage.anchoredPosition = startAnchoredPosition;
        titleImage.localScale = _primaryStartScale;
        _running = null;
    }

    private IEnumerator Animate()
    {
        if (!titleImage) yield break;

        titleImage.anchoredPosition = startAnchoredPosition;
        _primaryEndScale = _primaryStartScale * 0.5f;

        Color color = userImage.color;
        color.a = 0f;
        userImage.color = color;
        matchImage.color = color;

        overlayObject.SetActive(true);
        subtextObject.SetActive(false);

        float t = 0f;
        float d = Mathf.Max(0.0001f, duration);

        while (t < d)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / d);         // 0..1
            float w = easing.Evaluate(u);           // eased 0..1

            titleImage.anchoredPosition = Vector2.LerpUnclamped(startAnchoredPosition, endAnchoredPosition, w);
            titleImage.localScale = Vector3.LerpUnclamped(_primaryStartScale, _primaryEndScale, w);

            overlayObject.GetComponent<RectTransform>().anchoredPosition = Vector2.LerpUnclamped(overlayStartAnchoredPosition, overlayEndAnchoredPosition, w);
            color.a = w;
            userImage.color = color;
            matchImage.color = color;

            yield return null;
        }

        titleImage.anchoredPosition = endAnchoredPosition;
        titleImage.localScale = _primaryEndScale;
        _running = null;
    }

    public void FindMatchButton()
    {
        Debug.LogError("SCUMCRO");
        if (UserDetails.userProfileId != null)
        {
            GameLiftClient client = FindObjectOfType<GameLiftClient>();
            switch (playerReadiness)
            {
                case 0: //Join
                    playerReadiness = 1;

                    MatchBackOne();

                    client.StartMatchmaking();

                    break;
                case 1: //Cancel
                    if (!exitedButtonFlag)
                    {
                        exitedButtonFlag = true;

                        client.ExitGameSession();
                    }

                    break;
            }
        }
    }

    public void ResetPlayerReadiness()
    {
        playerReadiness = 0;
    }

    public void ResetExitReadyButton()
    {
        exitedButtonFlag = false;

        MatchBackZero();
    }

    private void MatchBackZero()
    {
        usernameTextRight.gameObject.SetActive(false);
        titleTextRight.gameObject.SetActive(false);
        ratingTextRight.gameObject.SetActive(false);
        recordTextRight.gameObject.SetActive(false);
        lastTenTextRight.gameObject.SetActive(false);
        streakTextRight.gameObject.SetActive(false);
        gnightImageRight.gameObject.SetActive(false);

        matchImage.sprite = findMatchSprite;
    }

    private void MatchBackOne()
    {
        usernameTextRight.gameObject.SetActive(false);
        titleTextRight.gameObject.SetActive(false);
        ratingTextRight.gameObject.SetActive(false);
        recordTextRight.gameObject.SetActive(false);
        lastTenTextRight.gameObject.SetActive(false);
        streakTextRight.gameObject.SetActive(false);
        gnightImageRight.gameObject.SetActive(false);

        matchImage.sprite = cancelFindSprite;
    }

    public void MatchBackTwo(int opponentId, string opponentUsername, int opponentGnight, string color, int rating, int ranking, bool[] lastTenResults,
            int activeStreak, int numWins, int numLosses, int totalWagered, int recentNetEarningsEma)
    {
        //On matchmaking completed get opponent info


        usernameTextRight.text = opponentUsername;

        //Ranking Divisions
        if (rating < 900)
        {
            titleTextRight.text = "Beginner"; //Could change to bum
        }
        else if (rating >= 900 && rating < 1400)
        {
            titleTextRight.text = "Novice"; //Could change to rookie
        }
        else if (rating >= 1400 && rating < 1700)
        {
            titleTextRight.text = "Intermediate"; //Could change to pro
        }
        else if (rating >= 1700 && rating < 1900)
        {
            titleTextRight.text = "Advanced"; //Could change to contender
        }
        else if (rating >= 1900 && rating < 2100)
        {
            titleTextRight.text = "Expert"; //Could change to champion
        }
        else if (rating >= 2100 && rating < 2400)
        {
            titleTextRight.text = "Master";
        }
        else if (rating >= 2400 && rating < 2600)
        {
            titleTextRight.text = "Grandmaster";
        }
        else if (rating >= 2600)
        {
            titleTextRight.text = "Ultramaster";
        }


        ratingTextRight.text = rating.ToString();

        float spacer = 55f;
        float vert = 188f;
        //ranking = 3; //FOR TESTING

        //SWITCH TO MAKE MORE EFFICIENT
        switch (ranking)
        {
            case 1:
                Vector3 adj1 = matchImage.transform.position - new Vector3(0f, vert, 0f);
                Instantiate(starPrefab, adj1, Quaternion.identity, matchImage.transform);
                break;
            case 2:
                Vector3 adj2 = matchImage.transform.position - new Vector3(0f, vert, 0f);
                Instantiate(starPrefab, adj2 + new Vector3(-0.5f * spacer, 0f, 0f), Quaternion.identity, matchImage.transform);
                Instantiate(starPrefab, adj2 + new Vector3(0.5f * spacer, 0f, 0f), Quaternion.identity, matchImage.transform);
                break;
            case 3:
                Vector3 adj3 = matchImage.transform.position - new Vector3(0f, vert, 0f);
                Instantiate(starPrefab, adj3 + new Vector3(-1f * spacer, 0f, 0f), Quaternion.identity, matchImage.transform);
                Instantiate(starPrefab, adj3, Quaternion.identity, matchImage.transform);
                Instantiate(starPrefab, adj3 + new Vector3(1f * spacer, 0f, 0f), Quaternion.identity, matchImage.transform);
                break;
            case 4:
                Vector3 adj4 = matchImage.transform.position - new Vector3(0f, vert, 0f);
                Instantiate(starPrefab, adj4 + new Vector3(-1.5f * spacer, 0f, 0f), Quaternion.identity, matchImage.transform);
                Instantiate(starPrefab, adj4 + new Vector3(-0.5f * spacer, 0f, 0f), Quaternion.identity, matchImage.transform);
                Instantiate(starPrefab, adj4 + new Vector3(0.5f * spacer, 0f, 0f), Quaternion.identity, matchImage.transform);
                Instantiate(starPrefab, adj4 + new Vector3(1.5f * spacer, 0f, 0f), Quaternion.identity, matchImage.transform);
                break;
            case 5:
                Vector3 adj5 = matchImage.transform.position - new Vector3(0f, vert, 0f);
                Instantiate(starPrefab, adj5 + new Vector3(-2f * spacer, 0f, 0f), Quaternion.identity, matchImage.transform);
                Instantiate(starPrefab, adj5 + new Vector3(-1f * spacer, 0f, 0f), Quaternion.identity, matchImage.transform);
                Instantiate(starPrefab, adj5, Quaternion.identity, matchImage.transform);
                Instantiate(starPrefab, adj5 + new Vector3(1f * spacer, 0f, 0f), Quaternion.identity, matchImage.transform);
                Instantiate(starPrefab, adj5 + new Vector3(2f * spacer, 0f, 0f), Quaternion.identity, matchImage.transform);
                break;
        }


        recordTextRight.text = numWins.ToString() + "-" + numLosses.ToString();

        int wins = 0;
        int losses = 0;
        foreach (bool res in lastTenResults)
        {
            if (res)
            {
                wins++;
            }
            else
            {
                losses++;
            }
        }
        lastTenTextRight.text = wins.ToString() + "-" + losses.ToString();

        if (activeStreak >= 0)
        {
            streakTextRight.text = "W" + activeStreak.ToString();
        }
        else
        {
            streakTextRight.text = "L" + (-1 * activeStreak).ToString();
        }

        gnightImageRight.sprite = gnightSprites[opponentGnight - 1];



        usernameTextRight.gameObject.SetActive(true);
        titleTextRight.gameObject.SetActive(true);
        ratingTextRight.gameObject.SetActive(true);
        recordTextRight.gameObject.SetActive(true);
        lastTenTextRight.gameObject.SetActive(true);
        streakTextRight.gameObject.SetActive(true);
        gnightImageRight.gameObject.SetActive(true);

        //User sprite depending on colour
        if (color == "pink")
        {
            userImage.sprite = pinkUserSprite;
            matchImage.sprite = greenUserSprite;
        }
        else if (color == "green")
        {
            userImage.sprite = greenUserSprite;
            matchImage.sprite = pinkUserSprite;
        }

        //Do versus animation
        StartCoroutine(VersusAnimation(AirHockeyDetails.ranking, ranking, color));
    }

    private IEnumerator VersusAnimation(int playerRanking, int opponentRanking, string playerColor)
    {
        versusImage.gameObject.SetActive(true);

        Color color = versusImage.color;
        color.a = 0f;
        versusImage.color = color;


        float t = 0f;
        float d = Mathf.Max(0.0001f, duration);

        while (t < d)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / d);         // 0..1
            float w = easing.Evaluate(u);           // eased 0..1

            versusImage.transform.localScale = Vector3.LerpUnclamped(new Vector3(0.1f, 0.1f, 0.1f), new Vector3(4f, 4f, 4f), w);

            color.a = w;
            versusImage.color = color;

            yield return null;
        }

        while (t < d)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / d);         // 0..1
            float w = 1f - easing.Evaluate(u);           // eased 0..1

            versusImage.transform.localScale = Vector3.LerpUnclamped(new Vector3(4f, 4f, 4f), new Vector3(1f, 1f, 1f), w);

            yield return null;
        }

        versusImage.transform.localScale = new Vector3(1f, 1f, 1f);

        yield return new WaitForSeconds(2f);

        //Call back to GameLiftClient to switch scene for client
        usernameTextRight.gameObject.SetActive(true);
        titleTextRight.gameObject.SetActive(true);
        ratingTextRight.gameObject.SetActive(true);
        recordTextRight.gameObject.SetActive(true);
        lastTenTextRight.gameObject.SetActive(true);
        streakTextRight.gameObject.SetActive(true);
        gnightImageRight.gameObject.SetActive(true);

        FindObjectOfType<GameLiftClient>().LoadGameScene(usernameTextLeft.text, titleTextLeft.text, ratingTextLeft.text, recordTextLeft.text, lastTenTextLeft.text, streakTextLeft.text, gnightImageLeft.sprite,
            usernameTextRight.text, titleTextRight.text, ratingTextRight.text, recordTextRight.text, lastTenTextRight.text, streakTextRight.text, gnightImageRight.sprite, playerRanking, opponentRanking, playerColor);

        _running = null;
    }

    public void MatchmakingCompleted()
    {
        playerReadiness = 2;

        matchImage.sprite = matchFoundSprite;
    }
}
