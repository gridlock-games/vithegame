using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

namespace Vi.Core
{
    public class ColliderWeapon : RuntimeWeapon
    {
        private List<Attributes> hitsOnThisPhysicsUpdate = new List<Attributes>();

        private void OnTriggerEnter(Collider other) { ProcessTriggerEvent(other); }
        private void OnTriggerStay(Collider other) { ProcessTriggerEvent(other); }

        private void ProcessTriggerEvent(Collider other)
        {
            if (!NetworkManager.Singleton.IsServer) { return; }

            if (other.isTrigger) { return; }
            if (!parentWeaponHandler) { return; }
            if (!parentWeaponHandler.IsAttacking) { return; }
            if (parentWeaponHandler.CurrentActionClip.effectedWeaponBones == null) { return; }
            if (!parentWeaponHandler.CurrentActionClip.effectedWeaponBones.Contains(weaponBone)) { return; }

            if (other.TryGetComponent(out NetworkCollider networkCollider))
            {
                if (parentAttributes == networkCollider.Attributes) { return; }
                if (!CanHit(networkCollider.Attributes)) { return; }

                if (hitsOnThisPhysicsUpdate.Contains(networkCollider.Attributes)) { return; }

                bool bHit = networkCollider.Attributes.ProcessMeleeHit(parentAttributes,
                    parentWeaponHandler.CurrentActionClip,
                    this,
                    other.ClosestPointOnBounds(transform.position),
                    parentAttributes.transform.position
                );

                if (bHit)
                {
                    hitsOnThisPhysicsUpdate.Add(networkCollider.Attributes);
                    parentWeaponHandler.lastMeleeHitTime = Time.time;
                }
            }
        }

        private bool clearListNextUpdate;
        private void FixedUpdate()
        {
            if (clearListNextUpdate) { hitsOnThisPhysicsUpdate.Clear(); }
            clearListNextUpdate = hitsOnThisPhysicsUpdate.Count > 0;
        }

        [SerializeField] private Material weaponTrailMaterial;
        [SerializeField] private int weaponTrailGranularity = 60;
        [SerializeField] private float weaponTrailDuration = 0.5f;
        [SerializeField] private float weaponTrailFadeTime = 0.25f;

        private WeaponTrail weaponTrail;
        private Vector3 weaponTrailPointA;
        private Vector3 weaponTrailPointB;
        private void Awake()
        {
            if (weaponTrailMaterial)
            {
                Collider c = GetComponentInChildren<Collider>();
                weaponTrailPointA = new Vector3(c.bounds.center.x, c.bounds.center.y, c.bounds.center.z - c.bounds.extents.z);
                weaponTrailPointB = new Vector3(c.bounds.center.x, c.bounds.center.y, c.bounds.center.z + c.bounds.extents.z);

                weaponTrail = new WeaponTrail()
                {
                    pointA = weaponTrailPointA,
                    pointB = weaponTrailPointB
                };

                weaponTrail.Initialize(
                    weaponTrailMaterial,
                    weaponTrailGranularity,
                    weaponTrailDuration
                );
            }
        }

        private void Update()
        {
            if (weaponTrail == null) { return; }

            if (parentWeaponHandler.IsAttacking & !isStowed)
            {
                weaponTrail.Activate();
            }
            else
            {
                weaponTrail.Deactivate(weaponTrailFadeTime);
            }
        }

        private void LateUpdate()
        {
            if (weaponTrail == null) { return; }
            weaponTrail.Tick(transform.TransformPoint(weaponTrailPointA), transform.TransformPoint(weaponTrailPointB));
        }

        private void OnDestroy()
        {
            if (weaponTrail != null)
            {
                if (weaponTrail.Trail) { Destroy(weaponTrail.Trail); }
            }
        }

