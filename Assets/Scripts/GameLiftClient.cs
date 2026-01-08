using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NativeWebSocket; // Import the WebSocket library
using System.Text;
using System.Threading.Tasks;
using System;
using UnityEngine.SceneManagement;

public class GameLiftClient : MonoBehaviour
{
    private string apiKey; // Shouldnt be named apiKey (or exist) but dont want to waste time changing

    public string id = "default";
    public int teamNum = 0;
    public int playerNum = 0;
    private string lobbyName = "$5-"; //FOR TESTING
    public string fullLobbyName = "default";
    private string matchmakingTicketId;
    private string playerSessionId;
    private string serverIpAddress;
    private int serverPort;

    public GameObject canvasPrefab;

    public DateTime gameStartTime = default(DateTime);
    private int gamePlayerCount;
    public float gameDuration = 0f;
    public string gameMap;
    public string gameMode;
    private int gameWager = 10;
    public bool gameFakeCoinFlag;
    public int gameOdds;
    private string gameWeather = " ";

    public List<PlayerScore> sortedPlayers = new List<PlayerScore>();

    private WebSocket webSocket;

    private GameObject playerPrefab;
    public GameObject playerPrefabTemplate;
    public Dictionary<string, GameObject> otherPlayers = new Dictionary<string, GameObject>();

    private int playerSkinIndex = 0;

    private string primaryWeapon = "default";
    private string secondaryWeapon = "default";
    private string tertiaryWeapon = "default";
    private Dictionary<string, int> meleeWeapons = new Dictionary<string, int>();

    private bool isGameSceneLoaded = false;

    public GameObject muzzleFlashPrefab;
    public GameObject impactPrefab;
    public GameObject hitImpactPrefab;
    public GameObject killImpactPrefab;

    private bool pollMatchmakingFlag = false;

    private string lobbyDataUrl = "https://script.google.com/macros/s/AKfycbxSScK6MObvzd4U_wFq5vft_vsjnZaGnlDrdcvg9A4N4w49F___HsR-pcekURxFKuuR/exec";

    private Coroutine getPlayerCountCoroutine;
    private bool getPlayerCountFlag = true;

    private bool rematchAllowedFlag = true;

    public bool puckOnSide = false;

    private Coroutine puckUpdatesCoroutine = null;

    private bool homeFlag;

    void Awake()
    {
        // Ensure the GameObject is not destroyed when loading a new scene
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        //#if UNITY_WEBGL && !UNITY_EDITOR
        //WebGLInput.captureAllKeyboardInput = false;
        //#endif

        //id = Guid.NewGuid().ToString();
        apiKey = "arifialkov";

        id = UserDetails.userName;
        //playerSkinIndex = UserDetails.gnight; //LET USERS PICK ANY GNIGHT FOR NOW

        StartCoroutine(DelayedGetUser());


        if (id == "default")
        {
            return;
        }


        getPlayerCountCoroutine = StartCoroutine(GetPlayerCount());
    }

    private IEnumerator DelayedGetUser()
    {
        yield return new WaitForSeconds(3f);

        APIHandler existingHandler = FindObjectOfType<APIHandler>();
        if (existingHandler != null)
        {
            StartCoroutine(existingHandler.GetUserDetails());
        }
        else
        {
            UnityEngine.Debug.LogError("Get user details failed: API Handler not found.");
        }
    }

    public void SetWagerAmount(int wagerAmount)
    {
        gameWager = wagerAmount;
    }

    public void StartMatchmaking()
    {
        UnityEngine.Debug.LogError("StartMatchmaking()");


        StartCoroutine(StartMatchmakingCoroutine(UserDetails.userName, lobbyName));
    }

