using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Agent : MonoBehaviour
{
    #region VARIABLES
    [Header("Car Properties")]
    public CarInfo carInfo;
    public struct CarInfo
    {
        public int id;                                                  //Unique Id assigned to link the car with your hud information
        public Players playerInfo;
    }

    [Space(10), Header("Waypoints")]
    [SerializeField] public float waypointRange = 10;                   //Waypoint radius to reach it
    [SerializeField] public float angleDeadZone = 10;                   //Avoid flick when looking at next waypoint
    private Vector3[] wpPositions;                                      //All path points position
    private int wpCount;                                                //Cache of wpPosition.length
    public int totalWaypointsToWin;

    [Space(10), Header("Collision Detection")]
    [SerializeField] private bool debugGizmos;
    [SerializeField, Range(5,60)] private float diagonalAngle;          //Collision FOV
    [SerializeField] private float detectionRange = 20f;                //Collision detection distance
    [SerializeField] private float playerDetection = 7.5f;              //Collision detection distance
    [SerializeField] private AnimationCurve collisionAvoidRate;         //AI React force curve
    [SerializeField] private AnimationCurve frontalAvoidRate;           //AI React force curve (frontal collision)
    [SerializeField] private LayerMask playerLayer, scenaryLayer;       //Collision layers

    [Space(10), Header("Components References")]
    [SerializeField] private MeshRenderer[] myMesh;
    [SerializeField] private Rigidbody rigid;

    [HideInInspector] public int currentLap;
    [HideInInspector] public int waypointIndex = 0;
    private Material mat;

    private bool ready = false;
    private float multiplier;               //React force multiplier

    //Hit information and result (bool) of collision avoidance raycasts
    private RaycastHit leftHit, rightHit, forwardHit;
    private bool hitLeft, hitRight, hitForward;
    private RaycastHit fwdLeftHit, fwdRightHit, sideLeftHit, sideRightHit, diagonalLeftHit, diagonalRightHit;
    private bool hitFwdLeft, hitFwdRight, hitSideLeft, hitSideRight, hitDiagonalLeft, hitDiagonalRight;
    
    //Low Trash Vars || Avoid Garbage Collection
    private Ray ray;
    private Vector3 rayAngle;               //Raycast angle direction
    private Vector3 frameAngle;             //Angle between the next waypoint and the car orientation
    private float finalAngle;
    private Vector2 way2d, my2d;
    #endregion

    private void OnEnable()
    {
        GameManager.Instance.NotifyEndGameObservers += GameOver;
        currentLap = 1;
    }

    private void Update()
    {
        if (!ready)
            return;

        AlignCar();
        //They must move using the engine physics system and should collide with the track
        DetectFutureScenaryCollisions();
        //The players must avoid colliding with each other for which you should provide only some basic AI
        DetectFuturePlayerCollisions();
        //Every player must follow the path with a uniform velocity which is provided by the player information
        MoveCar();

        if (waypointIndex >= totalWaypointsToWin)
            GameManager.Instance.EndGame(carInfo.id);
    }

    /// <summary>
    /// Align the car toward the next waypoint 
    /// </summary>
    private void AlignCar()
    {
        //Get the angle between the next waypoint and the car orientation
        frameAngle = (wpPositions[waypointIndex % wpCount] - transform.position).normalized;
        frameAngle.y = transform.position.y;
        finalAngle = Vector3.Angle(frameAngle, transform.forward);

        if (finalAngle > angleDeadZone)
        {
            float auxAngle = Vector3.Angle(frameAngle, transform.right);
            if (auxAngle < 90)
            {
                transform.Rotate(Vector3.up, finalAngle * Time.deltaTime);
            }
            else
            {
                transform.Rotate(Vector3.up, -finalAngle * Time.deltaTime);
            }
        }
    }

    private void MoveCar()
    {
        //Move car forward
        rigid.velocity = transform.forward * carInfo.playerInfo.Velocity * Time.deltaTime * 50;
        //wpPositions[waypointIndex % wpCount] => next waypoint
        way2d = new Vector2(wpPositions[waypointIndex % wpCount].x, wpPositions[waypointIndex % wpCount].z);    //Position of next waypoint (x,z)
        my2d = new Vector2(transform.position.x, transform.position.z);
        //Checks if reached the next waypoint
        if (Vector2.Distance(way2d, my2d) < waypointRange)
        {
            waypointIndex++;
            //Checks if a lap was completed
            if (waypointIndex % wpCount == 0)
                currentLap++;
            //Update HUD Progress
            HUDManager.Instance.RefreshCarProgress(this);
        }
    }

    /// <summary>
    /// Update the path information from Waypoint system
    /// </summary>
    public void SetPathInfo(Vector3[] pointsPosition)
    {
        wpCount = pointsPosition.Length;
        wpPositions = pointsPosition;
        totalWaypointsToWin = 1 + wpCount * GameManager.Instance.lapsTotal;
    }

    /// <summary>
    /// Turn this prefab into a player instance
    /// </summary>
    public void SetCarInfo(Players player, int id)
    {
        gameObject.name = player.Name;
        carInfo.id = id;
        carInfo.playerInfo = player;
        if (myMesh != null)
        {
            mat = new Material(myMesh[0].material);
        }
        mat.SetColor("_Color", carInfo.playerInfo.trueColor);
        foreach (MeshRenderer mesh in myMesh)
        {
            mesh.material = mat;
        }
        ready = true;
    }

    /// <summary>
    /// Called when game ended / Freezes all racers
    /// </summary>
    private void GameOver()
    {
        ready = false;
    }

    private float DistanceToNextWaypoint()
    {
        return Vector3.Distance(transform.position, wpPositions[waypointIndex % wpCount]);
    }

    public float GetCurrentProgress()
    {
        return waypointIndex * 1000 - DistanceToNextWaypoint();
    }

    /// <summary>
    /// Basic AI to detect and avoid collisions
    /// </summary>
    #region DETECTIONS

    void ResetHits()
    {
        hitForward = hitLeft = hitRight = hitFwdLeft = hitFwdRight = hitSideRight = hitSideLeft = false;
    }

    public float CollisionCurveForce(float dist, float range)
    {
        return collisionAvoidRate.Evaluate(dist / range);
    }

    public float FrontalCurveForce(float dist, float range)
    {
        return frontalAvoidRate.Evaluate(dist / range);
    }

    public void DetectFutureScenaryCollisions()
    {

        ResetHits();
        ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out forwardHit, detectionRange, scenaryLayer))
        {
            hitForward = true;
            Debug.DrawLine(transform.position, forwardHit.point, Color.magenta);
        }

        float angle = Mathf.Deg2Rad * diagonalAngle;
        rayAngle = (transform.forward * (Mathf.Cos(angle))) + (transform.right * Mathf.Sin(angle));
        rayAngle = rayAngle.normalized;
        ray = new Ray(transform.position, rayAngle);
        if (Physics.Raycast(ray, out rightHit, detectionRange, scenaryLayer))
        {
            hitRight = true;
            Debug.DrawLine(transform.position, rightHit.point, Color.magenta);
        }

        rayAngle = (transform.forward * (Mathf.Cos(angle))) + (-transform.right * Mathf.Sin(angle));
        rayAngle = rayAngle.normalized;
        ray = new Ray(transform.position, rayAngle);

        if (Physics.Raycast(ray, out leftHit, detectionRange, scenaryLayer))
        {
            hitLeft = true;
            Debug.DrawLine(transform.position, leftHit.point, Color.magenta);
        }

        if (hitForward)
        {
            multiplier = FrontalCurveForce(forwardHit.distance, detectionRange);
        }
        else
        {
            multiplier = 1;
        }

        float force = 0;

        if (hitRight)
        {
            force -= CollisionCurveForce(rightHit.distance, detectionRange);
            // Debug.Log("right");
        }
        else if (hitLeft)
        {
            force += CollisionCurveForce(leftHit.distance, detectionRange);
            //Debug.Log("left");
        }
        //Debug.Log("multiplier: " + multiplier +" force: " + force);
        transform.Rotate(Vector3.up, (force * multiplier) * Time.deltaTime * 45f);
    }

    public void DetectFuturePlayerCollisions()
    {

        ResetHits();
        //Cast 
        ray = new Ray(transform.position - transform.right, transform.forward);
        if (Physics.Raycast(ray, out fwdLeftHit, detectionRange, playerLayer))
        {
            hitFwdLeft = true;
            Debug.DrawLine(transform.position - transform.right, fwdLeftHit.point, Color.cyan);
        }

        ray = new Ray(transform.position + transform.right, transform.forward);
        if (Physics.Raycast(ray, out fwdRightHit, detectionRange, playerLayer))
        {
            hitFwdRight = true;
            Debug.DrawLine(transform.position + transform.right, fwdRightHit.point, Color.cyan);
        }

        ray = new Ray(transform.position + transform.forward * 2f, -transform.right);
        if (Physics.Raycast(ray, out sideLeftHit, detectionRange, playerLayer))
        {
            hitSideLeft = true;
            Debug.DrawLine(transform.position + transform.forward * 2f, sideLeftHit.point, Color.cyan);
        }

        ray = new Ray(transform.position + transform.forward * 2f, transform.right);
        if (Physics.Raycast(ray, out sideRightHit, detectionRange, playerLayer))
        {
            hitSideRight = true;
            Debug.DrawLine(transform.position + transform.forward * 2f, sideRightHit.point, Color.cyan);
        }

        ray = new Ray(transform.position + transform.forward * 2f, -transform.right + transform.forward);
        if (Physics.Raycast(ray, out diagonalLeftHit, detectionRange, playerLayer))
        {
            hitDiagonalLeft = true;
            Debug.DrawLine(transform.position + transform.forward * 2f, diagonalLeftHit.point, Color.cyan);
        }

        ray = new Ray(transform.position + transform.forward * 2f, transform.right + transform.forward);
        if (Physics.Raycast(ray, out diagonalRightHit, detectionRange, playerLayer))
        {
            hitDiagonalRight = true;
            Debug.DrawLine(transform.position + transform.forward * 2f, diagonalRightHit.point, Color.cyan);
        }

        float force = 0;

        if (hitFwdLeft && hitFwdRight)
        {
            force += FrontalCurveForce(fwdLeftHit.distance, playerDetection) - FrontalCurveForce(fwdRightHit.distance, playerDetection);
        }
        else
        {
            if (hitFwdLeft)
            {
                force += CollisionCurveForce(fwdLeftHit.distance, playerDetection);
            }
            if (hitFwdRight)
            {
                force -= CollisionCurveForce(fwdRightHit.distance, playerDetection);
            }
        }

        if (hitSideLeft && hitDiagonalLeft)
        {
            if (diagonalLeftHit.distance < sideLeftHit.distance)
            {
                force += CollisionCurveForce(sideLeftHit.distance, playerDetection);
            }
            else
            {
                force += CollisionCurveForce(diagonalLeftHit.distance, playerDetection);
            }
        }
        else if (hitSideLeft)
        {
            force += CollisionCurveForce(sideLeftHit.distance, playerDetection);
        }
        else if (hitDiagonalLeft)
        {
            force += CollisionCurveForce(diagonalLeftHit.distance, playerDetection);
        }


        if (hitSideRight && hitDiagonalRight)
        {
            if (diagonalRightHit.distance < sideRightHit.distance)
            {
                force -= CollisionCurveForce(sideRightHit.distance, playerDetection);
            }
            else
            {
                force -= CollisionCurveForce(diagonalRightHit.distance, playerDetection);
            }
        }
        else if (hitSideRight)
        {
            force -= CollisionCurveForce(sideRightHit.distance, playerDetection);
        }
        else if (hitDiagonalRight)
        {
            force -= CollisionCurveForce(diagonalRightHit.distance, playerDetection);
        }



        transform.Rotate(Vector3.up, (force * multiplier) * Time.deltaTime * 60f);
    }
    #endregion


    private void OnDrawGizmos()
    {
        if (!debugGizmos)
            return;
        float angles = Mathf.Deg2Rad * diagonalAngle;
        Vector3 angleVector = transform.forward * (Mathf.Cos(angles));
        angleVector += transform.right * Mathf.Sin(angles);
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, (angleVector * detectionRange));
        angleVector = transform.forward * (Mathf.Cos(angles));
        angleVector -= transform.right * Mathf.Sin(angles);
        Gizmos.DrawRay(transform.position, (angleVector * detectionRange));

        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * detectionRange);
        if (wpCount != 0)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(wpPositions[waypointIndex % wpCount], waypointRange);
        }
        else
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, waypointRange);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position + transform.right, transform.forward * playerDetection);
        Gizmos.DrawRay(transform.position - transform.right, transform.forward * playerDetection);

        Gizmos.DrawRay(transform.position + transform.forward * 2f, transform.right * playerDetection);
        Gizmos.DrawRay(transform.position + transform.forward * 2f, -transform.right * playerDetection);
    }
}
