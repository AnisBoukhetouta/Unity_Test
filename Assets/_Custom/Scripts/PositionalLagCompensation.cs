using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System;

public class PositionalLagCompensation : MonoBehaviourPun, IPunObservable
{
    private Rigidbody rb;

    private Vector3 _netPosition;
    private Quaternion _netRotation;
    private Vector3 _previousPos;

    public bool teleportIfFar;
    public float teleportIfFarDistance;

    public float smoothPos = 5;
    public float smoothRot = 5;

    public Transform myCamera;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }


    void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // We own this player: send the others our data
            //This script is local, you write to stream
            stream.SendNext(rb.position);
            stream.SendNext(rb.rotation);
            stream.SendNext(rb.velocity);

            // Send the camera's position and rotation
            stream.SendNext(myCamera.position);
            stream.SendNext(myCamera.rotation);
        }
        else
        {
            // Network player, receive data
            //This script is receiving data from remote players script
            _netPosition = (Vector3)stream.ReceiveNext();
            _netRotation = (Quaternion)stream.ReceiveNext();
            rb.velocity = (Vector3)stream.ReceiveNext();

            // Receive the camera's position and rotation
            myCamera.position = (Vector3)stream.ReceiveNext();
            myCamera.rotation = (Quaternion)stream.ReceiveNext();

            float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime)); //the stream is outdated due to lag so we need to know the time past since
                                                                                      //it was sent. This is used to modify time based things on local instances
                                                                                      //This is used for prediction models like position
                                                                                      //This should also be used in combination with PhotonNetwork.GetPing();
            _netPosition += (rb.velocity * lag);
        }
    }
    public void LateUpdate()
    {
        if (!photonView.IsMine)
        {
            rb.position = Vector3.Lerp(rb.position, _netPosition, smoothPos * Time.fixedDeltaTime);
            rb.rotation = Quaternion.Lerp(rb.rotation, _netRotation, smoothRot * Time.fixedDeltaTime);

            if (teleportIfFar)
            {
                if (Vector3.Distance(rb.position, _netPosition) > teleportIfFarDistance)
                {
                    rb.position = _netPosition;
                }
            }
        }
    }
}
