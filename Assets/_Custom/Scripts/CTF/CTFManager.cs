using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class CTFManager : MonoBehaviourPunCallbacks
{
    public static CTFManager Instance;

    public int team1Score;
    public int team2Score;

    public Flag team1Flag;
    public Flag team2Flag;

    public Text team1ScoreText;
    public Text team2ScoreText;

    public float flagRespawnDelay = 5f; // Delay in seconds before the flag respawns after being captured

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        team1ScoreText.text = team1Score.ToString();
        team2ScoreText.text = team2Score.ToString();
    }

    [PunRPC]
    public void UpdateScore(int team)
    {
        if (team == 0)
        {
            team1Score++;
        }
        else
        {
            team2Score++;
        }
    }
}