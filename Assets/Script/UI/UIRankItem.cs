using UnityEngine;
using UnityEngine.UI;

public class UIRankItem : MonoBehaviour
{
    [HideInInspector] public int playerID;
    [SerializeField] private Image playerIcon;
    [SerializeField] private Text playerName;
    [SerializeField] private Slider lapProgressBar;
    [SerializeField] private Button camViewButton;

    private SmoothCamera smoothCamera;

    private void Start()
    {
        smoothCamera = Camera.main.GetComponent<SmoothCamera>();
    }

    /// <summary>
    /// Setup UI ranking player info
    /// </summary>
    public void SetupUI(int idRef, Sprite icon, string name, Transform car, int totalProgress)
    {
        playerID = idRef;
        playerIcon.sprite = icon == null ? playerIcon.sprite : icon;
        playerName.text = name;
        lapProgressBar.value = 0;
        lapProgressBar.maxValue = totalProgress;
        
        //Change Camera event
        camViewButton.onClick.AddListener(() => {
            smoothCamera.SetTarget(car);
        } );
    }

    public void RefreshLapProgressBar(int newValue)
    {
        lapProgressBar.value = newValue;
    }
}
