using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Waypoint and path creation editor.
/// <summary>
[CustomEditor(typeof(WaypointManager))]
public class WaypointEditor : Editor
{
    //manager reference
    private WaypointManager script;
	//new path name
	private string pathName = "Waypoints";
	//if we are placing new waypoints in editor
    private static bool placing = false;
    //new path gameobject
    private static GameObject path;
    //Path Manager reference for editing waypoints
    private static PathManager pathMan;
    //temporary list for editor created waypoints in a path
    private static List<GameObject> wpList = new List<GameObject>();   

    public void OnSceneGUI()
    {
        //with creation mode enabled, place new waypoints on keypress
        if (Event.current.type != EventType.keyDown || !placing)
            return;

        if (Event.current.keyCode == script.placementKey)
        {
            //cast a ray against mouse position
            Ray worldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hitInfo;
            if (Physics.Raycast(worldRay, out hitInfo))
            {
                Event.current.Use();
                //place a waypoint at clicked point
                PlaceWaypoint(hitInfo.point);
            }
            else
            {
                Debug.LogWarning("Waypoint Manager: Trying to place a waypoint but couldn't "
                                    + "find valid target. Have you clicked on a collider?");
            }
        }
    }

    public override void OnInspectorGUI()
    {
        //show default variables of manager
        DrawDefaultInspector();
        //get manager reference
        script = (WaypointManager)target;
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        SceneView view = GetSceneView();

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();

        //draw path text label
        //GUILayout.Label("Enter Path Name: ", GUILayout.Height(15));
        //display text field for creating a path with that name
        //pathName = EditorGUILayout.TextField(pathName, GUILayout.Height(15));
        pathName = "Waypoints";

        EditorGUILayout.EndHorizontal();

        //draw path creation button
        if (!placing && GUILayout.Button("Start Path", GUILayout.Height(40)))
        {
            if (pathName == "")
            {
                EditorUtility.DisplayDialog("No Path Name", "Please enter a unique name for your path.", "Ok");
                return;
            }

            if (script.transform.Find(pathName) != null)
            {
                if(EditorUtility.DisplayDialog("Path Exists Already",
                    "A path with this name exists already.\n\nWould you like to edit it?", "Ok", "Cancel"))
                {
                    Selection.activeTransform = script.transform.Find(pathName);
                }
                return;
            }

            //create a new container transform which will hold all new waypoints
            path = new GameObject(pathName);
            //reset position and parent container gameobject to this manager gameobject
            path.transform.position = script.gameObject.transform.position;
            path.transform.parent = script.gameObject.transform;
            StartPath();

            //we passed all prior checks, toggle waypoint placement
            placing = true;
            //focus sceneview for placement
            view.Focus();
        }

        GUI.backgroundColor = Color.yellow;

        //finish path button
        if (placing && GUILayout.Button("Finish Editing", GUILayout.Height(40)))
        {
			FinishPath();
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space();
        //draw instructions
        EditorGUILayout.HelpBox("Hint:\nPress 'Start Path' to begin waypoint placement mode. "
                        + "\nPress '" + script.placementKey + "' on your keyboard to place new waypoints in the Scene view. "
                        + "You have to place waypoints onto game objects with colliders.", MessageType.Info);
    }

    //when losing editor focus
    void OnDisable()
    {
		FinishPath();
    }

    //differ between path selection
    void StartPath()
    {
        pathMan = path.AddComponent<PathManager>();
        pathMan.waypoints = new Transform[0];
    }

	public static void ContinuePath(PathManager p)
	{
		path = p.gameObject;
		pathMan = p;
		placing = true;

        wpList.Clear();
        for (int i = 0; i < p.waypoints.Length; i++)
            wpList.Add(p.waypoints[i].gameObject);

        GetSceneView().Focus();
    }

    //path manager placement
    void PlaceWaypoint(Vector3 placePos)
    {
        //instantiate waypoint gameobject
        GameObject wayp = new GameObject("Waypoint");

        //with every new waypoint, our waypoints array should increase by 1
        //but arrays gets erased on resize, so we use a classical rule of three
        Transform[] wpCache = new Transform[pathMan.waypoints.Length];
        System.Array.Copy(pathMan.waypoints, wpCache, pathMan.waypoints.Length);

        pathMan.waypoints = new Transform[pathMan.waypoints.Length + 1];
        System.Array.Copy(wpCache, pathMan.waypoints, wpCache.Length);
        pathMan.waypoints[pathMan.waypoints.Length - 1] = wayp.transform;

        //this is executed on placement of the first waypoint:
        //we position our path container transform to the first waypoint position,
        //so the transform (and grab/rotate/scale handles) aren't out of sight
        if (wpList.Count == 0)
            pathMan.transform.position = placePos;

        //position current waypoint at clicked position in scene view
        wayp.transform.position = placePos;
        wayp.transform.rotation = Quaternion.Euler(-90, 0, 0);
        //parent it to the defined path 
        wayp.transform.parent = pathMan.transform;
        //add waypoint to temporary list
        wpList.Add(wayp);
        //rename waypoint to match the list count
        wayp.name = "Waypoint " + (wpList.Count - 1);
    }

	void FinishPath()
	{
		if (!placing) return;

		if (wpList.Count < 2)
		{
			Debug.LogWarning("Not enough waypoints placed. Cancelling.");
			//if we have created a path already, destroy it again
			if (path) DestroyImmediate(path);
		}
			
		//toggle placement off
		placing = false;
		//clear list with temporary waypoint references,
		//we only needed this for getting the waypoint count
		wpList.Clear();
		//reset path name input field
		pathName = "";
		//make the new path the active selection
		Selection.activeGameObject = path;
	}

    /// <summary>
    /// Gets the active SceneView or creates one.
    /// </summary>
    public static SceneView GetSceneView()
    {
        SceneView view = SceneView.lastActiveSceneView;
        if (view == null)
            view = EditorWindow.GetWindow<SceneView>();

        return view;
    }
}
