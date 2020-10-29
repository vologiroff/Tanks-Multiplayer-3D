/*  This file is part of the "Tanks Multiplayer" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using static TanksMP.MultGameManager;

namespace TanksMP
{
    /// <summary>
    /// Custom implementation of the most Photon callback handlers for network workflows. This script is
    /// responsible for connecting to Photon's Cloud, spawning players and handling disconnects.
    /// </summary>
	public class NetworkManagerCustom : MonoBehaviourPunCallbacks
    {
        //reference to this script instance
        private static NetworkManagerCustom instance;

        /// <summary>
        /// Scene index that gets loaded when disconnecting from a game.
        /// </summary>
        public int offlineSceneIndex = 0;

        /// <summary>
        /// Scene index that gets loaded after a connection has been established.
        /// Will get overridden by random matching scene, when using GameMode filtering.
        /// </summary>
        public int onlineSceneIndex = 1;

        /// <summary>
        /// Maximum amount of players per room.
        /// </summary>
        public int maxPlayers = 12;

        /// <summary>
        /// References to the available player prefabs located in a Resources folder.
        /// </summary>
        public GameObject[] playerPrefabs;

        /// <summary>
        /// Event fired when a connection to the matchmaker service failed.
        /// </summary>
        public static event Action connectionFailedEvent;

        private UIGame uIGame;


        //initialize network view
        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            //adding a view to this gameobject with a unique viewID
            //this is to avoid having the same ID in a scene
            PhotonView view = gameObject.AddComponent<PhotonView>();
            view.ViewID = 999;
        }


        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static NetworkManagerCustom GetInstance()
        {
            return instance;
        }


        /// <summary>
        /// Starts initializing and connecting to a game. Depends on the selected network mode.
        /// Sets the current player name prior to connecting to the servers.
        /// </summary>
        public static void StartMatch(NetworkMode mode)
        {
            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.NickName = PlayerPrefs.GetString(PrefsKeys.playerName);

            switch (mode)
            {
                //connects to a cloud game available on the Photon servers
                case NetworkMode.Online:
                    PhotonNetwork.ConnectUsingSettings();
                    break;

                //search for open LAN games on the current network, otherwise open a new one
                case NetworkMode.LAN:
                    PhotonNetwork.ConnectToMaster(PlayerPrefs.GetString(PrefsKeys.serverAddress), 5055, PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime);
                    break;

                //enable Photon offline mode to not send any network messages at all
                case NetworkMode.Offline:
                    PhotonNetwork.OfflineMode = true;
                    break;
            }
        }


        /// <summary>
        /// Called if a connect call to the Photon server failed before or after the connection was established.
        /// See the official Photon docs for more details.
        /// </summary>
        public override void OnDisconnected(DisconnectCause cause)
        {
            if (connectionFailedEvent != null)
                connectionFailedEvent();

            //do not switch scenes automatically when the game over screen is being shown already
            if (MultGameManager.GetInstance() != null && MultGameManager.GetInstance().ui.gameOverMenu.activeInHierarchy)
                return;

            //switch from the online to the offline scene after connection is closed
            if (SceneManager.GetActiveScene().buildIndex != offlineSceneIndex)
                SceneManager.LoadScene(offlineSceneIndex);
        }


        /// <summary>
        /// Called after the connection to the master is established.
        /// See the official Photon docs for more details.
        /// </summary>
        public override void OnConnectedToMaster()
        {
            //set my own name and try joining a game
            PhotonNetwork.NickName = PlayerPrefs.GetString(PrefsKeys.playerName);

            //use this to define per-mode matchmaking selections instead of joining random rooms (also see OnPhotonRandomJoinFailed() method)
            //https://doc.photonengine.com/en-us/realtime/current/reference/matchmaking-and-lobby#not_so_random_matchmaking
            Hashtable expectedCustomRoomProperties = new Hashtable() { { "mode", (byte)PlayerPrefs.GetInt(PrefsKeys.gameMode) } };

            //for truly random matchmaking you would use this call without properties
            //PhotonNetwork.JoinRandomRoom();
            PhotonNetwork.JoinRandomRoom(expectedCustomRoomProperties, (byte)this.maxPlayers);
        }


        /// <summary>
        /// Called when a joining a random room failed.
        /// See the official Photon docs for more details.
        /// </summary>
        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.Log("Photon did not find any matches on the Master Client we are connected to. Creating our own room...");

            //joining failed so try to create our own room
            RoomOptions roomOptions = new RoomOptions();

            //same as in OnCennectedToMaster() method above, here we are setting room properties for matchmaking
            //comment out for fully random matchmaking
            roomOptions.CustomRoomPropertiesForLobby = new string[] { "mode" };
            roomOptions.CustomRoomProperties = new Hashtable() { { "mode", (byte)PlayerPrefs.GetInt(PrefsKeys.gameMode) } };

            roomOptions.MaxPlayers = (byte)this.maxPlayers;
            roomOptions.CleanupCacheOnLeave = false;
            PhotonNetwork.CreateRoom(null, roomOptions, null);
        }


        /// <summary>
        /// Called when a creating a room failed. 
        /// See the official Photon docs for more details.
        /// </summary>
        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            if (connectionFailedEvent != null)
                connectionFailedEvent();
        }


        /// <summary>
        /// Called when this client created a room and entered it.
        /// See the official Photon docs for more details.
        /// </summary>
        public override void OnCreatedRoom()
        {
            //the initial team size of the game for the server creating a new room.
            //unfortunately this cannot be set via the GameManager because it does not exist at that point
            short initialArrayLength;
            //get the selected game mode out of PlayerPrefs
            GameMode activeGameMode = ((GameMode)PlayerPrefs.GetInt(PrefsKeys.gameMode));

            //set the initial room array size initialization based on game mode
            switch(activeGameMode)
            {
                case GameMode.CTF:
                    initialArrayLength = 2;
                    break;
                default:
                    initialArrayLength = 4;
                    break;
            }

            //we created a room so we have to set the initial room properties for this room,
            //such as populating the team fill and score arrays
            Hashtable roomProps = new Hashtable();
            roomProps.Add(RoomExtensions.size, new int[initialArrayLength]);
            roomProps.Add(RoomExtensions.score, new int[initialArrayLength]);
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

            //load the online scene randomly out of all available scenes for the selected game mode
            //we are checking for a naming convention here, if a scene starts with the game mode abbreviation
            List<int> matchingScenes = new List<int>();
            for(int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string[] scenePath = SceneUtility.GetScenePathByBuildIndex(i).Split('/');
                if (scenePath[scenePath.Length - 1].StartsWith(activeGameMode.ToString()))
                {
                    matchingScenes.Add(i);
                }
            }
			
			//check that your scene begins with the game mode abbreviation
			if(matchingScenes.Count == 0)
            {
                Debug.LogWarning("No Scene for selected Game Mode found in Build Settings!");
                return;
            }

            //get random scene out of available scenes and assign it as the online scene
            onlineSceneIndex = matchingScenes[UnityEngine.Random.Range(0, matchingScenes.Count)];
            //then load it
            PhotonNetwork.LoadLevel(onlineSceneIndex);
            //Debug.Log("Loaded " + onlineSceneIndex);
        }


        /// <summary>
        /// Called on entering a lobby on the Master Server.
        /// See the official Photon docs for more details.
        /// </summary>
        public override void OnJoinedLobby()
        {
            //when connecting to the master, try joining a room
            PhotonNetwork.JoinRandomRoom();
        }


        /// <summary>
        /// Called when entering a room (by creating or joining it).
        /// See the official Photon docs for more details.
        /// </summary>
        public override void OnJoinedRoom()
        {
            //we've joined a finished room, disconnect immediately
            if (MultGameManager.GetInstance() != null && MultGameManager.GetInstance().IsGameOver())
            {
                PhotonNetwork.Disconnect();
                return;
            }

            if (!PhotonNetwork.IsMasterClient)
                return;

            //add ourselves to the game. This is only called for the master client
            //because other clients will trigger the OnPhotonPlayerConnected callback directly
            StartCoroutine(WaitForSceneChange());
        }


        //this wait routine is needed on offline mode for waiting on completed scene change,
        //because in offline mode Photon does not pause network messages. But it doesn't hurt
        //leaving this in for all other network modes too
        IEnumerator WaitForSceneChange()
        {
            while (SceneManager.GetActiveScene().buildIndex != onlineSceneIndex)
            {
                yield return null;
            }

            //we connected ourselves
            OnPlayerEnteredRoom(PhotonNetwork.LocalPlayer);
        }


        /// <summary>
        /// Called when a remote player entered the room. 
        /// See the official Photon docs for more details.
        /// </summary>
        public override void OnPlayerEnteredRoom(Photon.Realtime.Player player)
        {
            //only let the master client handle this connection
            if (!PhotonNetwork.IsMasterClient)
                return;

            //get the next team index which the player should belong to
            //assign it to the player and update player properties
            int teamIndex = MultGameManager.GetInstance().GetTeamFill();
            PhotonNetwork.CurrentRoom.AddSize(teamIndex, +1);
            player.SetTeam(teamIndex);

            //also player properties are not cleared when disconnecting and connecting
            //automatically, so we have to set all existing properties to null
            //these default values will get overriden by correct data soon
            player.Clear();

            //the master client sends an instruction to this player for adding him to the game
            this.photonView.RPC("AddPlayer", player);
        }


        //received from the master client, for this player, after successfully joining a game
		[PunRPC]
		void AddPlayer()
		{
            //get our selected player prefab index
			int prefabId = int.Parse(Encryptor.Decrypt(PlayerPrefs.GetString(PrefsKeys.activeTank)));
            //Debug.Log(PhotonNetwork.LocalPlayer.GetTeam());

            

            //get the spawn position where our player prefab should be instantiated at, depending on the team assigned
            //if we cannot get a position, spawn it in the center of that team area - otherwise use the calculated position
            Transform startPos = MultGameManager.GetInstance().teams[PhotonNetwork.LocalPlayer.GetTeam()].spawn;
			if (startPos != null)
                PhotonNetwork.Instantiate(playerPrefabs[prefabId].name, startPos.position, startPos.rotation, 0);
			else
                PhotonNetwork.Instantiate(playerPrefabs[prefabId].name, Vector3.zero, Quaternion.identity, 0);

            
		}


        /// <summary>
        /// Called when a remote player left the room.
        /// See the official Photon docs for more details.
        /// </summary>
        public override void OnPlayerLeftRoom(Photon.Realtime.Player player)
        {
            //only let the master client handle this connection
            if (!PhotonNetwork.IsMasterClient)
				return;

            //get player-controlled game object from disconnected player
            GameObject targetPlayer = GetPlayerGameObject(player);

            //process any collectibles assigned to that player
            if(targetPlayer != null)
            {
                Collectible[] collectibles = targetPlayer.GetComponentsInChildren<Collectible>(true);
                for (int i = 0; i < collectibles.Length; i++)
                {
                    //let the player drop the Collectible
                    PhotonNetwork.RemoveRPCs(collectibles[i].spawner.photonView);
                    collectibles[i].spawner.photonView.RPC("Drop", RpcTarget.AllBuffered, targetPlayer.transform.position);
                }
            }

            //clean up instances after processing leaving player
            PhotonNetwork.DestroyPlayerObjects(player);
            //decrease the team fill for the team of the leaving player and update room properties
            PhotonNetwork.CurrentRoom.AddSize(player.GetTeam(), -1);
        }


        /// <summary>
        /// Finds the remotely controlled Player game object of a specific player,
        /// by iterating over all Player components and searching for the matching creator.
        /// </summary>
        public GameObject GetPlayerGameObject(Photon.Realtime.Player player)
        {
            GameObject[] rootObjs = SceneManager.GetActiveScene().GetRootGameObjects();
            List<Player> playerList = new List<Player>();

            //get all Player components from root objects
            for (int i = 0; i < rootObjs.Length; i++)
            {
                Player p = rootObjs[i].GetComponentInChildren<Player>(true);
                if (p != null) playerList.Add(p);
            }

            //find the game object where the creator matches this specific player ID
            for (int i = 0; i < playerList.Count; i++)
            {
                if (playerList[i].photonView.CreatorActorNr == player.ActorNumber)
                {
                    return playerList[i].gameObject;
                }
            }

            return null;
        }
    }


    /// <summary>
    /// Network Mode selection for preferred network type.
    /// </summary>
    public enum NetworkMode
    {
        Online = 0,
        LAN = 1,
        Offline = 2
    }


    /// <summary>
    /// This class extends Photon's Room object by custom properties.
    /// Provides several methods for setting and getting variables out of them.
    /// </summary>
    public static class RoomExtensions
    {       
        /// <summary>
        /// The key for accessing team fill per team out of the room properties.
        /// </summary>
        public const string size = "size";
        
        /// <summary>
        /// The key for accessing player scores per team out of the room properties.
        /// </summary>
        public const string score = "score";
        
        
        /// <summary>
        /// Returns the networked team fill for all teams out of properties.
        /// </summary>
        public static int[] GetSize(this Room room)
        {
            return (int[])room.CustomProperties[size];
        }
        
        /// <summary>
        /// Increases the team fill for a team by one when a new player joined the game.
        /// This is also being used on player disconnect by using a negative value.
        /// </summary>
        public static int[] AddSize(this Room room, int teamIndex, int value)
        {
            int[] sizes = room.GetSize();
            sizes[teamIndex] += value;

            room.SetCustomProperties(new Hashtable() {{size, sizes}});
            return sizes;
        }
        
        /// <summary>
        /// Returns the networked team scores for all teams out of properties.
        /// </summary>
        public static int[] GetScore(this Room room)
        {
            return (int[])room.CustomProperties[score];
        }
        
        /// <summary>
        /// Increase the score for a team by one when a new player scored a point for his team.
        /// </summary>
        public static int[] AddScore(this Room room, int teamIndex, int value)
        {
            int[] scores = room.GetScore();
            scores[teamIndex] += value;
            
            room.SetCustomProperties(new Hashtable() {{score, scores}});
            return scores;
        }
    }
}