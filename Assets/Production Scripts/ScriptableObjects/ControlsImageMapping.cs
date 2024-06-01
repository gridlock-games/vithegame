using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

namespace Vi.ScriptableObjects
{
    [CreateAssetMenu(fileName = "ControlsImageMapping", menuName = "Production/Controls Image Mapping")]
    public class ControlsImageMapping : ScriptableObject
    {
        [SerializeField] private List<ControlsImageElement> controlsImageElements;

        public ActionSpriteResult GetActionSprite(InputControlScheme controlScheme, InputAction[] actions)
        {
            List<string> possiblePathList = new List<string>();

            List<Sprite> spriteList = new List<Sprite>();
            List<string> pathList = new List<string>();

            foreach (InputAction action in actions)
            {
                foreach (InputDevice device in System.Array.FindAll(InputSystem.devices.ToArray(), item => controlScheme.SupportsDevice(item)))
                {
                    string deviceName = device.name.ToLower();
                    deviceName = deviceName.Contains("controller") ? "gamepad" : deviceName;

                    foreach (InputBinding binding in action.bindings)
                    {
                        var ele = controlsImageElements.Find(item => item.inputPath == binding.effectivePath);
                        if (ele != null)
                        {
                            if (!pathList.Contains(ele.inputPath))
                            {
                                pathList.Add(ele.inputPath);
                                spriteList.Add(ele.sprite);
                            }
                        }
                        possiblePathList.Add(binding.effectivePath);
                    }
                }
            }
            
            if (pathList.Count == 0) { Debug.LogError("Could not find control image element"); }
            foreach (string path in possiblePathList)
            {
                Debug.Log(path);
            }

            return new ActionSpriteResult(pathList, spriteList);
        }

        public class ActionSpriteResult
        {
            public List<string> effectivePaths;
            public List<Sprite> sprites;

            public ActionSpriteResult(List<string> effectivePaths, List<Sprite> sprites)
            {
                this.effectivePaths = effectivePaths;
                this.sprites = sprites;
            }
        }

        [System.Serializable]
        private class ControlsImageElement
        {
            public string inputPath;
            public Sprite sprite;
        }
    }
}