using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TanksMP
{
	public class SettingsMenu : MonoBehaviour
	{
		protected GameManager gameManager;
		protected TankMovement tankMovement;
		protected TankMovementBot[] tankMovementBot;
		// Start is called before the first frame update

		public void Open()
		{
			gameObject.SetActive(true);
			gameManager = FindObjectOfType<GameManager>();
			tankMovement = FindObjectOfType<TankMovement>();
			GetComponentsInChildren<Slider>()[0].value = gameManager.GetComponent<AudioSource>().volume;
			GetComponentsInChildren<Slider>()[1].value = tankMovement.m_MovementAudio.volume;
		}

		// Update is called once per frame
		public void Close()
		{
			gameObject.SetActive(false);
		}

		public void OnMusicValue(float value)
		{
			//Debug.Log("Volume: " + value);
			gameManager = FindObjectOfType<GameManager>();
			gameManager.GetComponent<AudioSource>().volume = value;
			//Debug.Log(gameManager.GetComponent<AudioSource>().volume);
		}

		public void OnEffectsValue(float value)
		{
			//Debug.Log("Volume: " + value);
			tankMovement = FindObjectOfType<TankMovement>();
			tankMovementBot = FindObjectsOfType<TankMovementBot>();
			tankMovement.m_MovementAudio.volume = value;
			foreach (TankMovementBot tank in tankMovementBot)
			{
				tank.m_MovementAudio.volume = value;
			}
		}
	}
}
