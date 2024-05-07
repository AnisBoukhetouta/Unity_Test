using System.Collections;
using System.Collections.Generic;
using MalbersAnimations.Controller;
using Photon.Pun;
using UnityEngine;

public class NetComponentEnabler : MonoBehaviour
{
    [SerializeField] private PhotonView photonView;
    [SerializeField] private List<MonoBehaviour> disableComponents;
    [SerializeField] private List<GameObject> inactiveGameObjects;
    [SerializeField] private GameObject playerCamera;

    [SerializeField] private List<MonoBehaviour> enableComponents;

    [SerializeField] private List<GameObject> activeGameobjects;

    public MAnimal m;


    // Start is called before the first frame update
    void Awake()
    {
        if (!photonView.IsMine)
        {
            playerCamera.transform.Find("CM Brain").tag = "Untagged";
            ComponentsDisaber();
            //transform.parent.GetComponent<Animator>().applyRootMotion = false;
        }
        else
        {
            ComponentsEnabler();
        }
    }

    void ComponentsDisaber()
    {
        m.m_MainCamera.Value = playerCamera.transform.Find("CM Brain");
        m.Aimer.MainCamera = m.m_MainCamera;
        playerCamera.gameObject.transform.parent = null;
        ///playerCamera.enabled = false;
        foreach (var item in disableComponents)
        {
            item.enabled = false;
        }
        foreach (var item in inactiveGameObjects)
        {
            item.SetActive(false);
        }
    }
    void ComponentsEnabler()
    {
        m.m_MainCamera.Value = playerCamera.transform.Find("CM Brain");
        m.Aimer.MainCamera = m.m_MainCamera;

        playerCamera.gameObject.transform.parent = null;
        foreach (var item in enableComponents)
        {
            item.enabled = true;
        }
        foreach (var item in activeGameobjects)
        {
            item.SetActive(true);
        }
    }
}
