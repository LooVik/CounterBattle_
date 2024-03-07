using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class Launcher : MonoBehaviourPunCallbacks
{
  public static Launcher instance;
  public GameObject loadingScreen;
  public GameObject MenuButtons;
  public GameObject createRoomScreen;
  public GameObject roomScreen;
  public GameObject errorScreen;
  public GameObject roomBrowserScreen;
  public GameObject nameInputScreen;
  public RoomButton theRoomButton;

  public TMP_InputField roomNameInput;
  public TMP_Text roomNameText, playerNameLabel;
  public TMP_Text loadingText;
  public TMP_Text errorText;
  public TMP_InputField nameInput;

  private List<RoomButton> allRoomButtons = new List<RoomButton>();
  private List<TMP_Text> allPlayerNames = new List<TMP_Text>();
  public static bool hasSetNickName;

  public string levelToPlay;
  public GameObject startButton;

  public GameObject roomTestButton;

  public string[] allMaps;
  public bool changeMapBetweenRounds = true;



  private void Awake()
  {
    instance = this;
  }



    // Start is called before the first frame update
    void Start()
    {
      CloseMenus();
      loadingScreen.SetActive(true);
      loadingText.text = "Connecting to Network....";

      if(!PhotonNetwork.IsConnected)
      {
        PhotonNetwork.ConnectUsingSettings();
      }

#if UNITY_EDITOR
      roomTestButton.SetActive(true);
#endif

      Cursor.lockState = CursorLockMode.None;
      Cursor.visible = true;
    }

    void CloseMenus()
    {
      loadingScreen.SetActive(false);
      MenuButtons.SetActive(false);
      createRoomScreen.SetActive(false);
      roomScreen.SetActive(false);
      errorScreen.SetActive(false);
      roomBrowserScreen.SetActive(false);
      nameInputScreen.SetActive(false);

    }

    public override void OnConnectedToMaster()
    {
      PhotonNetwork.JoinLobby();
      //Photon will automatically control the scene to load which scene.
      PhotonNetwork.AutomaticallySyncScene = true;

      loadingText.text = "Joinning Lobby....";
    }

    public override void OnJoinedLobby()
    {
      CloseMenus();
      MenuButtons.SetActive(true);

      PhotonNetwork.NickName = Random.Range(0,1000).ToString();

      if(!hasSetNickName)
      {
        CloseMenus();
        nameInputScreen.SetActive(true);

        //Haskey is to check is there any name inside the list.
        if(PlayerPrefs.HasKey("playerName"))
        {
          nameInput.text = PlayerPrefs.GetString("playerName");
        }
      }
      else
      {
        PhotonNetwork.NickName = PlayerPrefs.GetString("playerName");
      }
    }

    public void OpenRoomCreate()
    {
      CloseMenus();
      createRoomScreen.SetActive(true);
    }

    public void CreateRoom()
    {
      if(!string.IsNullOrEmpty(roomNameInput.text))
      {
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 8;

        PhotonNetwork.CreateRoom(roomNameInput.text, options);
        CloseMenus();
        loadingText.text = "Creating Room....";
        loadingScreen.SetActive(true);
      }
    }

    public override void OnJoinedRoom()
    {
      CloseMenus();
      roomScreen.SetActive(true);

      roomNameText.text = PhotonNetwork.CurrentRoom.Name;
      ListAllPlayers();

      if(PhotonNetwork.IsMasterClient)
      {
        startButton.SetActive(true);
      }
      else
      {
        startButton.SetActive(false);
      }
    }

    private void ListAllPlayers()
    {
      foreach(TMP_Text player in allPlayerNames)
      {
        Destroy(player.gameObject);
      }
      allPlayerNames.Clear();

      Player[] players = PhotonNetwork.PlayerList;
      for(int i = 0; i < players.Length; i++)
      {
        TMP_Text newPlayerLabel = Instantiate(playerNameLabel, playerNameLabel.transform.parent);
        newPlayerLabel.text = players[i].NickName;
        newPlayerLabel.gameObject.SetActive(true);
        allPlayerNames.Add(newPlayerLabel);
      }

    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
      TMP_Text newPlayerLabel = Instantiate(playerNameLabel, playerNameLabel.transform.parent);
      newPlayerLabel.text = newPlayer.NickName;
      newPlayerLabel.gameObject.SetActive(true);
      allPlayerNames.Add(newPlayerLabel);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
      ListAllPlayers();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
      errorText.text = "Failed To Create Room:" + message;
      CloseMenus();
      errorScreen.SetActive(true);
    }

    public void CloseErrorScreen()
    {
      CloseMenus();
      MenuButtons.SetActive(true);
    }

    public void LeaveRoom()
    {
      PhotonNetwork.LeaveRoom();
      CloseMenus();
      loadingText.text = "Leaving Room";
      loadingScreen.SetActive(true);
    }

    public override void OnLeftRoom()
    {
      CloseMenus();
      MenuButtons.SetActive(true);
    }

    public void OpenRoomBrowser()
    {
      CloseMenus();
      roomBrowserScreen.SetActive(true);
    }

    //If Close/Leave Room Return to MenuButtons Screen.
    public void CloseRoomBrowser()
    {
      CloseMenus();
      MenuButtons.SetActive(true);
    }

    //Call anytime if there are a change of the room in the Lobby.
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
      foreach(RoomButton rb in allRoomButtons)
      {
        Destroy(rb.gameObject);
      }
      allRoomButtons.Clear();

      theRoomButton.gameObject.SetActive(false);

      //Loop Through and make sure all the room that are aviable and The amount of Player in the room. IF yes then will be removed.
      for(int i = 0; i < roomList.Count; i++)
      {
        if(roomList[i].PlayerCount != roomList[i].MaxPlayers && !roomList[i].RemovedFromList)
        {
          //If the current Room is created for was set into the button in the List.
          RoomButton newButton = Instantiate(theRoomButton, theRoomButton.transform.parent);
          newButton.SetButtonDetails(roomList[i]);
          newButton.gameObject.SetActive(true);

          allRoomButtons.Add(newButton);
        }
      }
    }

    public void SetNickName()
    {
      if(!string.IsNullOrEmpty(nameInput.text))
      {
        PhotonNetwork.NickName = nameInput.text;

        //Store Player NickName When its create. So that player do not need to type in all time.
        PlayerPrefs.SetString("playerName", nameInput.text);

        CloseMenus();
        MenuButtons.SetActive(true);

        hasSetNickName = true;

      }
    }

    public void JoinRoom(RoomInfo inputInfo)
    {
      PhotonNetwork.JoinRoom(inputInfo.Name);
      CloseMenus();
      loadingText.text = "Joinning Room";
      loadingScreen.SetActive(true);
    }

    public void StartGame()
    {
      //PhotonNetwork.LoadLevel(levelToPlay);
      PhotonNetwork.LoadLevel(allMaps[Random.Range(0,allMaps.Length)]);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
      if(PhotonNetwork.IsMasterClient)
      {
        startButton.SetActive(true);
      }
      else
      {
        startButton.SetActive(false);
      }
    }

    public void QuickJoin()
    {
      RoomOptions options = new RoomOptions();
      options.MaxPlayers = 8;

      PhotonNetwork.CreateRoom("Test", options);
      CloseMenus();
      loadingText.text = "Creating Room";
      loadingScreen.SetActive(true);
    }

    public void QuitGame()
    {
      Application.Quit();
    }
}
