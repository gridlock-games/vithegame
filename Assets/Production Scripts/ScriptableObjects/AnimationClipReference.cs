using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;
using System.Linq;

namespace Vi.ScriptableObjects
{
    [CreateAssetMenu(fileName = "AnimationClipReference", menuName = "Production/Animation Clip Reference")]
    public class AnimationClipReference : ScriptableObject
    {
        public AnimationData GetAnimationData(AnimationClip animationClip)
        {
            if (!animationClip) { Debug.LogError("Do not call AnimationClipReference.GetAnimationData() with a null animation clip!"); return default; }
            foreach (AnimationData data in serializedAnimationData)
            {
                if (data.animationClip == animationClip) { return data; }
            }
            Debug.LogWarning(animationClip.name + " has no animation data!");
            return default;
        }

        [SerializeField] private List<AnimationData> serializedAnimationData;

        [System.Serializable]
        public class AnimationData : System.IEquatable<AnimationData>
        {
            public string name;
            public AnimationClip animationClip;
            public AnimationCurve sidesMotion;
            public AnimationCurve verticalMotion;
            public AnimationCurve forwardMotion;

            public AnimationData(AnimationClip animationClip, AnimationCurve sidesMotion, AnimationCurve verticalMotion, AnimationCurve forwardMotion)
            {
                name = animationClip.name;
                this.animationClip = animationClip;
                this.sidesMotion = sidesMotion;
                this.verticalMotion = verticalMotion;
                this.forwardMotion = forwardMotion;
            }

            public bool Equals(AnimationData other)
            {
                return animationClip == other.animationClip;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Find Animations")]
        private void FindAnimations()
        {
            string animatorControllerFolder = @"Assets\Production\AnimationControllers";

            List<AnimationClip> allClips = new List<AnimationClip>();

            foreach (string animatorControllerPath in Directory.GetFiles(animatorControllerFolder, "*.controller", SearchOption.AllDirectories))
            {
                RuntimeAnimatorController animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(animatorControllerPath);
                allClips.AddRange(animatorController.animationClips);
            }

            foreach (string animatorControllerPath in Directory.GetFiles(animatorControllerFolder, "*.overrideController", SearchOption.AllDirectories))
            {
                AnimatorOverrideController animatorController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(animatorControllerPath);
                allClips.AddRange(animatorController.animationClips);
            }

            serializedAnimationData.Clear();
            foreach (AnimationClip animationClip in allClips.Distinct())
            {
                if (!animationClip.hasRootCurves) { continue; }

                AnimationCurve sidesMotion = default;
                AnimationCurve verticalMotion = default;
                AnimationCurve forwardMotion = default;

                EditorCurveBinding[] curves = AnimationUtility.GetCurveBindings(animationClip);
                for (int i = 0; i < curves.Length; ++i)
                {
                    if (curves[i].propertyName == "RootT.x")
                    {
                        sidesMotion = AnimationUtility.GetEditorCurve(
                            animationClip,
                            curves[i]
                        );
                    }

                    if (curves[i].propertyName == "RootT.y")
                    {
                        verticalMotion = AnimationUtility.GetEditorCurve(
                            animationClip,
                            curves[i]
                        );
                    }

                    if (curves[i].propertyName == "RootT.z")
                    {
                        forwardMotion = AnimationUtility.GetEditorCurve(
                            animationClip,
                            curves[i]
                        );
                    }
                }

                AnimationData animationData = new AnimationData(animationClip, sidesMotion, verticalMotion, forwardMotion);
                if (!serializedAnimationData.Contains(animationData)) { serializedAnimationData.Add(animationData); }
            }
        }
#endif
    }
}