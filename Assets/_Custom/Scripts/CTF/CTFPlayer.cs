using Photon.Pun;
using UnityEngine;

public class CTFPlayer : MonoBehaviourPunCallbacks
{
    public int team; // 0 for team 1, 1 for team 2
    public Transform baseTransform; // The base where the player can score
    public float pickupRange = 2f; // Distance within which the player can pick up the flag
    public float dropRange = 5f; // Distance from the base within which the player can drop the flag

    private PhotonView photonView;
    public Flag carriedFlag;

    private void Start()
    {
        photonView = GetComponent<PhotonView>();
        if (!photonView.IsMine)
        {
            enabled = false;
        }

        // Assign team based on the value passed during instantiation
        object[] instantiationData = photonView.InstantiationData;
        team = (int)instantiationData[0];

        // Set the player's base transform based on the team
        baseTransform = team == 0 ? GameObject.Find("Team1Base").transform : GameObject.Find("Team2Base").transform;
    }

    private void Update()
    {
        // Check if the player is close enough to pick up the opposing team's flag
        Flag opposingFlag = GetOpposingFlag();
        if (opposingFlag != null && carriedFlag == null && !opposingFlag.isCarried)
        {
            Transform flagTransform = opposingFlag.transform;
            if (flagTransform != null)
            {
                Vector3 direction = flagTransform.position - transform.position;
                if (direction.magnitude <= pickupRange)
                {
                    photonView.RPC("PickUpFlag", RpcTarget.All, opposingFlag.photonView.ViewID);
                }
            }
        }

        // Check if the player is close enough to their base to drop the flag
        if (carriedFlag != null)
        {
            Vector3 direction = baseTransform.position - transform.position;
            if (direction.magnitude <= dropRange)
            {
                photonView.RPC("DropFlag", RpcTarget.All);
            }
        }
    }

    [PunRPC]
    private void PickUpFlag(int flagPhotonViewID)
    {
        PhotonView flagPhotonView = PhotonView.Find(flagPhotonViewID);
        // Transfer ownership of the flag to this player
        flagPhotonView.TransferOwnership(photonView.Owner);

        // Get the Flag script from the PhotonView
        carriedFlag = flagPhotonView.GetComponent<Flag>();

        // Parent the flag to the player's game object
        carriedFlag.transform.SetParent(transform);
        carriedFlag.transform.localPosition = Vector3.zero;

        // Notify the flag that it's being carried
        carriedFlag.photonView.RPC("SetCarried", RpcTarget.All, true);
    }

    [PunRPC]
    private void DropFlag()
    {
        // Notify the game manager to update the score for the team
        //CTFManager.Instance.photonView.RPC("UpdateScore", RpcTarget.All, team);
        if (team == 0)
        {
            CTFManager.Instance.team1Score++;
        }
        else
        {
            CTFManager.Instance.team2Score++;
        }
        // Respawn the flag after a delay
        if (carriedFlag != null)
        {
            carriedFlag.photonView.RPC("RespawnFlag", RpcTarget.All, null);

            // Notify the flag that it's no longer being carried
            carriedFlag.photonView.RPC("SetCarried", RpcTarget.All, false);
        }

        // Reset the carried flag for the player
        carriedFlag = null;
    }

    private Flag GetOpposingFlag()
    {
        return team == 0 ? CTFManager.Instance.team2Flag : CTFManager.Instance.team1Flag;
    }
}