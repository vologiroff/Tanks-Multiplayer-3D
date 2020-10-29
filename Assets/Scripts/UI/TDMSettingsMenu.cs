using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TanksMP
{
	public class TDMSettingsMenu : MonoBehaviour
	{
		protected AudioManager gameManager;
		protected Player[] tankMovement;
		// Start is called before the first frame update

		public void Open()
		{
			gameObject.SetActive(true);
			gameManager = FindObjectOfType<AudioManager>();
			tankMovement = FindObjectsOfType<Player>();
			GetComponentsInChildren<Slider>()[0].value = gameManager.GetComponent<AudioSource>().volume;
			GetComponentsInChildren<Slider>()[1].value = tankMovement[0].m_MovementAudio.volume;
		}

		// Update is called once per frame
		public void Close()
		{
			gameObject.SetActive(false);
		}

		public void OnMusicValue(float value)
		{
			//Debug.Log("Volume: " + value);
			gameManager = FindObjectOfType<AudioManager>();
			gameManager.GetComponent<AudioSource>().volume = value;
			//Debug.Log(gameManager.GetComponent<AudioSource>().volume);
		}

		public void OnEffectsValue(float value)
		{
			//Debug.Log("Volume: " + value);
			tankMovement = FindObjectsOfType<Player>();
			foreach (Player tank in tankMovement)
			{
				tank.m_MovementAudio.volume = value;
			}
		}
	}
}
