using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Custom path inspector.
/// <summary>
[CustomEditor(typeof(PathManager))]
public class PathEditor : Editor
{
    //define Serialized Objects we want to use/control
    //this will be our serialized reference to the inspected script
    private SerializedObject targetClass;
    //serialized waypoint array
    private SerializedProperty waypoint;
    //serialized waypoint array count
    private SerializedProperty waypointsCount;
    //serialized path gizmo property booleans
    private SerializedProperty check1;
    //serialized scene view gizmo colors
    private SerializedProperty color1;
    private SerializedProperty color2;

    //waypoint array size, define path to know where to lookup for this variable
    //(we expect an array, so it's "name_of_array.data_type.size")
    private static string wpArraySize = "waypoints.Array.size";
    //.data gives us the data of the array,
    //we replace this {0} token with an index we want to get
    private static string wpArrayData = "waypoints.Array.data[{0}]";
    //currently selected waypoint node for position/rotation editing
    private int activeNode = -1;
    //modifier action that executes specific path manipulations

    //called whenever this inspector window is loaded 
    public void OnEnable()
    {
        //we create a reference to our script object by passing in the target
        targetClass = new SerializedObject(base.target);

        //from this object, we pull out the properties we want to use
        //these are just the names of our variables in the manager
        check1 = targetClass.FindProperty("drawCurved");
        color1 = targetClass.FindProperty("pathEndColor");
        color2 = targetClass.FindProperty("lineColor");

        //set serialized waypoint array count by passing in the path to our array size
        waypointsCount = targetClass.FindProperty(wpArraySize);
        //reset selected waypoint
        activeNode = -1;
    }

    private Transform[] GetWaypointArray()
    {
        //get array count from serialized property and store its int value into var arrayCount
        var arrayCount = targetClass.FindProperty(wpArraySize).intValue;
        //create new waypoint transform array with size of arrayCount
        var transformArray = new Transform[arrayCount];
        //loop over waypoints
        for (var i = 0; i < arrayCount; i++)
        {
            //for each one use "FindProperty" to get the associated object reference
            //of waypoints array, string.Format replaces {0} token with index i
            //and store the object reference value as type of transform in transformArray[i]
            transformArray[i] = targetClass.FindProperty(string.Format(wpArrayData, i)).objectReferenceValue as Transform;
        }
        //finally return that array copy for modification purposes
        return transformArray;
    }

    //similiar to GetWaypointArray(), find serialized property which belongs to index
    //and set this value to parameter transform "waypoint" directly
    private void SetWaypoint(int index, Transform waypoint)
    {
        //reset selected waypoint before manipulating array
        activeNode = -1;
            
        targetClass.FindProperty(string.Format(wpArrayData, index)).objectReferenceValue = waypoint;
    }

    //similiar to SetWaypoint(), this will find the waypoint from array at index position
    //and returns it instead of modifying
    private Transform GetWaypointAtIndex(int index)
    {
        return targetClass.FindProperty(string.Format(wpArrayData, index)).objectReferenceValue as Transform;
    }

    //get the corresponding waypoint and destroy the whole gameobject in editor
    private void RemoveWaypointAtIndex(int index)
    {
        Undo.DestroyObjectImmediate(GetWaypointAtIndex(index).gameObject);

        //iterate over the array, starting at index,
        //and replace it with the next one
        for (int i = index; i < waypointsCount.intValue - 1; i++)
            SetWaypoint(i, GetWaypointAtIndex(i + 1));

        //decrement array count by 1
        waypointsCount.intValue--;
		RenameWaypoints(GetWaypointArray());
    }

