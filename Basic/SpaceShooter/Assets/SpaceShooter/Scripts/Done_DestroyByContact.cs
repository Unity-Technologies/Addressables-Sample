using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class Done_DestroyByContact : MonoBehaviour
{
    // ADDRESSABLES UPDATES
    public AssetReference explosion;
	public AssetReference playerExplosion;

	public int scoreValue;
	Done_GameController gameController;

	void Start ()
	{
		GameObject gameControllerObject = GameObject.FindGameObjectWithTag ("GameController");
		if (gameControllerObject != null)
		{
			gameController = gameControllerObject.GetComponent <Done_GameController>();
		}
		if (gameController == null)
		{
			Debug.Log ("Cannot find 'GameController' script");
		}
	}

	void OnTriggerEnter (Collider other)
	{
		if (other.tag == "Boundary" || other.tag == "Enemy")
		{
			return;
		}

		if (explosion != null)
		{
            // ADDRESSABLES UPDATES
            explosion.InstantiateAsync(transform.position, transform.rotation);
		}

		if (other.tag == "Player")
		{
            // ADDRESSABLES UPDATES
            playerExplosion.InstantiateAsync(other.transform.position, other.transform.rotation);

			gameController.GameOver();
		}
		
		gameController.AddScore(scoreValue);

        // ADDRESSABLES UPDATES
        Addressables.ReleaseInstance(other.gameObject);
        Addressables.ReleaseInstance(gameObject);
	}
}