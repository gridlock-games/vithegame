using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Video;

[Serializable]
class GameMode {
    public string gameModeTitle;
    public string gameModeDesc;
    public Text targetGamemodeTitle;
    public Text targetGameModeDesc;
    public VideoPlayer targetVideoPlayer;
    public GameObject gameModePanel;
    public RawImage gamemodeImg;
    public Button targetGameModeButton; 

    public RenderTexture renderTexture;
    public VideoPlayer VideoPlayer;
}

public class gameModeManagement : MonoBehaviour
{
    
    [SerializeField] private GameMode gameMode;

    public Image blackOverlay; // Reference to the black overlay Image
    public float fadeDuration = 2.0f;

    private GameMode _gameMode = new GameMode();

    void Start() {
        if(gameMode != null) {
            _gameMode = gameMode;
            _gameMode.targetGamemodeTitle.text = _gameMode.gameModeTitle;
        }
    }

    public void onModeSelect()
    {
        Color color = blackOverlay.color;
        blackOverlay.color = new Color(color.r, color.g, color.b, 1.0f);
        blackOverlay.gameObject.SetActive(true);

        StartCoroutine(FadeIn());

        _gameMode.targetGameModeDesc.text = gameMode.gameModeDesc;
        _gameMode.targetVideoPlayer.clip = gameMode.VideoPlayer.clip;
        _gameMode.targetVideoPlayer.targetTexture = gameMode.VideoPlayer.targetTexture;
        _gameMode.gamemodeImg.texture = gameMode.renderTexture;
        _gameMode.targetVideoPlayer.Play();
    
    }

    IEnumerator FadeIn()
    {
        Color color = blackOverlay.color;
        float alpha = 1.0f;
        
        while (alpha > 0f)
        {
            alpha -= Time.deltaTime / fadeDuration;
            blackOverlay.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
            yield return null;
        }
        
        blackOverlay.color = new Color(color.r, color.g, color.b, 0f);
        blackOverlay.gameObject.SetActive(false);
    }
}