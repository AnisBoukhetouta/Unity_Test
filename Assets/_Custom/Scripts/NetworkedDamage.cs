using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using MalbersAnimations;
using System.Diagnostics;

// Attach this script to the object with the IMDamage component
public class NetworkedDamage : MonoBehaviourPun
{
    private MDamageable damageable;
    public StatModifier takeDamage;
    public Stat stattest;

    private void Awake()
    {
        damageable = GetComponent<MDamageable>();
        // Subscribe to the OnReceivingDamage event
        damageable.events.OnReceivingDamage.AddListener(HandleDamageReceived);
        stattest = damageable.stats.Stat_Get(takeDamage.ID);
    }

    private void HandleDamageReceived(float damageAmount)
    {
        // Call the RPC method
        photonView.RPC("RPC_HandleDamageReceived", RpcTarget.Others, damageAmount);
    }

    [PunRPC]
    private void RPC_HandleDamageReceived(float damageAmount)
    {
        UnityEngine.Debug.LogWarning("DAMAGE RECIEVED" + damageAmount.ToString());

         var stat = damageable.stats.Stat_Get(takeDamage.ID);
         
            if (stat == null || !stat.Active || stat.IsEmpty || stat.IsInmune) return; //Do nothing if the stat is empty, null or disabled

        takeDamage.MinValue = damageAmount;
        takeDamage.MaxValue = damageAmount;

        takeDamage.ModifyStat(stat);
        if(stat.Value <= 0)
        {
            //photonView.RPC("Die",RpcTarget.All);
        }
    }

}
