using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using static TanksMP.MultGameManager;
using System;

namespace TanksMP
{
    /// <summary>
    /// Networked player class implementing movement control and shooting.
    /// Contains both server and client logic in an authoritative approach.
    /// </summary> 
    public class Player : MonoBehaviourPunCallbacks, IPunObservable
    {
        
        protected Joystick joystick;
        protected Joybutton joybutton;
        // How fast the tank moves forward and back.
        public float m_Speed = 13f;
        // How fast the tank turns in degrees per second.
        public float m_TurnSpeed = 50f;
        // Reference to the audio source used to play engine sounds.
        //NB: different to the shooting audio source.
        public AudioSource m_MovementAudio;
        // Audio to play when the tank isn't moving.
        public AudioClip m_EngineIdling;
        // Audio to play when the tank is moving.
        public AudioClip m_EngineDriving;
        // The amount by which the pitch of the engine noises can vary.
        public float m_PitchRange = 0.2f;
        // Reference used to move the tank.
        private Rigidbody m_Rigidbody;
        // The pitch of the audio source at the start of the scene.
        private float m_OriginalPitch;
        public Color m_FullHealthColor = Color.green;
        public Color m_ZeroHealthColor = Color.red;

        private bool back;           
        protected BackButton backButton;
        // The current value of the movement input.
        private float m_MovementInputValue;
        private float m_MovementInputValue1; 
        private float m_TurnInputValue1;
        public Slider m_AimSlider;
        public CameraControl m_CameraControl;
        public float m_MaxChargeTime = 0.3f;
        public float m_MinLaunchForce = 15f;
        private float m_MinLaunchForce1;
        public float m_MaxLaunchForce = 40f;
        private float m_CurrentLaunchForce;
        private bool begin;
        private bool m_Fired;
        private Vector3 correctPosition;

        /// <summary>
        /// UI Text displaying the player name.
        /// </summary>    
        public Text label;

        /// <summary>
        /// Maximum health value at game start.
        /// </summary>
        public int maxHealth = 100;

        /// <summary>
        /// Maximum health value at game start.
        /// </summary>
        public int maxShield = 50;

        /// <summary>
        /// Current turret rotation and shooting direction.
        /// </summary>
        [HideInInspector]
        public short tankRotation;

        /// <summary>
        /// Turret to rotate with look direction.
        /// </summary>
        public Transform tank;

        /// <summary>
        /// Position to spawn new bullets at.
        /// </summary>
        public Transform shotPos;

        /// <summary>
        /// Array of available bullets for shooting.
        /// </summary>
        public GameObject[] bullets;

        /// <summary>
        /// MeshRenderers that should be highlighted in team color.
        /// </summary>
        public MeshRenderer[] renderers;

        /// <summary>
        /// Last player gameobject that killed this one.
        /// </summary>
        [HideInInspector]
        public GameObject killedBy;

        /// <summary>
        /// UI Slider visualizing health value.
        /// </summary>
        public Slider healthSlider;
        public Image m_FillImage;

        /// <summary>
        /// UI Slider visualizing shield value.
        /// </summary>
        public Slider shieldSlider;
        public Image m_FillImageSH;

        /// <summary>
        /// Object to spawn on shooting.
        /// </summary>
        public GameObject shotFX;

        /// <summary>
        /// Clip to play when a shot has been fired.
        /// </summary>
        public AudioClip shotClip;

        /// <summary>
        /// Object to spawn on player death.
        /// </summary>
        public GameObject explosionFX;

         /// <summary>
        /// Clip to play on player death.
        /// </summary>
        public AudioClip explosionClip;

        //timestamp when next shot should happen
        private float nextFire;

        /// <summary>
        /// Delay between shots.
        /// </summary>
        public float fireRate;

        private UIGame uIGame;

        private string playerName;
        private int playerTeam;

        private SendMsgButton sendMsg;



        //initialize server values for this player
        void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            sendMsg = FindObjectOfType<SendMsgButton>();
            uIGame = FindObjectOfType<UIGame>();
            playerName = GetView().GetName();
            playerTeam = GetView().GetTeam();

            //only let the master do initialization
            if (!PhotonNetwork.IsMasterClient)
                return;

            this.photonView.RPC("CmdSendMsgConnected", RpcTarget.AllViaServer, GetView().GetName(), GetView().GetTeam());

