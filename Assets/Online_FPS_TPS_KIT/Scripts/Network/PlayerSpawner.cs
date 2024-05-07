using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    /*[SerializeField] private GameObject playerPrefab;

    public Transform[] spawnPoints;
    // Start is called before the first frame update
    void Start()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            Transform sPoint = GetRandomSpawnPoint();
            PhotonNetwork.Instantiate(playerPrefab.name, sPoint.position, Quaternion.identity);
        }

    }

    public Transform GetRandomSpawnPoint()
    {
        int spawnID = Random.Range(0, spawnPoints.Length);

        return spawnPoints[spawnID];
    }*/

    [SerializeField] private GameObject playerPrefab;
    public Transform[] spawnPointsTeam1;
    public Transform[] spawnPointsTeam2;

    private void Start()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            SpawnPlayer();
        }
    }

 private void SpawnPlayer()
    {
        int teamIndex = PhotonNetwork.LocalPlayer.ActorNumber % 2; // 0 for team 1, 1 for team 2

        Transform spawnPoint = teamIndex == 0
            ? GetRandomSpawnPoint(spawnPointsTeam1)
            : GetRandomSpawnPoint(spawnPointsTeam2);

        object[] instantiationData = new object[] { teamIndex };
        PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, Quaternion.identity, 0, instantiationData);
    }

    private Transform GetRandomSpawnPoint(Transform[] spawnPoints)
    {
        int spawnID = Random.Range(0, spawnPoints.Length);
        return spawnPoints[spawnID];
    }
}
