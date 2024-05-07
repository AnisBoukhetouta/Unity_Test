using UnityEngine;
using MalbersAnimations.Controller;
using MalbersAnimations;
using MalbersAnimations.Scriptables;

public class StateToggle : MonoBehaviour
{
    public MAnimal animal;
    public StateID flyStateID;
    public KeyCode flyToggleKey = KeyCode.T;
    public KeyCode doubleJumpToggleKey = KeyCode.Y;

    private bool isFlyEnabled = false;
    private bool canDoubleJump = true;

    private void Update()
    {
        // Check for fly state toggle input
        if (Input.GetKeyDown(flyToggleKey))
        {
            ToggleFlyState();
        }

        // Check for double jump toggle input
        if (Input.GetKeyDown(doubleJumpToggleKey))
        {
            ToggleDoubleJump();
        }
    }

    private void ToggleFlyState()
    {
        if (isFlyEnabled)
        {
            // Disable the fly state
            animal.State_Disable(flyStateID);
            isFlyEnabled = false;
            Debug.Log("Fly state disabled.");
        }
        else
        {
            // Enable the fly state
            animal.State_Enable(flyStateID);
            isFlyEnabled = true;
            Debug.Log("Fly state enabled.");
        }
    }
    public virtual void State_SetJumps(int stateID, int numberOfJumps)
    {
        var jumpState = animal.State_Get(stateID) as Jump;
        if (jumpState != null)
        {
            jumpState.Jumps = new IntReference(numberOfJumps);
            jumpState.ResetState();
        }
    }
    private void ToggleDoubleJump()
    {
        if (canDoubleJump)
        {
            // Disable double jump
            State_SetJumps(StateEnum.Jump, 1);
            canDoubleJump = false;
            Debug.Log("Double jump disabled.");
        }
        else
        {
            // Enable double jump
            State_SetJumps(StateEnum.Jump, 2);
            canDoubleJump = true;
            Debug.Log("Double jump enabled.");
        }
    }
}