            //set players current health value after joining
            GetView().SetHealth(maxHealth);
            GetView().SetShield(0);

        }



        [PunRPC]
        protected void CmdSendMsgConnected(string msg, int team)
        {
            uIGame.SetPlayerConnected(msg, team);
        }



        public void SendMsg()
        {
            uIGame = FindObjectOfType<UIGame>();
            Debug.Log(playerName);
            this.photonView.RPC("CmdSendMsg", RpcTarget.AllViaServer, uIGame.inputMsg.text, playerName, playerTeam);
            uIGame.inputMsg.text = "";
        }



        [PunRPC]
        protected void CmdSendMsg(string msg, string owner, int team)
        {
            uIGame.SendMsg(msg, owner, team);
        }



        private void OnEnable()
        {
            // When the tank is turned on, make sure it's not kinematic.
            m_Rigidbody.isKinematic = false;
            back = false;
            // Also reset the input values.
            m_MovementInputValue1 = 0f;
            m_TurnInputValue1 = 0f;

            m_CurrentLaunchForce = m_MinLaunchForce;
            m_MinLaunchForce1 = m_MinLaunchForce;
            m_AimSlider.value = m_MinLaunchForce;
            
            begin = true;
        }

        private void OnDisable()
        {
            // When the tank is turned off, set it to kinematic so it stops moving.
            m_Rigidbody.isKinematic = true;
        }


        void Start()
        {
            //get corresponding team and colorize renderers in team color
            Team team = MultGameManager.GetInstance().teams[GetView().GetTeam()];
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].material = team.material;

            //set name in label
            label.text = GetView().GetName();
            //call hooks manually to update
            OnHealthChange(GetView().GetHealth());
            OnShieldChange(GetView().GetShield());

            //called only for this client 
            if (!photonView.IsMine)
                return;

            Button send = sendMsg.GetComponent<Button>();
            send.onClick.AddListener(() => SendMsg());

            //set a global reference to the local player
            MultGameManager.GetInstance().localPlayer = this;

            //get components and set camera target
            Transform[] targets = new Transform[1];
            targets[0] = this.transform;
            m_CameraControl = FindObjectOfType<CameraControl>();
            m_CameraControl.m_Targets = targets;

            m_OriginalPitch = m_MovementAudio.pitch;
            joystick = FindObjectOfType<Joystick>();
            backButton = FindObjectOfType<BackButton>();
            joybutton = FindObjectOfType<Joybutton>();
            

            fireRate = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
            m_Fired = true;
            /*
            rb = GetComponent<Rigidbody>();
            camFollow = Camera.main.GetComponent<FollowTarget>();
            camFollow.target = turret; */