    private IEnumerator StartMatchmakingCoroutine(string playerId, string lobbyName)
    {
        // Debugging the start of matchmaking
        UnityEngine.Debug.LogError("StartMatchmakingCoroutine()");

        UnityEngine.Debug.LogError($"Player ID: {playerId}");
        UnityEngine.Debug.LogError($"Lobby Name: {lobbyName}");

        // Matchmaking API endpoint
        string apiUrl = "https://45gsbt8bm6.execute-api.us-west-2.amazonaws.com/test/matchmakingstart";

        // Creating the JSON payload
        var payload = new MatchmakingRequest
        {
            PlayerId = playerId,
            LobbyName = lobbyName,
            Skill = AirHockeyDetails.rating,
            GameId = 7
        };

        string jsonPayload = JsonUtility.ToJson(payload);

        // Setting up the UnityWebRequest with JSON payload
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        UnityEngine.Debug.LogError("Request Sent");

        // Sending the request
        yield return request.SendWebRequest();

        UnityEngine.Debug.LogError("After Request Returned");

        // Handling response
        if (request.result == UnityWebRequest.Result.Success)
        {
            // Parse the matchmaking response
            var response = JsonUtility.FromJson<MatchmakingResponse>(request.downloadHandler.text);
            matchmakingTicketId = response.ticketId;
            UnityEngine.Debug.Log($"Matchmaking started. Ticket ID: {matchmakingTicketId}");

            //StartCoroutine(PostPlayerJoin());

            pollMatchmakingFlag = true;
            // Poll the matchmaking status
            yield return StartCoroutine(PollMatchmakingStatus(matchmakingTicketId));
        }
        else
        {
            // Log matchmaking failure
            UnityEngine.Debug.LogError($"Matchmaking failed: {request.error}");
        }
    }


    private IEnumerator PollMatchmakingStatus(string ticketId)
    {
        string apiUrl = $"https://45gsbt8bm6.execute-api.us-west-2.amazonaws.com/test/matchmakingstatus";


        while (pollMatchmakingFlag)
        {
            UnityWebRequest request = UnityWebRequest.Get(apiUrl);
            request.url += $"?ticketId={ticketId}&playerId={UserDetails.userName}";//ADDED
            yield return request.SendWebRequest();

            UnityEngine.Debug.LogError("R1");

            MenuManager menuManager = FindObjectOfType<MenuManager>();

            if (request.result == UnityWebRequest.Result.Success)
            {
                UnityEngine.Debug.LogError("R2");
                var response = JsonUtility.FromJson<MatchmakingStatusResponse>(request.downloadHandler.text);

                UnityEngine.Debug.LogError(response.status); //JUST ADDED, BUILD NOW, GET RESPONSE AND ASK GPT

                if (response.status == "COMPLETED")
                {
                    UnityEngine.Debug.Log("Matchmaking complete. Connecting to server...");


                    menuManager.MatchmakingCompleted();

                    //playerSessionId = response.playerSessionId;
                    serverIpAddress = response.serverIpAddress;
                    serverPort = response.serverPort;

                    getPlayerCountFlag = false;

                    APIHandler existingHandler = FindObjectOfType<APIHandler>();
                    if (existingHandler != null)
                    {
                        //Switch to check for fake balance when both balances incorporated into arcade
                        existingHandler.ResetSessionID();
                        StartCoroutine(existingHandler.StartGameSession(UserDetails.userProfileId, gameWager));
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("Wager placement failed: API Handler not found.");
                        yield break;
                    }

                    ConnectToGameLiftServer();
                    yield break;
                }
                else if (response.status == "FAILED")
                {
                    UnityEngine.Debug.LogError("Matchmaking failed.");

                    menuManager.ResetPlayerReadiness();
                    menuManager.ResetExitReadyButton();

                    yield break;
                }

                UnityEngine.Debug.LogError("R3");
            }
            else
            {
                UnityEngine.Debug.LogError($"Polling failed: {request.error}");
                yield break;
            }

            UnityEngine.Debug.Log("LOOP");

            yield return new WaitForSeconds(5); // Poll every 5 seconds
        }
    }

