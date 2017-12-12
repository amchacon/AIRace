using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Stores waypoints, accessed by walker objects.
/// Provides gizmo visualization in the editor.
/// <summary>
public class PathManager : MonoBehaviour
{
    //Waypoint array creating the path.
    public Transform[] waypoints = new Transform[]{};
    //Toggles drawing of linear or curved gizmo lines.
    public bool drawCurved = true;
    //Gizmo color for path ends.
    public Color pathEndColor = new Color(1f, 0f, 0f, 0.7f);
    //Gizmo color for lines and waypoints.
    public Color lineColor = new Color(1f, 1f, 0f, 0.7f);
    //Gizmo size for path ends.
    public Vector3 size = new Vector3(.7f, .7f, .7f);
    //Gizmo radius for waypoints.
    public float radius = .4f;
    //Skip custom names on waypoint renaming.
    public bool skipCustomNames = true;
    //Gameobject for replacing waypoints.
    public GameObject replaceObject;

	//Auto-add to WaypointManager
    void Start()
    {
        WaypointManager.AddPath(gameObject);
    }

    /// <summary>
    /// Create or update waypoint representation from child objects or external parent.
    /// </summary>
    public void Create(Transform parent = null)
    {
        if (parent == null)
            parent = transform;

        List<Transform> childs = new List<Transform>();
        foreach(Transform child in parent)
            childs.Add(child);

        Create(childs.ToArray());
    }

    /// <summary>
    /// Create or update waypoint representation from the array passed in, optionally parenting them to the path.
    /// </summary>
    public virtual void Create(Transform[] waypoints, bool makeChildren = false)
    {
        if(waypoints.Length < 2)
        {
            Debug.LogWarning("Not enough waypoints placed - minimum is 2. Cancelling.");
            return;
        }

        if(makeChildren)
        {
            for(int i = 0; i < waypoints.Length; i++)
                waypoints[i].parent = transform;
        }

        this.waypoints = waypoints;
    }

    //Editor visualization
    void OnDrawGizmos()
    {
        if (waypoints.Length <= 0) return;

        //get positions
        Vector3[] wpPositions = GetPathPoints();

        //assign path ends color
        Vector3 start = wpPositions[0];
        Vector3 end = wpPositions[wpPositions.Length - 1];
        Gizmos.color = pathEndColor;
        Gizmos.DrawCube(start, size * GetHandleSize(start) * 1.5f);
        Gizmos.DrawCube(end, size * GetHandleSize(end) * 1.5f);

        //assign line and waypoints color
        Gizmos.color = lineColor;
        for (int i = 1; i < wpPositions.Length - 1; i++)
            Gizmos.DrawSphere(wpPositions[i], radius * GetHandleSize(wpPositions[i]));

        //draw linear or curved lines with the same color
        if (drawCurved && wpPositions.Length >= 2)
            WaypointManager.DrawCurved(wpPositions);
        else
            WaypointManager.DrawStraight(wpPositions);
    }

    //Helper method to get screen based handle sizes
    public virtual float GetHandleSize(Vector3 pos)
    {
        float handleSize = 1f;
        #if UNITY_EDITOR
            handleSize = UnityEditor.HandleUtility.GetHandleSize(pos) * 0.4f;
            handleSize = Mathf.Clamp(handleSize, 0, 1.2f);
        #endif
        return handleSize;
    }

    /// <summary>
    /// Returns waypoint positions (path positions) as Vector3 array.
    /// <summary>
    public virtual Vector3[] GetPathPoints()
    {
        Vector3[] pathPoints = new Vector3[waypoints.Length + 1];
        for (int i = 0; i < waypoints.Length; i++)
            pathPoints[i] = waypoints[i].position;
        pathPoints[pathPoints.Length-1] = waypoints[0].position;
        return pathPoints;
    }

    /// <summary>
	/// Returns this waypoint transform according to the index passed in.
	/// </summary>
    public virtual Transform GetWaypoint(int index)
    {
        return waypoints[index];
    }

	/// <summary>
	/// Returns waypoint length (should be equal to events count).
	/// </summary>
	public virtual int GetWaypointCount()
	{
		return waypoints.Length;
	}

    public virtual bool IsPathValid 
    {
        get { return GetWaypointCount() >= 2; }
    }
}
