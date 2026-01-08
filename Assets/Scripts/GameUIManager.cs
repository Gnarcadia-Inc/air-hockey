using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class GameUIManager : MonoBehaviour
{
    public Image leftMatchupImage;
    public Image rightMatchupImage;

    public Image homeGnightImage;
    public TextMeshProUGUI homeUsername;
    public TextMeshProUGUI homeTitle;
    public TextMeshProUGUI homeRating;
    public TextMeshProUGUI homeRecord;
    public TextMeshProUGUI homeLast;
    public TextMeshProUGUI homeStreak;

    public Image awayGnightImage;
    public TextMeshProUGUI awayUsername;
    public TextMeshProUGUI awayTitle;
    public TextMeshProUGUI awayRating;
    public TextMeshProUGUI awayRecord;
    public TextMeshProUGUI awayLast;
    public TextMeshProUGUI awayStreak;

    public Image wagerImage;
    public TextMeshProUGUI wagerAmount;

    public Image indicatorImage;

    public Sprite[] backSprites;

    public GameObject starPrefab;

    [Min(0.0001f)] private float duration = 0.5f;
    private AnimationCurve easing = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private Vector3 promptStartPosition = new Vector3(-200f, -200f, 0f);
    private Vector3 promptEndPosition = new Vector3(-200f, 0f, 0f);

    public Image endPanel;
    public TextMeshProUGUI resultText;

    public Image endLeftBack;
    public RectTransform endLeftHighlight;
    public Image endLeftGnightImage;
    public TextMeshProUGUI endLeftUsername;
    public TextMeshProUGUI endLeftTitle;
    public TextMeshProUGUI endLeftRating;
    public TextMeshProUGUI endLeftRecord;
    public TextMeshProUGUI endLeftLast;
    public TextMeshProUGUI endLeftStreak;

    public Image endRightBack;
    public RectTransform endRightHighlight;
    public Image endRightGnightImage;
    public TextMeshProUGUI endRightUsername;
    public TextMeshProUGUI endRightTitle;
    public TextMeshProUGUI endRightRating;
    public TextMeshProUGUI endRightRecord;
    public TextMeshProUGUI endRightLast;
    public TextMeshProUGUI endRightStreak;

    public TextMeshProUGUI rematchTitle;
    public Image acceptButton;
    public Image acceptRing;
    public Image declineButton;
    public Image declineRing;

    public Sprite acceptButtonGreenSprite;
    public Sprite acceptRingGreenSprite;
    public Sprite acceptButtonGreySprite;
    public Sprite acceptRingGreySprite;
    public Sprite acceptButtonWhiteSprite;
    public Sprite acceptRingWhiteSprite;
    public Sprite acceptButtonYellowSprite;
    public Sprite acceptRingYellowSprite;

    private Vector2 resultStartAnchoredPosition = new Vector2(0f, 300f);
    private Vector2 resultEndAnchoredPosition = new Vector2(0f, 400f);
    private Vector2 startAnchoredPosition = new Vector2(0f, -200f);
    private Vector2 winnerAnchoredPosition = new Vector2(0f, 0f);
    private Vector2 offsetAnchoredPosition = new Vector2(100f, -200f);
    private Vector2 flippedAnchoredPosition = new Vector2(-100f, -200f);
    private Vector2 endAnchoredPosition = new Vector2(-100f, -50f);
    private Vector2 rematchStartAnchoredPosition = new Vector2(0f, -400f);
    private Vector2 rematchEndAnchoredPosition = new Vector2(0f, -300f);

    private Vector3 startScale = new Vector3(2f, 2f, 2f);
    private Vector3 endScale = new Vector3(1f, 1f, 1f);
    private Vector3 highlightStartScale = new Vector3(2f, 2f, 2f);
    private Vector3 highlightEndScale = new Vector3(1f, 1f, 1f);

    public Sprite pinkUserSprite;
    public Sprite greenUserSprite;

    private float timerDuration = 10f;

    private bool rematchAllowedFlag = true;
    private bool declineAllowedFlag = true;

    public Image versusImage;


    public void BackButton()
    {

    }

    public void AcceptRematchButton()
    {
        if (rematchAllowedFlag)
        {
            FindObjectOfType<GameLiftClient>().AcceptRematchButton();

            rematchAllowedFlag = false;
            declineAllowedFlag = false;

            acceptButton.sprite = acceptButtonYellowSprite;
            acceptRing.sprite = acceptRingYellowSprite;
        }
    }

    public void DeclineRematchButton()
    {
        if (declineAllowedFlag)
        {
            FindObjectOfType<GameLiftClient>().DeclineRematchButton();
        }
    }

    public void ReceiveRematchDecline()
    {
        rematchAllowedFlag = false;
        declineAllowedFlag = true;

        acceptButton.sprite = acceptButtonGreySprite;
        acceptRing.sprite = acceptRingGreySprite;
    }

    public void ReceiveRematchAccept()
    {
        acceptButton.sprite = acceptButtonGreenSprite;
        acceptRing.sprite = acceptRingGreenSprite;
    }

    private IEnumerator EndScreenAnimation(int winnerId, bool rematchAllowed)
    {
        rematchAllowedFlag = rematchAllowed;

        if (rematchAllowed)
        {
            acceptButton.sprite = acceptButtonWhiteSprite;
            acceptRing.sprite = acceptRingWhiteSprite;
        }
        else
        {
            acceptButton.sprite = acceptButtonGreySprite;
            acceptRing.sprite = acceptRingGreySprite;
        }

        rematchTitle.gameObject.SetActive(false);
        acceptButton.gameObject.SetActive(false);
        acceptRing.gameObject.SetActive(false);
        declineButton.gameObject.SetActive(false);
        declineRing.gameObject.SetActive(false);

        versusImage.gameObject.SetActive(false);


        if (winnerId == UserDetails.userProfileId)
        {
            StartCoroutine(WinnerHighlightAnimation(true));

            resultText.gameObject.SetActive(false);
            resultText.text = "VICTORY";

            endLeftBack.gameObject.SetActive(false);
            endLeftHighlight.gameObject.SetActive(false);
            endRightBack.gameObject.SetActive(false);
            endRightHighlight.gameObject.SetActive(false);

            Color color = endPanel.color;
            color.a = 0f;
            endPanel.color = color;
            endPanel.gameObject.SetActive(true);


            //FADE IN PANEL WHILE SLIDING UI ELEMENTS UP
            RectTransform rect = resultText.GetComponent<RectTransform>();
            rect.anchoredPosition = resultStartAnchoredPosition;

            RectTransform endLeftBackRect = endLeftBack.GetComponent<RectTransform>();
            RectTransform endLeftHighlightRect = endLeftHighlight.GetComponent<RectTransform>();

            endLeftBackRect.anchoredPosition = startAnchoredPosition;
            endLeftHighlightRect.anchoredPosition = startAnchoredPosition;
            endLeftBackRect.localScale = startScale;
            endLeftHighlightRect.localScale = highlightStartScale;

            resultText.gameObject.SetActive(true);
            endLeftBack.gameObject.SetActive(true);
            endLeftHighlight.gameObject.SetActive(true);


            float t = 0f;
            float d = Mathf.Max(0.0001f, duration);

            while (t < d)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / d);         // 0..1
                float w = easing.Evaluate(u);           // eased 0..1

                rect.anchoredPosition = Vector2.LerpUnclamped(resultStartAnchoredPosition, resultEndAnchoredPosition, w);
                endLeftBackRect.anchoredPosition = Vector2.LerpUnclamped(startAnchoredPosition, winnerAnchoredPosition, w);
                endLeftHighlightRect.anchoredPosition = Vector2.LerpUnclamped(startAnchoredPosition, winnerAnchoredPosition, w);

                color.a = w;
                endPanel.color = color;

                yield return null;
            }

            rect.anchoredPosition = resultEndAnchoredPosition;
            endLeftBackRect.anchoredPosition = winnerAnchoredPosition;
            endLeftHighlightRect.anchoredPosition = winnerAnchoredPosition;

            color.a = 1f;
            endPanel.color = color;

            yield return new WaitForSeconds(2f);

            //AFTER DELAY SHRINK OUT WINNER BACK AND MOVE TO LEFT WHILE BRINGING IN OTHER UI ELEMENTS

            RectTransform endRightBackRect = endRightBack.GetComponent<RectTransform>();
            RectTransform endRightHighlightRect = endRightHighlight.GetComponent<RectTransform>();

            endRightBackRect.anchoredPosition = offsetAnchoredPosition;
            endRightHighlightRect.localScale = endScale;

            RectTransform rematchTitleRect = rematchTitle.GetComponent<RectTransform>();
            RectTransform acceptButtonRect = acceptButton.GetComponent<RectTransform>();
            RectTransform acceptRingRect = acceptRing.GetComponent<RectTransform>();
            RectTransform declineButtonRect = declineButton.GetComponent<RectTransform>();
            RectTransform declineRingRect = declineRing.GetComponent<RectTransform>();

            rematchTitleRect.anchoredPosition = rematchStartAnchoredPosition;
            acceptButtonRect.anchoredPosition = rematchStartAnchoredPosition + new Vector2(-100f, -25f);
            acceptRingRect.anchoredPosition = rematchStartAnchoredPosition + new Vector2(-100f, -25f);
            declineButtonRect.anchoredPosition = rematchStartAnchoredPosition + new Vector2(100f, -25f);
            declineRingRect.anchoredPosition = rematchStartAnchoredPosition + new Vector2(100f, -25f);

            color.a = 0f;
            endRightBack.color = color;
            rematchTitle.color = color;
            acceptButton.color = color;
            acceptRing.color = color;
            declineButton.color = color;
            declineRing.color = color;

            endRightBack.gameObject.SetActive(true);
            rematchTitle.gameObject.SetActive(true);
            acceptButton.gameObject.SetActive(true);
            acceptRing.gameObject.SetActive(true);
            declineButton.gameObject.SetActive(true);
            declineRing.gameObject.SetActive(true);

            t = 0f;
            d = Mathf.Max(0.0001f, duration);

            while (t < d)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / d);         // 0..1
                float w = easing.Evaluate(u);           // eased 0..1

                endLeftBackRect.anchoredPosition = Vector2.LerpUnclamped(winnerAnchoredPosition, endAnchoredPosition, w);
                endLeftHighlightRect.anchoredPosition = Vector2.LerpUnclamped(winnerAnchoredPosition, endAnchoredPosition, w);
                endLeftBackRect.localScale = Vector3.LerpUnclamped(startScale, endScale, w);
                endLeftHighlightRect.localScale = Vector3.LerpUnclamped(highlightStartScale, highlightEndScale, w);

                endRightBackRect.anchoredPosition = Vector2.LerpUnclamped(offsetAnchoredPosition, -endAnchoredPosition, w);
                rematchTitleRect.anchoredPosition = Vector2.LerpUnclamped(rematchStartAnchoredPosition, rematchEndAnchoredPosition, w);
                acceptButtonRect.anchoredPosition = Vector2.LerpUnclamped(rematchStartAnchoredPosition + new Vector2(-100f, -25f), rematchEndAnchoredPosition + new Vector2(-100f, -25f), w);
                acceptRingRect.anchoredPosition = Vector2.LerpUnclamped(rematchStartAnchoredPosition + new Vector2(-100f, -25f), rematchEndAnchoredPosition + new Vector2(-100f, -25f), w);
                declineButtonRect.anchoredPosition = Vector2.LerpUnclamped(rematchStartAnchoredPosition + new Vector2(100f, -25f), rematchEndAnchoredPosition + new Vector2(100f, -25f), w);
                declineRingRect.anchoredPosition = Vector2.LerpUnclamped(rematchStartAnchoredPosition + new Vector2(100f, -25f), rematchEndAnchoredPosition + new Vector2(100f, -25f), w);

                color.a = w;
                endRightBack.color = color;
                rematchTitle.color = color;
                acceptButton.color = color;
                acceptRing.color = color;
                declineButton.color = color;
                declineRing.color = color;

                yield return null;
            }

            endLeftBackRect.anchoredPosition = endAnchoredPosition;
            endLeftHighlightRect.anchoredPosition = endAnchoredPosition;
            endLeftBackRect.localScale = endScale;
            endLeftHighlightRect.localScale = highlightEndScale;

            endRightBackRect.anchoredPosition = -endAnchoredPosition;
            rematchTitleRect.anchoredPosition = rematchEndAnchoredPosition;
            acceptButtonRect.anchoredPosition = rematchEndAnchoredPosition + new Vector2(-100f, -25f);
            acceptRingRect.anchoredPosition = rematchEndAnchoredPosition + new Vector2(-100f, -25f);
            declineButtonRect.anchoredPosition = rematchEndAnchoredPosition + new Vector2(100f, -25f);
            declineRingRect.anchoredPosition = rematchEndAnchoredPosition + new Vector2(100f, -25f);

            color.a = 1f;
            endRightBack.color = color;
            rematchTitle.color = color;
            acceptButton.color = color;
            acceptRing.color = color;
            declineButton.color = color;
            declineRing.color = color;

        }
        else
        {
            StartCoroutine(WinnerHighlightAnimation(false));

            resultText.gameObject.SetActive(false);
            resultText.text = "DEFEAT";

            endRightBack.gameObject.SetActive(false);
            endRightHighlight.gameObject.SetActive(false);
            endLeftBack.gameObject.SetActive(false);
            endLeftHighlight.gameObject.SetActive(false);

            Color color = endPanel.color;
            color.a = 0f;
            endPanel.color = color;
            endPanel.gameObject.SetActive(true);


            //FADE IN PANEL WHILE SLIDING UI ELEMENTS UP
            RectTransform rect = resultText.GetComponent<RectTransform>();
            rect.anchoredPosition = resultStartAnchoredPosition;

            RectTransform endRightBackRect = endRightBack.GetComponent<RectTransform>();
            RectTransform endRightHighlightRect = endRightHighlight.GetComponent<RectTransform>();

            endRightBackRect.anchoredPosition = startAnchoredPosition;
            endRightHighlightRect.anchoredPosition = startAnchoredPosition;
            endRightBackRect.localScale = startScale;
            endRightHighlightRect.localScale = highlightStartScale;

            resultText.gameObject.SetActive(true);
            endRightBack.gameObject.SetActive(true);
            endRightHighlight.gameObject.SetActive(true);


            float t = 0f;
            float d = Mathf.Max(0.0001f, duration);

            while (t < d)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / d);         // 0..1
                float w = easing.Evaluate(u);           // eased 0..1

                rect.anchoredPosition = Vector2.LerpUnclamped(resultStartAnchoredPosition, resultEndAnchoredPosition, w);
                endRightBackRect.anchoredPosition = Vector2.LerpUnclamped(startAnchoredPosition, winnerAnchoredPosition, w);
                endRightHighlightRect.anchoredPosition = Vector2.LerpUnclamped(startAnchoredPosition, winnerAnchoredPosition, w);

                color.a = w;
                endPanel.color = color;

                yield return null;
            }

            rect.anchoredPosition = resultEndAnchoredPosition;
            endRightBackRect.anchoredPosition = winnerAnchoredPosition;
            endRightHighlightRect.anchoredPosition = winnerAnchoredPosition;

            color.a = 1f;
            endPanel.color = color;

            yield return new WaitForSeconds(2f);

            //AFTER DELAY SHRINK OUT WINNER BACK AND MOVE TO LEFT WHILE BRINGING IN OTHER UI ELEMENTS

            RectTransform endLeftBackRect = endLeftBack.GetComponent<RectTransform>();
            RectTransform endLeftHighlightRect = endLeftHighlight.GetComponent<RectTransform>();

            endLeftBackRect.anchoredPosition = offsetAnchoredPosition;
            endLeftHighlightRect.localScale = endScale;

            RectTransform rematchTitleRect = rematchTitle.GetComponent<RectTransform>();
            RectTransform acceptButtonRect = acceptButton.GetComponent<RectTransform>();
            RectTransform acceptRingRect = acceptRing.GetComponent<RectTransform>();
            RectTransform declineButtonRect = declineButton.GetComponent<RectTransform>();
            RectTransform declineRingRect = declineRing.GetComponent<RectTransform>();

            rematchTitleRect.anchoredPosition = rematchStartAnchoredPosition;
            acceptButtonRect.anchoredPosition = rematchStartAnchoredPosition + new Vector2(-100f, -25f);
            acceptRingRect.anchoredPosition = rematchStartAnchoredPosition + new Vector2(-100f, -25f);
            declineButtonRect.anchoredPosition = rematchStartAnchoredPosition + new Vector2(100f, -25f);
            declineRingRect.anchoredPosition = rematchStartAnchoredPosition + new Vector2(100f, -25f);

            color.a = 0f;
            endLeftBack.color = color;
            rematchTitle.color = color;
            acceptButton.color = color;
            acceptRing.color = color;
            declineButton.color = color;
            declineRing.color = color;

            endLeftBack.gameObject.SetActive(true);
            rematchTitle.gameObject.SetActive(true);
            acceptButton.gameObject.SetActive(true);
            acceptRing.gameObject.SetActive(true);
            declineButton.gameObject.SetActive(true);
            declineRing.gameObject.SetActive(true);

            t = 0f;
            d = Mathf.Max(0.0001f, duration);

            while (t < d)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / d);         // 0..1
                float w = easing.Evaluate(u);           // eased 0..1

                endRightBackRect.anchoredPosition = Vector2.LerpUnclamped(winnerAnchoredPosition, -endAnchoredPosition, w);
                endRightHighlightRect.anchoredPosition = Vector2.LerpUnclamped(winnerAnchoredPosition, -endAnchoredPosition, w);
                endRightBackRect.localScale = Vector3.LerpUnclamped(startScale, endScale, w);
                endRightHighlightRect.localScale = Vector3.LerpUnclamped(highlightStartScale, highlightEndScale, w);

                endLeftBackRect.anchoredPosition = Vector2.LerpUnclamped(flippedAnchoredPosition, endAnchoredPosition, w);
                rematchTitleRect.anchoredPosition = Vector2.LerpUnclamped(rematchStartAnchoredPosition, rematchEndAnchoredPosition, w);
                acceptButtonRect.anchoredPosition = Vector2.LerpUnclamped(rematchStartAnchoredPosition + new Vector2(-100f, -25f), rematchEndAnchoredPosition + new Vector2(-100f, -25f), w);
                acceptRingRect.anchoredPosition = Vector2.LerpUnclamped(rematchStartAnchoredPosition + new Vector2(-100f, -25f), rematchEndAnchoredPosition + new Vector2(-100f, -25f), w);
                declineButtonRect.anchoredPosition = Vector2.LerpUnclamped(rematchStartAnchoredPosition + new Vector2(100f, -25f), rematchEndAnchoredPosition + new Vector2(100f, -25f), w);
                declineRingRect.anchoredPosition = Vector2.LerpUnclamped(rematchStartAnchoredPosition + new Vector2(100f, -25f), rematchEndAnchoredPosition + new Vector2(100f, -25f), w);

                color.a = w;
                endLeftBack.color = color;
                rematchTitle.color = color;
                acceptButton.color = color;
                acceptRing.color = color;
                declineButton.color = color;
                declineRing.color = color;

                yield return null;
            }

            endRightBackRect.anchoredPosition = -endAnchoredPosition;
            endRightHighlightRect.anchoredPosition = -endAnchoredPosition;
            endRightBackRect.localScale = endScale;
            endRightHighlightRect.localScale = highlightEndScale;

            endLeftBackRect.anchoredPosition = endAnchoredPosition;
            rematchTitleRect.anchoredPosition = rematchEndAnchoredPosition;
            acceptButtonRect.anchoredPosition = rematchEndAnchoredPosition + new Vector2(-100f, -25f);
            acceptRingRect.anchoredPosition = rematchEndAnchoredPosition + new Vector2(-100f, -25f);
            declineButtonRect.anchoredPosition = rematchEndAnchoredPosition + new Vector2(100f, -25f);
            declineRingRect.anchoredPosition = rematchEndAnchoredPosition + new Vector2(100f, -25f);

            color.a = 1f;
            endLeftBack.color = color;
            rematchTitle.color = color;
            acceptButton.color = color;
            acceptRing.color = color;
            declineButton.color = color;
            declineRing.color = color;
        }

        StartCoroutine(AcceptTimerAnimation());

        yield return new WaitForSeconds(2f);

        //DELAY 

        //USE FOR STATS UPDATE CALCULATION + ANIMATION
        //endLeftTitle;
        //endLeftRating;
        //endLeftLevel;
        //endLeftRecord;
        //endLeftLast;
        //endLeftStreak;

        //USE FOR STATS UPDATE CALCULATION + ANIMATION
        //endRightTitle;
        //endRightRating;
        //endRightLevel;
        //endRightRecord;
        //endRightLast;
        //endRightStreak;
    }

    public IEnumerator VersusAnimation()
    {
        versusImage.gameObject.SetActive(true);
        resultText.text = "REMATCH";
        rematchTitle.gameObject.SetActive(false);
        acceptButton.gameObject.SetActive(false);
        acceptRing.gameObject.SetActive(false);
        declineButton.gameObject.SetActive(false);
        declineRing.gameObject.SetActive(false);

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
    }

    public IEnumerator WinnerHighlightAnimation(bool leftFlag)
    {
        RectTransform rect = new RectTransform();
        if (leftFlag)
        {
            rect = endLeftHighlight.GetComponent<RectTransform>();
        }
        else
        {
            rect = endRightHighlight.GetComponent<RectTransform>();
        }

        float min = Mathf.Clamp01(0.9f);
        float max = 1f;
        float amp = (max - min) * 0.5f;           // 0.05 for 0.9↔1.0
        float mid = min + amp;                    // 0.95
        float omega = Mathf.PI * 2f * Mathf.Max(0.0001f, 1.5f);

        float t0 = Time.time;

        Vector3 originalScale = new Vector3(1f, 1f, 1f);

        while (true)
        {

            float t = Time.time - t0;
            float s = mid + amp * Mathf.Sin(omega * t);

            rect.localScale = originalScale * s;

            yield return null;
        }
    }

    public IEnumerator AcceptTimerAnimation()
    {
        float t = 0f;
        acceptRing.fillAmount = 1f;

        while (t < timerDuration)
        {
            t += Time.deltaTime;
            acceptRing.fillAmount = Mathf.Lerp(1f, 0f, t / timerDuration);
            yield return null;
        }

        acceptRing.fillAmount = 0f;

        rematchAllowedFlag = false;
    }

    public void InitEndScreen(int winnerId, bool rematchAllowed)
    {
        StartCoroutine(EndScreenAnimation(winnerId, rematchAllowed));
    }

    public void InitGameUI(string usernameTextLeft, string titleTextLeft, string ratingTextLeft, string recordTextLeft, string lastTenTextLeft, string streakTextLeft, Sprite gnightImageLeft,
        string usernameTextRight, string titleTextRight, string ratingTextRight, string recordTextRight, string lastTenTextRight, string streakTextRight, Sprite gnightImageRight,
        int playerRanking, int opponentRanking, string playerColor)
    {
        endPanel.gameObject.SetActive(false);

        if (playerColor == "pink")
        {
            leftMatchupImage.sprite = backSprites[0];
            rightMatchupImage.sprite = backSprites[1];
            endLeftBack.sprite = pinkUserSprite;
            endRightBack.sprite = greenUserSprite;
        }
        else if (playerColor == "green")
        {
            leftMatchupImage.sprite = backSprites[2];
            rightMatchupImage.sprite = backSprites[3];
            endLeftBack.sprite = greenUserSprite;
            endRightBack.sprite = pinkUserSprite;
        }


        homeGnightImage.sprite = gnightImageRight;
        endRightGnightImage.sprite = gnightImageRight;
        homeUsername.text = usernameTextRight;
        endRightUsername.text = usernameTextRight;
        homeTitle.text = titleTextRight;
        endRightTitle.text = titleTextRight;
        homeRating.text = ratingTextRight;
        endRightRating.text = ratingTextRight;
        //homeLevel;
        //endRightLevel;
        homeRecord.text = recordTextRight;
        endRightRecord.text = recordTextRight;
        homeLast.text = lastTenTextRight;
        endRightLast.text = lastTenTextRight;
        homeStreak.text = streakTextRight;
        endRightStreak.text = streakTextRight;

        awayGnightImage.sprite = gnightImageLeft;
        endLeftGnightImage.sprite = gnightImageLeft;
        awayUsername.text = usernameTextLeft;
        endLeftUsername.text = usernameTextLeft;
        awayTitle.text = titleTextLeft;
        endLeftTitle.text = titleTextLeft;
        awayRating.text = ratingTextLeft;
        endLeftRating.text = ratingTextLeft;
        //awayLevel;
        //endLeftLevel;
        awayRecord.text = recordTextLeft;
        endLeftRecord.text = recordTextLeft;
        awayLast.text = lastTenTextLeft;
        endLeftLast.text = lastTenTextLeft;
        awayStreak.text = streakTextLeft;
        endLeftStreak.text = streakTextLeft;

        float horz = -75f;
        float vert = -115f;
        float spacer = 35f;

        //SWITCH TO MAKE MORE EFFICIENT & ADD END PANEL STARS
        switch (playerRanking)
        {
            case 1:
                Vector3 adj1 = awayGnightImage.transform.position - new Vector3(vert, horz, 0f);
                Instantiate(starPrefab, adj1, Quaternion.identity, awayGnightImage.transform);
                break;
            case 2:
                Vector3 adj2 = awayGnightImage.transform.position - new Vector3(vert, horz, 0f);
                Instantiate(starPrefab, adj2 + new Vector3(0f, 0.5f * spacer, 0f), Quaternion.identity, awayGnightImage.transform);
                Instantiate(starPrefab, adj2 + new Vector3(0f, -0.5f * spacer, 0f), Quaternion.identity, awayGnightImage.transform);
                break;
            case 3:
                Vector3 adj3 = awayGnightImage.transform.position - new Vector3(vert, horz, 0f);
                Instantiate(starPrefab, adj3 + new Vector3(0f, 1f * spacer, 0f), Quaternion.identity, awayGnightImage.transform);
                Instantiate(starPrefab, adj3, Quaternion.identity, awayGnightImage.transform);
                Instantiate(starPrefab, adj3 + new Vector3(0f, -1f * spacer, 0f), Quaternion.identity, awayGnightImage.transform);
                break;
            case 4:
                Vector3 adj4 = awayGnightImage.transform.position - new Vector3(vert, horz, 0f);
                Instantiate(starPrefab, adj4 + new Vector3(0f, 1.5f * spacer, 0f), Quaternion.identity, awayGnightImage.transform);
                Instantiate(starPrefab, adj4 + new Vector3(0f, 0.5f * spacer, 0f), Quaternion.identity, awayGnightImage.transform);
                Instantiate(starPrefab, adj4 + new Vector3(0f, -0.5f * spacer, 0f), Quaternion.identity, awayGnightImage.transform);
                Instantiate(starPrefab, adj4 + new Vector3(0f, -1.5f * spacer, 0f), Quaternion.identity, awayGnightImage.transform);
                break;
            case 5:
                Vector3 adj5 = awayGnightImage.transform.position - new Vector3(vert, horz, 0f);
                Instantiate(starPrefab, adj5 + new Vector3(0f, 2f * spacer, 0f), Quaternion.identity, awayGnightImage.transform);
                Instantiate(starPrefab, adj5 + new Vector3(0f, 1f * spacer, 0f), Quaternion.identity, awayGnightImage.transform);
                Instantiate(starPrefab, adj5, Quaternion.identity, awayGnightImage.transform);
                Instantiate(starPrefab, adj5 + new Vector3(0f, -1f * spacer, 0f), Quaternion.identity, awayGnightImage.transform);
                Instantiate(starPrefab, adj5 + new Vector3(0f, -2f * spacer, 0f), Quaternion.identity, awayGnightImage.transform);
                break;
        }

        switch (opponentRanking)
        {
            case 1:
                Vector3 adj1 = homeGnightImage.transform.position - new Vector3(vert, horz, 0f);
                Instantiate(starPrefab, adj1, Quaternion.identity, homeGnightImage.transform);
                break;
            case 2:
                Vector3 adj2 = homeGnightImage.transform.position - new Vector3(vert, horz, 0f);
                Instantiate(starPrefab, adj2 + new Vector3(0f, 0.5f * spacer, 0f), Quaternion.identity, homeGnightImage.transform);
                Instantiate(starPrefab, adj2 + new Vector3(0f, -0.5f * spacer, 0f), Quaternion.identity, homeGnightImage.transform);
                break;
            case 3:
                Vector3 adj3 = homeGnightImage.transform.position - new Vector3(vert, horz, 0f);
                Instantiate(starPrefab, adj3 + new Vector3(0f, 1f * spacer, 0f), Quaternion.identity, homeGnightImage.transform);
                Instantiate(starPrefab, adj3, Quaternion.identity, homeGnightImage.transform);
                Instantiate(starPrefab, adj3 + new Vector3(0f, -1f * spacer, 0f), Quaternion.identity, homeGnightImage.transform);
                break;
            case 4:
                Vector3 adj4 = homeGnightImage.transform.position - new Vector3(vert, horz, 0f);
                Instantiate(starPrefab, adj4 + new Vector3(0f, 1.5f * spacer, 0f), Quaternion.identity, homeGnightImage.transform);
                Instantiate(starPrefab, adj4 + new Vector3(0f, 0.5f * spacer, 0f), Quaternion.identity, homeGnightImage.transform);
                Instantiate(starPrefab, adj4 + new Vector3(0f, -0.5f * spacer, 0f), Quaternion.identity, homeGnightImage.transform);
                Instantiate(starPrefab, adj4 + new Vector3(0f, -1.5f * spacer, 0f), Quaternion.identity, homeGnightImage.transform);
                break;
            case 5:
                Vector3 adj5 = homeGnightImage.transform.position - new Vector3(vert, horz, 0f);
                Instantiate(starPrefab, adj5 + new Vector3(0f, 2f * spacer, 0f), Quaternion.identity, homeGnightImage.transform);
                Instantiate(starPrefab, adj5 + new Vector3(0f, 1f * spacer, 0f), Quaternion.identity, homeGnightImage.transform);
                Instantiate(starPrefab, adj5, Quaternion.identity, homeGnightImage.transform);
                Instantiate(starPrefab, adj5 + new Vector3(0f, -1f * spacer, 0f), Quaternion.identity, homeGnightImage.transform);
                Instantiate(starPrefab, adj5 + new Vector3(0f, -2f * spacer, 0f), Quaternion.identity, homeGnightImage.transform);
                break;
        }

        //wagerImage;
        //wagerAmount;
    }


    public IEnumerator IndicatorAnimation(int indicatorIdx, bool opponentFlag)
    {
        indicatorImage.gameObject.SetActive(true);

        Color color = indicatorImage.color;
        color.a = 0f;
        indicatorImage.color = color;
        


        float t = 0f;
        float d = Mathf.Max(0.0001f, duration);

        while (t < d)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / d);         // 0..1
            float w = easing.Evaluate(u);           // eased 0..1

            indicatorImage.transform.localScale = Vector3.LerpUnclamped(new Vector3(0.1f, 0.1f, 0.1f), new Vector3(2f, 2f, 2f), w);

            color.a = w;
            indicatorImage.color = color;

            yield return null;
        }

        while (t < d)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / d);         // 0..1
            float w = 1f - easing.Evaluate(u);           // eased 0..1

            indicatorImage.transform.localScale = Vector3.LerpUnclamped(new Vector3(2f, 2f, 2f), new Vector3(1f, 1f, 1f), w);

            color.a = w;
            indicatorImage.color = color;

            yield return null;
        }

        indicatorImage.transform.localScale = new Vector3(1f, 1f, 1f);

        color.a = 0f;
        indicatorImage.color = color;

        indicatorImage.gameObject.SetActive(false);

        yield return null;
    }
}
