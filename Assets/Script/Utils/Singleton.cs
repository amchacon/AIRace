using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    private static T s_instance;

    /// <summary>
    /// The public static reference to the instance.
    /// </summary>
    public static T Instance {

        get {
            if (s_instance == null) Debug.Log("Missing " + s_instance + " reference.");
            return s_instance;
        }
        private set { s_instance = value; }
    }

    /// <summary>
    /// Gets whether an instance of this singleton exists.
    /// </summary>
    public static bool InstanceExists { get { return s_instance != null; } }

    /// <summary>
    /// Awake method to associate singleton with instance.
    /// </summary>
    protected virtual void Awake() {

        if (s_instance != null) {
            Destroy(gameObject);
            Debug.Log("WARNING: DUPLICATE INSTANCE DELETED - " + s_instance);
        }
        else {
            s_instance = (T)this;
        }
    }

    /// <summary>
    /// OnDestroy method to clear singleton association.
    /// </summary>
    protected virtual void OnDestroy() {

        if (s_instance == this) {
            s_instance = null;
        }
    }
}
