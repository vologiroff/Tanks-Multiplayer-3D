/*  This file is part of the "Tanks Multiplayer" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using System.Collections;
using Photon.Pun;

namespace TanksMP
{
    /// <summary>
    /// Manages network-synced spawning of prefabs, in this case collectibles and powerups.
    /// With the respawn time synced on all clients it supports host migration too.
    /// </summary>
    public class ObjectSpawner : MonoBehaviourPunCallbacks
	{
        /// <summary>
        /// Prefab to sync the instantiation for over the network.
        /// </summary>
		public GameObject prefab;

        /// <summary>
        /// Checkbox whether the object should be respawned after being despawned.
        /// </summary>
        public bool respawn;

        /// <summary>
        /// Delay until respawning the object again after it got despawned.
        /// </summary>
        public int respawnTime;

        /// <summary>
        /// Reference to the spawned prefab gameobject instance in the scene.
        /// </summary>
        [HideInInspector]
        public GameObject obj;

        /// <summary>
        /// Type of Collectible this spawner should utilize. This is set automatically,
        /// thus hidden in the inspector. Fires network messages depending on type.
        /// </summary>
        [HideInInspector]
        public CollectionType colType = CollectionType.Use;

        //time value when the next respawn should happen measured in game time
        private float nextSpawn;


        //when entering the game scene for the first time as a master client,
        //the master should spawn the object in the scene for all other clients
        void Start()
        {
            if(PhotonNetwork.IsMasterClient)
                OnMasterClientSwitched(PhotonNetwork.LocalPlayer);
        }
        
        
        /// <summary>
        /// Synchronizes current active state of the object to joining players.
        /// </summary>
        public override void OnPlayerEnteredRoom(Photon.Realtime.Player player)
        {
            //don't execute as a non-master, but also don't execute it for the master itself
            if(!PhotonNetwork.IsMasterClient || player.IsMasterClient)
                return;

            //the object is active in the scene on the master. Thus send an instantiate call
            //to the joining player so the object gets enabled/instantiated on that client too
            if (obj != null && obj.activeInHierarchy)
            {
                this.photonView.RPC("Instantiate", player);
            }

            //defining cases in which the SetRespawn method should be called instead
            switch (colType)
            {
                case CollectionType.Use:
                    //on the master the object is not active in the scene. As a client we have to know the
                    //remaining respawn time so we are able to take over in a host migration scenario
                    if (obj == null || !obj.activeInHierarchy)
                    {
                        this.photonView.RPC("SetRespawn", player, nextSpawn);
                    }
                    break;

                case CollectionType.Pickup:
                    //in addition to the check above, here we check for the current state too
                    //if the item got dropped, the master should send an updated respawn time as well
                    if (obj == null || !obj.activeInHierarchy ||
                      (obj.transform.parent != PoolManager.GetPool(obj).transform && obj.transform.position != transform.position))
                    {
                        this.photonView.RPC("SetRespawn", player, nextSpawn);
                    }
                    break;
            }
        }


        /// <summary>
        /// Called after switching to a new MasterClient when the current one leaves.
        /// Here the new master has to decide whether to enable the object in the scene.
        /// </summary>
		public override void OnMasterClientSwitched(Photon.Realtime.Player newMaster)
		{         
            //only execute on the new master client
            if(PhotonNetwork.LocalPlayer != newMaster)
                return;

            //defining cases in which the SpawnRoutine should be skipped
            switch (colType)
            {
                case CollectionType.Use:
                    //the object is already active thus do not trigger a respawn coroutine
                    if (obj != null && obj.activeInHierarchy)
                        return;
                    break;
                case CollectionType.Pickup:
                    //in addition to the check above, here we check for the current state too
                    //if the item is not being carried around and at the home base we can skip the respawn
                    if (obj != null && obj.activeInHierarchy &&
                        obj.transform.parent == PoolManager.GetPool(obj).transform &&
                        obj.transform.position == transform.position)
                        return;
                    break;
            }

            StartCoroutine(SpawnRoutine());
        }


        //calculates the remaining time until the next respawn,
        //waits for the delay to have passed and then instantiates the object
        IEnumerator SpawnRoutine()
		{
            yield return new WaitForEndOfFrame();
            float delay = Mathf.Clamp(nextSpawn - (float)PhotonNetwork.Time, 0, respawnTime);
			yield return new WaitForSeconds(delay);

            if (PhotonNetwork.IsConnected)
            {
                //differ between CollectionType
                if(colType == CollectionType.Pickup && obj != null)
                {
                    //if the item is of type Pickup, it should not be destroyed after
                    //the routine is over but returned to its original position again
                    PhotonNetwork.RemoveRPCs(this.photonView);
                    this.photonView.RPC("Return", RpcTarget.All);
                }
                else
                {
                    //instantiate a new copy on all clients
                    this.photonView.RPC("Instantiate", RpcTarget.All);
                }
            }
        }
		
        
        /// <summary>
        /// Instantiates the object in the scene using PoolManager functionality.
        /// </summary>
        [PunRPC]
		public void Instantiate()
		{
            //sanity check in case there already is an object active
            if (obj != null)
                return;

			obj = PoolManager.Spawn(prefab, transform.position, transform.rotation);
            //set the reference on the instantiated object for cross-referencing
            Collectible colItem = obj.GetComponent<Collectible>();
            if(colItem != null)
            {
                //set cross-reference
                colItem.spawner = this;
                //set internal item type automatically
                if (colItem is CollectibleTeam) colType = CollectionType.Pickup;
                else colType = CollectionType.Use;
            }
		}


        /// <summary>
        /// Collects the object and assigns it to the player with the corresponding view.
        /// </summary>
        [PunRPC]
        public void Pickup(short viewId)
        {
            //in case this method call is received over the network earlier than the
            //spawner instantiation, here we make sure to catch up and instantiate it directly
            if (obj == null)
                Instantiate();

            //get target view transform to parent to
            PhotonView view = PhotonView.Find(viewId);
            obj.transform.parent = view.transform;
            obj.transform.localPosition = Vector3.zero + new Vector3(0, 2, 0);
            
            //assign carrier to Collectible
            Collectible colItem = obj.GetComponent<Collectible>();
            if (colItem != null)
            {
                colItem.carrierId = viewId;
                colItem.OnPickup();
            }

            //cancel return timer as this object is now being carried around
            if (PhotonNetwork.IsMasterClient)
                StopAllCoroutines();
        }


        /// <summary>
        /// Unparents the object from any carrier and drops it at the targeted position.
        /// </summary>
        [PunRPC]
        public void Drop(Vector3 position)
        {
            //in case this method call is received over the network earlier than the
            //spawner instantiation, here we make sure to catch up and instantiate it directly
            if (obj == null)
                Instantiate();

            //re-parent object to this spawner
            obj.transform.parent = PoolManager.GetPool(obj).transform;
            obj.transform.position = position;

            //reset carrier
            Collectible colItem = obj.GetComponent<Collectible>();
            if (colItem != null)
            {
                colItem.carrierId = -1;
                colItem.OnDrop();
            }

            //update respawn counter for a future point in time
            SetRespawn();
            //if the respawn mechanic is selected, trigger a new coroutine
            if (PhotonNetwork.IsMasterClient && respawn)
            {
                StopAllCoroutines();
                StartCoroutine(SpawnRoutine());
            }
        }


        /// <summary>
        /// Returns the object back to this spawner's position. E.g. in Capture The Flag mode this
        /// can occur if a team collects its own flag, or a flag timed out after being dropped. 
        /// </summary>
        [PunRPC]
        public void Return()
        {
            //re-parent object to this spawner
            obj.transform.parent = PoolManager.GetPool(obj).transform;
            obj.transform.position = transform.position;

            //reset carrier
            Collectible colItem = obj.GetComponent<Collectible>();
            if (colItem != null)
            {
                colItem.carrierId = -1;
                colItem.OnReturn();
            }

            //cancel return timer as the object is now back at its base position
            if (PhotonNetwork.IsMasterClient)
                StopAllCoroutines();
        }


        /// <summary>
        /// Called by the spawned object to destroy itself on this managing component.
        /// This could be the case when it has been collected by players.
        /// </summary>
        [PunRPC]
		public void Destroy()
		{
            //despawn object and clear references
			PoolManager.Despawn(obj);
            obj = null;
			
            //if it should respawn again, trigger a new coroutine
			if(PhotonNetwork.IsMasterClient && respawn)
                StartCoroutine(SpawnRoutine());
		}
        
        
        /// <summary>
        /// Called by the spawned object to reset its respawn counter when it is despawned
        /// in the scene. Also called on all clients with the current counter on host migration.
        /// </summary>
        [PunRPC]
        public void SetRespawn(float init = 0f)
        {
            if(init > 0f)
                nextSpawn = init;
            else
                nextSpawn = (float)PhotonNetwork.Time + respawnTime;
        }
	}


    /// <summary>
    /// Collectible type used on the ObjectSpawner, to define whether the item is consumed or picked up.
    /// </summary>
    public enum CollectionType
    {
        Use,
        Pickup
    }
}