    private void AddWaypointAtIndex(int index)
    {
        //increment array count so the waypoint array is one unit larger
        waypointsCount.intValue++;

        //since we're adding a new waypoint for example in the middle of the array,
        //we need to push all existing waypoints after that selected waypoint
        //1 slot upwards to have one free slot in the middle.
        for (int i = waypointsCount.intValue - 1; i > index; i--)
            SetWaypoint(i, GetWaypointAtIndex(i - 1));

        //create new waypoint gameobject
        GameObject wp = new GameObject("Waypoint " + (index + 1));
        Undo.RegisterCreatedObjectUndo(wp, "Created WP");

        //set its position to the selected one
        wp.transform.position = GetWaypointAtIndex(index).position;
        //parent it to the path gameobject
		wp.transform.SetParent(GetWaypointAtIndex(index).parent);
		wp.transform.SetSiblingIndex(index + 1);
        //finally, set this new waypoint after the one clicked in waypoints array
        SetWaypoint(index + 1, wp.transform);
		RenameWaypoints(GetWaypointArray());
		activeNode = index + 1;
    }

    //called whenever the inspector gui gets rendered
    public override void OnInspectorGUI()
    {
        //this pulls the relative variables from unity runtime and stores them in the object
        targetClass.Update();

        //get waypoint array
        var waypoints = GetWaypointArray();

        //don't draw inspector fields if the path contains less than 2 points
        //(a path with less than 2 points really isn't a path)
        if (waypointsCount.intValue < 2)
        {
            //button to create path manually
            if (GUILayout.Button("Create Path from Children"))
            {
                Undo.RecordObjects(waypoints, "Create Path");
                (targetClass.targetObject as PathManager).Create();
                SceneView.RepaintAll();
            }

            return;
        }
        
        //create new checkboxes for path gizmo property 
        check1.boolValue = EditorGUILayout.Toggle("Draw Smooth Lines", check1.boolValue);

        //create new property fields for editing waypoint gizmo colors
        EditorGUILayout.PropertyField(color1);
        EditorGUILayout.PropertyField(color2);

        //calculate path length of all waypoints
        Vector3[] wpPositions = new Vector3[waypoints.Length];
        for (int i = 0; i < waypoints.Length; i++)
            wpPositions[i] = waypoints[i].position;
                
        //button for switching over to the WaypointManager for further path editing
		if (GUILayout.Button("Continue Editing")) 
		{
			Selection.activeGameObject = (GameObject.FindObjectOfType(typeof(WaypointManager)) as WaypointManager).gameObject;
			WaypointEditor.ContinuePath(targetClass.targetObject as PathManager);
		}

		EditorGUILayout.Space();

        //waypoint index header
        GUILayout.Label("Waypoints: ", EditorStyles.boldLabel);

        float pathLength = WaypointManager.GetPathLength(wpPositions);
        //path length label, show calculated path length
        GUILayout.Label("Path Length: " + pathLength);

        //loop through the waypoint array
        for (int i = 0; i < waypoints.Length; i++)
        {
            GUILayout.BeginHorizontal();
            //indicate each array slot with index number in front of it
            GUILayout.Label(i + ".", GUILayout.Width(20));
            //create an object field for every waypoint
            EditorGUILayout.ObjectField(waypoints[i], typeof(Transform), true);

            //display an "Add Waypoint" button for every array row except the last one
            if (i < waypoints.Length && GUILayout.Button("+", GUILayout.Width(30f)))
            {
                AddWaypointAtIndex(i);
                break;
            }

            //display an "Remove Waypoint" button for every array row except the first and last one
            if (i > 0 && i < waypoints.Length - 1 && GUILayout.Button("-", GUILayout.Width(30f)))
            {
                RemoveWaypointAtIndex(i);
                break;
            }

            GUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Place To Ground", GUILayout.Height(35)))
        {
            PlaceToGround(waypoints);
        }
        if (GUILayout.Button("Rename Now", GUILayout.Height(35)))
        {
            RenameWaypoints(waypoints);
        }
        if (GUILayout.Button("Invert Direction", GUILayout.Height(35)))
        {
            InvertDirection(waypoints);
        }
        if (GUILayout.Button("Repath (Children Based)", GUILayout.Height(35)))
        {
            (targetClass.targetObject as PathManager).Create();
            SceneView.RepaintAll();
        }
        EditorGUILayout.EndHorizontal();

        //we push our modified variables back to our serialized object
        targetClass.ApplyModifiedProperties();
    }

    //if this path is selected, display small info boxes above all waypoint positions
    //also display handles for the waypoints
    void OnSceneGUI()
    {
        //again, get waypoint array
        var waypoints = GetWaypointArray();
        //do not execute further code if we have no waypoints defined
        //(just to make sure, practically this can not occur)
        if (waypoints.Length == 0) return;
        Vector3 wpPos = Vector3.zero;
        float size = 1f;

        //loop through waypoint array
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (!waypoints[i]) continue;
            wpPos = waypoints[i].position;
                
            size = HandleUtility.GetHandleSize(wpPos) * 0.4f;

            //do not draw waypoint header if too far away
            if (size < 3f)
            {
                //begin 2D GUI block
                Handles.BeginGUI();
                //translate waypoint vector3 position in world space into a position on the screen
                var guiPoint = HandleUtility.WorldToGUIPoint(wpPos);
                //create rectangle with that positions and do some offset
                var rect = new Rect(guiPoint.x - 50.0f, guiPoint.y - 40, 100, 20);
                //draw box at position with current waypoint name
                GUI.Box(rect, waypoints[i].name);
                Handles.EndGUI(); //end GUI block
            }

            //draw handles per waypoint, clamp size
            Handles.color = color2.colorValue;
            size = Mathf.Clamp(size, 0, 1.2f);
                
            #if UNITY_5_6_OR_NEWER
            Handles.FreeMoveHandle(wpPos, Quaternion.identity, size, Vector3.zero, (controlID, position, rotation, hSize, eventType) => 
            {
                Handles.SphereHandleCap(controlID, position, rotation, hSize, eventType);
                if(controlID == GUIUtility.hotControl && GUIUtility.hotControl != 0)
                    activeNode = i;
            });
            #else
            Handles.FreeMoveHandle(wpPos, Quaternion.identity, size, Vector3.zero, (controlID, position, rotation, hSize) => 
            {
                Handles.SphereCap(controlID, position, rotation, hSize);
                if(controlID == GUIUtility.hotControl && GUIUtility.hotControl != 0)
                    activeNode = i;
            });
            #endif

            Handles.RadiusHandle(waypoints[i].rotation, wpPos, size / 2);
        }
            
        if(activeNode > -1)
        {
            wpPos = waypoints[activeNode].position;
            Quaternion wpRot = waypoints[activeNode].rotation;
            switch(Tools.current)
            {
                case Tool.Move:
                    if(Tools.pivotRotation == PivotRotation.Global)
                        wpRot = Quaternion.identity;
                            
                    Vector3 newPos = Handles.PositionHandle(wpPos, wpRot);
                    if(wpPos != newPos)
                    {
                        Undo.RecordObject(waypoints[activeNode], "Move Handle");
                        waypoints[activeNode].position = newPos;
                    }
                    break;

                case Tool.Rotate:
                    Quaternion newRot = Handles.RotationHandle(wpRot, wpPos);

                    if(wpRot != newRot) 
                    {
                        Undo.RecordObject(waypoints[activeNode], "Rotate Handle");
                        waypoints[activeNode].rotation = newRot;
                    }
                    break;
            }
        }
            
        //waypoint direction handles drawing
        Vector3[] pathPoints = new Vector3[waypoints.Length+1];           
        for(int i = 0; i < pathPoints.Length-1; i++)
            pathPoints[i] = waypoints[i].position;
        pathPoints[pathPoints.Length - 1] = waypoints[0].position;

        //create list of path segments (list of Vector3 list)
        List<List<Vector3>> segments = new List<List<Vector3>>();
        int curIndex = 0;
        float lerpVal = 0f;
            
        //differ between linear and curved display
        switch(check1.boolValue)
        {
            case true:
                //convert waypoints to curved path points
                pathPoints = WaypointManager.GetCurved(pathPoints);
                //calculate approximate path point amount per segment
                int detail = Mathf.FloorToInt((pathPoints.Length - 1f) / (waypoints.Length - 1f));
                    
                for(int i = 0; i < waypoints.Length - 1; i++)
                {
                    float dist = Mathf.Infinity;
                    //loop over path points to find single segments
                    segments.Add(new List<Vector3>());
                        
                    //we are not checking for absolute path points on standard paths, because 
                    //path points could also be located before or after waypoint positions.
                    //instead a minimum distance is searched which marks the nearest path point
                    for(int j = curIndex; j < pathPoints.Length; j++)
                    {
                        //add path point to current segment
                        segments[i].Add(pathPoints[j]);
                            
                        //start looking for distance after a certain amount of path points of this segment
                        if(j >= (i+1) * detail)
                        {
                            //calculate distance of current path point to waypoint
                            float pointDist = Vector3.Distance(waypoints[i].position, pathPoints[j]);
                            //we are getting closer to the waypoint
                            if(pointDist < dist)
                                dist = pointDist;
                            else
                            {
                                //current path point is more far away than the last one
                                //the segment ends here, continue with new segment
                                curIndex = j + 1;
                                break;
                            }
                        }
                    }
                }
                break;
                 
            case false:
                //detail for arrows between waypoints
                int lerpMax = 16;
                //loop over waypoints to add intermediary points
                for(int i = 0; i < waypoints.Length - 1; i++)
                {
                    segments.Add(new List<Vector3>());
                    for(int j = 0; j < lerpMax; j++)
                    {
                        //linear lerp between waypoints to get additional points for drawing arrows at
                        segments[i].Add(Vector3.Lerp(pathPoints[i], pathPoints[i+1], j / (float)lerpMax));
                    }
                }
                break;
        }

        //loop over segments
        for(int i = 0; i < segments.Count; i++)
        {
            //loop over single positions on the segment
            for(int j = 0; j < segments[i].Count; j++)
            {
                //get current lerp value for interpolating rotation
                //draw arrow handle on current position with interpolated rotation
                lerpVal = j / (float)segments[i].Count;
                #if UNITY_5_6_OR_NEWER
                Handles.SphereHandleCap(0, segments[i][j], Quaternion.Lerp(waypoints[i].rotation, waypoints[i + 1].rotation, lerpVal), 0.35f, EventType.Repaint);
                #else
                Handles.SphereCap(0, segments[i][j], Quaternion.Lerp(waypoints[i].rotation, waypoints[i + 1].rotation, lerpVal), size);
                #endif
            }
        }
    }

