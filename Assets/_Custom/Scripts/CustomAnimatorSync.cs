using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PhotonView))]
public class CustomAnimatorSync : MonoBehaviourPunCallbacks, IPunObservable
{
    #region private fields
    Animator anim;
    PhotonView pw;
    #endregion

    #region monobehaviours
    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.NetworkingClient.EventReceived -= OnEvent;
    }

    private void Start()
    {
        anim = GetComponent<Animator>();
        pw = GetComponent<PhotonView>();
        _rigidbody = GetComponent<Rigidbody>();
    }

    #endregion

    #region private methods
    private void OnEvent(EventData photonEvent)
    {
        anim = GetComponent<Animator>();
        byte eventCode = photonEvent.Code;
        if (eventCode == PlayAnimationEventCode)
        {
            object[] data = (object[])photonEvent.CustomData;
            int targetPhotonView = (int)data[0];
            if (targetPhotonView == this.photonView.ViewID)
            {
                string animatorParameter = (string)data[1];
                string parameterType = (string)data[2];
                object parameterValue = (object)data[3];

                switch (parameterType)
                {
                    case "Trigger":
                        Debug.Log(animatorParameter);
                        anim.SetTrigger(animatorParameter);
                        break;
                    //case "Bool":
                    //    anim.SetBool(animatorParameter, (bool)parameterValue);
                    //    break;
                    //case "Float":
                    //    anim.SetFloat(animatorParameter, (float)parameterValue);
                    //    break;
                    //case "Int":
                    //    anim.SetInteger(animatorParameter, (int)parameterValue);
                    //    break;
                    default:
                        break;
                }
            }
        }
    }
    #endregion

    #region public methods

    public const byte PlayAnimationEventCode = 1;

    public void SendPlayAnimationEvent(int photonViewID, string animatorParameter, string parameterType, object parameterValue = null)
    {
        object[] content = new object[] { photonViewID, animatorParameter, parameterType, parameterValue };
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(PlayAnimationEventCode, content, raiseEventOptions, SendOptions.SendReliable);
    }

    #endregion
    private Vector3 _netPosition;
    private Quaternion _netRotation;
    private Rigidbody _rigidbody;
    public float teleportIfFarDistance;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (stream.IsWriting)
        {
            stream.SendNext(_rigidbody.position);
            stream.SendNext(_rigidbody.rotation);
            stream.SendNext(_rigidbody.velocity);
        }
        else
        {
            _netPosition = (Vector3)stream.ReceiveNext();
            _netRotation = (Quaternion)stream.ReceiveNext();
            _rigidbody.velocity = (Vector3)stream.ReceiveNext();
            float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
            _netPosition += (_rigidbody.velocity * lag);
        }
    }

    private void Update()
    {
        if (!pw.IsMine)
        {
            transform.position = Vector3.MoveTowards(_rigidbody.position, _netPosition, Time.deltaTime);
            transform.rotation = Quaternion.RotateTowards(_rigidbody.rotation, _netRotation, Time.deltaTime * PhotonNetwork.SerializationRate);

            //if (Vector3.Distance(transform.position, _netPosition) > teleportIfFarDistance)
            //{
            //    transform.position = _netPosition;
            //}
        }
    }
}