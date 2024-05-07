using Photon.Pun;
using UnityEngine;

public class Flag : MonoBehaviourPunCallbacks
{
    public int team; // 0 for team 1's flag, 1 for team 2's flag
    public bool isCarried; // Whether the flag is currently being carried by a player

    private Vector3 startPosition; // Initial position of the flag

    private void Start()
    {
        startPosition = transform.position;
    }

    [PunRPC]
    public void RespawnFlag()
    {
        // Reset the flag's position and parent
        transform.position = startPosition;
        transform.SetParent(null);
        isCarried = false;
    }

    [PunRPC]
    public void RespawnFlagWithDelay(float delay)
    {
        photonView.RPC("RespawnFlag", RpcTarget.All, delay);
    }

    [PunRPC]
    public void SetCarried(bool carried)
    {
        isCarried = carried;
    }
}