    private async void ConnectToGameLiftServer()
    {
        //UnityEngine.Debug.Log($"Connecting to server at wss://{serverIpAddress}");

        // Initialize WebSocket connection
        webSocket = new WebSocket("wss://ws.airhockey.gnarcadia.com/");

        webSocket.OnOpen += () =>
        {
            UnityEngine.Debug.Log("WebSocket connection opened!");
            SendAuthMessage();
        };

        webSocket.OnMessage += (bytes) =>
        {
            string message = Encoding.UTF8.GetString(bytes);
            HandleServerMessage(message);
        };

        webSocket.OnClose += (e) =>
        {
            UnityEngine.Debug.Log($"WebSocket connection closed with code: {e}");
        };

        webSocket.OnError += (e) =>
        {
            UnityEngine.Debug.LogError($"WebSocket error: {e}");
        };

        await webSocket.Connect();
    }

    private IEnumerator WaitForSessionId()
    {
        while (FindObjectOfType<APIHandler>().sessionId == -1)
        {
            yield return null;
        }

        SendAuthMessageAsync();
    }

    private void SendAuthMessage()
    {
        StartCoroutine(WaitForSessionId());
    }

    private async void SendAuthMessageAsync()
    {
        if (webSocket.State == WebSocketState.Open)
        {
            int adjustedWager = gameFakeCoinFlag ? -Mathf.Abs(gameWager) : Mathf.Abs(gameWager);
            gameDuration = 120f;
            int sessionId = FindObjectOfType<APIHandler>().sessionId;
            string authMessage = $"AUTH {UserDetails.userProfileId} {UserDetails.userName} {UserDetails.gnight} {adjustedWager} {gameOdds} {sessionId} {gameDuration}";
            await webSocket.SendText(authMessage);
            UnityEngine.Debug.Log("Sent PlayerSessionId and API key for validation.");
        }
    }

    public bool GetHomeFlag()
    {
        return homeFlag;
    }

    private void HandleServerMessage(string message)
    {
        var parts = message.Split(' ');

        if (parts[0] == "AUTH_SUCCESS")
        {
            UnityEngine.Debug.Log($"Authentication successful");
        }
        else if (parts[0] == "STOPPER_UPDATE")
        {
            float posX = float.Parse(parts[1]);
            float posZ = float.Parse(parts[2]);
            float veloX = float.Parse(parts[3]);
            float veloZ = float.Parse(parts[4]);

            GameManager.Instance.OnRemoteStopperState(posX, posZ, veloX, veloZ);
        }
        else if (parts[0] == "PUCK_UPDATE")
        {
            if (!GameManager.Instance.isAuthority)
            {
                float posX = float.Parse(parts[1]);
                float posZ = float.Parse(parts[2]);
                float veloX = float.Parse(parts[3]);
                float veloZ = float.Parse(parts[4]);

                GameManager.Instance.OnRemotePuckState(posX, posZ, veloX, veloZ);
            }
        }
        else if (parts[0] == "PUCK_HIT")
        {
            float veloX = float.Parse(parts[1]);
            float veloZ = float.Parse(parts[2]);

            UnityEngine.Debug.LogError("HITTA");

            GameManager.Instance.OnSwitchRemotePuckState(veloX, veloZ);

            GameManager.Instance.isAuthority = false;
        }
        else if (parts[0] == "PUCK_SWITCH")
        {
            float posX = float.Parse(parts[1]);
            float posZ = float.Parse(parts[2]);
            float veloX = float.Parse(parts[3]);
            float veloZ = float.Parse(parts[4]);

            PuckOnSide(posX, posZ, veloX, veloZ);
        }
        else if (parts[0] == "ADD_GOAL")
        {
            int youScore = int.Parse(parts[1]);
            int oppScore = int.Parse(parts[2]);

            GameManager.Instance.UpdateScoreText(youScore, oppScore);
        }
        else if (parts[0] == "AUTH_FAILED")
        {
            UnityEngine.Debug.LogError("Authentication failed. Closing connection.");
            webSocket.Close();
        }
        else if (parts[0] == "GAME_START")
        {
            //GET OPPONENT DATA
            UnityEngine.Debug.LogError("GAME START");

            int playerId = int.Parse(parts[1]);
            string playerUsername = parts[2];
            int playerGnight = int.Parse(parts[3]);
            string color = parts[4];
            homeFlag = bool.Parse(parts[5]);
            string startTimeString = parts[6];
            int duration = int.Parse(parts[7]);

            gameStartTime = DateTime.Parse(startTimeString, null, System.Globalization.DateTimeStyles.RoundtripKind);
            gameDuration = (float)duration;

            UnityEngine.Debug.LogError(playerId + ", " + playerUsername + ", " + playerGnight + ", " + color);

            StartCoroutine(FindObjectOfType<APIHandler>().GetOpponentAirHockeyData(playerId, playerUsername, playerGnight, color));
        }
        else if (parts[0] == "GAME_END")
        {
            int winnerId = int.Parse(parts[1]);
            bool rematchAllowed = bool.Parse(parts[2]);

            rematchAllowedFlag = rematchAllowed;

            EndGame(winnerId, rematchAllowed);
        }
        else if (parts[0] == "ACCEPT_REMATCH")
        {
            FindObjectOfType<GameUIManager>().ReceiveRematchAccept();
        }
        else if (parts[0] == "DECLINE_REMATCH")
        {
            FindObjectOfType<GameUIManager>().ReceiveRematchDecline();
        }
        else if (parts[0] == "INIT_REMATCH")
        {
            StartCoroutine(ReloadGameScene());
        }
        else if (parts[0] == "SERVER_SHUTDOWN")
        {
            LoadLobbyScene();
        }
    }

