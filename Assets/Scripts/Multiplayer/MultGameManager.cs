using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Photon.Pun;


namespace TanksMP
{
    public class MultGameManager : MonoBehaviour
    {
        //reference to this script instance
        private static MultGameManager instance;

        /// <summary>
        /// The local player instance spawned for this client.
        /// </summary>
        [HideInInspector]
        public Player localPlayer;

        /// <summary>
        /// Active game mode played in the current scene.
        /// </summary>
        public GameMode gameMode = GameMode.TDM;

        /// <summary>
        /// Reference to the UI script displaying game stats.
        /// </summary>
        public UIGame ui;

        /// <summary>
        /// Definition of playing teams with additional properties.
        /// </summary>
        public Team[] teams;

        /// <summary>
        /// The maximum amount of kills to reach before ending the game.
        /// </summary>
        public int maxScore = 30;

        /// <summary>
        /// The delay in seconds before respawning a player after it got killed.
        /// </summary>
        public int respawnTime = 5;

        /// <summary>
        /// Enable or disable friendly fire. This is verified in the Bullet script on collision.
        /// </summary>
        public bool friendlyFire = false;



        //initialize variables
        void Awake()
        {
            instance = this;

            //if Unity Ads is enabled, hook up its result callback
#if UNITY_ADS
        UnityAdsManager.adResultEvent += HandleAdResult;
#endif
        }



        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static MultGameManager GetInstance()
        {
            return instance;
        }


        /// <summary>
        /// Global check whether this client is the match master or not.
        /// </summary>
        public static bool isMaster()
        {
            return PhotonNetwork.IsMasterClient;
        }


        /// <summary>
        /// Returns the next team index a player should be assigned to.
        /// </summary>
        public int GetTeamFill()
        {
            //init variables
            int[] size = PhotonNetwork.CurrentRoom.GetSize();
            int teamNo = 0;

            int min = size[0];
            //loop over teams to find the lowest fill
            for (int i = 0; i < teams.Length; i++)
            {
                //if fill is lower than the previous value
                //store new fill and team for next iteration
                if (size[i] < min)
                {
                    min = size[i];
                    teamNo = i;
                }
            }

            //return index of lowest team
            return teamNo;
        }


        /// <summary>
        /// Returns a random spawn position within the team's spawn area.
        /// </summary>
        public Vector3 GetSpawnPosition(int teamIndex)
        {
            //init variables
            Vector3 pos = teams[teamIndex].spawn.position;
            BoxCollider col = teams[teamIndex].spawn.GetComponent<BoxCollider>();

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
                    pos.x = UnityEngine.Random.Range(col.bounds.min.x, col.bounds.max.x);
                    pos.z = UnityEngine.Random.Range(col.bounds.min.z, col.bounds.max.z);
                    counter--;
                }
                while (!col.bounds.Contains(pos) && counter > 0);
            }

            return pos;
        }


        //implements what to do when an ad view completes
#if UNITY_ADS
        void HandleAdResult(ShowResult result)
        {
            switch (result)
            {
                //in case the player successfully watched an ad,
                //it sends a request for it be respawned
                case ShowResult.Finished:
                case ShowResult.Skipped:
                    localPlayer.CmdRespawn();
                    break;
                
                //in case the ad can't be shown, just handle it
                //like we wouldn't have tried showing a video ad
                //with the regular death countdown (force ad skip)
                case ShowResult.Failed:
                    DisplayDeath(true);
                    break;
            }
        }
