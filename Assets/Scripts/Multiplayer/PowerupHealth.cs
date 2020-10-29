/*  This file is part of the "Tanks Multiplayer" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace TanksMP
{
    /// <summary>
    /// Custom powerup implementation for adding player health points.
    /// </summary>
	public class PowerupHealth : Collectible
    {
        /// <summary>
        /// Amount of health points to add per consumption.
        /// </summary>
        public int amount = 5;


        /// <summary>
        /// Overrides the default behavior with a custom implementation.
        /// Check for the current health and adds additional health.
        /// </summary>
        public override bool Apply(Player p)
        {
            if (p == null)
                return false;

            double value = p.GetView().GetHealth();

            //don't add health if it is at the maximum already
            if (value == p.maxHealth)
                return false;

            //get current health value and add amount to it
            value += amount;

            //we have to clamp the health to the maximum, so that
            //we don't go over the maximum by accident. Then assign
            //the new health value back to the player
            value = Mathf.Clamp((float) value, (float) value, p.maxHealth);
            p.GetView().SetHealth(value);

            //return successful collection
            return true;
        }
    }
}
