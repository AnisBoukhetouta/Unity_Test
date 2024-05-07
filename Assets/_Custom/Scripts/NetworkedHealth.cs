using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using MalbersAnimations;
using MalbersAnimations.Controller.AI;
using MalbersAnimations.Controller;
using Unity.Mathematics;

public class NetworkedHealth : MonoBehaviourPun
{
    private MDamageable damageable;
    public StatModifier takeDamage;
    public Stat stattest;
    public Vector3 spawnPos;

    MalbersInput mi;
    MAnimal ma;

    Animator animator;
    public Stat stat;

    CTFPlayer cTFPlayer;
    private void Awake()
    {
        spawnPos = transform.position;
        cTFPlayer = GetComponent<CTFPlayer>();
        animator = GetComponent<Animator>();
        ma = GetComponent<MAnimal>();
        mi = GetComponent<MalbersInput>();
        damageable = GetComponent<MDamageable>();


        stat = GetComponent<Stats>().stats[0];

        stat.OnStatEmpty.AddListener(HealthEmpty);

        //damageable.events.OnReceivingDamage.AddListener(HandleDamageReceived);

        // Subscribe to the OnReceivingDamage event
        //damageable.events.OnReceivingDamage.AddListener(HandleDamageReceived);
        //stattest = damageable.stats.Stat_Get(takeDamage.ID);
    }
    void HealthEmpty()
    {
        photonView.RPC("Die", RpcTarget.All);
    }
    void Update()
    {
        if (photonView.IsMine)
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                //PhotonNetwork.Instantiate("Bow Collectable", Vector3.zero, Quaternion.identity, 0);
            }
            if (Input.GetKeyDown(KeyCode.G))
            {
                photonView.RPC("Respawn", RpcTarget.All);
            }
        }
    }

    [PunRPC]
    private void Die()
    {
        ma.State_Activate(10);
        if (cTFPlayer)
        {
            if (cTFPlayer.carriedFlag != null)
            {
                // Drop the flag at the player's death position
                cTFPlayer.carriedFlag.transform.SetParent(null);
                cTFPlayer.carriedFlag.transform.position = transform.position;
                // Notify the flag that it's no longer being carried
                if (photonView.IsMine)
                {
                    cTFPlayer.enabled = false;
                    cTFPlayer.carriedFlag.photonView.RPC("SetCarried", RpcTarget.All, false);
                }

                // Reset the carried flag for the player
                cTFPlayer.carriedFlag = null;
            }
        }
        /*ma.enabled = false;
        mi.enabled = false;
        animator.SetBool("StateOn", true);
        animator.SetInteger("State", 10);*/
    }
    [PunRPC]
    void Respawn()
    {
        if (photonView.IsMine)
        {
            cTFPlayer.enabled = true;
            ma.enabled = true;
            mi.enabled = true;
        }
        GetComponent<Collider>().enabled = true;
        //animator.SetBool("StateOn", true);
        //animator.SetInteger("State", 1);
        animator.Rebind();
        ma.Teleport(spawnPos);
        ma.ResetController();
        stat.value = 100;
        stat.active = true;
    }
}
