using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TanksMP
{
	public class TDMUIController : MonoBehaviour
	{
		[SerializeField] public Text scoreLabel;
		[SerializeField] private TDMSettingsMenu settingsMenu;

		void Start()
		{
			settingsMenu.Close();
		}

		void Update()
		{
			scoreLabel.text = Time.realtimeSinceStartup.ToString();
		}

		public void OnOpenSettings()
		{
			settingsMenu.Open();
		}

        public void Quit()
        {
			SceneManager.LoadScene("startScreen");
		}

	}
}
