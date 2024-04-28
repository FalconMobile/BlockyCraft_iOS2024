using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main player script, contains most methods needed for score management
/// </summary>
public partial class NetworkPlayer
{
    private const char SCORE_SEPARATOR = ':';

    [Header("Scoring")]
    [Tooltip("Score given when this player dies")]
    public int scoreByKill = 1000;

    [Tooltip("Score given when this player is hit")]
    public int scoreByHit = 20;

    public int GetScoreByKill() => scoreByKill;
    public int GetScoreByHit() => scoreByHit;

    // Score
    [SyncVar(hook = nameof(OnScoreChanged))]
    public int score = -1;
    TextMeshProUGUI scoreDisplay;
    [SyncVar(hook = nameof(OnTopPlayersChanged))]
    string topPlayers;
    Text topPlayersDisplay;


    [Server]
    public void ResetScore()
    {
        score = 0;
    }

    [Server]
    public void IncreaseScore(int amount)
    {
        score += amount;
    }

    [Command]
    void CmdResetScore()
    {
        ResetScore();
    }

    void OnScoreChanged(int previousScore, int newScore)
    {
        if (scoreDisplay != null)
        {
            string scoreText = scoreDisplay.text;
            int separatorInd = scoreText.IndexOf(SCORE_SEPARATOR) + 1;
            scoreText = scoreText.Remove(separatorInd, scoreText.Length - separatorInd);

            scoreDisplay.text = scoreText + newScore;
        }
    }

    [Server]
    public void NotifyTopPlayersChange(string newTop)
    {
        topPlayers = newTop;
    }

    void OnTopPlayersChanged(string previousText, string newText)
    {
        if (topPlayersDisplay != null)
        {
            topPlayersDisplay.text = newText;
            topPlayersDisplay.gameObject.SetActive(!string.IsNullOrEmpty(newText));
        }
    }
}