    private void EndGame(int winnerId, bool rematchAllowed)
    {
        FindObjectOfType<GameUIManager>().InitEndScreen(winnerId, rematchAllowed);
    }

    public void AcceptRematchButton()
    {
        if (rematchAllowedFlag)
        {
            string message = $"ACCEPT_REMATCH {UserDetails.userProfileId}";
            SendMessageToServer(message);
        }
    }

    public void DeclineRematchButton()
    {
        string message = $"DECLINE_REMATCH {UserDetails.userProfileId}";
        SendMessageToServer(message);

        LoadLobbyScene();
    }

    public void LoadLobbyScene()
    {
        UnityEngine.Debug.Log("Server is shutting down. Returning to lobby...");

        StopCoroutine(puckUpdatesCoroutine);

        if (webSocket != null)
        {
            webSocket.Close();
            webSocket = null;
        }

        SceneManager.LoadScene("MenuScene");
    }

    public IEnumerator ReloadGameScene()
    {

        APIHandler existingHandler = FindObjectOfType<APIHandler>();
        if (existingHandler != null)
        {
            //Switch to check for fake balance when both balances incorporated into arcade
            existingHandler.ResetSessionID();
            StartCoroutine(existingHandler.StartGameSession(UserDetails.userProfileId, gameWager));
        }
        else
        {
            UnityEngine.Debug.LogError("Wager placement failed: API Handler not found.");
            yield break;
        }


        StartCoroutine(FindObjectOfType<GameUIManager>().VersusAnimation());

        yield return new WaitForSeconds(3f);


        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MainScene");
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        isGameSceneLoaded = true;

        if (puckUpdatesCoroutine != null)
        {
            StopCoroutine(puckUpdatesCoroutine);
        }
        puckUpdatesCoroutine = StartCoroutine(SendPuckUpdates());
    }

    public void LoadGameScene(string usernameTextLeft, string titleTextLeft, string ratingTextLeft, string recordTextLeft, string lastTenTextLeft, string streakTextLeft, Sprite gnightImageLeft,
        string usernameTextRight, string titleTextRight, string ratingTextRight, string recordTextRight, string lastTenTextRight, string streakTextRight, Sprite gnightImageRight, int playerRanking, int opponentRanking, string playerColor)
    {
        StartCoroutine(LoadSceneAsync("MainScene", usernameTextLeft, titleTextLeft, ratingTextLeft, recordTextLeft, lastTenTextLeft, streakTextLeft, gnightImageLeft,
            usernameTextRight, titleTextRight, ratingTextRight, recordTextRight, lastTenTextRight, streakTextRight, gnightImageRight, playerRanking, opponentRanking, playerColor));
    }

