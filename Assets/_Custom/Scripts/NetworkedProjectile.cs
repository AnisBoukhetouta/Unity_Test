using MalbersAnimations.Weapons;
using Photon.Pun;
using UnityEngine;

public class NetworkedProjectile : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback
{
    // This method is automatically called by Photon after the projectile is instantiated
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        Debug.LogError("we spawned");
        object[] instantiationData = info.photonView.InstantiationData;
        int parentViewID = (int)instantiationData[0];
        Debug.LogError(parentViewID);

        PhotonView parentPhotonView = PhotonView.Find(parentViewID);
        if (parentPhotonView != null)
        {
            Debug.LogError("found");

            transform.SetParent(parentPhotonView.GetComponent<MShootable>().ProjectileParent);
        }
    }
}
