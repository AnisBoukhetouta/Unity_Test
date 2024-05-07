using Photon.Pun;
using UnityEngine;
using MalbersAnimations.Controller;
using MalbersAnimations;
using Unity.Mathematics;
using MalbersAnimations.Weapons;

using MalbersAnimations.Events;
using UnityEngine.Events;


public class MAnimalNetworkSync : MonoBehaviourPunCallbacks, IPunObservable
{
    public MAnimal animalController;

    public MWeaponManager weaponManager;
    private StateID activeState;
    private int activeStateID;

    private int activeModeID;
    private int abilityIndex;
    public Vector3 rawInputAxis;

    public Transform myCamera;

    private Rigidbody rb;

    private Vector3 _netPosition;
    private Quaternion _netRotation;
    private Vector3 _previousPos;

    public bool teleportIfFar;
    public float teleportIfFarDistance;

    public float smoothPos = 5;
    public float smoothRot = 5;
    public Vector3 _netCamPos;
    public Quaternion _netCamRot;
    private int stanceID;
    private bool isGrounded;

    private int previousActiveStateID;
    private int previousActiveModeID;
    private int previousAbilityIndex;
    private int previousStanceID;
    private bool previousGrounded;


    private float aimHorizontal;
    private float aimVertical;

    private bool isAiming;


    // Add more variables as needed