    private IEnumerator LoadSceneAsync(string sceneName, string usernameTextLeft, string titleTextLeft, string ratingTextLeft, string recordTextLeft, string lastTenTextLeft, string streakTextLeft, Sprite gnightImageLeft,
        string usernameTextRight, string titleTextRight, string ratingTextRight, string recordTextRight, string lastTenTextRight, string streakTextRight, Sprite gnightImageRight, int playerRanking, int opponentRanking, string playerColor)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        isGameSceneLoaded = true;

        if (puckUpdatesCoroutine != null)
        {
            StopCoroutine(puckUpdatesCoroutine);
        }
        puckUpdatesCoroutine = StartCoroutine(SendPuckUpdates());

        //Update UI -- UNCOMMENT WHEN SCRIPT ADDED
        FindObjectOfType<GameUIManager>().InitGameUI(usernameTextLeft, titleTextLeft, ratingTextLeft, recordTextLeft, lastTenTextLeft, streakTextLeft, gnightImageLeft,
            usernameTextRight, titleTextRight, ratingTextRight, recordTextRight, lastTenTextRight, streakTextRight, gnightImageRight, playerRanking, opponentRanking, playerColor);

    }

    private async void OnApplicationQuit()
    {
        if (webSocket != null)
        {
            await webSocket.Close();
        }
    }

    public void ExitGameSession()
    {
        StartCoroutine(ExitGameSessionCoroutine());
    }

    public void SendMessageToServer(string message)
    {
        if (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            webSocket.SendText(message);

        }
        else
        {
            UnityEngine.Debug.LogError("WebSocket is not connected. Failed to send message.");
        }
    }

    public IEnumerator SendPuckUpdates()
    {
        var wait = new WaitForSeconds(0.05f); // 20 Hz
        while (true)
        {
            if (webSocket != null)
            {
                if (GameManager.Instance.puckRb != null && GameManager.Instance.isAuthority)
                {
                    var rb = GameManager.Instance.puckRb;
                    var pos = rb.position;
                    var vel = rb.velocity;

                    // include vel so the remote can predict between packets
                    string msg = $"PUCK_UPDATE {UserDetails.userProfileId} {pos.x} {pos.z} {vel.x} {vel.z}";
                    _ = webSocket.SendText(msg);
                }

                if (GameManager.Instance.stopperRb != null)
                {
                    var rb = GameManager.Instance.stopperRb;
                    var pos = rb.position;
                    var vel = rb.velocity;

                    string msg = $"STOPPER_UPDATE {UserDetails.userProfileId} {pos.x} {pos.z} {vel.x} {vel.z}";
                    _ = webSocket.SendText(msg);
                }
            }

            yield return wait;
        }
    }

    public void SendGoalUpdate()
    {
        string puckMessage = $"ADD_GOAL {UserDetails.userProfileId}";
        SendMessageToServer(puckMessage);
    }

    public void SendPuckHit(Vector3 newVelo)
    {
        string puckMessage = $"PUCK_HIT {UserDetails.userProfileId} {newVelo.x} {newVelo.z}";
        SendMessageToServer(puckMessage);

        UnityEngine.Debug.LogError("QUOIF");
    }

    public void PuckOffSide()
    {
        puckOnSide = false;

        var puckPhysics = GameManager.Instance.GetPuckState();

        string puckMessage = $"PUCK_SWITCH {UserDetails.userProfileId} {puckPhysics.puckPos.x} {puckPhysics.puckPos.z}" +
            $" {puckPhysics.puckVelo.x} {puckPhysics.puckVelo.z}";
        SendMessageToServer(puckMessage);

        GameManager.Instance.BeginHandoffBallistic(0.20f);
    }

    public void PuckOnSide(float posX, float posZ, float veloX, float veloZ)
    {
        puckOnSide = true;

        GameManager.Instance.ClearRemotePuck();
        //GameManager.Instance.FreezeRemotePuck(0.15f);

        float eps = 0.03f; // small (tune 0.01 - 0.08)
        float nudgedX = homeFlag ? Mathf.Min(posX, -eps) : Mathf.Max(posX, eps);

        GameManager.Instance.TurnOnPuckPhysics(nudgedX, posZ, veloX, veloZ);
    }

    public void ResetPuckOnGoal(float posX, float posZ, float veloX, float veloZ)
    {
        puckOnSide = false;

        GameManager.Instance.TurnOnPuckPhysics(posX, posZ, veloX, veloZ);
    }

    private IEnumerator ExitGameSessionCoroutine()
    {
        string apiUrl = "https://45gsbt8bm6.execute-api.us-west-2.amazonaws.com/test/sessionexit";

        if (matchmakingTicketId != null)
        {
            var payload = new SessionExitRequest
            {
                TicketId = matchmakingTicketId
            };

            string jsonPayload = JsonUtility.ToJson(payload);

            UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            UnityEngine.Debug.LogError("Session exit request sent");

            yield return request.SendWebRequest();

            UnityEngine.Debug.LogError("Session exit response received");

            MenuManager menuManager = FindObjectOfType<MenuManager>();

            if (request.result == UnityWebRequest.Result.Success)
            {
                UnityEngine.Debug.Log($"Session exit successful: {request.downloadHandler.text}");

                StartCoroutine(PostPlayerLeave());

                pollMatchmakingFlag = false;
                menuManager.ResetPlayerReadiness();
            }
            else
            {
                UnityEngine.Debug.LogError($"Session exit failed: {request.error}");
            }

            menuManager.ResetExitReadyButton();

            fullLobbyName = lobbyName;
        }
    }

    private IEnumerator PostPlayerJoin()
    {
        string encodedLobbyCode = UnityWebRequest.EscapeURL(lobbyName);
        string encodedUsername = UnityWebRequest.EscapeURL(id);

        string url = $"{lobbyDataUrl}?action=join&lobbyCode={encodedLobbyCode}&joinLimit={gamePlayerCount}&username={encodedUsername}";

        UnityWebRequest request = UnityWebRequest.PostWwwForm(url, "");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            JoinResponse response = JsonUtility.FromJson<JoinResponse>(request.downloadHandler.text);
            fullLobbyName = response.fullLobbyCode;
            UnityEngine.Debug.Log("Joined lobby: " + fullLobbyName);
        }
        else
        {
            UnityEngine.Debug.LogError("Join failed: " + request.error);
        }
    }

    private IEnumerator GetPlayerCount()
    {
        while (getPlayerCountFlag)
        {
            string encodedLobbyCode = UnityWebRequest.EscapeURL(fullLobbyName);

            string url = $"{lobbyDataUrl}?action=count&lobbyCode={encodedLobbyCode}";

            UnityWebRequest request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string count = request.downloadHandler.text;
                UpdateJoinedText(count);
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private void UpdateJoinedText(string ct)
    {

    }

    private IEnumerator PostPlayerLeave()
    {
        string encodedLobbyCode = UnityWebRequest.EscapeURL(fullLobbyName);
        string encodedUsername = UnityWebRequest.EscapeURL(id);

        string url = $"{lobbyDataUrl}?action=remove&username={encodedUsername}&lobbyCode={encodedLobbyCode}";

        UnityWebRequest request = UnityWebRequest.PostWwwForm(url, "");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            UnityEngine.Debug.Log(request.downloadHandler.text);
        }
        else
        {
            UnityEngine.Debug.LogError(request.error);
        }
    }
}

[System.Serializable]
public class MatchmakingRequest
{
    public string PlayerId;
    public string LobbyName;
    public int Skill;
    public int GameId;
}

[System.Serializable]
public class MatchmakingResponse
{
    public string ticketId;
}

[System.Serializable]
public class MatchmakingStatusResponse
{
    public string status;
    public string playerSessionId;
    public string serverIpAddress;
    public int serverPort;
}

[System.Serializable]
public class SessionExitRequest
{
    public string TicketId;
}

[System.Serializable]
public class JoinResponse
{
    public string fullLobbyCode;
}

public class PlayerScore
{
    public string playerId;
    public int kills;
    public int deaths;
    public string team;
}

public class FullPuckPhysics
{
    public Vector3 puckPos;
    public Vector3 puckVelo;
}