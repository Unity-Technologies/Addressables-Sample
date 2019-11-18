using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class AddScenes : MonoBehaviour
{
	public string addressToAdd;
	Button m_AddButton;
	public TMPro.TMP_Text textArea;
	SceneInstance m_LoadedScene;
	bool m_ReadyToLoad = true;
	// Use this for initialization
	void Start () {
		
		m_AddButton = GetComponent<Button>();
		m_AddButton.onClick.AddListener(OnButtonClick);

		textArea.text = "Load " + addressToAdd;
	}

	void OnButtonClick()
	{
		if(string.IsNullOrEmpty(addressToAdd))
			Debug.LogError("Address To Add not set.");
		else
		{
			if (m_ReadyToLoad)
				Addressables.LoadSceneAsync(addressToAdd, LoadSceneMode.Additive).Completed += OnSceneLoaded;
			else
			{
				Addressables.UnloadSceneAsync(m_LoadedScene).Completed += OnSceneUnloaded;
			}
		}
	}

	void OnSceneUnloaded(AsyncOperationHandle<SceneInstance> obj)
	{
		if (obj.Status == AsyncOperationStatus.Succeeded)
		{
			textArea.text = "Reload " + addressToAdd;
			m_ReadyToLoad = true;
			m_LoadedScene = new SceneInstance();
		}
		else
		{
			Debug.LogError("Failed to unload scene at address: " + addressToAdd);
		}
	}

	void OnSceneLoaded(AsyncOperationHandle<SceneInstance> obj)
	{
		if (obj.Status == AsyncOperationStatus.Succeeded)
		{
			textArea.text = "Unload " + addressToAdd;
			m_LoadedScene = obj.Result;
			m_ReadyToLoad = false;
		}
		else
		{
			Debug.LogError("Failed to load scene at address: " + addressToAdd);
		}
	}

}
	
