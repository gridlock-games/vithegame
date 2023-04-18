﻿using System;
using System.Collections.Generic;
using UnityEngine;
using GameCreator.Core;
using GameCreator.Variables;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NJG.Graph
{
    public class StateMachineController : LocalVariables
    {
        public enum PlayMode
        {
            OnStart,
            OnEnable,
            Manual
        }

        public PlayMode playMode = PlayMode.OnStart;
        public GraphReactions reactions;
        public StateMachine stateMachine;
        public MBVariable invokerVariable;
        public bool overrideColliderValues = true;
        public Collider stateMachineCollider;
        public bool canDrawCollider;

        private GameObject _reactionsInstance;

        public bool VariablesInitialized => initalized;

        private readonly Dictionary<string, Variable> _registeredVariables = new Dictionary<string, Variable>();

#if UNITY_EDITOR
        private static bool _executionOrderSet;

        [UnityEditor.InitializeOnLoadMethod]
        private static void SetSmExecutionOrder()
        {
            if (!_executionOrderSet)
            {
                _executionOrderSet = true;
                int executionOrder = -13000;
                GameObject go = new GameObject();
                StateMachineController smc = go.AddComponent<StateMachineController>();
                MonoScript monoScript = MonoScript.FromMonoBehaviour(smc);

                if (executionOrder != MonoImporter.GetExecutionOrder(monoScript))
                {
                    MonoImporter.SetExecutionOrder(monoScript,
                        executionOrder); // very early but allows other scripts to run even earlier...
                }

                DestroyImmediate(go);
            }
        }
#endif

        protected override void Start()
        {
            base.Start();

            if (Application.isPlaying && playMode == PlayMode.OnStart) Execute();
        }

        private void OnEnable()
        {
            if (Application.isPlaying && playMode == PlayMode.OnEnable) Execute();
        }

        public void Execute()
        {
            if (!stateMachine)
            {
                Debug.LogWarning("[StateMachineController Error] StateMachine is null.", gameObject);
                return;
            }

            if (!stateMachine.sourceReactions)
            {
                Debug.LogWarning("[StateMachineController Error] StateMachine Reactions reference is null.",
                    gameObject);
                return;
            }

            if (!reactions)
            {
                GameObject instance = Instantiate(
                    stateMachine.sourceReactions.gameObject,
                    transform.position,
                    transform.rotation,
                    transform
                );

                instance.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
                instance.layer = gameObject.layer;
                reactions = instance.GetComponent<GraphReactions>();
                reactions.Initialize();
                var list = GetComponent<ListVariables>();
                if (list)
                {
                    var newList = reactions.gameObject.AddComponent<ListVariables>();
                    newList.variables = list.variables;
                    newList.references = list.references;
                }

                if (stateMachineCollider)
                {
                    var col = reactions.GetComponent<Collider>();
                    CopyCollider(stateMachineCollider, col);
                    col.enabled = true;
                    stateMachineCollider.enabled = false;
                }

                UpdateVariableValues();
            }

            _reactionsInstance = stateMachine.Init(this);
        }

        private void UpdateVariableValues()
        {
            for (int i = 0, imax = references.Length; i < imax; i++)
            {
                MBVariable controllerVar = references[i];
                if (!controllerVar) continue;
                for (int e = 0, emax = reactions.references.Length; e < emax; e++)
                {
                    MBVariable reactionVar = reactions.references[e];
                    if (reactionVar.variable.name == controllerVar.variable.name)
                    {
                        var cachedValue = controllerVar.variable.Get();

                        references[i].variable = reactionVar.variable;
                        if (cachedValue != null) reactionVar.variable.Update(cachedValue);

                        if (!_registeredVariables.ContainsKey(reactionVar.variable.name))
                        {
                            VariablesManager.events.SetOnChangeLocal(
                                OnReactionsVariable,
                                reactions.gameObject,
                                reactionVar.variable.name
                            );
                            VariablesManager.events.SetOnChangeLocal(
                                OnControllerVariable,
                                gameObject,
                                controllerVar.variable.name
                            );
                            _registeredVariables.Add(reactionVar.variable.name, controllerVar.variable);
                        }
                    }
                }
            }

            reactions.Reset();
            RequireInit(true);
        }

        private void OnControllerVariable(string variableID)
        {
            if (_registeredVariables.ContainsKey(variableID))
            {
                var controllerVar = Get(variableID);
                var reactionsVar = reactions.Get(variableID);
                if (controllerVar != null && reactionsVar != null && controllerVar.Get() != null &&
                    reactionsVar.Get() != null && !reactionsVar.Get().Equals(controllerVar.Get()))
                {
                    reactionsVar.Update(controllerVar.Get());
                    VariablesManager.events.OnChangeLocal(reactions.gameObject, variableID);
                }
            }
        }

        private void OnReactionsVariable(string variableID)
        {
            if (_registeredVariables.ContainsKey(variableID))
            {
                var controllerVar = Get(variableID);
                var reactionsVar = reactions.Get(variableID);
                if (controllerVar != null && reactionsVar != null && controllerVar.Get() != null &&
                    reactionsVar.Get() != null && !controllerVar.Get().Equals(reactionsVar.Get()))
                {
                    controllerVar.Update(reactionsVar.Get());
                    VariablesManager.events.OnChangeLocal(gameObject, variableID);
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_reactionsInstance) Destroy(_reactionsInstance);
        }

        public IActionsList GetActions(string name)
        {
            ActionsState state = stateMachine.GetActionsState(name);

            if (state)
            {
                return state.GetActions(reactions);
            }
            else
            {
                Debug.LogWarningFormat("Couldn't find node with name {0} on state machine {1}", name, this, gameObject);
            }

            return null;
        }

        public void StopActions(string name)
        {
            IActionsList actions = GetActions(name);

            if (actions)
            {
                actions.Stop();
            }
            else
            {
                Debug.LogWarningFormat("Couldn't find node with name {0} on state machine {1}", name, this, gameObject);
            }
        }

        public void ExecuteNode(string name)
        {
            stateMachine.GetNode(name).OnEnter(reactions, gameObject);
        }

        private void CopyCollider(Collider source, Collider destiny)
        {
            if (source is BoxCollider boxCollider)
            {
                var col2 = destiny as BoxCollider;
                col2.center = boxCollider.center;
                col2.size = boxCollider.size;
                col2.contactOffset = boxCollider.contactOffset;
                col2.isTrigger = boxCollider.isTrigger;
                col2.sharedMaterial = boxCollider.sharedMaterial;
                col2.tag = boxCollider.tag;
            }

            if (source is SphereCollider sphereCollider)
            {
                var col2 = destiny as SphereCollider;
                col2.center = sphereCollider.center;
                col2.radius = sphereCollider.radius;
                col2.contactOffset = sphereCollider.contactOffset;
                col2.isTrigger = sphereCollider.isTrigger;
                col2.sharedMaterial = sphereCollider.sharedMaterial;
                col2.tag = sphereCollider.tag;
            }

            if (source is CapsuleCollider capsuleCollider)
            {
                var col2 = destiny as CapsuleCollider;
                col2.center = capsuleCollider.center;
                col2.radius = capsuleCollider.radius;
                col2.height = capsuleCollider.height;
                col2.direction = capsuleCollider.direction;
                col2.contactOffset = capsuleCollider.contactOffset;
                col2.isTrigger = capsuleCollider.isTrigger;
                col2.sharedMaterial = capsuleCollider.sharedMaterial;
                col2.tag = capsuleCollider.tag;
            }

            if (source is MeshCollider col1)
            {
                var col2 = destiny as MeshCollider;
                col2.convex = col1.convex;
                col2.cookingOptions = col1.cookingOptions;
                col2.sharedMesh = col1.sharedMesh;
                col2.contactOffset = col1.contactOffset;
                col2.isTrigger = col1.isTrigger;
                col2.sharedMaterial = col1.sharedMaterial;
                col2.tag = col1.tag;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (canDrawCollider && stateMachine)
            {
                if (stateMachineCollider)
                {
                    if (overrideColliderValues) SyncCollider(false);
                    else SyncCollider(true);

                    Color c = Color.green;
                    c.a = 0.3f;
                    Gizmos.color = c;
                    if (stateMachineCollider is MeshCollider)
                    {
                        var col = stateMachineCollider as MeshCollider;
                        if (col)
                        {
                            var transform1 = col.transform;
                            Gizmos.DrawMesh(col.sharedMesh, transform1.position, transform1.rotation,
                                transform1.localScale);
                        }
                    }
                    else if (stateMachineCollider is BoxCollider)
                    {
                        var col = stateMachineCollider as BoxCollider;
                        if (col)
                        {
                            var transform1 = col.transform;
                            Gizmos.DrawCube(transform1.position + col.center,
                                Vector3.Scale(col.size, transform1.localScale));
                        }
                    }
                    else if (stateMachineCollider is SphereCollider)
                    {
                        var col = stateMachineCollider as SphereCollider;
                        if (col)
                        {
                            var transform1 = col.transform;
                            Gizmos.DrawSphere(transform1.position + col.center,
                                col.radius * GetHighestValue(transform1.localScale));
                        }
                    }
                    else if (stateMachineCollider is CapsuleCollider)
                    {
                        c.a = 1f;
                        var col = stateMachineCollider as CapsuleCollider;
                        if (col)
                        {
                            var transform1 = col.transform;
                            DrawWireCapsule(transform1.position + col.center, transform1.rotation,
                                col.radius * GetHighestValueXZ(transform1.localScale),
                                col.height * col.transform.localScale.y, c);
                        }
                    }
                }
            }
        }

        public float GetHighestValueXZ(Vector3 vector)
        {
            float maxValue = float.MinValue;
            if (vector.x > maxValue) maxValue = vector.x;
            // if (vector.y > maxValue) maxValue = vector.y;
            if (vector.z > maxValue) maxValue = vector.z;
            return maxValue;
        }

        public float GetHighestValue(Vector3 vector)
        {
            float maxValue = float.MinValue;
            if (vector.x > maxValue) maxValue = vector.x;
            if (vector.y > maxValue) maxValue = vector.y;
            if (vector.z > maxValue) maxValue = vector.z;
            return maxValue;
        }

        public void SyncCollider(bool copyFromController)
        {
            if (!stateMachine) return;
            if (!stateMachine.sourceReactions) return;

            var sourceCollider = stateMachine.sourceReactions.GetComponent<Collider>();

            if ((!sourceCollider && stateMachineCollider) ||
                (sourceCollider && stateMachineCollider
                                && sourceCollider.GetType() != stateMachineCollider.GetType()))
            {
                if (!(stateMachineCollider is CharacterController)) DestroyImmediate(stateMachineCollider, true);
            }

            if (sourceCollider && !stateMachineCollider && gameObject)
            {
                stateMachineCollider = gameObject.AddComponent(sourceCollider.GetType()) as Collider;
                EditorUtility.CopySerialized(sourceCollider, stateMachineCollider);
            }
            else if (sourceCollider && stateMachineCollider &&
                     sourceCollider.GetType() == stateMachineCollider.GetType())
            {
                if (copyFromController) EditorUtility.CopySerialized(stateMachineCollider, sourceCollider);
                else EditorUtility.CopySerialized(sourceCollider, stateMachineCollider);
            }
        }

        private void DrawWireCapsule(Vector3 pos, Quaternion rot, float radius, float height,
            Color color = default(Color))
        {
            if (color != default(Color))
                Handles.color = color;
            Matrix4x4 angleMatrix = Matrix4x4.TRS(pos, rot, Handles.matrix.lossyScale);
            using (new Handles.DrawingScope(angleMatrix))
            {
                var pointOffset = (height - (radius * 2)) / 2;

                //draw sideways
                Handles.DrawWireArc(Vector3.up * pointOffset, Vector3.left, Vector3.back, -180, radius);
                Handles.DrawLine(new Vector3(0, pointOffset, -radius), new Vector3(0, -pointOffset, -radius));
                Handles.DrawLine(new Vector3(0, pointOffset, radius), new Vector3(0, -pointOffset, radius));
                Handles.DrawWireArc(Vector3.down * pointOffset, Vector3.left, Vector3.back, 180, radius);
                //draw frontways
                Handles.DrawWireArc(Vector3.up * pointOffset, Vector3.back, Vector3.left, 180, radius);
                Handles.DrawLine(new Vector3(-radius, pointOffset, 0), new Vector3(-radius, -pointOffset, 0));
                Handles.DrawLine(new Vector3(radius, pointOffset, 0), new Vector3(radius, -pointOffset, 0));
                Handles.DrawWireArc(Vector3.down * pointOffset, Vector3.back, Vector3.left, -180, radius);
                //draw center
                Handles.DrawWireDisc(Vector3.up * pointOffset, Vector3.up, radius);
                Handles.DrawWireDisc(Vector3.down * pointOffset, Vector3.up, radius);
            }
        }
#endif
    }
}