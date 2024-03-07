using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;


public class PlayerSpawner : MonoBehaviour
{
  public GameObject playerPrefab;
  public GameObject deadEffect;
  private GameObject player;

  public float respawnTime = 5f;



  public static PlayerSpawner instance;

  private void Awake()
  {
    instance = this;
  }
    // Start is called before the first frame update
    void Start()
    {
      if(PhotonNetwork.IsConnected)
      {
        SpawnPlayer();
      }
    }

    public void SpawnPlayer()
    {
      Transform spawnPoint = SpawnManager.instance.GetSpawnPoint();

      player = PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, spawnPoint.rotation);
    }

    public void Die(string damager)
    {
      //Update UI Controller to death message.
      UIController.instance.deathText.text = "Killed by " + damager;
      MatchManager.instance.UpdateStatsSend(PhotonNetwork.LocalPlayer.ActorNumber,1,1);

      if(player != null)
      {
        StartCoroutine(DieCoroutine());
      }
    }

    public IEnumerator DieCoroutine()
    {
      PhotonNetwork.Instantiate(deadEffect.name, player.transform.position, Quaternion.identity);
      
      PhotonNetwork.Destroy(player);
      player = null;

      UIController.instance.deathScreen.SetActive(true);

      yield return new WaitForSeconds(respawnTime);
      UIController.instance.deathScreen.SetActive(false);

      if(MatchManager.instance.state == MatchManager.GameState.Playing && player == null)
      {
        SpawnPlayer();
      }
    }
}
