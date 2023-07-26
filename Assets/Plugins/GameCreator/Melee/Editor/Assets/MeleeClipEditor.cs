namespace GameCreator.Melee
{
    using UnityEngine;
    using UnityEditor;
    using GameCreator.Core;
    using GameCreator.Characters;
    using System;
    using System.IO;

    [CustomEditor(typeof(MeleeClip))]
    [CanEditMultipleObjects]
    public class MeleeClipEditor : IMeleeEditor
    {
        private const float SEC_2_FPS = 30f;
        private const float FRAME_DISTANCE = 0.0335f;
        private const float SPACING = 2f;

        public enum AttachVFXPhase {
            OnExecute,
            OnActivate,
            OnHit,
            OnRecovery
        }

        private static CharacterAnimator REF_OBJECT;
        private static Editor REF_OBJECT_EDITOR;
        private static readonly GUIContent GC_PREVIEW = new GUIContent("Preview Character");

        private static Color COLOR_PHASE1 = new Color(0.21f, 0.58f, 0.99f);
        private static Color COLOR_PHASE2 = new Color(0.23f, 0.79f, 0.33f);
        private static Color COLOR_PHASE3 = new Color(0.95f, 0.62f, 0.16f);
        private static Color COLOR_DARK = new Color(0.2f, 0.2f, 0.2f);

        private static Texture2D TEX_PREVIEW_ACCEPT;
        private static Texture2D TEX_PREVIEW_REJECT;
        private static Texture2D TEX_DARKER;

        private static Texture2D TEX_ATK_PHASE1;
        private static Texture2D TEX_ATK_PHASE2;
        private static Texture2D TEX_ATK_PHASE3;

        private const string PREFAB_NAME = "{0}.prefab";
        private const string PATH_ONHIT = "Assets/Plugins/GameCreatorData/Melee/Actions/OnHit";

        // PROPERTIES: ----------------------------------------------------------------------------

        // VFX: ----------------------------------------------------------------------------
        private SerializedProperty spAttackVFX;
        private SerializedProperty spVFXAttachmentPhase;
        private SerializedProperty spVFXPositionOffset;
        private SerializedProperty spVFXRotationOffset;


        private MeleeClip instance;

        private float timeline;

        private Section sectionAnimation;
        private Section sectionMotion;
        private Section sectionEffects;
        private Section sectionCombat;
        private Section sectionAbilities;

        private SerializedProperty spAnimationClip;
        private SerializedProperty spAttackDodgeClip;
        private SerializedProperty spAvatarMask;
        private SerializedProperty spTransitionIn;
        private SerializedProperty spTransitionOut;

        // Original
        private SerializedProperty spMovementForward;
        private SerializedProperty spMovementSides;
        private SerializedProperty spMovementVertical;
        private SerializedProperty spGravityInfluence;
        private SerializedProperty spMovementMultiplier;

        // Lunge Attack Purposes:
        private SerializedProperty spMovementForward_OnLunge;
        private SerializedProperty spMovementSides_OnLunge;

        // Attack Dodge
        private SerializedProperty spMovementForward_OnAttack;
        private SerializedProperty spMovementSides_OnAttack;
        private SerializedProperty spMovementVertical_OnAttack;
        private SerializedProperty spGravityInfluence_OnAttack;
        private SerializedProperty spMovementMultiplier_OnAttack;

        private SerializedProperty spSoundEffect;
        private SerializedProperty spPushForce;
        private SerializedProperty spHitPause;
        private SerializedProperty spHitPauseAmount;
        private SerializedProperty spHitPauseDuration;

        private SerializedProperty spIsAttack;
        private SerializedProperty spIsModifyFocus;
        private SerializedProperty spBoxCastHalfExtents;
        private SerializedProperty spIsHeavy;
        private SerializedProperty spBladeMultiplier;
        private SerializedProperty spIsOrbitLocked;
        private SerializedProperty spIsLunge;
        private SerializedProperty spAttackType;
        private SerializedProperty spIsDodge;
        private SerializedProperty spIsBlockable;

        private SerializedProperty spPoiseDamage;
        private SerializedProperty spDefenseDamage;
        private SerializedProperty spBaseDamage;

        private SerializedProperty spInterruptible;
        private SerializedProperty spVulnerability;
        private SerializedProperty spPosture;
        private SerializedProperty spAffectedBones;
        private SerializedProperty spAnimationSpeed;

        private SerializedProperty spAttackPhase;

        private SerializedProperty spAttackSpawnVFX;

        // abilities
        private SerializedProperty spIsSequence;
        private SerializedProperty spSequencedClips;
        private SerializedProperty spHitCount;
        private SerializedProperty spHitCountDelay;

        // end of abilities

        private int drawDragType;

        private bool initStyles = true;
        private GUIStyle stylePreviewText;
        private GUIStyle styleLabelRight;
        private GUIStyle styleTimelineBackground;
        private GUIStyle styleTimelineThumb;

        private IActionsListEditor actionsOnHit;
        private SerializedProperty spActionsOnHit;

        private IActionsListEditor actionsOnExecute;
        private SerializedProperty spActionsOnExecute;
        private IActionsListEditor actionOnActivate;
        private SerializedProperty spActionsOnActivate;

        // INITIALIZER: ---------------------------------------------------------------------------

        private void OnEnable()
        {
            this.instance = this.target as MeleeClip;
            this.spAffectedBones = this.serializedObject.FindProperty("affectedBones");

            this.sectionAnimation = new Section("Animation", this.LoadIcon("Animation"), this.Repaint);
            this.sectionMotion = new Section("Motion", this.LoadIcon("Animation"), this.Repaint);
            this.sectionEffects = new Section("Effects", this.LoadIcon("Effects"), this.Repaint);
            this.sectionCombat = new Section("Combat", this.LoadIcon("Animation"), this.Repaint);
            this.sectionAbilities = new Section("Abilities", this.LoadIcon("Effects"), this.Repaint);

            this.spAnimationClip = this.serializedObject.FindProperty("animationClip");
            this.spAttackDodgeClip = this.serializedObject.FindProperty("attackDodgeClip");
            this.spAvatarMask = this.serializedObject.FindProperty("avatarMask");
            this.spTransitionIn = this.serializedObject.FindProperty("transitionIn");
            this.spTransitionOut = this.serializedObject.FindProperty("transitionOut");
            this.spAnimationSpeed = this.serializedObject.FindProperty("animSpeed");

            // VFX
            this.spAttackVFX = this.serializedObject.FindProperty("abilityVFX");
            this.spVFXAttachmentPhase = this.serializedObject.FindProperty("attachVFXOnPhase");
            this.spVFXPositionOffset = this.serializedObject.FindProperty("vfxPositionOffset");
            this.spVFXRotationOffset = this.serializedObject.FindProperty("vfxRotationOffset");


            // Original
            this.spMovementForward = this.serializedObject.FindProperty("movementForward");
            this.spMovementSides = this.serializedObject.FindProperty("movementSides");
            this.spMovementVertical = this.serializedObject.FindProperty("movementVertical");
            this.spGravityInfluence = this.serializedObject.FindProperty("gravityInfluence");
            this.spMovementMultiplier = this.serializedObject.FindProperty("movementMultiplier");

            // Lunge Attack
            this.spMovementForward_OnLunge = this.serializedObject.FindProperty("movementForward_OnLunge");
            this.spMovementSides_OnLunge = this.serializedObject.FindProperty("movementSides_OnLunge");


            // On Attack Dodge
            this.spMovementForward_OnAttack = this.serializedObject.FindProperty("movementForward_OnAttack");
            this.spMovementSides_OnAttack = this.serializedObject.FindProperty("movementSides_OnAttack");
            this.spMovementVertical_OnAttack = this.serializedObject.FindProperty("movementVertical_OnAttack");
            this.spGravityInfluence_OnAttack = this.serializedObject.FindProperty("gravityInfluence_OnAttack");
            this.spMovementMultiplier_OnAttack = this.serializedObject.FindProperty("movementMultiplier_OnAttack");

            this.spSoundEffect = this.serializedObject.FindProperty("soundEffect");
            this.spPushForce = this.serializedObject.FindProperty("pushForce");
            this.spHitPause = this.serializedObject.FindProperty("hitPause");
            this.spHitPauseAmount = this.serializedObject.FindProperty("hitPauseAmount");
            this.spHitPauseDuration = this.serializedObject.FindProperty("hitPauseDuration");

            this.spIsAttack = this.serializedObject.FindProperty("isAttack");
            this.spIsModifyFocus = this.serializedObject.FindProperty("isModifyFocus");
            this.spIsSequence = this.serializedObject.FindProperty("isSequence");
            this.spSequencedClips = this.serializedObject.FindProperty("sequencedClips");
            this.spBoxCastHalfExtents = this.serializedObject.FindProperty("boxCastHalfExtents");
            this.spIsHeavy = this.serializedObject.FindProperty("isHeavy");
            this.spBladeMultiplier = this.serializedObject.FindProperty("bladeSizeMultiplier"); 
            this.spIsOrbitLocked = this.serializedObject.FindProperty("isOrbitLocked");
            this.spIsLunge = this.serializedObject.FindProperty("isLunge");
            this.spHitCount = this.serializedObject.FindProperty("hitCount");
            this.spHitCountDelay = this.serializedObject.FindProperty("multiHitRegDelay");
            this.spAttackType = this.serializedObject.FindProperty("attackType");
            this.spIsDodge = this.serializedObject.FindProperty("isDodge");
            this.spIsBlockable = this.serializedObject.FindProperty("isBlockable");

            this.spPoiseDamage = this.serializedObject.FindProperty("poiseDamage");
            this.spDefenseDamage = this.serializedObject.FindProperty("defenseDamage");
            this.spBaseDamage = this.serializedObject.FindProperty("baseDamage");

            this.spInterruptible = this.serializedObject.FindProperty("interruptible");
            this.spVulnerability = this.serializedObject.FindProperty("vulnerability");
            this.spPosture = this.serializedObject.FindProperty("posture");

            this.spAttackPhase = serializedObject.FindProperty("attackPhase");
            this.spAttackSpawnVFX = serializedObject.FindProperty("attackSpawnVFX");

            if (!TEX_PREVIEW_ACCEPT) TEX_PREVIEW_ACCEPT = MakeTexture(Color.green, 0.25f);
            if (!TEX_PREVIEW_REJECT) TEX_PREVIEW_REJECT = MakeTexture(Color.red, 0.25f);
            if (!TEX_DARKER) TEX_DARKER = MakeTexture(Color.black, 0.5f);

            if (!TEX_ATK_PHASE1) TEX_ATK_PHASE1 = MakeTexture(COLOR_PHASE1);
            if (!TEX_ATK_PHASE2) TEX_ATK_PHASE2 = MakeTexture(COLOR_PHASE2);
            if (!TEX_ATK_PHASE3) TEX_ATK_PHASE3 = MakeTexture(COLOR_PHASE3);

            this.spActionsOnHit = this.serializedObject.FindProperty("actionsOnHit");
            if (this.spActionsOnHit.objectReferenceValue == null)
            {
                string actionsName = Guid.NewGuid().ToString("N");

                GameCreatorUtilities.CreateFolderStructure(PATH_ONHIT);
                string path = Path.Combine(PATH_ONHIT, string.Format(PREFAB_NAME, actionsName));
                path = AssetDatabase.GenerateUniqueAssetPath(path);

                GameObject sceneInstance = new GameObject(actionsName);
                sceneInstance.AddComponent<Actions>();

                GameObject prefabInstance = PrefabUtility.SaveAsPrefabAsset(sceneInstance, path);
                DestroyImmediate(sceneInstance);

                Actions prefabActions = prefabInstance.GetComponent<Actions>();
                prefabActions.destroyAfterFinishing = true;
                this.spActionsOnHit.objectReferenceValue = prefabActions.actionsList;

                this.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                this.serializedObject.Update();
            }

            this.spActionsOnExecute = this.serializedObject.FindProperty("actionsOnExecute");
            if (this.spActionsOnExecute.objectReferenceValue == null)
            {
                string actionsName = Guid.NewGuid().ToString("N");

                GameCreatorUtilities.CreateFolderStructure(PATH_ONHIT);
                string path = Path.Combine(PATH_ONHIT, string.Format(PREFAB_NAME, actionsName));
                path = AssetDatabase.GenerateUniqueAssetPath(path);

                GameObject sceneInstance = new GameObject(actionsName);
                sceneInstance.AddComponent<Actions>();

                GameObject prefabInstance = PrefabUtility.SaveAsPrefabAsset(sceneInstance, path);
                DestroyImmediate(sceneInstance);

                Actions prefabActions = prefabInstance.GetComponent<Actions>();
                prefabActions.destroyAfterFinishing = true;
                this.spActionsOnExecute.objectReferenceValue = prefabActions.actionsList;

                this.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                this.serializedObject.Update();
            }

            this.spActionsOnActivate = this.serializedObject.FindProperty("actionOnActivate");
            if (this.spActionsOnActivate.objectReferenceValue == null)
            {
                string actionsName = Guid.NewGuid().ToString("N");

                GameCreatorUtilities.CreateFolderStructure(PATH_ONHIT);
                string path = Path.Combine(PATH_ONHIT, string.Format(PREFAB_NAME, actionsName));
                path = AssetDatabase.GenerateUniqueAssetPath(path);

                GameObject sceneInstance = new GameObject(actionsName);
                sceneInstance.AddComponent<Actions>();

                GameObject prefabInstance = PrefabUtility.SaveAsPrefabAsset(sceneInstance, path);
                DestroyImmediate(sceneInstance);

                Actions prefabActions = prefabInstance.GetComponent<Actions>();
                prefabActions.destroyAfterFinishing = true;
                this.spActionsOnActivate.objectReferenceValue = prefabActions.actionsList;

                this.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                this.serializedObject.Update();
            }
        }

        private void OnDisable()
        {
            AnimationMode.StopAnimationMode();
        }

        // PAINT METHODS: -------------------------------------------------------------------------

        public override void OnInspectorGUI()
        {
            if (this.initStyles)
            {
                this.InitializeStyles();
                this.initStyles = false;
            }

            this.serializedObject.Update();
            EditorGUILayout.PropertyField(spAffectedBones);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            bool duplicate = GUILayout.Button("Duplicate");
            EditorGUILayout.EndHorizontal();
            if (duplicate)
            {
                this.DuplicateMeleeClip();
                return;
            }

            GUILayout.Space(SPACING);
            this.PaintSectionAnimation();

            GUILayout.Space(SPACING);
            this.PaintSectionMotion();

            GUILayout.Space(SPACING);
            this.PaintSectionEffects();

            GUILayout.Space(SPACING);
            this.PaintSectionCombat();

             
            GUILayout.Space(SPACING);
            this.PaintSectionAbilities();

            this.serializedObject.ApplyModifiedProperties();
            this.serializedObject.Update();

            if (this.spIsAttack.boolValue)
            {
                EditorGUILayout.Space();
                this.PaintTimeline();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("On Hit", EditorStyles.boldLabel);
                this.PaintActionsOnHit();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("On Activate", EditorStyles.boldLabel);
                this.PaintActionsOnActivate();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("On Execute", EditorStyles.boldLabel);
            this.PaintActionsOnExecute();

            this.serializedObject.ApplyModifiedProperties();
        }

        public override bool RequiresConstantRepaint()
        {
            return AnimationMode.InAnimationMode();
        }

        private void PaintSectionAnimation()
        {
            this.sectionAnimation.PaintSection();
            using (var group = new EditorGUILayout.FadeGroupScope(this.sectionAnimation.state.faded))
            {
                if (group.visible)
                {
                    EditorGUILayout.BeginVertical(CoreGUIStyles.GetBoxExpanded());

                    EditorGUILayout.PropertyField(this.spAnimationClip);
                    EditorGUILayout.PropertyField(this.spAvatarMask);

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(this.spTransitionIn);
                    EditorGUILayout.PropertyField(this.spTransitionOut);
                    EditorGUILayout.PropertyField(this.spAnimationSpeed);

                    EditorGUILayout.EndVertical();
                }
            }
        }

        private void PaintSectionMotion()
        {
            this.sectionMotion.PaintSection();
            using (var group = new EditorGUILayout.FadeGroupScope(this.sectionMotion.state.faded))
            {
                if (group.visible)
                {
                    EditorGUILayout.BeginVertical(CoreGUIStyles.GetBoxExpanded());


                    if (!this.spIsAttack.boolValue)
                    {
                        EditorGUILayout.PropertyField(this.spIsDodge);
                    }

                    if (this.spIsDodge.boolValue)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Dodge from Idle", EditorStyles.boldLabel);
                    }

                    EditorGUILayout.PropertyField(this.spMovementForward);
                    EditorGUILayout.PropertyField(this.spMovementSides);
                    EditorGUILayout.PropertyField(this.spMovementVertical);

                    Rect buttonRect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.button);
                    buttonRect = new Rect(
                        buttonRect.x + EditorGUIUtility.labelWidth,
                        buttonRect.y,
                        buttonRect.width - EditorGUIUtility.labelWidth,
                        buttonRect.height
                    );

                    EditorGUI.BeginDisabledGroup(
                        this.spAnimationClip.objectReferenceValue == null ||
                        !(this.spAnimationClip.objectReferenceValue as AnimationClip).hasRootCurves);
                    if (GUI.Button(buttonRect, "Extract Root Motion"))
                    {
                        this.ExtractRootMotion(false);
                    }
                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(this.spMovementMultiplier);
                    EditorGUILayout.PropertyField(this.spGravityInfluence);

                    if (this.spIsDodge.boolValue)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Dodge from Attack", EditorStyles.boldLabel);
                        EditorGUILayout.Space();

                        EditorGUILayout.PropertyField(this.spAttackDodgeClip);
                        EditorGUILayout.Space();

                        EditorGUILayout.PropertyField(this.spMovementForward_OnAttack);
                        EditorGUILayout.PropertyField(this.spMovementSides_OnAttack);
                        EditorGUILayout.PropertyField(this.spMovementVertical_OnAttack);

                        Rect buttonRectDodge = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.button);
                        buttonRectDodge = new Rect(
                            buttonRectDodge.x + EditorGUIUtility.labelWidth,
                            buttonRectDodge.y,
                            buttonRectDodge.width - EditorGUIUtility.labelWidth,
                            buttonRectDodge.height
                        );

                        if (GUI.Button(buttonRectDodge, "Extract Root Motion"))
                        {
                            this.ExtractRootMotion(true);
                        }
                        EditorGUI.EndDisabledGroup();

                        EditorGUILayout.Space();
                        EditorGUILayout.PropertyField(this.spMovementMultiplier_OnAttack);
                        EditorGUILayout.PropertyField(this.spGravityInfluence_OnAttack);

                        EditorGUILayout.Space();
                        EditorGUILayout.Space();
                    }

                    EditorGUILayout.EndVertical();
                }
            }
        }

        private void PaintSectionEffects()
        {
            this.sectionEffects.PaintSection();
            using (var group = new EditorGUILayout.FadeGroupScope(this.sectionEffects.state.faded))
            {
                if (group.visible)
                {
                    EditorGUILayout.BeginVertical(CoreGUIStyles.GetBoxExpanded());

                    EditorGUILayout.PropertyField(this.spSoundEffect);
                    EditorGUILayout.PropertyField(this.spPushForce);

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(this.spHitPause);
                    EditorGUI.BeginDisabledGroup(!this.spHitPause.boolValue);

                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(this.spHitPauseAmount);
                    EditorGUILayout.PropertyField(this.spHitPauseDuration);

                    EditorGUI.indentLevel--;
                    EditorGUI.EndDisabledGroup();

                    

                    EditorGUILayout.Space();
                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(this.spAttackVFX);
                    EditorGUILayout.PropertyField(this.spVFXAttachmentPhase);
                    EditorGUILayout.PropertyField(this.spVFXPositionOffset);
                    EditorGUILayout.PropertyField(this.spVFXRotationOffset);

                    EditorGUILayout.EndVertical();
                }
            }
        }

        private void PaintSectionAbilities()
        {
            this.sectionAbilities.PaintSection();
            using (var group = new EditorGUILayout.FadeGroupScope(this.sectionAbilities.state.faded))
            {
                if (group.visible)
                {
                    EditorGUILayout.PropertyField(this.spIsSequence);

                    if (this.spIsSequence.boolValue == false)
                    {
                        EditorGUILayout.PropertyField(this.spHitCount);
                        EditorGUILayout.PropertyField(this.spHitCountDelay);
                    } else {
                        
                        EditorGUILayout.PropertyField(this.spSequencedClips);
                    }
                }
            }

        }

        private void PaintSectionCombat()
        {
            this.sectionCombat.PaintSection();
            using (var group = new EditorGUILayout.FadeGroupScope(this.sectionCombat.state.faded))
            {
                if (group.visible)
                {
                    EditorGUILayout.BeginVertical(CoreGUIStyles.GetBoxExpanded());

                    EditorGUILayout.PropertyField(this.spIsAttack);
                    EditorGUI.BeginDisabledGroup(!this.spIsAttack.boolValue);
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(this.spIsLunge);
                    EditorGUILayout.PropertyField(this.spIsModifyFocus);

                    EditorGUI.BeginDisabledGroup(!this.spIsModifyFocus.boolValue);

                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(this.spBoxCastHalfExtents);
                    EditorGUI.indentLevel--;

                    EditorGUI.EndDisabledGroup();


                    EditorGUILayout.PropertyField(this.spIsBlockable);
                    EditorGUILayout.PropertyField(this.spIsHeavy);
                    EditorGUILayout.PropertyField(this.spIsOrbitLocked);
                    EditorGUILayout.PropertyField(this.spBladeMultiplier);

                    EditorGUILayout.PropertyField(this.spAttackType);
                    EditorGUILayout.PropertyField(this.spDefenseDamage);
                    EditorGUILayout.PropertyField(this.spPoiseDamage);
                    EditorGUILayout.PropertyField(this.spBaseDamage);

                    EditorGUI.indentLevel--;
                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(this.spInterruptible);
                    EditorGUILayout.PropertyField(this.spVulnerability);
                    EditorGUILayout.PropertyField(this.spPosture);

                    EditorGUILayout.EndVertical();
                }
            }
        }

        // PREVIEW WINDOW: ------------------------------------------------------------------------

        public override bool HasPreviewGUI()
        {
            return true;
        }

        public override GUIContent GetPreviewTitle()
        {
            return GC_PREVIEW;
        }

        public override void OnPreviewSettings()
        {
            bool inAnimationModeOrigin = AnimationMode.InAnimationMode();

            EditorGUI.BeginDisabledGroup(REF_OBJECT == null);
            bool inAnimationModeChange = GUILayout.Toggle(
                inAnimationModeOrigin, "Preview",
                EditorStyles.toolbarButton
            );
            EditorGUI.EndDisabledGroup();

            if (inAnimationModeChange != inAnimationModeOrigin)
            {
                switch (inAnimationModeChange)
                {
                    case true: AnimationMode.StartAnimationMode(); break;
                    case false: AnimationMode.StopAnimationMode(); break;
                }
            }
        }

        public override void DrawPreview(Rect previewArea)
        {
            if (REF_OBJECT != null)
            {
                if (REF_OBJECT_EDITOR == null) REF_OBJECT_EDITOR = CreateEditor(REF_OBJECT.animator.gameObject);
                REF_OBJECT_EDITOR.OnPreviewGUI(previewArea, null);
            }
            else
            {
                EditorGUI.LabelField(
                    previewArea,
                    "Drop a scene Character here",
                    this.stylePreviewText
                );
            }

            if (this.drawDragType == 2) GUI.DrawTexture(
                previewArea, TEX_PREVIEW_ACCEPT,
                ScaleMode.StretchToFill, true
            );

            if (this.drawDragType == 1) GUI.DrawTexture(
                previewArea, TEX_PREVIEW_REJECT,
                ScaleMode.StretchToFill, true
            );

            EventType currentEvent = Event.current.type;
            switch (currentEvent)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    this.drawDragType = 1;
                    if (!previewArea.Contains(Event.current.mousePosition))
                    {
                        this.drawDragType = 0;
                        break;
                    }

                    if (DragAndDrop.objectReferences.Length == 1)
                    {
                        GameObject dropObject = DragAndDrop.objectReferences[0] as GameObject;
                        if (dropObject != null)
                        {
                            CharacterAnimator animator = dropObject.GetComponentInChildren<CharacterAnimator>();
                            if (animator)
                            {
                                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                                this.drawDragType = 2;
                                if (currentEvent == EventType.DragPerform)
                                {
                                    this.drawDragType = 0;
                                    DragAndDrop.AcceptDrag();
                                    REF_OBJECT = animator;
                                }
                            }
                        }
                    }
                    Event.current.Use();
                    break;

                case EventType.DragExited:
                    this.drawDragType = 0;
                    break;
            }
        }

        // EXTRACT: ------------------------------------------------------------------------------

        private void ExtractRootMotion(bool isAttackDodge)
        {
            AnimationClip animationClip = !isAttackDodge ? this.spAnimationClip.objectReferenceValue as AnimationClip : this.spAttackDodgeClip.objectReferenceValue as AnimationClip;
            if (animationClip != null)
            {
                if (animationClip.hasRootCurves)
                {
                    EditorCurveBinding[] curves = AnimationUtility.GetCurveBindings(animationClip);

                    for (int i = 0; i < curves.Length; ++i)
                    {
                        if (curves[i].propertyName == "RootT.x")
                        {
                            AnimationCurve curve = AnimationUtility.GetEditorCurve(
                                animationClip,
                                curves[i]
                            );

                            curve = this.ProcessRootCurve(curve);

                            if (!isAttackDodge)
                            {
                                this.spMovementSides.animationCurveValue = curve;
                            }
                            else
                            {
                                this.spMovementSides_OnAttack.animationCurveValue = curve;
                            }
                        }

                        if (curves[i].propertyName == "RootT.y")
                        {
                            AnimationCurve curve = AnimationUtility.GetEditorCurve(
                                animationClip,
                                curves[i]
                            );

                            curve = this.ProcessRootCurve(curve);

                            if (!isAttackDodge)
                            {
                                this.spMovementVertical.animationCurveValue = curve;
                            }
                            else
                            {
                                this.spMovementVertical_OnAttack.animationCurveValue = curve;
                            }
                        }

                        if (curves[i].propertyName == "RootT.z")
                        {
                            AnimationCurve curve = AnimationUtility.GetEditorCurve(
                                animationClip,
                                curves[i]
                            );

                            curve = this.ProcessRootCurve(curve);

                            if (!isAttackDodge)
                            {
                                this.spMovementForward.animationCurveValue = curve;
                            }
                            else
                            {
                                this.spMovementForward_OnAttack.animationCurveValue = curve;
                            }
                        }
                    }
                }
            }
        }

        private AnimationCurve ProcessRootCurve(AnimationCurve source)
        {

            MeleeClip meleeClip = this.target as MeleeClip;
            float value = source.Evaluate(0f);
            float duration = source.keys[source.length - 1].time;
            AnimationCurve result = new AnimationCurve();

            for (int i = 0; i < source.keys.Length; ++i)
            {
                result.AddKey(new Keyframe(
                    source.keys[i].time / duration,
                    source.keys[i].value - value,
                    source.keys[i].inTangent,
                    source.keys[i].outTangent,
                    source.keys[i].inWeight,
                    source.keys[i].outWeight
                ));
            }

            return result;
        }

        // TIMELINE: ------------------------------------------------------------------------------

        private void PaintTimeline()
        {
            if (!this.spAnimationClip.objectReferenceValue)
            {
                EditorGUILayout.HelpBox(
                    "No Animation Clip is set",
                    MessageType.Warning
                );
                return;
            }

            if (!REF_OBJECT || !REF_OBJECT.animator)
            {
                EditorGUILayout.HelpBox(
                    "Drop a scene Character onto the Preview window",
                    MessageType.Warning
                );
            }

            MeleeClip meleeClip = this.target as MeleeClip;

            AnimationClip clip = this.spAnimationClip.objectReferenceValue as AnimationClip;
            float clipLength = clip.length / meleeClip.animSpeed;

            Rect rectScrub = GUILayoutUtility.GetRect(
                0f, 9999f,
                EditorGUIUtility.singleLineHeight,
                EditorGUIUtility.singleLineHeight
            );

            AnimationCurve attackPhase = this.spAttackPhase.animationCurveValue;

            if (attackPhase.keys.Length != 4)
            {
                attackPhase = this.CreateAttackPhase(
                    clipLength * 0.35f,
                    clipLength * 0.65f,
                    clipLength
                );
            }

            float attackPhaseMin = attackPhase[1].time;
            float attackPhaseMax = attackPhase[2].time;

            EditorGUI.LabelField(
                rectScrub,
                this.timeline.ToString("Time: 0.00"),
                this.styleLabelRight
            );

            Rect rectTimeline = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.toggle);
            GUI.DrawTexture(rectTimeline, TEX_DARKER);

            rectTimeline = new Rect(
                rectTimeline.x + 1f,
                rectTimeline.y + 1f,
                rectTimeline.width - 2f,
                rectTimeline.height - 2f
            );

            Rect rectPhase1 = new Rect(
                rectTimeline.x,
                rectTimeline.y,
                rectTimeline.width * (attackPhaseMin / clipLength),
                rectTimeline.height
            );
            Rect rectPhase2 = new Rect(
                rectPhase1.x + rectPhase1.width,
                rectPhase1.y,
                rectTimeline.width * (attackPhaseMax - attackPhaseMin) / clipLength,
                rectPhase1.height
            );
            Rect rectPhase3 = new Rect(
                rectPhase2.x + rectPhase2.width,
                rectPhase2.y,
                rectTimeline.width * (clipLength - attackPhaseMax) / clipLength,
                rectPhase2.height
            );

            GUI.DrawTexture(rectPhase1, TEX_ATK_PHASE1);
            GUI.DrawTexture(rectPhase2, TEX_ATK_PHASE2);
            GUI.DrawTexture(rectPhase3, TEX_ATK_PHASE3);

            EditorGUI.BeginChangeCheck();
            this.timeline = GUI.HorizontalSlider(
                rectTimeline,
                this.timeline,
                0.0f, clipLength,
                this.styleTimelineBackground,
                this.styleTimelineThumb
            );

            if (EditorGUI.EndChangeCheck())
            {
                AnimationMode.StartAnimationMode();
            }

            Rect rectAttack = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.toggle);
            float prevAttackPhaseMin = attackPhaseMin;
            float prevAttackPhaseMax = attackPhaseMax;

            EditorGUI.MinMaxSlider(
                rectAttack,
                GUIContent.none,
                ref attackPhaseMin,
                ref attackPhaseMax,
                0.0f, clipLength
            );

            if (attackPhaseMin > attackPhaseMax - FRAME_DISTANCE) attackPhaseMin = prevAttackPhaseMin;
            if (attackPhaseMin < FRAME_DISTANCE) attackPhaseMin = prevAttackPhaseMin;
            if (attackPhaseMax > clipLength - FRAME_DISTANCE) attackPhaseMax = prevAttackPhaseMax;
            if (attackPhaseMax < attackPhaseMin + FRAME_DISTANCE) attackPhaseMax = prevAttackPhaseMax;

            float diffMin = Mathf.Abs(prevAttackPhaseMin - attackPhaseMin);
            float diffMax = Mathf.Abs(prevAttackPhaseMax - attackPhaseMax);

            if (diffMin > 0.01f || diffMax > 0.01f)
            {
                if (!AnimationMode.InAnimationMode())
                {
                    AnimationMode.StartAnimationMode();
                }

                switch (diffMin >= diffMax)
                {
                    case true: this.timeline = attackPhaseMin; break;
                    case false: this.timeline = attackPhaseMax; break;
                }

                spAttackPhase.animationCurveValue = this.CreateAttackPhase(
                    attackPhaseMin,
                    attackPhaseMax,
                    clipLength
                );
            }

            EditorGUILayout.Space();

            this.PaintLegendLabel(
                COLOR_PHASE1, "Anticipation",
                Mathf.RoundToInt(attackPhaseMin * SEC_2_FPS)
            );

            this.PaintLegendLabel(
                COLOR_PHASE2, "Active",
                Mathf.RoundToInt((attackPhaseMax - attackPhaseMin) * SEC_2_FPS)
            );

            this.PaintLegendLabel(
                COLOR_PHASE3, "Recovery",
                Mathf.RoundToInt((clipLength - attackPhaseMax) * SEC_2_FPS)
            );

            this.PaintLegendLabel(
                COLOR_DARK, "Total",
                Mathf.RoundToInt(clipLength * SEC_2_FPS)
            );

            if (!EditorApplication.isPlaying && REF_OBJECT != null && AnimationMode.InAnimationMode())
            {
                AnimationMode.BeginSampling();

                AnimationMode.SampleAnimationClip(
                    REF_OBJECT.animator.gameObject,
                    this.instance.animationClip,
                    this.timeline
                );

                AnimationMode.EndSampling();
            }
        }

        private AnimationCurve CreateAttackPhase(float activeTime, float recoveryTime, float totalTime)
        {
            activeTime = Mathf.Clamp(activeTime, 0f, totalTime);
            recoveryTime = Mathf.Clamp(recoveryTime, 0f, totalTime);

            AnimationCurve animationCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(activeTime, 1f),
                new Keyframe(recoveryTime, 2f),
                new Keyframe(totalTime, 2f)
            );

            for (int i = 0; i < 4; ++i)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    animationCurve, i, AnimationUtility.TangentMode.Constant
                );

                AnimationUtility.SetKeyRightTangentMode(
                    animationCurve, i, AnimationUtility.TangentMode.Constant
                );
            }

            return animationCurve;
        }

        private void PaintActionsOnHit()
        {
            if (this.actionsOnHit == null)
            {
                this.actionsOnHit = Editor.CreateEditor(
                    this.spActionsOnHit.objectReferenceValue
                ) as IActionsListEditor;
            }

            this.actionsOnHit.OnInspectorGUI();
        }

        private void PaintActionsOnExecute()
        {
            if (this.actionsOnExecute == null)
            {
                this.actionsOnExecute = Editor.CreateEditor(
                    this.spActionsOnExecute.objectReferenceValue
                ) as IActionsListEditor;
            }

            this.actionsOnExecute.OnInspectorGUI();
        }

        private void PaintActionsOnActivate()
        {
            if (this.actionOnActivate == null)
            {
                this.actionOnActivate = Editor.CreateEditor(
                    this.spActionsOnActivate.objectReferenceValue
                ) as IActionsListEditor;
            }

            this.actionOnActivate.OnInspectorGUI();
        }

        private void DuplicateMeleeClip()
        {
            string path = AssetDatabase.GetAssetPath(this.target);
            string newPath = AssetDatabase.GenerateUniqueAssetPath(path);

            MeleeClip newInstance = Instantiate(this.target) as MeleeClip;
            AssetDatabase.CreateAsset(newInstance, newPath);

            SerializedObject soNewInstance = new SerializedObject(
                AssetDatabase.LoadAssetAtPath<MeleeClip>(newPath)
            );

            SerializedProperty newActionsOnHit = soNewInstance.FindProperty("actionsOnHit");
            SerializedProperty newActionsOnExec = soNewInstance.FindProperty("actionsOnExecute");
            SerializedProperty newActionsOnActivate = soNewInstance.FindProperty("actionOnActivate");

            IActionsList a1 = newActionsOnHit.objectReferenceValue as IActionsList;
            IActionsList a2 = newActionsOnExec.objectReferenceValue as IActionsList;
            IActionsList a3 = newActionsOnActivate.objectReferenceValue as IActionsList;

            GameObject a1Src = PrefabUtility.InstantiatePrefab(a1.gameObject) as GameObject;
            GameObject a2Src = PrefabUtility.InstantiatePrefab(a2.gameObject) as GameObject;
            GameObject a3Src = PrefabUtility.InstantiatePrefab(a3.gameObject) as GameObject;

            PrefabUtility.UnpackPrefabInstance(a1Src, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            PrefabUtility.UnpackPrefabInstance(a2Src, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            PrefabUtility.UnpackPrefabInstance(a3Src, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);

            string a1Path = AssetDatabase.GetAssetPath(a1.gameObject);
            string a2Path = AssetDatabase.GetAssetPath(a2.gameObject);
            string a3Path = AssetDatabase.GetAssetPath(a3.gameObject);

            string a1NewPath = AssetDatabase.GenerateUniqueAssetPath(a1Path);
            string a2NewPath = AssetDatabase.GenerateUniqueAssetPath(a2Path);
            string a3NewPath = AssetDatabase.GenerateUniqueAssetPath(a3Path);

            GameObject p1Src = PrefabUtility.SaveAsPrefabAsset(a1Src, a1NewPath);
            GameObject p2Src = PrefabUtility.SaveAsPrefabAsset(a2Src, a2NewPath);
            GameObject p3Src = PrefabUtility.SaveAsPrefabAsset(a3Src, a3NewPath);

            newActionsOnHit.objectReferenceValue = p1Src.GetComponent<IActionsList>();
            newActionsOnExec.objectReferenceValue = p2Src.GetComponent<IActionsList>();
            newActionsOnActivate.objectReferenceValue = p3Src.GetComponent<IActionsList>();

            soNewInstance.ApplyModifiedPropertiesWithoutUndo();
            soNewInstance.Update();

            DestroyImmediate(a1Src);
            DestroyImmediate(a2Src);
            DestroyImmediate(a3Src);
        }

        // STYLES: --------------------------------------------------------------------------------

        private void InitializeStyles()
        {
            this.stylePreviewText = new GUIStyle(EditorStyles.whiteLargeLabel);
            this.stylePreviewText.alignment = TextAnchor.MiddleCenter;

            this.styleLabelRight = new GUIStyle(EditorStyles.miniLabel);
            this.styleLabelRight.alignment = TextAnchor.MiddleRight;

            this.styleTimelineBackground = new GUIStyle();
            this.styleTimelineBackground.normal.background = MakeTexture(COLOR_DARK, 0.0f);
            this.styleTimelineBackground.fixedHeight = EditorGUIUtility.singleLineHeight - 2f;

            this.styleTimelineThumb = new GUIStyle();
            this.styleTimelineThumb.normal.background = MakeTexture(new Color(0.9f, 0.9f, 0.9f));
            this.styleTimelineThumb.fixedWidth = 2;
            this.styleTimelineThumb.fixedHeight = EditorGUIUtility.singleLineHeight - 2f;
        }

        private void PaintLegendLabel(Color color, string label, int frames)
        {
            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.label);

            float padding = 2f;
            Rect rectIcon = new Rect(
                rect.x + padding,
                rect.y + padding,
                rect.height - (padding * 2f),
                rect.height - (padding * 2f)
            );

            Rect rectLabel = new Rect(
                rect.x + rect.height + EditorGUIUtility.standardVerticalSpacing,
                rect.y,
                rect.width - rect.height - EditorGUIUtility.standardVerticalSpacing,
                rect.height
            );

            EditorGUI.DrawRect(rectIcon, color);
            EditorGUI.LabelField(rectLabel, label, string.Format("Frames: {0}", frames));
        }

        private static Texture2D MakeTexture(Color color, float alpha = 1.0f)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, new Color(color.r, color.g, color.b, alpha));
            texture.Apply();
            return texture;
        }
    }
}
