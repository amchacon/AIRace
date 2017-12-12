using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class HUDManager : Singleton<HUDManager>
{
    [Header("Menu Panels")]
    [SerializeField] private PopUpRaceResult endGamePopUp;
    [SerializeField] private GameObject modalFade;
    
    [Header("Ranking UI")]
    [SerializeField] private Transform contentHolder;
    [SerializeField] private UIRankItem playerUIPrefab;

    [Space(10)]
    [SerializeField] private Text lapCounterText;

    private int maxLaps;

    private void Start()
    {
        maxLaps = GameManager.Instance.lapsTotal;
    }

    /// <summary>
    /// Instantiates a new line (player) in the Ranking Panel
    /// </summary>
    public void InstantiateRankItem(Agent newPlayer)
    {
        UIRankItem playerUI = Instantiate(playerUIPrefab, contentHolder) as UIRankItem;
        playerUI.SetupUI(newPlayer.carInfo.id, newPlayer.carInfo.playerInfo.iconSprite, newPlayer.carInfo.playerInfo.Name, newPlayer.transform, newPlayer.totalWaypointsToWin);
    }

    /// <summary>
    /// Opens a dialogue box (PopUp) on race finish showing the winner information
    /// </summary>
    public void ShowEndGamePopUp(Players winnerPlayer)
    {
        modalFade.SetActive(true);
        endGamePopUp.gameObject.SetActive(true);
        endGamePopUp.SetWinnerResult(winnerPlayer);
    }

    /// <summary>
    /// Refreshes the lap counter on canvas
    /// </summary>
    public void RefreshLapCounter(int currentLap)
    {
        if (currentLap > maxLaps)
            currentLap = maxLaps;
        lapCounterText.text = currentLap + "/" + maxLaps;
    }

    /// <summary>
    /// Updates the position of race participants in the ranking (canvas)
    /// </summary>
    public void RebuildRanking(List<Agent> playersOnRace)
    {
        foreach (Transform child in contentHolder)
        {
            UIRankItem rankItem = child.GetComponent<UIRankItem>();
            int newIndex = playersOnRace.FindIndex(x => x.carInfo.id == rankItem.playerID);
            child.SetSiblingIndex(newIndex);
        }
    }

    /// <summary>
    /// Updates the progress bar of a given runner
    /// </summary>
    public void RefreshCarProgress(Agent car)
    {
        UIRankItem uiItem = contentHolder.GetComponentsInChildren<UIRankItem>().FirstOrDefault(x => x.playerID == car.carInfo.id);
        uiItem.RefreshLapProgressBar(car.waypointIndex);
    }
}
