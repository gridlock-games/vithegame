using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class VideoPlayerSystem : MonoBehaviour
{

  [SerializeField] private VideoPlayer videoPlayer;

  public void Start()
  {
    videoPlayer = this.GetComponent<VideoPlayer>();
  }
  public void PlayPauseVideo()
  {
    if (videoPlayer.isPlaying)
    {
      videoPlayer.Pause();
      //Change button Image here
    }
    else
    {
      videoPlayer.Play();
      //Change button Image here
    }
  }

  public void StopVideo()
  {
    videoPlayer.Stop();
  }

  public void ResetVideo()
  {
    videoPlayer.Stop();
    videoPlayer.Play();
  }
  public void ReplaceVideo(VideoClip clip)
  {
    videoPlayer.clip = clip;
  }

}