    private void Awake()
    {

        rb = GetComponent<Rigidbody>();

        animalController = GetComponent<MAnimal>();
        if (!photonView.IsMine)
        {
            foreach (Stat s in GetComponent<Stats>().stats)
            {
                s.invokeHealthEvents = false;
            }
            animalController.Aimer.cam = myCamera.GetComponent<Camera>();
            animalController.Aimer.shouldAim = true;
            animalController.isSyncingPosition = true;
            animalController.RootMotion = false;
            animalController.isPlayer.Value = false;
            animalController.FreeMovement = true;
        }
        else
        {
            animalController.Aimer.cam = myCamera.GetComponent<Camera>();
            animalController.Aimer.shouldAim = true;
            animalController.isPlayer.Value = true;
        }
    }
    public void RemoveUIHealthEvents()
    {
        var health = MTools.GetInstance<StatID>("Health");

        if (health != null)
        {
            var HealthStat = GetComponent<Stats>().stats.Find(x => x.ID == health);

            if (HealthStat != null)
            {
                // Create a new FloatEvent and assign it to OnValueChangeNormalized
                FloatEvent onValueChangeNormalized = new FloatEvent();
                HealthStat.OnValueChangeNormalized = onValueChangeNormalized;

                // Create a new UnityEvent and assign it to OnStatFull
                UnityEvent onStatFull = new UnityEvent();
                HealthStat.OnStatFull = onStatFull;
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Send the camera's position and rotation which controls so much of the malbers controller
            stream.SendNext(myCamera.position);
            stream.SendNext(myCamera.rotation);

            // Serialize the necessary data
            //Vector3 transformedInputAxis = myCamera.TransformDirection(animalController.RawInputAxis);
            stream.SendNext(animalController.RawInputAxis);
            stream.SendNext(animalController.ActiveStateID.ID);

            stream.SendNext(animalController.ActiveModeID);
            stream.SendNext(animalController.ActiveMode?.AbilityIndex ?? -1);
            stream.SendNext(animalController.Stance.ID);
            stream.SendNext(animalController.Grounded);

            stream.SendNext(animalController.Sprint); // Serialize the sprinting state and strafe
            stream.SendNext(animalController.Strafe);

            // We own this player: send the others our data
            stream.SendNext(rb.position);
            stream.SendNext(rb.rotation);
            stream.SendNext(rb.velocity);


            // Serialize aim data
            stream.SendNext(animalController.Aimer.HorizontalAngle);
            stream.SendNext(animalController.Aimer.VerticalAngle);

            //serialse correct rotations so feet aren't messed up
            stream.SendNext(animalController.Rotate_at_Direction);
            stream.SendNext(animalController.AdditiveRotation);
            stream.SendNext(animalController.currentSpeedModifier.position.Value);

            //            stream.SendNext(weaponManager.Weapon.IsAiming);



        }
        else
        {
            // Receive the camera's position and rotation
            _netCamPos = (Vector3)stream.ReceiveNext();
            _netCamRot = (Quaternion)stream.ReceiveNext();

            // Deserialize the received data
            rawInputAxis = (Vector3)stream.ReceiveNext();
            activeStateID = (int)stream.ReceiveNext();
            activeModeID = (int)stream.ReceiveNext();
            abilityIndex = (int)stream.ReceiveNext();
            stanceID = (int)stream.ReceiveNext();
            isGrounded = (bool)stream.ReceiveNext();
            animalController.Sprint = (bool)stream.ReceiveNext();
            bool strafe = (bool)stream.ReceiveNext();
            animalController.Strafe = strafe;



            // Apply the deserialized data to the animal controller only if they have changed
            if (activeStateID != previousActiveStateID)
            {
                animalController.State_Activate(activeStateID);
                previousActiveStateID = activeStateID;
            }
            if (activeModeID != previousActiveModeID || abilityIndex != previousAbilityIndex)
            {
                if (activeModeID == 0)
                {
                    animalController.Mode_Interrupt();
                    previousActiveModeID = activeModeID;
                    previousAbilityIndex = abilityIndex;
                }
                if (animalController.Mode_Get(activeModeID) != null)
                {
                    //animalController.Mode_Interrupt();
                    previousActiveModeID = activeModeID;
                    previousAbilityIndex = abilityIndex;
                    animalController.Mode_ForceActivate(activeModeID, abilityIndex);
                }

            }

            if (stanceID != previousStanceID)
            {
                animalController.Stance_Set(stanceID);
                previousStanceID = stanceID;
            }

            if (isGrounded != previousGrounded)
            {
                animalController.Grounded = isGrounded;
                previousGrounded = isGrounded;
            }

            // Network player, receive data
            _netPosition = (Vector3)stream.ReceiveNext();
            _netRotation = (Quaternion)stream.ReceiveNext();
            rb.velocity = (Vector3)stream.ReceiveNext();

            aimHorizontal = (float)stream.ReceiveNext();
            aimVertical = (float)stream.ReceiveNext();
            bool rotateAtDirection = (bool)stream.ReceiveNext();
            animalController.Rotate_at_Direction = rotateAtDirection;
            Quaternion additiveRotation = (Quaternion)stream.ReceiveNext();
            animalController.AdditiveRotation = additiveRotation;
            float currentSpeed = (float)stream.ReceiveNext();
            animalController.currentSpeedModifier.position.Value = currentSpeed;




            float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
            _netPosition += (rb.velocity * lag);
        }
    }

    public void Update()
    {
        if (!photonView.IsMine)
        {
            myCamera.position = _netCamPos;
            myCamera.rotation = _netCamRot;
            animalController.RawInputAxis = rawInputAxis;

            transform.position = Vector3.Lerp(transform.position, _netPosition, smoothPos * Time.fixedDeltaTime);
            // transform.rotation = Quaternion.Lerp(transform.rotation, _netRotation, smoothRot * Time.fixedDeltaTime);
            // Apply the aim horizontal and vertical values to the animator
            animalController.Aimer.SetAimHorizontal(aimHorizontal);
            animalController.Aimer.SetAimVertical(aimVertical);
            if (teleportIfFar)
            {
                if (Vector3.Distance(transform.position, _netPosition) > teleportIfFarDistance)
                {
                    transform.position = _netPosition;
                }
            }
        }
    }
    public void WeaponSwitched(int holsterID)
    {
        if (photonView.IsMine)
        {
            photonView.RPC("SyncWeapon", RpcTarget.Others, holsterID);
        }
    }
    [PunRPC]
    public void SyncWeapon(int holsterID)
    {
        weaponManager.Holster_Equip(holsterID);
    }
    [PunRPC]
    private void SyncHolsterSetWeapon(int weaponViewID)
    {
        GameObject weaponObject = PhotonView.Find(weaponViewID).gameObject;
        var weapon = weaponObject.GetComponent<MWeapon>();
        if (weapon != null)
        {
            weaponManager.Holster_SetWeapon(weapon);
        }
    }


    /*[PunRPC]
    private void SyncPickupItem(GameObject itemObject)
    {
        var item = itemObject.GetComponent<Pickable>();
        if (item != null)
        {
            MPickUp.Item = item;
            PickUpItem();
        }
    }*/
}