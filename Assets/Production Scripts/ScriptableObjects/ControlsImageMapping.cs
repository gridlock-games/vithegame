using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using UnityEditor;
using System.Text;

namespace Vi.ScriptableObjects
{
    [CreateAssetMenu(fileName = "ControlsImageMapping", menuName = "Production/Controls Image Mapping")]
    public class ControlsImageMapping : ScriptableObject
    {
        [SerializeField] private List<ControlSchemeActionImage> controlSchemeActionImages;

        public List<Sprite> GetControlSchemeActionImages(InputControlScheme controlScheme, InputAction action)
        {
            List<Sprite> spriteList = new List<Sprite>();
            foreach (ControlSchemeActionImage controlSchemeActionImage in controlSchemeActionImages.Where(item => item.controlSchemeName == controlScheme.name & item.actionName == action.name))
            {
                spriteList.Add(controlSchemeActionImage.sprite);
            }
            return spriteList;
        }

        [SerializeField] private List<ControlsImageElement> controlsImageElements;

        public ActionSpriteResult GetActionSprite(InputControlScheme controlScheme, InputAction[] actions)
        {
            List<string> inputPathList = new List<string>();
            List<string> displayPathList = new List<string>();
            List<string> shortDisplayPathList = new List<string>();
            List<Sprite> pressedSpriteList = new List<Sprite>();
            List<Sprite> releasedSpriteList = new List<Sprite>();

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
                            if (!inputPathList.Contains(ele.inputPath))
                            {
                                inputPathList.Add(ele.inputPath);
                                displayPathList.Add(ele.displayName);
                                shortDisplayPathList.Add(ele.shortDisplayName);
                                pressedSpriteList.Add(ele.pressedSprite);
                                releasedSpriteList.Add(ele.releasedSprite);
                            }
                        }
                    }
                }
            }
            
            if (inputPathList.Count == 0) { Debug.LogError("Could not find control image element"); }

            return new ActionSpriteResult(inputPathList, displayPathList, shortDisplayPathList, pressedSpriteList, releasedSpriteList);
        }

        public class ActionSpriteResult
        {
            public List<string> inputPaths;
            public List<string> displayPaths;
            public List<string> shortDisplayPaths;
            public List<Sprite> pressedSprites;
            public List<Sprite> releasedSprites;

            public ActionSpriteResult(List<string> inputPaths, List<string> displayPaths, List<string> shortDisplayPaths, List<Sprite> pressedSprites, List<Sprite> releasedSprites)
            {
                this.inputPaths = inputPaths;
                this.displayPaths = displayPaths;
                this.shortDisplayPaths = shortDisplayPaths;
                this.pressedSprites = pressedSprites;
                this.releasedSprites = releasedSprites;
            }
        }

        [System.Serializable]
        private class ControlsImageElement : System.IEquatable<ControlsImageElement>
        {
            public string inputPath;
            public string displayName;
            public string shortDisplayName;
            public Sprite pressedSprite;
            public Sprite releasedSprite;

            public ControlsImageElement(string inputPath, string displayName, string shortDisplayName)
            {
                this.inputPath = inputPath;
                this.displayName = displayName;
                this.shortDisplayName = shortDisplayName;
            }

            public bool Equals(ControlsImageElement other)
            {
                return inputPath == other.inputPath;
            }
        }

        [System.Serializable]
        private class ControlSchemeActionImage : System.IEquatable<ControlSchemeActionImage>
        {
            public string controlSchemeName;
            public string actionName;
            public Sprite sprite;

            public bool Equals(ControlSchemeActionImage other)
            {
                return false;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Set Dirty")]
        private void SetDirtyAtWill()
        {
            EditorUtility.SetDirty(this);
        }

        [ContextMenu("Add Missing Paths")]
        private void AddMissingPaths()
        {
            foreach (InputDevice device in InputSystem.devices.ToArray())
            {
                foreach (InputControl control in device.allControls)
                {
                    try
                    {
                        string finalPath = control.path;

                        StringBuilder sb = new StringBuilder(finalPath);
                        sb[finalPath.IndexOf('/')] = '<';
                        finalPath = sb.ToString();

                        sb = new StringBuilder(finalPath);
                        int index = finalPath.IndexOf('/');
                        sb[index] = '>';
                        finalPath = sb.ToString();
                        finalPath = finalPath.Insert(index+1, "/");

                        ControlsImageElement ele = new ControlsImageElement(finalPath, control.displayName, control.shortDisplayName);
                        if (!controlsImageElements.Exists(item => item.Equals(ele))) { controlsImageElements.Add(ele); }
                    }
                    catch
                    {
                        Debug.Log("Error during " + device.name + " - " + control.path);
                    }
                }
            }
        }
        #endif
    }
}