            //initialize input controls for mobile devices
            //[0]=left joystick for movement, [1]=right joystick for shooting
            /*#if !UNITY_STANDALONE && !UNITY_WEBGL
                MultGameManager.GetInstance().ui.controls[0].onDrag += Move;
                MultGameManager.GetInstance().ui.controls[0].onDragEnd += MoveEnd;

                GameManager.GetInstance().ui.controls[1].onDragBegin += ShootBegin;
                GameManager.GetInstance().ui.controls[1].onDrag += RotateTurret;
                GameManager.GetInstance().ui.controls[1].onDrag += Shoot;
            #endif */
        }


        public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player player, ExitGames.Client.Photon.Hashtable playerAndUpdatedProps)
        {
            //only react on property changes for this player
            if (player != photonView.Owner)
                return;

            //update values that could change any time for visualization to stay up to date

            OnHealthChange(player.GetHealth());
            OnShieldChange(player.GetShield());
        }



        //this method gets called multiple times per second, at least 10 times or more
        void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                //stream.SendNext(healthSlider.value);
                //here we send the turret rotation angle to other clients
                //stream.SendNext(tankRotation);
                //stream.SendNext(transform.position);
                //Debug.Log("Send");
                //stream.SendNext(transform.rotation);
            }
            else
            {
                //uIGame.SetText((string) stream.ReceiveNext());
                //Debug.Log((short)stream.ReceiveNext());
                //here we receive the turret rotation angle from others and apply it
                //this.tankRotation = (short)stream.ReceiveNext();
                //OnTurretRotation();
                //this.correctPosition = (Vector3)stream.ReceiveNext();
                //Debug.Log("Received");//Line 100
                //this.correctRotation = (Quaternion)stream.ReceiveNext();
            }
        }


        private void Update()
        {
            OnHealthChange(GetView().GetHealth());
            OnShieldChange(GetView().GetShield());
            //skip further calls for remote clients    
            if (!photonView.IsMine)
            {
                //keep turret rotation updated for all clients
                //OnTurretRotation();
                
                return;
            }

            m_AimSlider.value = m_MinLaunchForce;
            if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
            {
                //Debug.Log("LIMIT SHOT!!!");
                m_CurrentLaunchForce = m_MaxLaunchForce;
                Shoot();
            }
            else if (joybutton.Pressed && m_Fired)
            {
                begin = false;
                m_Fired = false;
                m_CurrentLaunchForce = m_MinLaunchForce;
                //m_ShootingAudio.clip = m_ChargingClip;
                //m_ShootingAudio.Play();
            }
            else if (joybutton.Hold && !m_Fired)
            {
                m_CurrentLaunchForce += fireRate * Time.deltaTime;
                m_AimSlider.value = m_CurrentLaunchForce;
            }
            else if (!joybutton.Pressed && !joybutton.Hold && !m_Fired && !begin)
            {
                //Debug.Log("SHOT!!!");
                //Debug.Log(m_Fired);
                Shoot();
            }
        }


        private void FixedUpdate()
        {
            //skip further calls for remote clients    
            if (!photonView.IsMine)
            {
                //keep turret rotation updated for all clients
                //OnTurretRotation();
                return;
            }
            //OnTurretRotation();

            m_MovementInputValue1 = joystick.Vertical;
            m_TurnInputValue1 = joystick.Horizontal;
            EngineAudio();

            //Debug.Log(backButton.back);
            if (backButton.changed)
            {
                backButton.changed = false;
                back = !back;
            }
            Vector2 moveDir;
            moveDir.x = 0;
            moveDir.y = 0;
            // Adjust the rigidbodies position and orientation in FixedUpdate.
            if (m_MovementInputValue1 != 0 && m_TurnInputValue1 != 0)
            {
                m_MovementInputValue = m_MovementInputValue1;
                if (Math.Abs(m_TurnInputValue1) > Math.Abs(m_MovementInputValue))
                    m_MovementInputValue = m_TurnInputValue1;
                if (Math.Abs(m_MovementInputValue) <= 0.5)
                    m_MovementInputValue = 0;
                Turn();
                Move();
                
            } else
            {
                m_Rigidbody.velocity = Vector3.zero;
                m_Rigidbody.angularVelocity = Vector3.zero;
            }
            /*if(joystick.Vertical != 0 && joystick.Horizontal != 0)
            {
                moveDir.x = joystick.Horizontal;
                moveDir.y = joystick.Vertical;
                Move2(moveDir);
            }
            else
            {
                m_Rigidbody.velocity = Vector3.zero;
                m_Rigidbody.angularVelocity = Vector3.zero;
            }*/


            tankRotation = (short)Quaternion.LookRotation(new Vector3(shotPos.position.x - transform.position.x, 0, shotPos.position.z - transform.position.z)).eulerAngles.y;

        }

        void Move2(Vector2 direction = default(Vector2))
        {
            //if direction is not zero, rotate player in the moving direction relative to camera
            if (direction != Vector2.zero)
                transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y));
            //* Quaternion.Euler(0, camFollow.camTransform.eulerAngles.y, 0);
            transform.Rotate(0, 60, 0);

            //create movement vector based on current rotation and speed
            Vector3 movementDir = transform.forward * m_Speed * Time.deltaTime;
            //apply vector to rigidbody position
            m_Rigidbody.MovePosition(m_Rigidbody.position + movementDir);
        }



        void ShootBegin()
        {
            nextFire = Time.time + m_MaxChargeTime;
        }


        //shoots a bullet in the direction passed in
        //we do not rely on the current turret rotation here, because we send the direction
        //along with the shot request to the server to absolutely ensure a synced shot position
        protected void Shoot(Vector2 direction = default(Vector2))
        {
            //if shot delay is over  
            if (Time.time > nextFire)
            {
                //set next shot timestamp
                nextFire = Time.time + m_MaxChargeTime;

                //send current client position and turret rotation along to sync the shot position
                //also we are sending it as a short array (only x,z - skip y) to save additional bandwidth
                short[] pos = new short[] { (short)(shotPos.position.x * 10), (short)(shotPos.position.z * 10) };
                //send shot request with origin to server
                m_Fired = true;
                this.photonView.RPC("CmdShoot", RpcTarget.AllViaServer, pos, tankRotation, (short)m_CurrentLaunchForce);
                m_CurrentLaunchForce = m_MinLaunchForce;
                m_Fired = true;
            }
        }


        //called on the server first but forwarded to all clients
        [PunRPC]
        protected void CmdShoot(short[] position, short angle, short force)
        {
            //get current bullet type
            int currentBullet = GetView().GetBullet();

            //calculate center between shot position sent and current server position (factor 0.6f = 40% client, 60% server)
            //this is done to compensate network lag and smoothing it out between both client/server positions
            Vector3 shotCenter = Vector3.Lerp(shotPos.position, new Vector3(position[0] / 10f, shotPos.position.y, position[1] / 10f), 0.6f);
            Quaternion syncedRot = Quaternion.Euler(0, angle, 0); 

            //spawn bullet using pooling
            GameObject obj = PoolManager.Spawn(bullets[currentBullet], shotCenter, syncedRot);
            obj.GetComponent<Bullet>().owner = gameObject;
            obj.GetComponent<Rigidbody>().velocity = force * obj.transform.forward;
            //m_CurrentLaunchForce = m_MinLaunchForce;
            //obj.GetComponent<Rigidbody>().rotation = transform.rotation;

            //GameObject shellInstance = Instantiate(bullets[currentBullet], shotCenter, syncedRot);
            //shellInstance.GetComponent<Rigidbody>().velocity = m_CurrentLaunchForce * shotPos.forward;

            //check for current ammunition
            //let the server decrease special ammunition, if present
            if (PhotonNetwork.IsMasterClient && currentBullet != 0)
            {
                //if ran out of ammo: reset bullet automatically
                GetView().DecreaseAmmo(1);
            }

            //send event to all clients for spawning effects
            if (shotFX || shotClip)
                RpcOnShot();
        }

        //called on all clients after bullet spawn
        //spawn effects or sounds locally, if set
        protected void RpcOnShot()
        {
            if (shotFX) PoolManager.Spawn(shotFX, shotPos.position, Quaternion.identity);
            if (shotClip) AudioManager.Play3D(shotClip, shotPos.position, 0.1f);
        }


        private void Move()
        {
            Vector3 movement = transform.forward * Math.Abs(m_MovementInputValue) * m_Speed * Time.deltaTime;
            // Create a vector in the direction the tank is facing with a magnitude based on the input, speed and the time between frames.
            if (back)
            {
                m_Speed = 5f;
                //m_MinLaunchForce = 5f;
                m_Rigidbody.MovePosition(m_Rigidbody.position - movement);
                
            }
            else
            {
                m_Speed = 13f;
                //m_MinLaunchForce = m_MinLaunchForce1;
                m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
              
            }
            // Apply this movement to the rigidbody's position.
            //controller.Move (transform.forward * Time.deltaTime * m_Speed);

        }


        private void Turn()
        {
            float x = m_TurnInputValue1 / 64;
            float y = m_MovementInputValue1 / 64;
            transform.rotation = Rotating(transform, x, y);
            transform.Rotate(0, 60, 0);
            //Vector3 toforward = new Vector3(transform.position.x, 0, transform.position.z - m_TurnInputValue1 * m_TurnSpeed * Time.deltaTime);
            //Quaternion targetRotation = Quaternion.LookRotation(toforward);
            //transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, m_TurnSpeed * Time.deltaTime);
            // Determine the number of degrees to be turned based on the input, speed and time between frames.
            //float turn = m_TurnInputValue1 * m_TurnSpeed * Time.deltaTime;
            // Make this into a rotation in the y axis.
            //        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, m_TurnSpeed * Time.deltaTime);
            //Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
            // Apply this rotation to the rigidbody's rotation.
            //m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
        }


        Quaternion Rotating(Transform tr, float horizontal, float vertical)
        {
            Vector3 targetDirection = new Vector3(horizontal, 0f, vertical);
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
            Quaternion newRotation = Quaternion.Lerp(tr.transform.rotation, targetRotation, m_TurnSpeed * Time.deltaTime);
            return newRotation;
        }


        //hook for updating turret rotation locally
        /*void OnTurretRotation()
        {
            //we don't need to check for local ownership when setting the turretRotation,
            //because OnPhotonSerializeView PhotonStream.isWriting == true only applies to the owner
            tank.rotation = Quaternion.Euler(0, tankRotation, 0);
        }*/

        /// <summary>
        /// Server only: calculate damage to be taken by the Player,
		/// triggers score increase and respawn workflow on death.
        /// </summary>
        public void TakeDamage(Bullet bullet)
        {
            //store network variables temporary
            float health = (float) GetView().GetHealth();
            float shield = (float) GetView().GetShield();

            //reduce shield on hit
            if (shield - bullet.damage >= 0)
            {
                GetView().DecreaseShield(bullet.damage);
                return;
            } else
            {
                health += shield;
                GetView().SetShield(0);
            }

            //substract health by damage
            //locally for now, to only have one update later on
            health -= bullet.damage;

            //bullet killed the player
            if (health <= 0)
            {
                //the game is already over so don't do anything
                if (MultGameManager.GetInstance().IsGameOver()) return;

                //get killer and increase score for that enemy team
                Player other = bullet.owner.GetComponent<Player>();
                int otherTeam = other.GetView().GetTeam();
                if (GetView().GetTeam() != otherTeam)
                    MultGameManager.GetInstance().AddScore(ScoreType.Kill, otherTeam);

                //the maximum score has been reached now
                if (MultGameManager.GetInstance().IsGameOver())
                {
                    //close room for joining players
                    PhotonNetwork.CurrentRoom.IsOpen = false;
                    //tell all clients the winning team
                    this.photonView.RPC("RpcGameOver", RpcTarget.All, (byte)otherTeam);
                    return;
                }

                //the game is not over yet, reset runtime values
                //also tell all clients to despawn this player
                GetView().SetHealth(maxHealth);
                GetView().SetBullet(0);

                //clean up collectibles on this player by letting them drop down
                Collectible[] collectibles = GetComponentsInChildren<Collectible>(true);
                for (int i = 0; i < collectibles.Length; i++)
                {
                    PhotonNetwork.RemoveRPCs(collectibles[i].spawner.photonView);
                    collectibles[i].spawner.photonView.RPC("Drop", RpcTarget.AllBuffered, transform.position);
                }

                //tell the dead player who killed him (owner of the bullet)
                short senderId = 0;
                if (bullet.owner != null)
                    senderId = (short)bullet.owner.GetComponent<PhotonView>().ViewID;

                this.photonView.RPC("RpcRespawn", RpcTarget.All, senderId);
            }
            else
            {
                //we didn't die, set health to new value
                GetView().SetHealth(health);
            }
        }


        private void EngineAudio()
        {
            // If there is no input (the tank is stationary)...
            if (Mathf.Abs(m_MovementInputValue1) < 0.1f && Mathf.Abs(m_TurnInputValue1) < 0.1f)
            {
                // ... and if the audio source is currently playing the driving clip...
                if (m_MovementAudio.clip == m_EngineDriving)
                {
                    // ... change the clip to idling and play it.
                    m_MovementAudio.clip = m_EngineIdling;
                    m_MovementAudio.pitch = UnityEngine.Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
            else
            {
                // Otherwise if the tank is moving and if the idling clip is currently playing...
                if (m_MovementAudio.clip == m_EngineIdling)
                {
                    // ... change the clip to driving and play.
                    m_MovementAudio.clip = m_EngineDriving;
                    m_MovementAudio.pitch = UnityEngine.Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
        }


        public PhotonView GetView()
        {
            return this.photonView;
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            throw new System.NotImplementedException();
        }

        //hook for updating health locally
        //(the actual value updates via player properties)
        protected void OnHealthChange(double value)
        {
            healthSlider.value = (float) value;
            m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, (float) value / maxHealth);
            //Debug.Log("Changed:" + healthSlider.value);
        }


        //hook for updating shield locally
        //(the actual value updates via player properties)
        protected void OnShieldChange(double value)
        {
            shieldSlider.value = (float) value;
            m_FillImageSH.color = Color.Lerp(Color.cyan, Color.cyan, (float)value / maxShield);
        }


        //called on all clients on both player death and respawn
        //only difference is that on respawn, the client sends the request
        [PunRPC]
        protected virtual void RpcRespawn(short senderId)
        {
            //toggle visibility for player gameobject (on/off)
            gameObject.SetActive(!gameObject.activeInHierarchy);
            bool isActive = gameObject.activeInHierarchy;
            killedBy = null;

            //the player has been killed
            if (!isActive)
            {
                //find original sender game object (killedBy)
                PhotonView senderView = senderId > 0 ? PhotonView.Find(senderId) : null;
                if (senderView != null && senderView.gameObject != null) killedBy = senderView.gameObject;

                //detect whether the current user was responsible for the kill, but not for suicide
                //yes, that's my kill: increase local kill counter
                if (this != MultGameManager.GetInstance().localPlayer && killedBy == MultGameManager.GetInstance().localPlayer.gameObject)
                {
                    MultGameManager.GetInstance().ui.killCounter[0].text = (int.Parse(MultGameManager.GetInstance().ui.killCounter[0].text) + 1).ToString();
                    MultGameManager.GetInstance().ui.killCounter[0].GetComponent<Animator>().Play("Animation");
                }

                if (explosionFX)
                {
                    //spawn death particles locally using pooling and colorize them in the player's team color
                    GameObject particle = PoolManager.Spawn(explosionFX, transform.position, transform.rotation);
                    ParticleColor pColor = particle.GetComponent<ParticleColor>();
                    if (pColor) pColor.SetColor(MultGameManager.GetInstance().teams[GetView().GetTeam()].material.color);
                }

                //play sound clip on player death
                if (explosionClip) AudioManager.Play3D(explosionClip, transform.position);
            }

            if (PhotonNetwork.IsMasterClient)
            {
                //send player back to the team area, this will get overwritten by the exact position from the client itself later on
                //we just do this to avoid players "popping up" from the position they died and then teleporting to the team area instantly
                //this is manipulating the internal PhotonTransformView cache to update the networkPosition variable
                GetComponent<PhotonTransformView>().OnPhotonSerializeView(new PhotonStream(false, new object[] { MultGameManager.GetInstance().GetSpawnPosition(GetView().GetTeam()),
                                                                                                                 Vector3.zero, Quaternion.identity }), new PhotonMessageInfo());
            }

            //further changes only affect the local client
            if (!photonView.IsMine)
                return;

            //local player got respawned so reset states
            if (isActive == true)
                ResetPosition();
            else
            {
                /*//local player was killed, set camera to follow the killer
                if (killedBy != null) camFollow.target = killedBy.transform;
                //hide input controls and other HUD elements
                camFollow.HideMask(true);*/
                //display respawn window (only for local player)
                MultGameManager.GetInstance().DisplayDeath();
            }
        }


        /// <summary>
        /// Command telling the server and all others that this client is ready for respawn.
        /// This is when the respawn delay is over or a video ad has been watched.
        /// </summary>
        public void CmdRespawn()
        {
            this.photonView.RPC("RpcRespawn", RpcTarget.AllViaServer, (short)0);
        }


        /// <summary>
        /// Repositions in team area and resets camera & input variables.
        /// This should only be called for the local player.
        /// </summary>
        public void ResetPosition()
        {
            //start following the local player again
            /*camFollow.target = turret;
            camFollow.HideMask(false);*/

            //get team area and reposition it there
            transform.position = MultGameManager.GetInstance().GetSpawnPosition(GetView().GetTeam());

            //reset forces modified by input
            m_Rigidbody.velocity = Vector3.zero;
            m_Rigidbody.angularVelocity = Vector3.zero;
            transform.rotation = Quaternion.identity;
            m_Fired = true;
            begin = true;

            //reset input left over
            /*GameManager.GetInstance().ui.controls[0].OnEndDrag(null);
            GameManager.GetInstance().ui.controls[1].OnEndDrag(null);*/
        }


        //called on all clients on game end providing the winning team
        [PunRPC]
        protected void RpcGameOver(byte teamIndex)
        {
            //display game over window
            MultGameManager.GetInstance().DisplayGameOver(teamIndex);
        }
    }
}