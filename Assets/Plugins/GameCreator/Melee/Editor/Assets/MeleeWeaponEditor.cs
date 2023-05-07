namespace GameCreator.Melee
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;
    using GameCreator.Core;
    using GameCreator.Characters;
    using UnityEditorInternal;

    [CustomEditor(typeof(MeleeWeapon))]
	public class MeleeWeaponEditor : IMeleeEditor
	{
        private static readonly Color ODD_LAYER = new Color(0, 0, 0, 0.1f);
        private static readonly Color ACTIVE_LAYER = new Color(0, 0, 0, 0.3f);

        private static readonly GUIContent GC_REACTION = new GUIContent("Hit Reaction");
        private static readonly GUIContent GC_WM = new GUIContent("Weapon Model");

        private const float PADDING = 4f;
        private const float SPACING = 2f;

        // PRIVATE PROPERTIES: --------------------------------------------------------------------

        private MeleeWeapon instance;

        private Section sectionGeneral;
        private Section sectionModel;
        private Section sectionEffects;
        private Section sectionDodges;

        private Section sectionGrabs;

        private Section sectionRecoveryStates;

        private SerializedProperty spName;
        private SerializedProperty spDescription;
        private SerializedProperty spMeleeWeaponName;

        private SerializedProperty spDefaultShield;
        private SerializedProperty spCharacterState;
        private SerializedProperty spAvatarMask;
        private SerializedProperty spWeaponImage;

        private SerializedProperty spPrefab;
        private SerializedProperty spAttachment;
        private SerializedProperty spPosition;
        private SerializedProperty spRotation;

        private SerializedProperty spAudioSheathe;
        private SerializedProperty spAudioDraw;
        private SerializedProperty spAudioImpactNormal;
        private SerializedProperty spAudioImpactKnockback;

        private SerializedProperty spPrefabImpactNormal;
        private SerializedProperty spPrefabImpactKnockback;

        private SerializedProperty spGroundHitReactionFront;
        private SerializedProperty spGroundHitReactionBehind;
        private SerializedProperty spAirborneHitReactionFront;
        private SerializedProperty spAirborneHitReactionBehind;
        private SerializedProperty spGroundHitReactionsLeftMiddle;
        private SerializedProperty spGroundHitReactionsRightMiddle;
        private SerializedProperty spKnockbackReactions;

        public SerializedProperty spDodgeF;
        public SerializedProperty spDodgeFL;
        public SerializedProperty spDodgeFR;
        public SerializedProperty spDodgeB;
        public SerializedProperty spDodgeBL;
        public SerializedProperty spDodgeBR;
        public SerializedProperty spDodgeL;
        public SerializedProperty spDodgeR;

        private SerializedProperty spCombos;

        private ReorderableList groundHitReactionsFrontList;
        private ReorderableList groundHitReactionsBehindList;
        private ReorderableList airborneHitReactionsFrontList;
        private ReorderableList airborneHitReactionsBehindList;
        private ReorderableList groundHitReactionsLeftMiddle;
        private ReorderableList groundHitReactionsRightMiddle;
        private ReorderableList knockbackReactionsList;
        private ReorderableList comboList;

        
        public SerializedProperty spGrabAttack;
        public SerializedProperty spGrabReaction;
        public SerializedProperty spGrabPlaceholderPosition;
        public SerializedProperty spStandFaceUp;
        public SerializedProperty spStandFaceDown;
        public SerializedProperty spRecoveryStun;

        public SerializedProperty spKnockbackF;
        public SerializedProperty spKnockbackB;

        private SerializedProperty spWeaponModels;
        
        // PROPERTIES: ----------------------------------------------------------------------------

        

        // INITIALIZER: ---------------------------------------------------------------------------

        private void OnEnable()
        {
            this.instance = this.target as MeleeWeapon;

            this.sectionGeneral = new Section("General", this.LoadIcon("General"), this.Repaint);
            this.sectionModel = new Section("Weapon Model", this.LoadIcon("Sword"), this.Repaint);
            this.sectionEffects = new Section("Effects", this.LoadIcon("Effects"), this.Repaint);
            this.sectionDodges = new Section("Dodge Animations", this.LoadIcon("General"), this.Repaint);
            this.sectionGrabs = new Section("Grab Animations", this.LoadIcon("Effects"), this.Repaint);

            
            this.sectionRecoveryStates = new Section("Ailment Recovery Animations", this.LoadIcon("Effects"), this.Repaint);

            this.spGrabAttack = this.serializedObject.FindProperty("grabAttack");
            this.spGrabReaction = this.serializedObject.FindProperty("grabReaction");
            this.spGrabPlaceholderPosition = this.serializedObject.FindProperty("grabPlaceholderPosition");

            this.spKnockbackB = this.serializedObject.FindProperty("knockbackB"); 
            this.spKnockbackF = this.serializedObject.FindProperty("knockbackF"); 

            this.spStandFaceUp = this.serializedObject.FindProperty("recoveryStandUp");
            this.spRecoveryStun = this.serializedObject.FindProperty("recoveryStun");
            this.spStandFaceDown = this.serializedObject.FindProperty("recoveryStandDown");
            

            this.spName = this.serializedObject.FindProperty("weaponName");
            this.spDescription = this.serializedObject.FindProperty("weaponDescription");
            this.spMeleeWeaponName = this.serializedObject.FindProperty("meleeWeaponName");

            this.spDefaultShield = this.serializedObject.FindProperty("defaultShield");
            this.spCharacterState = this.serializedObject.FindProperty("characterState");
            this.spAvatarMask = this.serializedObject.FindProperty("characterMask");
            this.spWeaponImage = this.serializedObject.FindProperty("weaponImage");

            this.spPrefab = this.serializedObject.FindProperty("prefab");
            this.spAttachment = this.serializedObject.FindProperty("attachment");
            this.spPosition = this.serializedObject.FindProperty("positionOffset");
            this.spRotation = this.serializedObject.FindProperty("rotationOffset");

            this.spAudioSheathe = this.serializedObject.FindProperty("audioSheathe");
            this.spAudioDraw = this.serializedObject.FindProperty("audioDraw");
            this.spAudioImpactNormal = this.serializedObject.FindProperty("audioImpactNormal");
            this.spAudioImpactKnockback = this.serializedObject.FindProperty("audioImpactKnockback");

            this.spPrefabImpactNormal = this.serializedObject.FindProperty("prefabImpactNormal");
            this.spPrefabImpactKnockback = this.serializedObject.FindProperty("prefabImpactKnockback");

            this.spGroundHitReactionFront = this.serializedObject.FindProperty("groundHitReactionsFront");
            this.spGroundHitReactionBehind = this.serializedObject.FindProperty("groundHitReactionsBehind");
            this.spAirborneHitReactionFront = this.serializedObject.FindProperty("airborneHitReactionsFront");
            this.spAirborneHitReactionBehind = this.serializedObject.FindProperty("airborneHitReactionsBehind");
            
            this.spGroundHitReactionsLeftMiddle = this.serializedObject.FindProperty("groundHitReactionsLeftMiddle");
            this.spGroundHitReactionsRightMiddle = this.serializedObject.FindProperty("groundHitReactionsRightMiddle");
            this.spKnockbackReactions = this.serializedObject.FindProperty("knockbackReaction");

            this.spDodgeF = this.serializedObject.FindProperty("dodgeF");
            this.spDodgeFL = this.serializedObject.FindProperty("dodgeFL");
            this.spDodgeFR = this.serializedObject.FindProperty("dodgeFR");
            this.spDodgeB = this.serializedObject.FindProperty("dodgeB");
            this.spDodgeBL = this.serializedObject.FindProperty("dodgeBL");
            this.spDodgeBR = this.serializedObject.FindProperty("dodgeBR");
            this.spDodgeL = this.serializedObject.FindProperty("dodgeL");
            this.spDodgeR = this.serializedObject.FindProperty("dodgeR");

            this.groundHitReactionsFrontList = new ReorderableList(
                this.serializedObject,
                this.spGroundHitReactionFront,
                true, true, true, true
            );

            this.groundHitReactionsBehindList = new ReorderableList(
                this.serializedObject,
                this.spGroundHitReactionBehind,
                true, true, true, true
            );

            this.airborneHitReactionsFrontList = new ReorderableList(
                this.serializedObject,
                this.spAirborneHitReactionFront,
                true, true, true, true
            );

            this.airborneHitReactionsBehindList = new ReorderableList(
                this.serializedObject,
                this.spAirborneHitReactionBehind,
                true, true, true, true
            );

            this.knockbackReactionsList = new ReorderableList(
                this.serializedObject,
                this.spKnockbackReactions,
                true, true, true, true
            );

            this.groundHitReactionsLeftMiddle = new ReorderableList(
                this.serializedObject,
                this.spGroundHitReactionsLeftMiddle,
                true, true, true, true
            );

            this.groundHitReactionsRightMiddle = new ReorderableList(
                this.serializedObject,
                this.spGroundHitReactionsRightMiddle,
                true, true, true, true
            );

            this.groundHitReactionsFrontList.drawHeaderCallback += this.PaintHitGroundFront_Title;
            this.groundHitReactionsBehindList.drawHeaderCallback += this.PaintHitGroundBehind_Title;
            this.airborneHitReactionsFrontList.drawHeaderCallback += this.PaintHitAirFront_Title;
            this.airborneHitReactionsBehindList.drawHeaderCallback += this.PaintHitAirBehind_Title;
            this.knockbackReactionsList.drawHeaderCallback += this.PaintKnockback_Title;
            this.groundHitReactionsLeftMiddle.drawHeaderCallback += this.PaintHitGroundLeft_Title;
            this.groundHitReactionsRightMiddle.drawHeaderCallback += this.PaintHitGroundRight_Title;

            this.groundHitReactionsFrontList.drawElementCallback += this.PaintHitGroundFront_Element;
            this.groundHitReactionsBehindList.drawElementCallback += this.PaintHitGroundBehind_Element;
            this.airborneHitReactionsFrontList.drawElementCallback += this.PaintHitAirFront_Element;
            this.airborneHitReactionsBehindList.drawElementCallback += this.PaintHitAirBehind_Element;
            this.knockbackReactionsList.drawElementCallback += this.PaintHitKnockback_Element;
            this.groundHitReactionsLeftMiddle.drawElementCallback += this.PaintHitGroundLeft_Element;
            this.groundHitReactionsRightMiddle.drawElementCallback += this.PaintHitGroundRight_Element;

            //combo
            this.spCombos = this.serializedObject.FindProperty("combos");

            this.comboList = new ReorderableList(
                this.serializedObject,
                this.spCombos,
                true, true, true, true
            );

            this.comboList.elementHeight = ComboPD.GetHeight() + PADDING * 2F;
            this.comboList.drawHeaderCallback += this.PaintCombo_Header;
            this.comboList.drawElementBackgroundCallback += this.PaintCombo_ElementBg;
            this.comboList.drawElementCallback += this.PaintCombo_Element;

            //weapon model
             this.spWeaponModels = this.serializedObject.FindProperty("weaponModels");
             //
             // this.weaponModelsList = new ReorderableList(
             //     this.serializedObject,
             //     this.spWeaponModels,
             //     true, true, true, true
             // );
             //
             // this.weaponModelsList.drawHeaderCallback += this.PaintWeaponModel_Header;
             // this.weaponModelsList.drawElementCallback += this.PaintWeaponModel_Element;
            
           
        }

        // PAINT METHODS: -------------------------------------------------------------------------

        public override void OnInspectorGUI()
        {
            this.serializedObject.ApplyModifiedProperties();

            GUILayout.Space(SPACING);
            this.PaintSectionGeneral();
            
            EditorGUILayout.Space();
            // this.weaponModelsList.DoLayoutList();
            EditorGUILayout.Space(5f);

            GUILayout.Space(SPACING);
            this.PaintSectionModel();

            GUILayout.Space(SPACING);
            this.PaintSectionEffects();

            EditorGUILayout.Space();
            this.comboList.DoLayoutList();

            EditorGUILayout.Space();
            this.PaintDodge_Element();

            EditorGUILayout.Space();
            this.PaintGrab_Element();

            

            EditorGUILayout.Space();
            this.PaintAilmentRecovery_Element();


            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Hit Reactions", EditorStyles.boldLabel);

            EditorGUILayout.Space();
            this.groundHitReactionsFrontList.DoLayoutList();

            EditorGUILayout.Space();
            this.groundHitReactionsBehindList.DoLayoutList();

            EditorGUILayout.Space();
            this.airborneHitReactionsFrontList.DoLayoutList();

            EditorGUILayout.Space();
            this.airborneHitReactionsBehindList.DoLayoutList();

            EditorGUILayout.Space();
            this.groundHitReactionsLeftMiddle.DoLayoutList();

            EditorGUILayout.Space();
            this.groundHitReactionsRightMiddle.DoLayoutList();

            EditorGUILayout.Space();
            this.knockbackReactionsList.DoLayoutList();

            this.serializedObject.ApplyModifiedProperties();

            
        }

        private void PaintSectionGeneral()
        {
            this.sectionGeneral.PaintSection();
            using (var group = new EditorGUILayout.FadeGroupScope(this.sectionGeneral.state.faded))
            {
                if (group.visible)
                {
                    EditorGUILayout.BeginVertical(CoreGUIStyles.GetBoxExpanded());

                    EditorGUILayout.PropertyField(this.spName);
                    EditorGUILayout.PropertyField(this.spDescription);
                    EditorGUILayout.PropertyField(this.spMeleeWeaponName);

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(this.spDefaultShield);

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(this.spCharacterState);
                    EditorGUILayout.PropertyField(this.spAvatarMask);
                    EditorGUILayout.PropertyField(this.spWeaponImage);

                    EditorGUILayout.PropertyField(this.spWeaponModels);
                    EditorGUILayout.EndVertical();
                }
            }
        }

        private void PaintSectionModel()
        {
            this.sectionModel.PaintSection();
            using (var group = new EditorGUILayout.FadeGroupScope(this.sectionModel.state.faded))
            {
                if (group.visible)
                {
                    EditorGUILayout.BeginVertical(CoreGUIStyles.GetBoxExpanded());

                    EditorGUILayout.PropertyField(this.spPrefab);
                    EditorGUILayout.PropertyField(this.spAttachment);

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(this.spPosition);
                    EditorGUILayout.PropertyField(this.spRotation);

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

                    EditorGUILayout.PropertyField(this.spAudioDraw);
                    EditorGUILayout.PropertyField(this.spAudioSheathe);

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(this.spAudioImpactNormal);
                    EditorGUILayout.PropertyField(this.spAudioImpactKnockback);

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(this.spPrefabImpactNormal);
                    EditorGUILayout.PropertyField(this.spPrefabImpactKnockback);

                    EditorGUILayout.EndVertical();
                }
            }
        }

        // HIT REACTIONS: -------------------------------------------------------------------------

        private void PaintHitGroundFront_Title(Rect rect)
        {
            EditorGUI.LabelField(rect, "Hit Reaction - Grounded & Front");
        }

        private void PaintHitGroundBehind_Title(Rect rect)
        {
            EditorGUI.LabelField(rect, "Hit Reaction - Grounded & Behind");
        }

        private void PaintHitAirFront_Title(Rect rect)
        {
            EditorGUI.LabelField(rect, "Hit Reaction - Airborne & Frontal");
        }

        private void PaintHitAirBehind_Title(Rect rect)
        {
            EditorGUI.LabelField(rect, "Hit Reaction - Airborne & Behind");
        }

        private void PaintHitGroundLeft_Title(Rect rect)
        {
            EditorGUI.LabelField(rect, "Hit Reaction - Grounded & Left");
        }

        private void PaintHitGroundRight_Title(Rect rect)
        {
            EditorGUI.LabelField(rect, "Hit Reaction - Grounded & Right");
        }

        private void PaintKnockback_Title(Rect rect)
        {
            EditorGUI.LabelField(rect, "Hit Reaction - Knockback");
        }

        private void PaintDodge_Title(Rect rect)
        {
            EditorGUI.LabelField(rect, "Dodge Animations");
        }

        private void PaintDodge_Element()
        {
            this.sectionDodges.PaintSection();
            using (var group = new EditorGUILayout.FadeGroupScope(this.sectionDodges.state.faded))
            {
                if (group.visible)
                {
                    EditorGUILayout.BeginVertical(CoreGUIStyles.GetBoxExpanded());

                    EditorGUILayout.PropertyField(this.spDodgeB);
                    EditorGUILayout.PropertyField(this.spDodgeBL);
                    EditorGUILayout.PropertyField(this.spDodgeBR);

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(this.spDodgeF);
                    EditorGUILayout.PropertyField(this.spDodgeFL);
                    EditorGUILayout.PropertyField(this.spDodgeFR);

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(this.spDodgeL);
                    EditorGUILayout.PropertyField(this.spDodgeR);

                    EditorGUILayout.EndVertical();
                }
            }
        }

        private void PaintGrab_Element()
        {
            this.sectionGrabs.PaintSection();
            using (var group = new EditorGUILayout.FadeGroupScope(this.sectionGrabs.state.faded))
            {
                if(group.visible)
                {
                    EditorGUILayout.BeginVertical(CoreGUIStyles.GetBoxExpanded());

                    EditorGUILayout.PropertyField(this.spGrabAttack);
                    EditorGUILayout.PropertyField(this.spGrabReaction);
                    EditorGUILayout.PropertyField(this.spGrabPlaceholderPosition);
                    

                    EditorGUILayout.EndVertical();
                }
            }


        }

        private void PaintAilmentRecovery_Element()
        {
            this.sectionRecoveryStates.PaintSection();
            using (var group = new EditorGUILayout.FadeGroupScope(this.sectionRecoveryStates.state.faded))
            {
                if(group.visible)
                {
                    EditorGUILayout.BeginVertical(CoreGUIStyles.GetBoxExpanded());

                    EditorGUILayout.PropertyField(this.spStandFaceUp);
                    EditorGUILayout.PropertyField(this.spRecoveryStun);
                    EditorGUILayout.PropertyField(this.spKnockbackF);
                    EditorGUILayout.PropertyField(this.spKnockbackB);
                    EditorGUILayout.EndVertical();
                }
            }
        }
        
        private void PaintHitGroundFront_Element(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect = new Rect(
                rect.x, rect.y + (rect.height - EditorGUIUtility.singleLineHeight) / 2f,
                rect.width, EditorGUIUtility.singleLineHeight
            );

            EditorGUI.PropertyField(
                rect, this.spGroundHitReactionFront.GetArrayElementAtIndex(index),
                GC_REACTION, true
            );
        }

        private void PaintHitGroundBehind_Element(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect = new Rect(
                rect.x, rect.y + (rect.height - EditorGUIUtility.singleLineHeight) / 2f,
                rect.width, EditorGUIUtility.singleLineHeight
            );

            EditorGUI.PropertyField(
                rect, this.spGroundHitReactionBehind.GetArrayElementAtIndex(index),
                GC_REACTION, true
            );
        }

        private void PaintHitAirFront_Element(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect = new Rect(
                rect.x, rect.y + (rect.height - EditorGUIUtility.singleLineHeight) / 2f,
                rect.width, EditorGUIUtility.singleLineHeight
            );

            EditorGUI.PropertyField(
                rect, this.spAirborneHitReactionFront.GetArrayElementAtIndex(index),
                GC_REACTION, true
            );
        }

        private void PaintHitAirBehind_Element(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect = new Rect(
                rect.x, rect.y + (rect.height - EditorGUIUtility.singleLineHeight) / 2f,
                rect.width, EditorGUIUtility.singleLineHeight
            );

            EditorGUI.PropertyField(
                rect, this.spAirborneHitReactionBehind.GetArrayElementAtIndex(index),
                GC_REACTION, true
            );
        }

        private void PaintHitGroundLeft_Element(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect = new Rect(
                rect.x, rect.y + (rect.height - EditorGUIUtility.singleLineHeight) / 2f,
                rect.width, EditorGUIUtility.singleLineHeight
            );

            EditorGUI.PropertyField(
                rect, this.spGroundHitReactionsLeftMiddle.GetArrayElementAtIndex(index),
                GC_REACTION, true
            );
        }

        private void PaintHitGroundRight_Element(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect = new Rect(
                rect.x, rect.y + (rect.height - EditorGUIUtility.singleLineHeight) / 2f,
                rect.width, EditorGUIUtility.singleLineHeight
            );

            EditorGUI.PropertyField(
                rect, this.spGroundHitReactionsRightMiddle.GetArrayElementAtIndex(index),
                GC_REACTION, true
            );
        }

        private void PaintHitKnockback_Element(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect = new Rect(
                rect.x, rect.y + (rect.height - EditorGUIUtility.singleLineHeight) / 2f,
                rect.width, EditorGUIUtility.singleLineHeight
            );

            EditorGUI.PropertyField(
                rect, this.spKnockbackReactions.GetArrayElementAtIndex(index),
                GC_REACTION, true
            );
        }

        // COMBO METHODS: -------------------------------------------------------------------------

        private void PaintCombo_Header(Rect rect)
        {
            EditorGUI.LabelField(rect, "Combo Creator");
        }

        private void PaintCombo_ElementBg(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect = new Rect(rect.x + 2f, rect.y, rect.width - 4f, rect.height);
            if (isActive || isFocused) EditorGUI.DrawRect(rect, ACTIVE_LAYER);
            else if (index % 2 == 0) EditorGUI.DrawRect(rect, ODD_LAYER);
        }

        private void PaintCombo_Element(Rect rect, int index, bool isActive, bool isFocused)
        {
            this.serializedObject.ApplyModifiedProperties();
            this.serializedObject.Update();

            Rect rectCombo = new Rect(
                rect.x,
                rect.y + PADDING,
                rect.width,
                rect.height - (PADDING * 2f)
            );

            EditorGUI.PropertyField(rectCombo, this.spCombos.GetArrayElementAtIndex(index), true);

            this.serializedObject.ApplyModifiedProperties();
            this.serializedObject.Update();
        }
        
        //Weapon model methods
        
        // private void PaintWeaponModel_Header(Rect rect)
        // {
        //     EditorGUI.LabelField(rect, "Weapon Model");
        // }
        //
        // private void PaintWeaponModel_Element(Rect rect, int index, bool isActive, bool isFocused)
        // {
        //     rect = new Rect(
        //         rect.x, rect.y + (rect.height - EditorGUIUtility.singleLineHeight) / 2f,
        //         rect.width, EditorGUIUtility.singleLineHeight
        //     );
        //
        //     EditorGUI.PropertyField(
        //         rect, this.spWeaponModels.GetArrayElementAtIndex(index),
        //         GC_WM, true
        //     );
        // }
    }
}