#endif


        // <summary>
        /// Adds points to the target team depending on matching game mode and score type.
        /// This allows us for granting different amount of points on different score actions.
        /// </summary>
        public void AddScore(ScoreType scoreType, int teamIndex)
        {
            //distinguish between game mode
            switch (gameMode)
            {
                //in TDM, we only grant points for killing
                case GameMode.TDM:
                    switch (scoreType)
                    {
                        case ScoreType.Kill:
                            PhotonNetwork.CurrentRoom.AddScore(teamIndex, 1);
                            break;
                    }
                    break;

                //in CTF, we grant points for both killing and flag capture
                case GameMode.CTF:
                    switch (scoreType)
                    {
                        case ScoreType.Kill:
                            PhotonNetwork.CurrentRoom.AddScore(teamIndex, 1);
                            break;

                        case ScoreType.Capture:
                            PhotonNetwork.CurrentRoom.AddScore(teamIndex, 10);
                            break;
                    }
                    break;
            }
        }


        /// <summary>
        /// Returns whether a team reached the maximum game score.
        /// </summary>
        public bool IsGameOver()
        {
            //init variables
            bool isOver = false;
            int[] score = PhotonNetwork.CurrentRoom.GetScore();

            //loop over teams to find the highest score
            for (int i = 0; i < teams.Length; i++)
            {
                //score is greater or equal to max score,
                //which means the game is finished
                if (score[i] >= maxScore)
                {
                    isOver = true;
                    break;
                }
            }

            //return the result
            return isOver;
        }


        /// <summary>
        /// Only for this player: sets the death text stating the killer on death.
        /// If Unity Ads is enabled, tries to show an ad during the respawn delay.
        /// By using the 'skipAd' parameter is it possible to force skipping ads.
        /// </summary>
        public void DisplayDeath(bool skipAd = false)
        {
            //get the player component that killed us
            Player other = localPlayer;
            string killedByName = "YOURSELF";
            if (localPlayer.killedBy != null)
                other = localPlayer.killedBy.GetComponent<Player>();

            //suicide or regular kill?
            if (other != localPlayer)
            {
                killedByName = other.GetView().GetName();
                //increase local death counter for this game
                ui.killCounter[1].text = (int.Parse(ui.killCounter[1].text) + 1).ToString();
                ui.killCounter[1].GetComponent<Animator>().Play("Animation");
            }

            //calculate if we should show a video ad
#if UNITY_ADS
            if (!skipAd && UnityAdsManager.ShowAd())
                return;
#endif

            //when no ad is being shown, set the death text
            //and start waiting for the respawn delay immediately
            ui.SetDeathText(killedByName, teams[other.GetView().GetTeam()]);
            StartCoroutine(SpawnRoutine());
        }


        //coroutine spawning the player after a respawn delay
        IEnumerator SpawnRoutine()
        {
            //calculate point in time for respawn
            float targetTime = Time.time + respawnTime;

            //wait for the respawn to be over,
            //while waiting update the respawn countdown
            while (targetTime - Time.time > 0)
            {
                ui.SetSpawnDelay(targetTime - Time.time);
                yield return null;
            }

            //respawn now: send request to the server
            ui.DisableDeath();
            localPlayer.CmdRespawn();
        }


        /// <summary>
        /// Only for this player: sets game over text stating the winning team.
        /// Disables player movement so no updates are sent through the network.
        /// </summary>
        public void DisplayGameOver(int teamIndex)
        {
            //PhotonNetwork.isMessageQueueRunning = false;
            localPlayer.enabled = false;
            //localPlayer.camFollow.HideMask(true);
            ui.SetGameOverText(teams[teamIndex]);

            //starts coroutine for displaying the game over window
            StartCoroutine(DisplayGameOver());
        }


        //displays game over window after short delay
        IEnumerator DisplayGameOver()
        {
            //give the user a chance to read which team won the game
            //before enabling the game over screen
            yield return new WaitForSeconds(3);

            //show game over window and disconnect from network
            ui.ShowGameOver();
            PhotonNetwork.Disconnect();
        }


        //clean up callbacks on scene switches
        void OnDestroy()
        {
#if UNITY_ADS
                UnityAdsManager.adResultEvent -= HandleAdResult;
#endif
        }

        /*private void Start()
        {
            m_StartWait = new WaitForSeconds(0);
            m_EndWait = new WaitForSeconds(m_EndDelay);
            m_userName = StartManager.m_userName;

            SpawnAllTanks();
            SetCameraTargets();

            StartCoroutine(GameLoop());
        }*/


        /*private void SpawnAllTanks()
        {
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                m_Tanks[i].m_Instance =
                    Instantiate(m_TankPrefab, m_Tanks[i].m_SpawnPoint.position, m_Tanks[i].m_SpawnPoint.rotation) as GameObject;
                m_Tanks[i].m_PlayerNumber = i + 1;
                m_Tanks[i].m_userName = m_userName;
                m_Tanks[i].Setup();
            }
            m_ColoredPlayerText = m_Tanks[0].m_ColoredPlayerText;

            for (int i = 0; i < m_TanksBot.Length; i++)
            {
                m_TanksBot[i].m_Instance =
                    Instantiate(m_TankPrefabBot, m_TanksBot[i].m_SpawnPoint.position, m_TanksBot[i].m_SpawnPoint.rotation) as GameObject;
                m_TanksBot[i].m_BotNumber = i + 1;
                m_TanksBot[i].Setup();
            }
            m_ColoredBotText = m_TanksBot[0].m_ColoredPlayerText;
        }*/


        /*private void SetCameraTargets()
        {
            //Transform[] targets = new Transform[m_Tanks.Length + m_TanksBot.Length];
            Transform[] targets = new Transform[m_Tanks.Length];

            for (int i = 0; i < m_Tanks.Length; i++)
            {
                targets[i] = m_Tanks[i].m_Instance.transform;
            }
            /*for (int i = m_Tanks.Length; i < targets.Length; i++)
            {
                targets[i] = m_TanksBot[i-m_Tanks.Length].m_Instance.transform;
            }

            m_CameraControl.m_Targets = targets;
        }*/


        /*private IEnumerator GameLoop()
        {
            yield return StartCoroutine(RoundStarting());
            yield return StartCoroutine(RoundPlaying());
            yield return StartCoroutine(RoundEnding());

            if (m_GameWinner != null)
            {
                m_PlayerWins = 0;
                m_BotWins = 0;
                Application.LoadLevel(Application.loadedLevel);

            }
            else
            {
                StartCoroutine(GameLoop());
            }
        }*/


        /*private IEnumerator RoundStarting()
        {
            ResetAllTanks();
            DisableTankControl();
            m_CameraControl.SetStartPositionAndSize();
            m_RoundNumber++;
            m_MessageText.text = "ROUND " + m_RoundNumber;
            yield return m_StartWait;
        }


        private IEnumerator RoundPlaying()
        {
            EnableTankControl();
            m_MessageText.text = string.Empty;
            while (!ZeroTankLeft())
            {
                yield return null;
            }
        }


        private IEnumerator RoundEnding()
        {
            DisableTankControl();
            m_RoundWinner = null;
            m_RoundWinnerBot = null;
            m_RoundWinner = GetRoundWinner();
            if (m_RoundWinner != null)
            {
                m_RoundWinner.m_Wins++;
                m_PlayerWins++;
            }
            else
            {
                m_RoundWinnerBot = GetRoundWinnerBot();
                m_BotWins++;
            }
            m_GameWinner = GetGameWinner();
            string message = EndMessage();
            m_MessageText.text = message;
            yield return m_EndWait;
        }


        private bool ZeroTankLeft()
        {
            int numTanksLeft = 0;
            int numTanksBotLeft = 0;

            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (m_Tanks[i].m_Instance.activeSelf)
                    numTanksLeft++;
            }
            for (int i = 0; i < m_TanksBot.Length; i++)
            {
                if (m_TanksBot[i].m_Instance.activeSelf)
                    numTanksBotLeft++;
            }

            return (numTanksLeft == 0 || numTanksBotLeft == 0);
        }


        private TankManager GetRoundWinner()
        {
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (m_Tanks[i].m_Instance.activeSelf)
                    return m_Tanks[i];
            }

            return null;
        }

        private TankManagerBot GetRoundWinnerBot()
        {
            for (int i = 0; i < m_TanksBot.Length; i++)
            {
                if (m_TanksBot[i].m_Instance.activeSelf)
                    return m_TanksBot[i];
            }

            return null;
        }


        private TankManager GetGameWinner()
        {
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (m_Tanks[i].m_Wins == m_NumRoundsToWin)

                    return m_Tanks[i];
            }

            return null;
        }


        private string EndMessage()
        {
            string message = "DRAW!";

            if (m_RoundWinner != null)
            {
                message = m_ColoredPlayerText + " WINS THE ROUND!";
                message += "\n\n\n\n";

                //for (int i = 0; i < m_Tanks.Length; i++) {
                message += m_ColoredPlayerText + ": " + m_PlayerWins + " WINS\n";
                message += m_ColoredBotText + ": " + m_BotWins + " WINS\n";

                //}
            }
            else if (m_RoundWinnerBot != null)
            {
                message = m_RoundWinnerBot.m_ColoredPlayerText + " WINS THE ROUND!";
                message += "\n\n\n\n";

                //for (int i = 0; i < m_TanksBot.Length; i++) {
                message += m_ColoredPlayerText + ": " + m_PlayerWins + " WINS\n";
                message += m_ColoredBotText + ": " + m_BotWins + " WINS\n";
                //}
            }

            if (m_GameWinner != null)
            {
                message = m_GameWinner.m_ColoredPlayerText + " WINS THE GAME!";
                message += "\n\n\n\n";
                message += "Your time is: <color=#3CB371>" + m_UIcontroller.text + "</color>";
            }

            return message;
        }


        private void ResetAllTanks()
        {
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                m_Tanks[i].Reset();
            }
            for (int i = 0; i < m_TanksBot.Length; i++)
            {
                m_TanksBot[i].Reset();
            }
        }


        private void EnableTankControl()
        {
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                m_Tanks[i].EnableControl();
            }
            for (int i = 0; i < m_TanksBot.Length; i++)
            {
                m_TanksBot[i].EnableControl();
            }
        }


        private void DisableTankControl()
        {
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                m_Tanks[i].DisableControl();
            }
            for (int i = 0; i < m_TanksBot.Length; i++)
            {
                m_TanksBot[i].DisableControl();
            }
        } */






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


        /// <summary>
        /// Defines the types that could grant points to players or teams.
        /// Used in the AddScore() method for filtering.
        /// </summary>
        public enum ScoreType
        {
            Kill,
            Capture
        }


        /// <summary>
        /// Available game modes selected per scene.
        /// Used in the AddScore() method for filtering.
        /// </summary>
        public enum GameMode
        {
            /// <summary>
            /// Team Deathmatch
            /// </summary>
            TDM,

            /// <summary>
            /// Capture The Flag
            /// </summary>
            CTF
        }
    }
}