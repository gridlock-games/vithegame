using System.Collections.Generic;
using UnityEngine;

namespace Vi.Core.MeshSlicing
{
    public class Slicer
    {
        public static GameObject[] RandomSlice(GameObject target)
        {
            if (Application.isPlaying) { Debug.LogError("You should only slice meshes at edit time"); return default; }

            Renderer renderer = target.GetComponent<Renderer>();
            Vector3 randomDir = Random.onUnitSphere;
            float sliceLength = 5;
            Vector3 enterTipPosition = renderer.bounds.center + randomDir * sliceLength;
            Vector3 enterBasePosition = renderer.bounds.center - randomDir * sliceLength;

            // Calculate the direction vector between the two points
            Vector3 normalizedDirection = (enterBasePosition - enterTipPosition).normalized;

            // Find a perpendicular vector
            Vector3 perpendicular = new Vector3(-normalizedDirection.z, 0, normalizedDirection.x);

            // Calculate the exitTipPosition
            Vector3 exitTipPosition = enterTipPosition + perpendicular * sliceLength;

            Debug.DrawLine(enterTipPosition, enterBasePosition, Color.red, 15);
            Debug.DrawLine(enterBasePosition, exitTipPosition, Color.blue, 15);
            Debug.DrawLine(exitTipPosition, enterTipPosition, Color.green, 15);

            //Create a triangle between the tip and base so that we can get the normal
            Vector3 side1 = exitTipPosition - enterTipPosition;
            Vector3 side2 = exitTipPosition - enterBasePosition;

            //Get the point perpendicular to the triangle above which is the normal
            //https://docs.unity3d.com/Manual/ComputingNormalPerpendicularVector.html
            Vector3 normal = Vector3.Cross(side1, side2).normalized;

            //Transform the normal so that it is aligned with the object we are slicing's transform.
            Vector3 transformedNormal = ((Vector3)(target.transform.localToWorldMatrix.transpose * normal)).normalized;

            //Get the enter position relative to the object we're cutting's local transform
            Vector3 transformedStartingPoint = target.transform.InverseTransformPoint(enterTipPosition);

            Plane plane = new Plane();
            plane.SetNormalAndPosition(transformedNormal, transformedStartingPoint);

            var direction = Vector3.Dot(Vector3.up, transformedNormal);

            //Flip the plane so that we always know which side the positive mesh is on
            if (direction < 0)
            {
                plane = plane.flipped;
            }

            Mesh mesh = null;
            if (target.TryGetComponent(out SkinnedMeshRenderer skinnedMeshRenderer))
            {
                mesh = skinnedMeshRenderer.sharedMesh;
            }
            else if (target.TryGetComponent(out MeshFilter meshFilter))
            {
                mesh = meshFilter.sharedMesh;
            }
            else
            {
                Debug.LogError("Mesh not found!");
            }

            return Slice(plane, mesh, target.GetComponent<Sliceable>(), renderer);
        }

        /// <summary>
        /// Slice the object by the plane 
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="objectToCut"></param>
        /// <returns></returns>
        public static GameObject[] Slice(Plane plane, Mesh mesh, Sliceable sliceable, Renderer renderer)
        {
            if (Application.isPlaying) { Debug.LogError("You should only slice meshes at edit time"); return default; }

            //Get the current mesh and its verts and tris
            if (sliceable == null)
            {
                Debug.LogError("Cannot slice non sliceable object, add the sliceable script to the object or inherit from sliceable to support slicing");
                return new GameObject[0];
            }

            //Create left and right slice of hollow object
            SlicesMetadata slicesMeta = new SlicesMetadata(plane, mesh, sliceable.IsSolid, sliceable.ReverseWireTriangles, sliceable.ShareVertices, sliceable.SmoothVertices);

            GameObject positiveObject = CreateMeshGameObject(renderer.sharedMaterials, sliceable);
            positiveObject.name = string.Format("{0}_positive", sliceable.name);

            GameObject negativeObject = CreateMeshGameObject(renderer.sharedMaterials, sliceable);
            negativeObject.name = string.Format("{0}_negative", sliceable.name);

            var positiveSideMeshData = slicesMeta.PositiveSideMesh;
            var negativeSideMeshData = slicesMeta.NegativeSideMesh;

            positiveObject.GetComponent<MeshFilter>().mesh = positiveSideMeshData;
            negativeObject.GetComponent<MeshFilter>().mesh = negativeSideMeshData;

            SetupCollidersAndRigidBodys(ref positiveObject, positiveSideMeshData, sliceable.UseGravity);
            SetupCollidersAndRigidBodys(ref negativeObject, negativeSideMeshData, sliceable.UseGravity);

            positiveObject.layer = LayerMask.NameToLayer("Character");
            negativeObject.layer = LayerMask.NameToLayer("Character");

            return new GameObject[] { positiveObject, negativeObject };
        }

        /// <summary>
        /// Creates the default mesh game object.
        /// </summary>
        /// <param name="originalObject">The original object.</param>
        /// <returns></returns>
        private static GameObject CreateMeshGameObject(Material[] originalMaterial, Sliceable originalSliceable)
        {
            GameObject meshGameObject = new GameObject();

            meshGameObject.AddComponent<MeshFilter>();
            meshGameObject.AddComponent<MeshRenderer>();
            Sliceable sliceable = meshGameObject.AddComponent<Sliceable>();

            sliceable.IsSolid = originalSliceable.IsSolid;
            sliceable.ReverseWireTriangles = originalSliceable.ReverseWireTriangles;
            sliceable.UseGravity = originalSliceable.UseGravity;

            meshGameObject.GetComponent<MeshRenderer>().materials = originalMaterial;

            meshGameObject.transform.localScale = originalSliceable.transform.localScale;
            meshGameObject.transform.rotation = originalSliceable.transform.rotation;
            meshGameObject.transform.position = originalSliceable.transform.position;

            meshGameObject.tag = originalSliceable.tag;

            return meshGameObject;
        }

        /// <summary>
        /// Add mesh collider and rigid body to game object
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="mesh"></param>
        private static void SetupCollidersAndRigidBodys(ref GameObject gameObject, Mesh mesh, bool useGravity)
        {
            if (!Application.isPlaying) { return; }
            MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
            meshCollider.convex = true;

            var rb = gameObject.AddComponent<Rigidbody>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.useGravity = useGravity;
        }
    }
}