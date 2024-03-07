using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;


public class MatchManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
  public static MatchManager instance;

  public List<PlayerInfo> allPlayers = new List<PlayerInfo>();
  //Keep track of users/player.
  private int index;
  private List<LeaderboardPlayer> lboardPlayers = new List<LeaderboardPlayer>();

  public int killsToWin = 2;
  public Transform mapCamPoint;
  public GameState state = GameState.Waiting;
  public float waitAfterEnding = 4f;
  public bool perpetual;

  public float matchLength = 180f;
  private float curretnMatchTime;

  private float sendTimer;

  private void Awake()
  {
    instance = this;
  }

  public enum EventCodes : byte
  {
    NewPlayer,
    ListPlayers,
    UpdateStat,
    NextMatch,
    TimerSync
  }

  public enum GameState
  {
    Waiting,
    Playing,
    Ending
  }



    // Start is called before the first frame update
    void Start()
    {
        if(!PhotonNetwork.IsConnected)
        {
          SceneManager.LoadScene(0);
        }
        else
        {
          NewPlayerSend(PhotonNetwork.NickName);
          state = GameState.Playing;
          SetUpTimer();
        }

        if(!PhotonNetwork.IsMasterClient)
        {
          UIController.instance.timerText.gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
      if(Input.GetKeyDown(KeyCode.Tab) && state != GameState.Ending)
      {
        if(UIController.instance.leaderBoard.activeInHierarchy)
        {
          UIController.instance.leaderBoard.SetActive(false);
        }
        else
        {
          ShowLeaderBoard();
        }
      }

      if(PhotonNetwork.IsMasterClient)
      {
        if(curretnMatchTime >0f && state == GameState.Playing)
        {
          curretnMatchTime -= Time.deltaTime;
          if(curretnMatchTime <= 0f)
          {
            curretnMatchTime = 0f;
            state = GameState.Ending;

              ListPlayersSend();
              StateCheck();
          }
          //ScoreCheck();
          UpdateTimerDisplay();
          sendTimer -= Time.deltaTime;
          if(sendTimer <= 0)
          {
            sendTimer += 1f;
          }
        }
      }
    }


    public void OnEvent(EventData photonEvent)
    {
      //Photon system reserved above event code of 200 like 201-250. So only check the code is below 200.
      if(photonEvent.Code < 200)
      {
        //Example explaining: If is code 1 it will convert into the events.
        EventCodes theEvent = (EventCodes)photonEvent.Code;
        //Whatever the CustomData received it will convert into the array of object. and will comvert the object into some information we can use.
        object[] data = (object[])photonEvent.CustomData;

        //Debug.Log("Received event " + theEvent);

        switch(theEvent)
        {
          case EventCodes.NewPlayer:
            NewPlayerReceive(data);
            break;

          case EventCodes.ListPlayers:
            ListPlayersReceive(data);
            break;

          case EventCodes.UpdateStat:
            UpdateStatsReceive(data);
            break;

          case EventCodes.NextMatch:
            NextMatchReceive();
            break;

          case EventCodes.TimerSync:
            TimerReceive(data);
            break;
        }
      }
    }

    public override void OnEnable()
    {
      //When event call back happen this will tell in editor.
      PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
      //when its remove it will tell the photon network system server and in game.
      PhotonNetwork.RemoveCallbackTarget(this);
    }

    //send to let the in game player knows that new comer player is in.
    public void NewPlayerSend(string username)
    {
      object[] package = new object[4];
      package[0] = username;
      package[1] = PhotonNetwork.LocalPlayer.ActorNumber;
      package[2] = 0; //kills.
      package[3] = 0; //deaths.

      PhotonNetwork.RaiseEvent(
        (byte) EventCodes.NewPlayer,
        package,
        new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient}, // Making sure that the only player will received a new player information is master client.
        new SendOptions { Reliability = true }
        );
    }

    public void NewPlayerReceive(object[] dataReceived)
    {
      PlayerInfo player = new PlayerInfo((string)dataReceived[0], (int)dataReceived[1], (int)dataReceived[2], (int)dataReceived[3]);
      allPlayers.Add(player);

      ListPlayersSend();
    }

    public void ListPlayersSend()
    {
      object[] package = new object[allPlayers.Count + 1];
      package[0] = state;


      for(int i = 0; i < allPlayers.Count; i++)
      {
        object[] piece = new object[4];
        piece[0] = allPlayers[i].name;
        piece[1] = allPlayers[i].actor;
        piece[2] = allPlayers[i].kills;
        piece[3] = allPlayers[i].deaths;

        package[i + 1] = piece;
      }

      PhotonNetwork.RaiseEvent(
        (byte) EventCodes.ListPlayers,
        package,
        new RaiseEventOptions { Receivers = ReceiverGroup.All}, // Making sure that the only player will received a new player information is All client.
        new SendOptions { Reliability = true }
        );
    }

    public void ListPlayersReceive(object[] dataReceived)
    {
      allPlayers.Clear();

      state = (GameState)dataReceived[0];

      for(int i = 1; i< dataReceived.Length; i++)
      {
        object[] piece = (object[])dataReceived[i];
        PlayerInfo player = new PlayerInfo(
          (string)piece[0],
          (int)piece[1],
          (int)piece[2],
          (int)piece[3]
        );

        allPlayers.Add(player);

        if(PhotonNetwork.LocalPlayer.ActorNumber == player.actor)
        {
          //indec i - 1 because (i starts at 1 but we adding into allPlayer list so the 1st one added will be in position 0 bot 1).
          index = i - 1;
        }
      }
      StateCheck();
    }

    public void UpdateStatsSend(int actorSending, int statToUpdate, int amountToChange)
    {
      object[] package = new object[] { actorSending, statToUpdate, amountToChange };

      PhotonNetwork.RaiseEvent(
        (byte) EventCodes.UpdateStat,
        package,
        new RaiseEventOptions { Receivers = ReceiverGroup.All}, // Making sure that the only player will received a new player information is All client.
        new SendOptions { Reliability = true }
        );
    }

    public void UpdateStatsReceive(object[] dataReceived)
    {
      int actor = (int)dataReceived[0];
      int statType = (int)dataReceived[1];
      int amount = (int)dataReceived[2];

      for(int i = 0; i < allPlayers.Count; i++)
      {
        if(allPlayers[i].actor == actor)
        {
          switch(statType)
          {
            case 0: //_kills
              allPlayers[i].kills += amount;
              Debug.Log("Player " + allPlayers[i].name + " : kills " + allPlayers[i].kills);
              break;

            case 1://deaths
              allPlayers[i].deaths += amount;
              Debug.Log("Player " + allPlayers[i].name + " deaths " + allPlayers[i].deaths);
              break;
          }
          break;
        }
        if(i == index)
        {
          UpdateStatsDisplay();
        }
      }
    }

    public void UpdateStatsDisplay()
    {
      if(allPlayers.Count > index)
      {
        UIController.instance.killsText.text = "Kills: " + allPlayers[index].kills;
        UIController.instance.deathsText.text = "Deaths: " + allPlayers[index].deaths;
      }
      else
      {
        UIController.instance.killsText.text = "Kills: 0";
        UIController.instance.deathsText.text = "Deaths: 0";
      }
    }

    void ShowLeaderBoard()
    {
      UIController.instance.leaderBoard.SetActive(true);
      foreach(LeaderboardPlayer lp in lboardPlayers)
      {
        Destroy(lp.gameObject);
      }
      lboardPlayers.Clear();

      UIController.instance.leaderBoardPlayerDisplay.gameObject.SetActive(false);

      List<PlayerInfo> sorted = SortPlayers(allPlayers);

      //Display all the player in the game server
      foreach(PlayerInfo player in allPlayers)
      {
        LeaderboardPlayer newPlayerDisplay = Instantiate(UIController.instance.leaderBoardPlayerDisplay, UIController.instance.leaderBoardPlayerDisplay.transform.parent);

        newPlayerDisplay.SetDetails(player.name, player.kills, player.deaths);
        newPlayerDisplay.gameObject.SetActive(true);
        lboardPlayers.Add(newPlayerDisplay);
      }
    }

    private List<PlayerInfo> SortPlayers(List<PlayerInfo> players)
    {
      List<PlayerInfo> sorted = new List<PlayerInfo>();

      while(sorted.Count < players.Count)
      {
        int highest = -1;
        PlayerInfo selectedPlayer = players[0];

        foreach(PlayerInfo player in players)
        {
          if(!sorted.Contains(player))
          {
            if(player.kills > highest)
            {
              selectedPlayer = player;
              highest = player.kills;
            }
          }
        }
        sorted.Add(selectedPlayer);
      }

      return sorted;
    }

    public override void OnLeftRoom()
    {
      base.OnLeftRoom();
      SceneManager.LoadScene(0);
    }

    void ScoreCheck()
    {
      bool winnerFound = false;
      foreach(PlayerInfo player in allPlayers)
      {
        if(player.kills >= killsToWin && killsToWin > 0)
        {
          winnerFound = true;
          break;
        }
      }

      if(winnerFound)
      {
        if(PhotonNetwork.IsMasterClient && state != GameState.Ending)
        {
          state = GameState.Ending;
          ListPlayersSend();
        }
      }
    }

    void StateCheck()
    {
      if(state == GameState.Ending)
      {
        EndGame();
      }
    }

    void EndGame()
    {
      state = GameState.Ending;

      if(PhotonNetwork.IsMasterClient)
      {
        PhotonNetwork.DestroyAll();
      }

      UIController.instance.endScreen.SetActive(true);
      ShowLeaderBoard();

      Cursor.lockState = CursorLockMode.None;
      Cursor.visible = true;

      Camera.main.transform.position = mapCamPoint.position;
      Camera.main.transform.rotation = mapCamPoint.rotation;

      StartCoroutine(EndCo());
    }

    private IEnumerator EndCo()
    {
      yield return new WaitForSeconds(waitAfterEnding);

      if(!perpetual)
      {
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.LeaveRoom();
      }
      else
      {
        if(PhotonNetwork.IsMasterClient)
        {
          if(!Launcher.instance.changeMapBetweenRounds)
          {
            NextMatchSend();
          }
          else
          {
            int newLevel = Random.Range(0, Launcher.instance.allMaps.Length);
            if(Launcher.instance.allMaps[newLevel] == SceneManager.GetActiveScene().name)
            {
              NextMatchSend();
            }
            else
            {
              PhotonNetwork.LoadLevel(Launcher.instance.allMaps[newLevel]);
            }
          }
        }
      }
    }

    public void NextMatchSend()
    {
      PhotonNetwork.RaiseEvent(
        (byte) EventCodes.NextMatch,
        null,
        new RaiseEventOptions { Receivers = ReceiverGroup.All}, // Making sure that the only player will received a new player information is All client.
        new SendOptions { Reliability = true }
        );
    }

    public void NextMatchReceive()
    {
      state = GameState.Playing;
      UIController.instance.endScreen.SetActive(false);
      UIController.instance.leaderBoard.SetActive(false);

      foreach(PlayerInfo player in allPlayers)
      {
        player.kills = 0;
        player.deaths = 0;
      }
      UpdateStatsDisplay();
      PlayerSpawner.instance.SpawnPlayer();
      SetUpTimer();
    }

    public void SetUpTimer()
    {
      if(matchLength > 0)
      {
        curretnMatchTime = matchLength;
        UpdateTimerDisplay();
      }
    }

    public void UpdateTimerDisplay()
    {
      //Start Timer from seconds.
      var timeToDisplay = System.TimeSpan.FromSeconds(curretnMatchTime);
      UIController.instance.timerText.text = timeToDisplay.Minutes.ToString("00") + " " + timeToDisplay.Seconds.ToString("00");
    }

    public void TimerSend()
    {
      object[] package = new object[] { (int)curretnMatchTime, state };

      PhotonNetwork.RaiseEvent(
        (byte) EventCodes.TimerSync,
        package,
        new RaiseEventOptions { Receivers = ReceiverGroup.All}, // Making sure that the only player will received a new player information is All client.
        new SendOptions { Reliability = true }
        );
    }

    public void TimerReceive(object[] dataReceived)
    {
      curretnMatchTime = (int)dataReceived[0];
      state = (GameState)dataReceived[1];

      UpdateTimerDisplay();
      UIController.instance.timerText.gameObject.SetActive(true);
    }

}

[System.Serializable]
public class PlayerInfo
{
  public string name;
  public int actor, kills, deaths;

  public PlayerInfo(string _name, int _actor, int _kills, int _deaths)
  {
    name = _name;
    actor = _actor;
    kills = _kills;
    deaths = _deaths;
  }

}