        private void OnDrawGizmos()
        {
            if (!parentWeaponHandler) { return; }

            if (TryGetComponent(out BoxCollider boxCollider))
            {
                if (parentWeaponHandler.CurrentActionClip)
                {
                    if (parentWeaponHandler.CurrentActionClip.effectedWeaponBones != null)
                    {
                        if (parentWeaponHandler.CurrentActionClip.effectedWeaponBones.Contains(weaponBone))
                        {
                            if (parentWeaponHandler.IsInAnticipation)
                                Gizmos.color = Color.yellow;
                            else if (parentWeaponHandler.IsAttacking)
                                Gizmos.color = Color.red;
                            else if (parentWeaponHandler.IsInRecovery)
                                Gizmos.color = Color.magenta;
                            else
                                Gizmos.color = Color.white;
                        }
                        else
                        {
                            Gizmos.color = Color.white;
                        }
                    }
                    else
                    {
                        Gizmos.color = Color.white;
                    }
                }
                else
                {
                    Gizmos.color = Color.white;
                }

                Matrix4x4 rotationMatrix = Matrix4x4.TRS(boxCollider.transform.position, boxCollider.transform.rotation, boxCollider.transform.lossyScale);
                Gizmos.matrix = rotationMatrix;
                Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
            }
            else if (TryGetComponent(out SphereCollider sphereCollider))
            {
                if (parentWeaponHandler.CurrentActionClip)
                {
                    if (parentWeaponHandler.CurrentActionClip.effectedWeaponBones != null)
                    {
                        if (parentWeaponHandler.CurrentActionClip.effectedWeaponBones.Contains(weaponBone))
                        {
                            if (parentWeaponHandler.IsInAnticipation)
                                Gizmos.color = Color.yellow;
                            else if (parentWeaponHandler.IsAttacking)
                                Gizmos.color = Color.red;
                            else if (parentWeaponHandler.IsInRecovery)
                                Gizmos.color = Color.magenta;
                            else
                                Gizmos.color = Color.white;
                        }
                        else
                        {
                            Gizmos.color = Color.white;
                        }
                    }
                    else
                    {
                        Gizmos.color = Color.white;
                    }
                }
                else
                {
                    Gizmos.color = Color.white;
                }

                Matrix4x4 rotationMatrix = Matrix4x4.TRS(sphereCollider.transform.position, sphereCollider.transform.rotation, sphereCollider.transform.lossyScale);
                Gizmos.matrix = rotationMatrix;
                Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
            }
        }
    }

    public class WeaponTrail
    {
        private class Data
        {
            public Vector3 pointA;
            public Vector3 pointB;
            public bool visible;
            public float time;

            public Data()
            {
                this.pointA = Vector3.zero;
                this.pointB = Vector3.zero;
                this.time = Time.time;
                this.visible = false;
            }

            public Data(Vector3 pointA, Vector3 pointB, bool invisible)
            {
                this.pointA = pointA;
                this.pointB = pointB;
                this.time = Time.time;
                this.visible = invisible;
            }
        }

        // PROPERTIES: ----------------------------------------------------------------------------

        private List<Data> points;
        private Mesh mesh;
        private Renderer render;
        public GameObject Trail { get; private set; }

        private bool isActive = false;
        private float deactiveTime;
        private float fadeDuration;

        public Vector3 pointA;
        public Vector3 pointB;

        private Material material;

        private int granularity;
        private float duration;

        // INITIALIZE METHODS: --------------------------------------------------------------------

        public void Initialize(Material material, int granularity = 60, float duration = 0.5f)
        {
            this.material = material;
            this.granularity = granularity;
            this.duration = duration;

            points = new List<Data>();

            mesh = new Mesh();
            mesh.MarkDynamic();

            Trail = new GameObject("Trail");
            Trail.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            MeshFilter meshFilter = Trail.AddComponent<MeshFilter>();
            render = Trail.AddComponent<MeshRenderer>();

            meshFilter.mesh = mesh;
            render.material = this.material;
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            render.receiveShadows = false;

            Trail.hideFlags = HideFlags.HideInHierarchy;
        }

        // PUBLIC METHODS: ------------------------------------------------------------------------

        public void Activate()
        {
            if (isActive) return;

            isActive = true;
            foreach (Data point in points)
            {
                point.visible = false;
            }
        }

        public void Deactivate()
        {
            Deactivate(0f);
        }

        public void Deactivate(float fade)
        {
            if (!isActive) return;

            isActive = false;
            fadeDuration = fade;
            deactiveTime = Time.time;
        }

        // UPDATE METHODS: ------------------------------------------------------------------------

        public void Tick(Vector3 pointA, Vector3 pointB)
        {
            this.pointA = pointA;
            this.pointB = pointB;

            GatherData();
            TrimData();

            UpdatePoints();
            UpdateMesh();
        }

        // GATHER METHODS: ------------------------------------------------------------------------

        private void GatherData()
        {
            bool visible = isActive || deactiveTime + fadeDuration >= Time.time;

            Data data = new Data(
                pointA,
                pointB,
                visible
            );

            if (points.Count < 2) points.Insert(0, data);
            else
            {
                points[0] = data;
                points.Insert(0, new Data(
                    data.pointA + (data.pointA - points[1].pointA),
                    data.pointB + (data.pointB - points[1].pointB),
                    visible
                ));
            }
        }

        // TRIM DATA: -----------------------------------------------------------------------------

        private void TrimData()
        {
            while (CheckLifetime() && CheckFading())
            {
                points.RemoveAt(points.Count - 1);
            }
        }

        private bool CheckLifetime()
        {
            if (points.Count == 0) return false;
            return points[^1].time + duration < Time.time;
        }

