using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class NextScene : MonoBehaviour
{

	public string NextSceneAddress;

	Button m_NextButton;
	
	// Use this for initialization
	void Start ()
	{
		m_NextButton = GetComponent<Button>();
		m_NextButton.onClick.AddListener(OnButtonClick);
	}

	void OnButtonClick()
	{
		Addressables.LoadSceneAsync(NextSceneAddress);
	}

	// Update is called once per frame
	void Update () {
		
	}
}
