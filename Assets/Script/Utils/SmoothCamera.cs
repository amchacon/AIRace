using UnityEngine;

public class SmoothCamera : MonoBehaviour
{
    //The target we are following
    public Transform target;
    //Caching this transform
    private Transform myTransform;
    //Reference to initial values
    private Vector3 initialPosRef;
    private Quaternion initialRotRef;
    //The distance in the x-z plane to the target
    [SerializeField] private float distance = 10.0f;
    //the height we want the camera to be above the target
    [SerializeField] private float height = 5.0f;

    [SerializeField] private float rotationDamping;
    [SerializeField] private float heightDamping;

    private void Start()
    {
        myTransform = transform;
        initialPosRef = myTransform.localPosition;
        initialRotRef = myTransform.localRotation;
    }

    private void LateUpdate()
    {
        //Early out if we don't have a target
        if (!target)
            return;

        //Calculate the current rotation angles
        var wantedRotationAngle = target.eulerAngles.y;
        var wantedHeight = target.position.y + height;
        var currentRotationAngle = transform.eulerAngles.y;
        var currentHeight = transform.position.y;
        //Damp the rotation around the y-axis
        currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, wantedRotationAngle, rotationDamping * Time.deltaTime);
        //Damp the height
        currentHeight = Mathf.Lerp(currentHeight, wantedHeight, heightDamping * Time.deltaTime);
        //Convert the angle into a rotation
        var currentRotation = Quaternion.Euler(0, currentRotationAngle, 0);
        //Set the position of the camera on the x-z plane to:
        //distance meters behind the target
        myTransform.position = target.position;
        myTransform.position -= currentRotation * Vector3.forward * distance;
        //Set the height of the camera
        myTransform.position = new Vector3(myTransform.position.x, currentHeight, myTransform.position.z);
        //Always look at the target
        myTransform.LookAt(target);
    }

    /// <summary>
    /// Reset camera to original position and rotation (Pan cam)
    /// </summary>
    public void ResetPosition()
    {
        SetTarget(null);
        myTransform.position = initialPosRef;
        myTransform.rotation = initialRotRef;
    }

    /// <summary>
    /// Makes the camera follow the selected car
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        if (newTarget == null)
        {
            target = null;
            //Reset this to Parent initial position/rotation
            if (transform.parent)
            {
                transform.position = transform.parent.position;
                transform.rotation = transform.parent.rotation;
            }
        }
        else target = newTarget;
    }
}
