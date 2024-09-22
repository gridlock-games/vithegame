using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.Core.MeshSlicing
{
    public class Slicer
    {
        /// <summary>
        /// Slice the object by the plane 
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="objectToCut"></param>
        /// <returns></returns>
        public static GameObject[] Slice(Plane plane, Mesh mesh, Sliceable sliceable, Renderer renderer)
        {
            //Get the current mesh and its verts and tris
            if (sliceable == null)
            {
                Debug.LogError("Cannot slice non sliceable object, add the sliceable script to the object or inherit from sliceable to support slicing");
                return new GameObject[0];
            }

            //Create left and right slice of hollow object
            SlicesMetadata slicesMeta = new SlicesMetadata(plane, mesh, sliceable.IsSolid, sliceable.ReverseWireTriangles, sliceable.ShareVertices, sliceable.SmoothVertices);

            GameObject positiveObject = CreateMeshGameObject(renderer.materials, sliceable);
            positiveObject.name = string.Format("{0}_positive", sliceable.name);

            GameObject negativeObject = CreateMeshGameObject(renderer.materials, sliceable);
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
            MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
            meshCollider.convex = true;

            var rb = gameObject.AddComponent<Rigidbody>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.useGravity = useGravity;
        }
    }
}