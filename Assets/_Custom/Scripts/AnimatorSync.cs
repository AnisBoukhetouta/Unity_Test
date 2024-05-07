using Photon.Pun;
using UnityEngine;

public class AnimatorSync : MonoBehaviourPunCallbacks, IPunObservable
{
    private Animator animator;
    private int stateOnHash;
    private int modeOnHash;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        stateOnHash = Animator.StringToHash("StateOn");
        modeOnHash = Animator.StringToHash("ModeOn");
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Send Animator parameters
            stream.SendNext(animator.GetFloat("Vertical"));
            stream.SendNext(animator.GetFloat("Horizontal"));
            stream.SendNext(animator.GetFloat("UpDown"));
            stream.SendNext(animator.GetFloat("DeltaUpDown"));
            stream.SendNext(animator.GetBool("Movement"));
            stream.SendNext(animator.GetBool("Grounded"));
            stream.SendNext(animator.GetFloat("DeltaAngle"));
            stream.SendNext(animator.GetFloat("SpeedMultiplier"));
            stream.SendNext(animator.GetFloat("Random"));
            stream.SendNext(animator.GetInteger("WeaponType"));
            stream.SendNext(animator.GetFloat("Slope"));
            stream.SendNext(animator.GetBool("Sprint"));
            stream.SendNext(animator.GetBool("Strafe"));
            stream.SendNext(animator.GetInteger("State"));
            stream.SendNext(animator.GetInteger("StateProfile"));
            stream.SendNext(animator.GetInteger("StateEnterStatus"));
            stream.SendNext(animator.GetInteger("StateExitStatus"));
            stream.SendNext(animator.GetInteger("Mode"));
            stream.SendNext(animator.GetInteger("ModeStatus"));
            stream.SendNext(animator.GetFloat("ModePower"));
            stream.SendNext(animator.GetInteger("Stance"));
            stream.SendNext(animator.GetInteger("LastState"));
            stream.SendNext(animator.GetFloat("StateFloat"));
            stream.SendNext(animator.GetFloat("StateTime"));
        }
        else
        {
            // Receive Animator parameters
            float vertical = (float)stream.ReceiveNext();
            float horizontal = (float)stream.ReceiveNext();
            float upDown = (float)stream.ReceiveNext();
            float deltaUpDown = (float)stream.ReceiveNext();
            bool movement = (bool)stream.ReceiveNext();
            bool grounded = (bool)stream.ReceiveNext();
            float deltaAngle = (float)stream.ReceiveNext();
            float speedMultiplier = (float)stream.ReceiveNext();
            float random = (float)stream.ReceiveNext();
            int weaponType = (int)stream.ReceiveNext();

            float slope = (float)stream.ReceiveNext();
            bool sprint = (bool)stream.ReceiveNext();
            bool strafe = (bool)stream.ReceiveNext();
            int state = (int)stream.ReceiveNext();
            int stateProfile = (int)stream.ReceiveNext();
            int stateEnterStatus = (int)stream.ReceiveNext();
            int stateExitStatus = (int)stream.ReceiveNext();
            int mode = (int)stream.ReceiveNext();
            int modeStatus = (int)stream.ReceiveNext();
            float modePower = (float)stream.ReceiveNext();
            int stance = (int)stream.ReceiveNext();
            int lastState = (int)stream.ReceiveNext();
            float stateFloat = (float)stream.ReceiveNext();
            float stateTime = (float)stream.ReceiveNext();

            // Update Animator parameters
            animator.SetFloat("Vertical", vertical);
            animator.SetFloat("Horizontal", horizontal);
            animator.SetFloat("UpDown", upDown);
            animator.SetFloat("DeltaUpDown", deltaUpDown);
            animator.SetBool("Movement", movement);
            animator.SetBool("Grounded", grounded);
            animator.SetFloat("DeltaAngle", deltaAngle);
            animator.SetFloat("SpeedMultiplier", speedMultiplier);
            animator.SetInteger("WeaponType", weaponType);

            animator.SetFloat("Random", random);
            animator.SetFloat("Slope", slope);
            animator.SetBool("Sprint", sprint);
            animator.SetBool("Strafe", strafe);
            animator.SetInteger("State", state);
            animator.SetInteger("StateProfile", stateProfile);
            animator.SetInteger("StateEnterStatus", stateEnterStatus);
            animator.SetInteger("StateExitStatus", stateExitStatus);
            animator.SetInteger("Mode", mode);
            animator.SetInteger("ModeStatus", modeStatus);
            animator.SetFloat("ModePower", modePower);
            animator.SetInteger("Stance", stance);
            animator.SetInteger("LastState", lastState);
            animator.SetFloat("StateFloat", stateFloat);
            animator.SetFloat("StateTime", stateTime);

            // Handle StateOn and ModeOn as events
            animator.SetTrigger(stateOnHash);
            animator.SetTrigger(modeOnHash);
        }
    }
}