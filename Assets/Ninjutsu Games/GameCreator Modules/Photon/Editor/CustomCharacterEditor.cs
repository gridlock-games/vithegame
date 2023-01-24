using System.IO;
using GameCreator.Characters;
using GameCreator.Core;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Events;
#if PHOTON_RPG
using NJG.GC.AI;
using NJG.RPG;
#endif

namespace NJG.PUN
{
    public class Section2
    {
        public const string ICONS_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/";
        private const string KEY_STATE = "network-action-section-{0}";

        private const float ANIM_BOOL_SPEED = 3.0f;

        // PROPERTIES: ------------------------------------------------------------------------

        public GUIContent name;
        public AnimBool state;

        // INITIALIZERS: ----------------------------------------------------------------------

        public Section2(string name, string icon, UnityAction repaint, string overridePath = "")
        {
            this.name = new GUIContent(
                string.Format(" {0}", name),
                GetTexture(icon, overridePath)
            );

            state = new AnimBool(GetState());
            state.speed = ANIM_BOOL_SPEED;
            state.valueChanged.AddListener(repaint);
        }

        // PUBLIC METHODS: --------------------------------------------------------------------

        public void PaintSection()
        {
            GUIStyle buttonStyle = (state.target
                ? CoreGUIStyles.GetToggleButtonNormalOn()
                : CoreGUIStyles.GetToggleButtonNormalOff()
            );

            if (GUILayout.Button(name, buttonStyle))
            {
                state.target = !state.target;
                string key = string.Format(KEY_STATE, name.text.GetHashCode());
                EditorPrefs.SetBool(key, state.target);
            }
        }

        // PRIVATE METHODS: -------------------------------------------------------------------

        private bool GetState()
        {
            string key = string.Format(KEY_STATE, name.text.GetHashCode());
            return EditorPrefs.GetBool(key, false);
        }

        private Texture2D GetTexture(string icon, string overridePath = "")
        {
            string path = Path.Combine(string.IsNullOrEmpty(overridePath) ? ICONS_PATH : overridePath, icon);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(PlayerCharacter), true)]
    public class CustomPlayerCharacterEditor : PlayerCharacterEditor
    {
        private const string ICONS_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/";
        private GUIContent GUI_SYNC = new GUIContent("Sync Character", "Enables synchronization of over the network for this Character.");

        private bool hasComponent;
        private bool initialized;
        private CharacterNetwork characterNetwork;
        private CharacterNetworkEditor characterNetworkEditor;
        private Section section;
        private SerializedProperty spActions;
        private SerializedObject actionSerializedObject;
        private bool lastHasComponent;

