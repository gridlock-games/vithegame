using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public static class EventDelegateManager
{
    public static event UnityAction<Scene> sceneLoaded;
    public static event UnityAction sceneUnloaded;

    public static void InvokeSceneLoadedEvent(Scene scene) { sceneLoaded?.Invoke(scene); }
    public static void InvokeSceneUnloadedEvent() { sceneUnloaded?.Invoke(); }

    public static event UnityAction<ulong> clientFinishedLoadingScenes;
    public static void InvokeClientFinishedLoadingScenesEvent(ulong clientId) { clientFinishedLoadingScenes?.Invoke(clientId); }
}
