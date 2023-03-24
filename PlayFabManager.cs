using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

public class PlayFabManager : MonoBehaviour
{
    // Leaderboard data and flags for success/error callbacks
    public List<PlayerLeaderboardEntry> returnedLeaderboard = new List<PlayerLeaderboardEntry>();
    public bool returned = false;
    public bool cleared = false;
    public bool setusername = false;
    public bool added = false;
    public bool loggedIn { get; private set; } = false;

    // Start is called before the first frame update
    void Start()
    {
        // Try to log in with a custom ID for this game object instance
        Login();
    }

    // Log in with a custom ID for this game object instance
    void Login() {
        var request = new LoginWithCustomIDRequest {
            CustomId = gameObject.GetInstanceID().ToString(),
            CreateAccount = true
        };
        PlayFabClientAPI.LoginWithCustomID(request, OnSuccess, OnError);
    }

    // Callback for successful login/account creation
    void OnSuccess(LoginResult result) {
        loggedIn = true;
    }

    // Callback for login/account creation errors
    void OnError(PlayFabError error) {
        Debug.Log(error.GenerateErrorReport());
    }

    // Clear a leaderboard by incrementing its version number
    public void ClearLeaderboard(string leaderboard) {
        PlayFabAdminAPI.IncrementPlayerStatisticVersion(
            new PlayFab.AdminModels.IncrementPlayerStatisticVersionRequest() {
                StatisticName = leaderboard
            },
            result => {
                cleared = true;
            },
            error => Debug.Log(error.GenerateErrorReport())
        );
    }

    // Set the display name for the current user
    public void SetDisplayNameForUser(string name) {
        UpdateUserTitleDisplayNameRequest requestData = new UpdateUserTitleDisplayNameRequest() {
            DisplayName = name
        };
        PlayFabClientAPI.UpdateUserTitleDisplayName(requestData, OnSetDisplayNameForUserResult, OnSetDisplayNameForUserError );
    }

    // Callback for successful display name update
    private void OnSetDisplayNameForUserResult(UpdateUserTitleDisplayNameResult response) {
        setusername = true;
    }

    // Callback for display name update errors
    private void OnSetDisplayNameForUserError(PlayFabError error) {
        Debug.Log(error.GenerateErrorReport());
    }

    // Add a value to a leaderboard for the current user
    public void AddLeaderboard(string leaderboard, int value) {
        UpdatePlayerStatisticsRequest requestData = new UpdatePlayerStatisticsRequest() {
            Statistics = new List<StatisticUpdate>() {
                new StatisticUpdate() { 
                    StatisticName = leaderboard, 
                    Value = value 
                }
            }
        };
        PlayFabClientAPI.UpdatePlayerStatistics(requestData, OnAddLeaderboardSuccess, OnAddLeaderboardError);
    }

    // Callback for successful leaderboard value addition
    private void OnAddLeaderboardSuccess(UpdatePlayerStatisticsResult response) {
        added = true;
    }

    // Callback for leaderboard value addition errors
    private void OnAddLeaderboardError(PlayFabError error) {
        Debug.Log(error.GenerateErrorReport());
    }

    // Get the top N entries for a leaderboard
    public void GetLeaderboard(string leaderboard, int numpositions) {
        GetLeaderboardRequest requestData = new GetLeaderboardRequest() {
            StatisticName = leaderboard,
            StartPosition = 0,
            MaxResultsCount = numpositions
        };
        PlayFabClientAPI.GetLeaderboard(requestData, OnGetLeaderboardDataResult, OnGetLeaderboardDataError);
    }

    //Callback for successful get leaderboard request
    private void OnGetLeaderboardDataResult(GetLeaderboardResult response) {
        returnedLeaderboard.Clear();
        returned = true;
        foreach(PlayerLeaderboardEntry entry in response.Leaderboard) {
            returnedLeaderboard.Add(entry);
        }
    }

    //Callback for failed get leaderboard request
    private void OnGetLeaderboardDataError(PlayFabError error) {
        Debug.Log(error.GenerateErrorReport());
    }
}
