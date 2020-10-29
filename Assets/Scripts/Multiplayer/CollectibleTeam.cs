/*  This file is part of the "Tanks Multiplayer" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using Photon.Pun;

namespace TanksMP
{
    /// <summary>
    /// Custom Collectible implementation for scene owned (unassigned) or team owned items.
    /// E.g. allowing for 'Rambo' pickups, Capture the Flag items etc.
    /// </summary>
	public class CollectibleTeam : Collectible
    {
        /// <summary>
        /// Team index this Collectible belongs to, or -1 if unassigned.
        /// Teams are defined in the GameManager script inspector.
        /// </summary>
        public int teamIndex = -1;

        /// <summary>
        /// Optional: Material that should be re-assigned if this Collectible is dropped or returned.
        /// </summary>
        public Material baseMaterial;

        /// <summary>
        /// Optional: Renderer on which the material should be modified depending on carrier team.
        /// </summary>
        public MeshRenderer targetRenderer;


        /// <summary>
        /// Server only: check for players colliding with the powerup.
        /// Possible collision are defined in the Physics Matrix.
        /// </summary>
        public override void OnTriggerEnter(Collider col)
        {
            if (!PhotonNetwork.IsMasterClient)
                return;

            GameObject obj = col.gameObject;
            Player player = obj.GetComponent<Player>();

            //try to apply collectible to player, the result should be true
            if (Apply(player))
            {
                //clean up previous buffered RPCs so we only keep the most recent one
                PhotonNetwork.RemoveRPCs(spawner.photonView);

                //check if colliding player belongs to the same team as the item
                if (teamIndex == player.GetView().GetTeam())
                {
                    //player collected team item, return it to team home base
                    //we do not have to send this as buffered RPC because this is the default spawn position
                    spawner.photonView.RPC("Return", RpcTarget.All);
                }
                else
                {
                    //player picked up item from other team, send out buffered RPC for it to be remembered
                    spawner.photonView.RPC("Pickup", RpcTarget.AllBuffered, (short)player.GetView().ViewID);
                }
            }
        }


        /// <summary>
        /// Overrides the default behavior with a custom implementation.
        /// Check for the carrier and item position to decide valid pickup.
        /// </summary>
        public override bool Apply(Player p)
        {
            //do not allow collection if the item is already carried around
            //but also skip any processing if our flag is on the home base already
            if (p == null || carrierId > 0 ||
                teamIndex == p.GetView().GetTeam() && transform.position == spawner.transform.position)
                return false;

            //if a target renderer is set, assign team material
            Colorize(p.GetView().GetTeam());

            //return successful collection
            return true;
        }


        /// <summary>
        /// Overrides the default behavior with a custom implementation.
        /// </summary>
        public override void OnDrop()
        {
            Colorize(this.teamIndex);
        }


        /// <summary>
        /// Overrides the default behavior with a custom implementation.
        /// </summary>
        public override void OnReturn()
        {
            Colorize(this.teamIndex);
        }


        //assign material based on team index passed in
        void Colorize(int teamIndex)
        {
            if (targetRenderer != null)
            {
                if (teamIndex >= 0)
                    targetRenderer.material = MultGameManager.GetInstance().teams[teamIndex].material;
                else
                    targetRenderer.material = baseMaterial;
            }
        }
    }
}