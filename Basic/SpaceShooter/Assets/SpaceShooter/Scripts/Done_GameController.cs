using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Done_GameController : MonoBehaviour
{
    // ADDRESSABLES UPDATES
    public AssetReference player;
    public AssetLabelReference hazardsLabel;
    List<IResourceLocation> hazardLocations;

    public Vector3 spawnValues;
    public int hazardCount;
    public float spawnWait;
    public float startWait;
    public float waveWait;

    public Text scoreText;
    public Text restartText;
    public Text gameOverText;
    public Text loadingText;

    public string nextSceneAddress;

    bool gameOver;
    bool restart;
    int score;

    AsyncOperationHandle preloadOp;

    void Start()
    {
        // ADDRESSABLES UPDATES
        loadingText.text = string.Format("Loading: {0}%", 0);
        preloadOp = Addressables.DownloadDependenciesAsync("preload");
        LoadHazards();
    }

    void LoadHazards()
    {
        Addressables.LoadResourceLocationsAsync(hazardsLabel.labelString).Completed += OnHazardsLoaded;
    }

    // ADDRESSABLES UPDATES
    void OnHazardsLoaded(AsyncOperationHandle<IList<IResourceLocation>> op)
    {
        if (op.Status == AsyncOperationStatus.Failed)
        {
            Debug.Log("Failed to load hazards, retrying in 1 second...");
            Invoke("LoadHazards", 1);
            return;
        }
        hazardLocations = new List<IResourceLocation>(op.Result);
        player.InstantiateAsync().Completed += op2 =>
        {
            if (op2.Status == AsyncOperationStatus.Failed)
            {
                gameOverText.text = "Failed to load player prefab. Check console for errors.";
                Invoke("LoadHazards", 1);
            }
            else
            {
                gameOver = false;
                restart = false;
                restartText.text = "";
                gameOverText.text = "";
                score = 0;
                UpdateScore();
                StartCoroutine(SpawnWaves());
            }
        };
    }

    void Update()
    {
        if (preloadOp.IsValid())
        {
            loadingText.text = string.Format("Loading: {0}%", (int)(preloadOp.PercentComplete * 100));
            if (preloadOp.PercentComplete == 1)
            {
                Addressables.Release(preloadOp);
                preloadOp = new AsyncOperationHandle();
                loadingText.text = "";
            }
        }
        if (restart)
        {
            if (Input.GetKeyDown(KeyCode.R) || Input.GetButton("Fire1"))
            {
                // ADDRESSABLES UPDATES
                Addressables.LoadSceneAsync(nextSceneAddress);
            }
        }
    }


    IEnumerator SpawnWaves()
    {
        yield return new WaitForSeconds(startWait);
        while (true)
        {
            for (int i = 0; i < hazardCount; i++)
            {
                var hazardAddress = hazardLocations[Random.Range(0, hazardLocations.Count)];
                Vector3 spawnPosition = new Vector3(Random.Range(-spawnValues.x, spawnValues.x), spawnValues.y, spawnValues.z);
                Quaternion spawnRotation = Quaternion.identity;

                // ADDRESSABLES UPDATES
                Addressables.InstantiateAsync(hazardAddress, spawnPosition, spawnRotation);

                yield return new WaitForSeconds(spawnWait);
            }
            yield return new WaitForSeconds(waveWait);

            if (gameOver)
            {
                restartText.text = "Press 'R' for Restart";
                restart = true;
                break;
            }
        }
    }

    public void AddScore(int newScoreValue)
    {
        score += newScoreValue;
        UpdateScore();
    }

    void UpdateScore()
    {
        scoreText.text = "Score: " + score;
    }

    public void GameOver()
    {
        gameOverText.text = "Game Over!";
        gameOver = true;
    }
}