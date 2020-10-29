using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TanksMP;

public class GameManager : MonoBehaviour
{
    public int m_NumRoundsToWin = 1;        
    public float m_StartDelay = 3f;         
    public float m_EndDelay = 5f;           
    public CameraControl m_CameraControl;   
    public Text m_MessageText;
    public Text m_UIcontroller;          
    public GameObject m_TankPrefab;
    public GameObject m_TankPrefabBot;
    public GameObject m_PowerUpHealthPrefab;
    public GameObject m_PowerUpShieldPrefab;
    public TankManager[] m_Tanks;
    public TankManagerBot[] m_TanksBot;
    public PowerupHealthSingle[] m_PowerUpHealth;
    public PowerupShieldSingle[] m_PowerUpShield;

    [HideInInspector] public string m_userName;


    private int m_RoundNumber;              
    private WaitForSeconds m_StartWait;     
    private WaitForSeconds m_EndWait;       
    private TankManager m_RoundWinner;
    private TankManagerBot m_RoundWinnerBot;
    private TankManager m_GameWinner; 
    private TankManagerBot m_GameWinnerBot;   
    private int m_PlayerWins = 0;  
    private int m_BotWins = 0;  
    private string m_ColoredPlayerText; 
    private string m_ColoredBotText;
    
   


    private void Start()
    {
        m_StartWait = new WaitForSeconds(0);
        m_EndWait = new WaitForSeconds(m_EndDelay);
        m_userName = StartManager.m_userName;

        SpawnAllTanks();
        SpawnAllPowerUpHealth(null);
        SpawnAllPowerUpShield(null);
        SetCameraTargets();

        StartCoroutine(GameLoop());
    }


    private void SpawnAllTanks()
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
    }


    public void SpawnAllPowerUpHealth(GameObject obj)
    {
        
        for (int i = 0; i < m_PowerUpHealth.Length; i++)
        {
            if(obj != null && m_PowerUpHealth[i].m_Instance.transform.position == obj.transform.position)
            {
                m_PowerUpHealth[i].m_Instance.SetActive(false);
                StartCoroutine(RespawnHealth(i));
                return;
            }
        }
           
        for (int i = 0; i < m_PowerUpHealth.Length; i++)
        {
            m_PowerUpHealth[i].m_Instance =
                Instantiate(m_PowerUpHealthPrefab, m_PowerUpHealth[i].m_SpawnPoint.position, m_PowerUpHealth[i].m_SpawnPoint.rotation) as GameObject;
            m_PowerUpHealth[i].m_number = i;
        }
    }


    public void SpawnAllPowerUpShield(GameObject obj)
    {
        for (int i = 0; i < m_PowerUpShield.Length; i++)
        {
            if (obj != null && m_PowerUpShield[i].m_Instance.transform.position == obj.transform.position)
            {
                m_PowerUpShield[i].m_Instance.SetActive(false);
                StartCoroutine(RespawnShield(i));
                return;
            }
        }

        for (int i = 0; i < m_PowerUpShield.Length; i++)
        {
            m_PowerUpShield[i].m_Instance =
                Instantiate(m_PowerUpShieldPrefab, m_PowerUpShield[i].m_SpawnPoint.position, m_PowerUpShield[i].m_SpawnPoint.rotation) as GameObject;
        }
    }


    private IEnumerator RespawnHealth(int i)
    {

        yield return new WaitForSeconds(10);
        m_PowerUpHealth[i].m_Instance.SetActive(true);
    }


    private IEnumerator RespawnShield(int i)
    {
        
        yield return new WaitForSeconds(20);
        m_PowerUpShield[i].m_Instance.SetActive(true);
    }


    private void SetCameraTargets()
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
        }*/

        m_CameraControl.m_Targets = targets;
    }


    private IEnumerator GameLoop()
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
    }


    private IEnumerator RoundStarting()
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
        while(!ZeroTankLeft())
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
        if(m_RoundWinner != null)
        {
            m_RoundWinner.m_Wins++;
            m_PlayerWins++;
        } else {
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

    private TankManagerBot GetRoundWinnerBot() {
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

        if (m_RoundWinner != null) {
            message = m_ColoredPlayerText + " WINS THE ROUND!";
            message += "\n\n\n\n";

            //for (int i = 0; i < m_Tanks.Length; i++) {
            message += m_ColoredPlayerText + ": " + m_PlayerWins + " WINS\n";
            message += m_ColoredBotText + ": " + m_BotWins + " WINS\n";
                
            //}
        } else if (m_RoundWinnerBot != null) {
            message = m_RoundWinnerBot.m_ColoredPlayerText + " WINS THE ROUND!";
            message += "\n\n\n\n";

            //for (int i = 0; i < m_TanksBot.Length; i++) {
            message += m_ColoredPlayerText + ": " + m_PlayerWins + " WINS\n";
            message += m_ColoredBotText + ": " + m_BotWins + " WINS\n";
            //}
        }

        if (m_GameWinner != null) {
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
    }
}