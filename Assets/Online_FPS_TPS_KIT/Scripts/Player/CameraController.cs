using UnityEngine;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField] private CinemachinePOVExtension cinemachinePOVExtension;
    [SerializeField] private float sensitivity = 80;

    [SerializeField] private Transform cameraTPVRotater;
    [SerializeField] private Transform cameraTPVRotater2;
    [SerializeField] private Vector3 cameraRotationValue;

    [SerializeField] float maxViewAngle = 80;


    void LateUpdate()
    {
        MouseLocker();
        if (Cursor.lockState != CursorLockMode.Locked) return;
        /* SetCameraRotation(Input.GetAxis("Mouse Y") * -sensitivity, Input.GetAxis("Mouse X") * sensitivity);

        cinemachineBrain.ManualUpdate(); */

    }

    public void SetCameraRotation(float vertical, float horizontal)
    {
        if (this.enabled == false) return;
        
        cameraRotationValue.x += vertical * Time.deltaTime;
        cameraRotationValue.y += horizontal * Time.deltaTime;
        cameraRotationValue.x = Mathf.Clamp(cameraRotationValue.x, -maxViewAngle, maxViewAngle);

        //cameraTPVRotater.rotation = Quaternion.Euler(cameraRotationValue);
        cameraTPVRotater2.rotation = Quaternion.Euler(cameraRotationValue);
        cinemachinePOVExtension.SetCameraRotation(cameraRotationValue);
    }

    void MouseLocker()
    {
        // mouse lock
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        // mouse unlock
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
