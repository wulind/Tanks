using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace TanksMP
{
	/// <summary>
	/// Manages game workflow and functions such as team fill, scores and ending a game
	/// </summary>
	public class GameManager : MonoBehaviour
	{
		//reference to this script instance
		private static GameManager instance;
        
		/// <summary>
		/// The local player instance spawned for this client.
		/// </summary>
		[HideInInspector]
		public Player localPlayer;
        
		/// <summary>
		/// Reference to the UI script displaying game stats.
		/// </summary>
		public UIGame ui;
        
		/// <summary>
		/// Definition of playing teams with additional properties.
		/// </summary>
		public Team[] teams;
        
		/// <summary>
		/// List storing team fill for each team.
		/// E.g. if size[0] = 2, there are two players in team 0.
		/// </summary>
		public List<int> size;
        
		/// <summary>
		/// List storing team scores for each team.
		/// E.g. if score[0] = 2, team 0 scored 2 points.
		/// </summary>
		public List<int> score;

		/// <summary>
		/// The maximum amount of kills to reach before ending the game.
		/// </summary>
		public int maxScore = 30;

		/// <summary>
		/// The delay in seconds before respawning a player after it got killed.
		/// </summary>
		public int respawnTime = 5;
        
		/// <summary>
		/// The delay in seconds before respawning a player after it got killed.
		/// </summary>
		public GameObject playerPrefab;

		/// <summary>
		/// The name of the player to appear above the tank.
		/// </summary>
		public string playerName = "Player";

		/// <summary>
		/// Keeps track of whether the game has ended.
		/// </summary>
		[HideInInspector]
		public bool gameOver;

		/// <summary>
		/// Keeps track of network for network game
		/// </summary>
		public NetworkManager networkManager;

		//initialize variables
		void Awake ()
		{
			instance = this;

			networkManager = GameObject.FindObjectOfType<NetworkManager>();
			networkManager.gameObject.SetActive(false);
			
			GameObject[] networkedObjects = GameObject.FindGameObjectsWithTag("Network");
			foreach(GameObject obj in networkedObjects){
				obj.gameObject.SetActive(false);
			}
			
		}

		/// <summary>
		/// Creates bots for local game
		/// </summary>
		public void CreateLocalGame()
		{
			if (size.Count != teams.Length)
			{
				for (int i = 0; i < teams.Length; i++)
				{
					size.Add (0);
					score.Add (0);
				}
			}

			//call the hooks manually for the first time, for each team
			for (int i = 0; i < teams.Length; i++)
				ui.OnTeamSizeChanged (i);
			for (int i = 0; i < teams.Length; i++)
				ui.OnTeamScoreChanged (i);
		
			//get the team value for this player
			int teamIndex = GameManager.GetInstance ().GetTeamFill ();
			//get spawn position for this team and instantiate the player there
			Vector3 startPos = GameManager.GetInstance ().GetSpawnPosition (teamIndex);
			playerPrefab = (GameObject)Instantiate (playerPrefab, startPos, Quaternion.identity);
            
			//assign name and team to Player component
			Player p = playerPrefab.GetComponent<Player> ();
			p.myName = playerName;
			p.teamIndex = teamIndex;

			DeactivateMenu();
		}

		/// <summary>
		/// Activates Network Manager for network game
		/// </summary>
		public void CreateNetworkGame()
		{
			networkManager.gameObject.SetActive(true);
			UIGame UICanvas = GameObject.FindObjectOfType<UIGame>(); //kill counters not needed
			UICanvas.gameObject.SetActive(false);

			GameObject[] SPObjects = GameObject.FindGameObjectsWithTag("SinglePlayer");
			foreach(GameObject obj in SPObjects){
				obj.gameObject.SetActive(false);
			}

			DeactivateMenu();
		}

		/// <summary>
		/// Deactivates buttons
		/// </summary>
		public void DeactivateMenu ()
		{
			GameObject menu = GameObject.FindWithTag("Menu");
			menu.gameObject.SetActive(false);
		}


		/// <summary>
		/// Returns a reference to this script instance.
		/// </summary>
		public static GameManager GetInstance ()
		{
			return instance;
		}

		/// <summary>
		/// Returns the next team index a player should be assigned to.
		/// </summary>
		public int GetTeamFill ()
		{
			//init variables
			int teamNo = 0;

			int min = size [0];
			//loop over teams to find the lowest fill
			for (int i = 0; i < teams.Length; i++)
			{
				//if fill is lower than the previous value
				//store new fill and team for next iteration
				if (size [i] < min)
				{
					min = size [i];
					teamNo = i;
				}
			}
            
			//return index of lowest team
			return teamNo;
		}

        
		/// <summary>
		/// Returns a random spawn position within the team's spawn area.
		/// </summary>
		public Vector3 GetSpawnPosition (int teamIndex)
		{
			//init variables
			Vector3 pos = teams [teamIndex].spawn.position;
			BoxCollider col = teams [teamIndex].spawn.GetComponent<BoxCollider> ();

			if (col != null)
			{
				//find a position within the box collider range, first set fixed y position
				//the counter determines how often we are calculating a new position if out of range
				pos.y = col.transform.position.y;
				int counter = 10;
                
				//try to get random position within collider bounds
				//if it's not within bounds, do another iteration
				do
				{
					pos.x = UnityEngine.Random.Range (col.bounds.min.x, col.bounds.max.x);
					pos.z = UnityEngine.Random.Range (col.bounds.min.z, col.bounds.max.z);
					counter--;
				} while(!col.bounds.Contains (pos) && counter > 0);
			}
            
			return pos;
		}

		/// <summary>
		/// Returns whether a team reached the maximum game score.
		/// </summary>
		public bool IsGameOver ()
		{
			//init variables
			bool isOver = false;
            
			//loop over teams to find the highest score
			for (int i = 0; i < teams.Length; i++)
			{
				//score is greater or equal to max score,
				//which means the game is finished
				if (score [i] >= maxScore)
				{
					isOver = true;
					gameOver = true;
					break;
				}
			}
            
			//return the result
			return isOver;
		}

        
		/// <summary>
		/// Only for this player: sets the death text stating the killer on death.
		/// </summary>
		public void DisplayDeath (bool skipAd = false)
		{   
			//get the player component that killed us
			Player other = localPlayer.killedBy.GetComponent<Player> ();
			//increase local death counter for this game
			ui.killCounter [1].text = (int.Parse (ui.killCounter [1].text) + 1).ToString ();
			ui.killCounter [1].GetComponent<Animator> ().Play ("Animation");

			//set the death text
			//and start waiting for the respawn delay immediately
			ui.SetDeathText (other.myName, teams [other.teamIndex]);
			StartCoroutine (SpawnRoutine ());
		}
        
        
		//coroutine spawning the player after a respawn delay
		IEnumerator SpawnRoutine ()
		{
			//calculate point in time for respawn
			float targetTime = Time.time + respawnTime;
           
			//wait for the respawn to be over,
			//while waiting update the respawn countdown
			while (targetTime - Time.time > 0)
			{
				ui.SetSpawnDelay (targetTime - Time.time);
				yield return null;
			}
            
			//respawn now: send request to the server
			ui.DisableDeath ();
			localPlayer.Respawn ();
		}

        
		/// <summary>
		/// Only for this player: sets game over text stating the winning team.
		/// Disables player movement so no updates are sent through the network.
		/// </summary>
		public void DisplayGameOver (int teamIndex)
		{           
			localPlayer.enabled = false;
			localPlayer.disableInput = true;
			ui.SetGameOverText (teams [teamIndex]);
            
			//starts coroutine for displaying the game over window
			StartCoroutine (DisplayGameOver ());
		}
        
        
		//displays game over window after short delay
		IEnumerator DisplayGameOver ()
		{
			//give the user a chance to read which team won the game
			//before enabling the game over screen
			yield return new WaitForSeconds (3);
            
			//show game over window and disconnect from network
			ui.ShowGameOver ();
		}
	}

    
	/// <summary>
	/// Defines properties of a team.
	/// </summary>
	[System.Serializable]
	public class Team
	{
		/// <summary>
		/// The name of the team shown on game over.
		/// </summary>
		public string name;
             
		/// <summary>
		/// The color of a team for UI and player prefabs.
		/// </summary>   
		public Material material;
            
		/// <summary>
		/// The spawn point of a team in the scene. In case it has a BoxCollider
		/// component attached, a point within the collider bounds will be used.
		/// </summary>
		public Transform spawn;
	}
}