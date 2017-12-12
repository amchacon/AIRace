using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(DataLoader))]
public class GameManager : Singleton<GameManager>
{
    public delegate void OnEndGame();
    public event OnEndGame NotifyEndGameObservers = delegate { };

    [HideInInspector] public int lapsTotal;

    [Header("Game Configuration")]
    [SerializeField, Range(2,10)] private int carsAmount = 8;
    [SerializeField] private GameObject playerCarPrefab;
    
    private Data data;
    private List<Players> playersOnGrid = new List<Players>();
    private List<Agent> playersOnRace = new List<Agent>();
    private PathManager pathManager;
    private int globalLap = 0;

    private IEnumerator Start ()
    {
        DataLoader dataLoader = GetComponent<DataLoader>();
        //Parse the json file “data.txt” placed in the streaming folder and store in memory
        data = dataLoader.LoadPlayerData();
        lapsTotal = data.GameConfiguration.lapsNumber;

        yield return new WaitForEndOfFrame();

        //Randomly choose 8 players that will take part in the race
        playersOnGrid = data.Players.PickRandom(carsAmount);
        //Order the 8 players by the velocity, from the slowest to the fastest
        playersOnGrid.Sort();
        
        pathManager = WaypointManager.Paths["Waypoints"];
        //If path manager valid then start spawning cars
        if (pathManager && pathManager.IsPathValid)
        {
            StartCoroutine(SpawnDelayed(playersOnGrid.Count, (data.GameConfiguration.playersInstantiationDelay / 1000)));                   //Delay is in Miliseconds
            StartCoroutine(CheckCurrentProgress());
        }
        HUDManager.Instance.RefreshLapCounter(0);
    }
    
    /// <summary>
    /// Spawn race member with delay
    /// </summary>
    private IEnumerator SpawnDelayed (int amountToSpawn , float delayTime)
    {
        delayTime = 1;
        WaitForSeconds delay = new WaitForSeconds(delayTime);
        yield return new WaitForSeconds(1);
        for (int i = 0; i < amountToSpawn; i++) {
            //Instantiate Car and Setup
            Agent carAI = Instantiate(playerCarPrefab, transform.position, Quaternion.identity).GetComponent<Agent>() as Agent;
            carAI.SetPathInfo(pathManager.GetPathPoints());
            carAI.SetCarInfo(playersOnGrid[i], i);
            playersOnRace.Add(carAI);
            HUDManager.Instance.InstantiateRankItem(carAI);
            yield return delay;
        }
    }

    /// <summary>
    /// Called when a member wins the race
    /// </summary>
    public void EndGame(int carID)
    {
        if (NotifyEndGameObservers != null)
            NotifyEndGameObservers();
        HUDManager.Instance.ShowEndGamePopUp(playersOnGrid[carID]);
    }

    public IEnumerator CheckCurrentProgress()
    {
        WaitForSeconds delay = new WaitForSeconds(1);
        for (;;)
        {
            //Ref List used to check if the members order has changed
            List<Agent> tempList = playersOnRace;
            //Reorder members list by highest progress (position)
            playersOnRace = playersOnRace.OrderByDescending(x => x.GetCurrentProgress()).ToList();

            if (!playersOnRace.SequenceEqual(tempList))
                HUDManager.Instance.RebuildRanking(playersOnRace);

            //Update Global Lap Counter
            if(playersOnRace.Count > 0 && globalLap != playersOnRace[0].currentLap)
            {
                HUDManager.Instance.RefreshLapCounter(playersOnRace[0].currentLap);
                globalLap = playersOnRace[0].currentLap;
            }
            yield return delay;
        }
    }
}
