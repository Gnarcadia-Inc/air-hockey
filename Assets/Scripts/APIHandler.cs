using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

public class APIHandler : MonoBehaviour
{
    private string baseUrl = "https://5se3a45vc1.execute-api.us-west-2.amazonaws.com/relay";
    private string token;
    private int userProfileId;
    public int sessionId = -1;
    private float roundAmount;

    void Start()
    {
        token = GetQueryParam("token");

        Debug.LogError("token: " + token);

        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("Missing authentication token in URL");

            //Load "please sign in" scene
            //SceneManager.LoadScene("BlockedScene", LoadSceneMode.Single);

            return;
        }

        Debug.LogError("TOKEN FOUND");
    }

    private string GetQueryParam(string key)
    {
        var uri = new System.Uri(Application.absoluteURL);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        return query.Get(key);
    }

    public IEnumerator GetUserDetails()
    {

        string tempUrl = "https://api.gnarcadia.com";
        string url = $"{baseUrl}/user?token={token}";

        UnityWebRequest request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("User details fetch failed: " + request.error);

            //Load "please sign in" scene
            //SceneManager.LoadScene("BlockedScene", LoadSceneMode.Single);

            yield break;
        }

        var json = request.downloadHandler.text;
        var user = JsonUtility.FromJson<UserJSONDetails>(json);
        userProfileId = user.userProfileId;
        UserDetails.userProfileId = user.userProfileId;
        UserDetails.userName = user.userName;
        UserDetails.gnight = user.gnight;
        UserDetails.balance = user.balance;

        // Continue to next step
        StartCoroutine(GetAirHockeyData());
    }

    public IEnumerator GetAirHockeyData()
    {
        string tempUrl = "https://api.gnarcadia.com";
        string url = $"{baseUrl}/airhockey/AirHockeyStats/mine?userProfileId={UserDetails.userProfileId}";

        UnityWebRequest request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("User airhockey details fetch failed: " + request.error);

            yield break;
        }

        var json = request.downloadHandler.text;
        var user = JsonUtility.FromJson<AirHockeyJSONDetails>(json);

        AirHockeyDetails.rating = user.rating;
        AirHockeyDetails.ranking = user.ranking;
        AirHockeyDetails.lastTenResults = user.lastTenResults;
        AirHockeyDetails.activeStreak = user.activeStreak;
        AirHockeyDetails.numWins = user.numWins;
        AirHockeyDetails.numLosses = user.numLosses;
        AirHockeyDetails.totalWagered = user.totalWagered;
        AirHockeyDetails.recentNetEarningsEma = user.recentNetEarningsEma;

        FindObjectOfType<MenuManager>().InitUserDetails();
    }

    public IEnumerator GetOpponentAirHockeyData(int opponentProfileId, string opponentUsername, int opponentGnight, string color)
    {
        string tempUrl = "https://api.gnarcadia.com";
        string url = $"{baseUrl}/airhockey/AirHockeyStats/mine?userProfileId={opponentProfileId}";

        UnityWebRequest request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("User airhockey details fetch failed: " + request.error);

            yield break;
        }

        var json = request.downloadHandler.text;
        var user = JsonUtility.FromJson<AirHockeyJSONDetails>(json);

        FindObjectOfType<MenuManager>().MatchBackTwo(opponentProfileId, opponentUsername, opponentGnight, color, user.rating, user.ranking, user.lastTenResults,
            user.activeStreak, user.numWins, user.numLosses, user.totalWagered, user.recentNetEarningsEma);
    }

    public void ResolveAirHockeyMatch()
    {
        //MOVE TO SERVER
    }

    public void ResetSessionID()
    {
        sessionId = -1;
    }

    public IEnumerator StartGameSession(int userId, float wagerAmount)
    {
        string url = $"{baseUrl}/session/start";

        var payload = new SessionStartRequest
        {
            gameId = 7, // Air Hockey gameId
            userprofileId = userId,
            sessionLimit = wagerAmount,
            roundAmount = wagerAmount,
            reservedFundsLostOnSessionEnd = false
        };

        string jsonPayload = JsonUtility.ToJson(payload);
        UnityWebRequest request = new UnityWebRequest(url, "POST");

        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonPayload));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

         long httpCode = request.responseCode;
        string body = request.downloadHandler != null ? request.downloadHandler.text : null;

        // Success path
        if (request.result == UnityWebRequest.Result.Success && httpCode >= 200 && httpCode < 300)
        {
            var session = JsonUtility.FromJson<SessionStartResponse>(body);
            sessionId = session.sessionId;
            roundAmount = session.roundAmount;

            StartCoroutine(ReserveWager(roundAmount));
            yield break;
        }

        // Error path (400 etc): your server sends a JSON body — parse it
        if (httpCode == 400 && !string.IsNullOrEmpty(body))
        {
            ApiErrorResponse err = null;

            try
            {
                err = JsonUtility.FromJson<ApiErrorResponse>(body);
            }
            catch
            {
                // JsonUtility is picky; if parsing fails, fall back to generic handling
            }

            if (err != null)
            {
                switch (err.errorCode)
                {
                    case 405:
                        OnSessionLimitExceedsBalance(err);
                        yield break;

                    case 406:
                        OnRoundAmountExceedsBalance(err);
                        yield break;

                    default:
                        OnStartSessionGenericError(httpCode, body);
                        yield break;
                }
            }
        }

        OnStartSessionGenericError(httpCode, body);
    }


    public IEnumerator ReserveWager(float wager)
    {
        string url = $"{baseUrl}/wager/reserve";

        string jsonPayload = $"[{{\"sessionId\": {sessionId}}}]";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to reserve wager: " + request.error);
            // Handle retry or show error to user

            StartCoroutine(ResolveCurrentGameSession());
        }
        else
        {
            Debug.Log("Wager reserved. SessionID: " + sessionId);

            // Proceed to gameplay
            FindObjectOfType<MenuManager>().WagerRoutineFinished((int)wager);
        }
    }

    private IEnumerator ResolveCurrentGameSession()
    {
        string url = $"{baseUrl}/session/end";

        string jsonPayload = $"{{\"sessionId\": {sessionId}}}";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Failed to end session: " + request.error);
        }
        else
        {
            Debug.Log("Session ended.");
        }
    }

    public IEnumerator ResolveCurrentGameSessionAndWager()
    {
        var wager = new WagerResult
        {
            sessionId = sessionId,
            status = 2,
            winAmount = 0f
        };

        string url = $"{baseUrl}/wager/resolve";
        string jsonPayload = JsonConvert.SerializeObject(wager);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Failed to cancel wager: " + request.error);
        }
        else
        {
            Debug.Log("Wager cancelled: " + request.downloadHandler.text);
        }



        url = $"{baseUrl}/session/end";

        jsonPayload = $"{{\"sessionId\": {sessionId}}}";

        request = new UnityWebRequest(url, "POST");
        bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Failed to end session: " + request.error);
        }
        else
        {
            Debug.Log("Session ended.");
        }
    }

    private void OnSessionLimitExceedsBalance(ApiErrorResponse err)
    {
        Debug.LogWarning($"Session limit too high: {err.detail} (code {err.errorCode})");

        FindObjectOfType<MenuManager>().InsufficientFundsIndicator();
    }

    private void OnRoundAmountExceedsBalance(ApiErrorResponse err)
    {
        Debug.LogWarning($"Round amount too high: {err.detail} (code {err.errorCode})");
        FindObjectOfType<MenuManager>().InsufficientFundsIndicator();
    }

    private void OnStartSessionGenericError(long httpCode, string body)
    {
        Debug.LogError($"Start session failed. HTTP {httpCode}. Body: {body}");
    }

}