        private bool CheckFading()
        {
            if (isActive) return true;
            if (points.Count == 0) return false;

            return deactiveTime + fadeDuration <= Time.time;
        }

        // UPDATE POINTS: -------------------------------------------------------------------------

        private void UpdatePoints()
        {
            float t = 0f;
            if (!isActive)
            {
                t = fadeDuration > float.Epsilon
                    ? (Time.time - deactiveTime) / fadeDuration
                    : 1f;
            }

            if (render.material.HasProperty("_Color"))
            {
                render.material.color = new Color(
                    material.color.r,
                    material.color.g,
                    material.color.b,
                    1f - Mathf.Clamp01(t)
                );
            }
        }

        // CATMULL-ROM METHODS: -------------------------------------------------------------------

        private void UpdateMesh()
        {
            mesh.Clear();
            if (points.Count == 0) return;

            Vector3 previous = points[0].pointA;
            float magnitude = 0f;

            foreach (Data point in points)
            {
                Vector3 position = (point.pointA + point.pointB) * 0.5f;
                magnitude += Vector3.Distance(previous, position);
                previous = position;
            }

            List<Vector3> vertices = new List<Vector3>();

            if (magnitude <= float.Epsilon)
            {
                mesh.vertices = vertices.ToArray();
                RegenerateMesh();
                return;
            }

            int index = 0;
            int count = points.Count;

            foreach (Data point in points)
            {
                if (!point.visible)
                {
                    index += 1;
                    continue;
                }

                if (index == 0)
                {
                    index += 1;
                    continue;
                }

                if (index == count - 1)
                {
                    index += 1;
                    continue;
                }

                if (index == count - 2)
                {
                    index += 1;
                    continue;
                }

                this.GenerateSpline(index, magnitude, ref vertices);
                index += 1;
            }

            this.mesh.vertices = vertices.ToArray();
            this.RegenerateMesh();
        }

        private void GenerateSpline(int position, float magnitude, ref List<Vector3> vertices)
        {
            Vector3 pA0 = points[position - 1].pointA;
            Vector3 pA1 = points[position + 0].pointA;
            Vector3 pA2 = points[position + 1].pointA;
            Vector3 pA3 = points[position + 2].pointA;

            Vector3 pB0 = points[position - 1].pointB;
            Vector3 pB1 = points[position + 0].pointB;
            Vector3 pB2 = points[position + 1].pointB;
            Vector3 pB3 = points[position + 2].pointB;

            Vector3 positionA = pA1;
            Vector3 positionB = pB1;

            vertices.Add(positionA);
            vertices.Add(positionB);

            float distance = (
                Vector3.Distance(pA0, pA1) +
                Vector3.Distance(pA1, pA2) +
                Vector3.Distance(pA2, pA3)
            );

            if (distance <= float.Epsilon) return;

            float resolution = (distance / magnitude) / (1f / granularity);
            int repetitions = Mathf.FloorToInt(resolution);

            for (int i = 1; i <= repetitions; ++i)
            {
                float t = i / resolution;

                positionA = CatmullRomPosition(t, pA0, pA1, pA2, pA3);
                positionB = CatmullRomPosition(t, pB0, pB1, pB2, pB3);

                vertices.Add(positionA);
                vertices.Add(positionB);
            }
        }

        private Vector3 CatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            Vector3 a = 2f * p1;
            Vector3 b = p2 - p0;
            Vector3 c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
            Vector3 d = -p0 + 3f * p1 - 3f * p2 + p3;

            return 0.5f * (a + (b * t) + (c * t * t) + (d * t * t * t));
        }

        private void RegenerateMesh()
        {
            Vector2[] uv = new Vector2[mesh.vertices.Length];
            for (int i = 0; i < uv.Length; i += 2)
            {
                float offset = (float)i / uv.Length;

                uv[i + 0] = new Vector2(offset, 1f);
                uv[i + 1] = new Vector2(offset, 0f);
            }

            int[] triangles = new int[mesh.vertices.Length * 3];
            for (int i = 0; i < mesh.vertices.Length - 2; i += 2)
            {
                triangles[i * 3 + 0] = i + 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 3;

                triangles[i * 3 + 3] = i + 0;
                triangles[i * 3 + 4] = i + 3;
                triangles[i * 3 + 5] = i + 2;
            }

            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
        }

        // GIZMOS: --------------------------------------------------------------------------------

        public void DrawGizmos()
        {
            if (!Application.isPlaying) return;
            if (mesh.vertexCount == 0) return;

            Gizmos.color = Color.blue;
            Gizmos.DrawWireMesh(mesh);

            Gizmos.color = new Color(Color.blue.r, Color.blue.g, Color.blue.b, 0.1f);
            Gizmos.DrawMesh(mesh);
        }
    }
}