	private void RenameWaypoints(Transform[] waypoints)
	{
		string wpName = string.Empty;
		for (int i = 0; i < waypoints.Length; i++)
		{
			//cache name and split into strings
			wpName = waypoints[i].name;
            wpName = "Waypoint " + i;
			//set the desired index or leave it
			waypoints[i].name = wpName;
		}
	}

    private void InvertDirection(Transform[] waypoints)
    {
        //to reverse the whole path we need to know where the waypoints were before
        //for this purpose a new copy must be created
        Vector3[] waypointCopy = new Vector3[waypoints.Length];
        for (int i = 0; i < waypoints.Length; i++)
            waypointCopy[i] = waypoints[i].position;

        //looping over the array in reversed order
        for (int i = 0; i < waypoints.Length; i++)
            waypoints[i].position = waypointCopy[waypointCopy.Length - 1 - i];
    }

    private void PlaceToGround(Transform[] waypoints)
    {
        foreach (Transform trans in waypoints)
        {
            //define ray to cast downwards waypoint position
            Ray ray = new Ray(trans.position + new Vector3(0, 20f, 0), -Vector3.up);
            RaycastHit hit;
            //cast ray against ground, if it hit:
            if (Physics.Raycast(ray, out hit, 100))
            {
                //position waypoint to hit point
                trans.position = hit.point;
            }
            //also try to raycast against 2D colliders
            RaycastHit2D hit2D = Physics2D.Raycast(ray.origin, -Vector2.up, 100);
            if (hit2D)
            {
                trans.position = new Vector3(hit2D.point.x, hit2D.point.y, trans.position.z);
            }
        }
    }
}