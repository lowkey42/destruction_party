using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public class JoinManager : MonoBehaviour
{

	public int startDelay = 5;

	public float gameTime = 60*4;

	public string teamSelectScene;
	public string[] levelScenes;

	public GameObject uiText;
	public GameObject gameOverOverlay;
	public GameObject gameOverBarDestroyers;
	public GameObject gameOverBarRepairers;
	public GameObject gameOverTextDestroyers;
	public GameObject gameOverTextRepairers;

	private float statDelayLeft = 0;

	private bool gameRunning = false;

	private float gameTimeLeft = 0;

	private float percentDestroyed = 0;

	private PlayerInputManager inputManager;

	void Start() {
		statDelayLeft = startDelay;

		inputManager = GetComponent<PlayerInputManager>();
		DontDestroyOnLoad(gameObject);
	}

    // Update is called once per frame
    void Update()
    {
		if(gameRunning) {
			GameUpdate();
		} else {
			JoiningUpdate();
		}
	}

	void JoiningUpdate() {
		var players =  Object.FindObjectsOfType<PlayerController>();

		int numDestroyers = 0;
		int numRepairers = 0;
		foreach(var p in players) {
			if(p.GetTeam()==PlayerTeam.destroyer) {
				numDestroyers++;
			} else if(p.GetTeam()==PlayerTeam.repairer) {
				numRepairers++;
			}
		}

		var ready = numDestroyers>0 && numRepairers>0 && numRepairers+numDestroyers == players.Length;
        
		if(ready) {
			statDelayLeft -= Time.deltaTime;
			if(statDelayLeft<=0) {
				gameRunning = true;
				gameTimeLeft = gameTime;
				inputManager.DisableJoining();
				StartCoroutine(StartGame());
			}
			uiText.GetComponent<Text>().text = ""+Mathf.Max(0, (int)statDelayLeft);

		} else {
			statDelayLeft = startDelay;
			uiText.GetComponent<Text>().text = "Waiting for players...";
		}
    }

	IEnumerator StartGame() {
		var asyncOp = SceneManager.LoadSceneAsync(levelScenes[Random.Range(0, levelScenes.Length)]);
		while(!asyncOp.isDone) {
			yield return null;
		}

		var spawnPointDestroyers = GameObject.Find("SpawnPointDestroyer");
		var spawnPointRepairer = GameObject.Find("SpawnPointRepairer");

		var players =  Object.FindObjectsOfType<PlayerController>();
		foreach(var p in players) {
			p.ResetBeerMeter();

			if(spawnPointDestroyers!=null && spawnPointRepairer!=null) {
				var offset = Random.insideUnitSphere * 2;
				offset.y = 0;

				if(p.GetTeam()!=PlayerTeam.destroyer) {
					p.transform.position = offset + spawnPointDestroyers.transform.position;
				} else {
					p.transform.position = offset + spawnPointRepairer.transform.position;
				}
			}
		}
	}

	void GameUpdate() {
		if(gameTimeLeft <= 0)
			return;

		gameTimeLeft -= Time.deltaTime;

		uiText.GetComponent<Text>().text = ""+Mathf.Max(0, (int)gameTimeLeft);

		if(gameTimeLeft<=0) {
			var sumDestoryables = 0f;
			var sumDestroyed = 0f;
			var destroyables =  Object.FindObjectsOfType<Destroyable>();
			foreach(var d in destroyables) {
				sumDestoryables += d.points;
				if(d.IsDestroyed())
					sumDestroyed += d.points;
			}

			percentDestroyed = sumDestroyed / sumDestoryables;

			uiText.GetComponent<Text>().text = "";

			StartCoroutine(HandleGameOver());
		}
	}

 	IEnumerator HandleGameOver()
    {
		gameOverOverlay.SetActive(true);

		gameOverBarDestroyers.transform.DOScaleX(percentDestroyed, 3f).SetEase(Ease.InBounce);
		var barTween = gameOverBarRepairers.transform.DOScaleX(1-percentDestroyed, 3f);
		barTween.SetEase(Ease.InBounce);

		float fadeTime = 0f;
		while(barTween.IsPlaying()) {
			var vd = (fadeTime/3f) * percentDestroyed;
			var vr = (fadeTime/3f) * (1-percentDestroyed);
			gameOverTextDestroyers.GetComponent<Text>().text = ""+(int)Mathf.Clamp(vd*100f, 0f, 100f)+" %";
			gameOverTextRepairers.GetComponent<Text>().text = ""+(int)Mathf.Clamp(vr*100f, 0f, 100f)+" %";
			
			yield return null;
			fadeTime+=Time.deltaTime;
		}

		gameOverTextDestroyers.GetComponent<Text>().text = ""+(int)(percentDestroyed*100)+" %";
		gameOverTextRepairers.GetComponent<Text>().text = ""+(int)(100-percentDestroyed*100)+" %";

        yield return new WaitForSeconds(3);

		gameOverOverlay.SetActive(false);
		inputManager.EnableJoining();
		var asyncOp = SceneManager.LoadSceneAsync(teamSelectScene);

		while(!asyncOp.isDone) {
			yield return null;
		}

		var spawnPoint = GameObject.Find("SpawnPoint");

		var players =  Object.FindObjectsOfType<PlayerController>();
		foreach(var p in players) {
			p.SwitchToNeutral();

			if(spawnPoint!=null) {
				var offset = Random.insideUnitSphere * 2;
				offset.y = 0;

				p.transform.position = offset + spawnPoint.transform.position;
			}
		}

    }

}