        private Texture2D GetTexture(string icon, string overridePath = "")
        {
            string path = Path.Combine(string.IsNullOrEmpty(overridePath) ? ICONS_PATH : overridePath, icon);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public override void OnInspectorGUI()
        {
            if (serializedObject == null) return;

            base.OnInspectorGUI();
            
            serializedObject.Update();


            EditorGUILayout.BeginVertical();
            GUILayout.Space(-3);

            if (section == null)
            {
                section = new Section("Network Settings", GetTexture("ActionNetwork.png"), Repaint);
            }

            if (!initialized)
            {
                characterNetwork = character.GetComponent<CharacterNetwork>();
                if(characterNetwork != null && characterNetworkEditor == null) characterNetworkEditor = (CharacterNetworkEditor)CreateEditor(characterNetwork, typeof(CharacterNetworkEditor));

                hasComponent = characterNetwork != null;
                initialized = true;
            }

            bool hasChanged = false;

            section.PaintSection();
            using (var group = new EditorGUILayout.FadeGroupScope(section.state.faded))
            {
                if (group.visible)
                {
                    EditorGUILayout.BeginVertical(CoreGUIStyles.GetBoxExpanded());

                    EditorGUI.BeginChangeCheck();
                    hasComponent = EditorGUILayout.Toggle(GUI_SYNC, hasComponent);
                    hasChanged = EditorGUI.EndChangeCheck();
                    
                    if (characterNetworkEditor != null)
                    {
                        characterNetworkEditor.serializedObject.Update();
                        characterNetworkEditor.PaintInspector();
                        characterNetworkEditor.serializedObject.ApplyModifiedProperties();
                    }
                    EditorGUILayout.EndVertical();
                }
            }

            if (hasChanged || hasChanged != lastHasComponent)
            {
                EditorUtility.SetDirty(character.gameObject);
                lastHasComponent = hasChanged;
            }

            if (hasChanged)
            {
                if (characterNetwork != null)
                {
                    DestroyImmediate(characterNetworkEditor);
                    DestroyImmediate(characterNetwork, true);
                    GUIUtility.ExitGUI();

                    characterNetwork = null;
                    characterNetworkEditor = null;
                    initialized = false;
                }
                else
                {
                    characterNetwork = character.GetComponent<CharacterNetwork>() ?? character.gameObject.AddComponent<CharacterNetwork>();

                    characterNetwork.SetupPhotonView();

                    characterNetworkEditor = (CharacterNetworkEditor)CreateEditor(characterNetwork, typeof(CharacterNetworkEditor));
                    characterNetwork.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;

                    hasComponent = true;
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(Character), true)]
    public class CustomCharacterEditor : CharacterEditor
    {
        private GUIContent GUI_SYNC = new GUIContent("Sync Character", "Enables synchronization of over the network for this Character.");

        private bool hasComponent;
        private bool initialized;
        private CharacterNetwork characterNetwork;
        private CharacterNetworkEditor characterNetworkEditor;
        private Section2 section;
        private SerializedProperty spActions;
        private SerializedObject actionSerializedObject;
        private bool lastHasComponent;

        public override void OnInspectorGUI()
        {
            if (serializedObject == null) return;

            base.OnInspectorGUI();
            
            serializedObject.Update();

            EditorGUILayout.BeginVertical();
            GUILayout.Space(-3);

            if (section == null)
            {
                section = new Section2("Network Settings", "ActionNetwork.png", Repaint);
            }

            if (!initialized)
            {
                characterNetwork = character.GetComponent<CharacterNetwork>();
                if (characterNetwork != null && characterNetworkEditor == null) characterNetworkEditor = (CharacterNetworkEditor)CreateEditor(characterNetwork, typeof(CharacterNetworkEditor));

                hasComponent = characterNetwork != null;
                initialized = true;
            }

            bool hasChanged = false;

            section.PaintSection();
            using (var group = new EditorGUILayout.FadeGroupScope(section.state.faded))
            {
                if (group.visible)
                {
                    EditorGUILayout.BeginVertical(CoreGUIStyles.GetBoxExpanded());

                    EditorGUI.BeginChangeCheck();
                    hasComponent = EditorGUILayout.Toggle(GUI_SYNC, hasComponent);
                    hasChanged = EditorGUI.EndChangeCheck();

                    if (characterNetworkEditor != null)
                    {
                        characterNetworkEditor.serializedObject.Update();
                        characterNetworkEditor.PaintInspector();
                        characterNetworkEditor.serializedObject.ApplyModifiedProperties();
                    }
                    EditorGUILayout.EndVertical();
                }
            }
            
            if (hasChanged || hasChanged != lastHasComponent)
            {
                EditorUtility.SetDirty(character.gameObject);
                lastHasComponent = hasChanged;
            }

            if (hasChanged)
            {
                if (characterNetwork != null)
                {
                    DestroyImmediate(characterNetworkEditor);
                    DestroyImmediate(characterNetwork, true);
                    GUIUtility.ExitGUI();

                    characterNetwork = null;
                    characterNetworkEditor = null;
                    initialized = false;
                }
                else
                {
                    characterNetwork = character.GetComponent<CharacterNetwork>() ?? character.gameObject.AddComponent<CharacterNetwork>();

                    characterNetwork.SetupPhotonView();

                    characterNetworkEditor = (CharacterNetworkEditor)CreateEditor(characterNetwork, typeof(CharacterNetworkEditor));
                    characterNetwork.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                    //characterNetwork.hideFlags = HideFlags.None;

                    hasComponent = true;
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
        }
    }
}