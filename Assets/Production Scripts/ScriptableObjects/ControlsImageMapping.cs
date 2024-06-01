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

        public ActionSpriteResult GetActionSprite(InputControlScheme controlScheme, InputAction action)
        {
            List<string> possiblePathList = new List<string>();
            foreach (InputDevice device in System.Array.FindAll(InputSystem.devices.ToArray(), item => controlScheme.SupportsDevice(item)))
            {
                string deviceName = device.name.ToLower();
                deviceName = deviceName.Contains("controller") ? "gamepad" : deviceName;

                foreach (InputBinding binding in action.bindings)
                {
                    var ele = controlsImageElements.Find(item => item.inputPath == binding.path);
                    if (ele != null)
                    {
                        return new ActionSpriteResult(ele.inputPath, ele.sprite);
                    }
                    possiblePathList.Add(binding.path);
                }
            }

            //Debug.LogError("Could not find control image element for " + action.name);
            //foreach (string path in possiblePathList)
            //{
            //    Debug.Log(path);
            //}
            return new ActionSpriteResult();
        }

        public struct ActionSpriteResult
        {
            public string effectivePath;
            public Sprite sprite;

            public ActionSpriteResult(string effectivePath, Sprite sprite)
            {
                this.effectivePath = effectivePath;
                this.sprite = sprite;
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