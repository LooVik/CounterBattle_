using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;


public class UIController : MonoBehaviour
{
  public static UIController instance;

  public Slider weaponTempSlider;
  public GameObject deathScreen;
  public Slider healthSlider;


  public TMP_Text overheatedMessage;
  public TMP_Text deathText;
  public TMP_Text killsText;
  public TMP_Text deathsText;
  public TMP_Text timerText;

  public GameObject leaderBoard;
  public LeaderboardPlayer leaderBoardPlayerDisplay;

  public GameObject endScreen;
  public GameObject optionsScreen;



    private void Awake()
    {
      instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
      if(Input.GetKeyDown(KeyCode.Escape))
      {
        ShowOptions();
      }
      if(optionsScreen.activeInHierarchy && Cursor.lockState != CursorLockMode.None)
      {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
      }
    }

    public void ShowOptions()
    {
      if(!optionsScreen.activeInHierarchy)
      {
        optionsScreen.SetActive(true);
      }
      else
      {
        optionsScreen.SetActive(false);
      }
    }

    public void ReturnToMainMenu()
    {
      PhotonNetwork.AutomaticallySyncScene = false;
      PhotonNetwork.LeaveRoom();
    }

    public void QuitGame()
    {
      Application.Quit();
    }
}
