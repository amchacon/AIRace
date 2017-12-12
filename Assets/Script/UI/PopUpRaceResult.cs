using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PopUpRaceResult : MonoBehaviour
{
    [SerializeField] private Image winnerIcon;
    [SerializeField] private Text winnerName;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button closeButton;

    public void SetWinnerResult(Players winnerPlayer)
    {
        winnerIcon.sprite = winnerPlayer.iconSprite == null ? winnerIcon.sprite : winnerPlayer.iconSprite;
        winnerName.text = winnerPlayer.Name;
    }

    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