public class UserJSONDetails
{
    public int userProfileId;
    public string userName;
    public int gnight;
    public float balance;
}

public class AirHockeyJSONDetails
{
    public int userProfileId;
    public int rating;
    public int ranking;
    public bool[] lastTenResults;
    public int activeStreak;
    public int numWins;
    public int numLosses;
    public int totalWagered;
    public int recentNetEarningsEma;
}

public static class UserDetails
{
    public static int userProfileId;
    public static string userName;
    public static int gnight;
    public static float balance;
}

public static class AirHockeyDetails
{
    public static int userProfileId;
    public static int rating;
    public static int ranking;
    public static bool[] lastTenResults;
    public static int activeStreak;
    public static int numWins;
    public static int numLosses;
    public static int totalWagered;
    public static int recentNetEarningsEma;
}


[System.Serializable]
public class SessionStartRequest
{
    public int gameId;
    public int userprofileId;
    public float sessionLimit;
    public float roundAmount;
    public bool reservedFundsLostOnSessionEnd;
}

[System.Serializable]
public class SessionStartResponse
{
    public int sessionId;
    public float sessionLimit;
    public float roundAmount;
    public bool reservedFundsLostOnSessionEnd;
}

[System.Serializable]
public class ApiErrorResponse
{
    public string type;
    public string title;
    public int status;
    public string detail;
    public int errorCode;
    public string traceId;
}

[Serializable]
public class WagerResult
{
    public int sessionId;
    public int status;      // 1 = win, 0 = loss
    public float winAmount; // always included, 0 for losses
}
