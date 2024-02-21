using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Events;

public class AndroidPermissions : MonoBehaviour
{
  MessageNotificationObject messageNotificationObject = new MessageNotificationObject();
  public UnityEvent storageUnityEvent;
  internal void StoragePermissionCallbacks_PermissionDeniedAndDontAskAgain(string permissionName)
  {
    storageUnityEvent.AddListener(promptStorageRequest);
    messageNotificationObject.ShowDialogueBox("Storage Permission Message", "Grant permission", storageUnityEvent);
  }

  internal void StoragePermissionCallbacks_PermissionGranted(string permissionName)
  {
    //run call to the next scene
  }

  internal void StoragePermissionCallbacks_PermissionDenied(string permissionName)
  {
    storageUnityEvent.AddListener(promptStorageRequest);
    messageNotificationObject.ShowDialogueBox("Storage Permission Message", "Grant permission", storageUnityEvent);
  }

  public void checkAndRequestStoragePermission()
  {
    if (Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead) && Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
    {
      //run call to the next scene
    }
    else
    {
      storageUnityEvent.AddListener(promptStorageRequest);
      messageNotificationObject.ShowDialogueBox("Storage Permission Message","Grant permission", storageUnityEvent);
      //Show notification prompt about the requirement permission before proceeding
    }
  }

  public void promptStorageRequest()
  {
    string[] permissions = { "ExternalStorageRead", "ExternalStorageWrite" };
    var callbacks = new PermissionCallbacks();
    callbacks.PermissionDenied += StoragePermissionCallbacks_PermissionDenied;
    callbacks.PermissionGranted += StoragePermissionCallbacks_PermissionGranted;
    callbacks.PermissionDeniedAndDontAskAgain += StoragePermissionCallbacks_PermissionDeniedAndDontAskAgain;
    Permission.RequestUserPermissions(permissions, callbacks);
  }

  public void checkAndRequestMicPermission()
  {
    if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
    {
      //Do nothing as the permission was already been granted
    }
    else
    {
      //Show notification prompt if the user is fine about having their mic disabled
    }
  }
}

