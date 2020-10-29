/*  This file is part of the "Tanks Multiplayer" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace TanksMP
{
    /// <summary>
    /// Custom powerup implementation for adding player shield points.
    /// </summary>
	public class PowerupShield : Collectible
    {
        /// <summary>
        /// Amount of shield points to add per consumption.
        /// </summary>
        public int amount = 50;


        /// <summary>
        /// Overrides the default behavior with a custom implementation.
        /// Check for the current shield and adds additional shield points.
        /// </summary>
		public override bool Apply(Player p)
        {
            if (p == null)
                return false;

            double value = p.GetView().GetShield();

            //don't add shield if it is at the maximum already
            if (value == amount)
                return false;

            //assign absolute shield points to player
            //we can't go over the maximum thus no need to check it here
            p.GetView().SetShield(amount);

            //return successful collection
            return true;
        }
    }
}
