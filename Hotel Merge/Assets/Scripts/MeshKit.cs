using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HotelMerge
{
    /// <summary>Procedural soft-edged geometry and cached URP materials for the toy-like 3D look.</summary>
    public static class MeshKit
    {
        static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        static readonly Dictionary<PrimitiveType, Mesh> prims = new Dictionary<PrimitiveType, Mesh>();

        /// <summary>Forgets cached meshes/materials (the scene builder saves them as assets and starts fresh).</summary>
        public static void ClearCache()
        {
            meshes.Clear();
            mats.Clear();
            prims.Clear();
        }

        /// <summary>
        /// Box with rounded edges. seg = steps around each edge; by default it follows the radius
        /// (tiny radii need 1–2 steps, big soft edges 3), which keeps the vertex count low.
        /// </summary>
        public static Mesh RoundedBox(Vector3 size, float r, int seg = 0)
        {
            r = Mathf.Clamp(r, 0.001f, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.5f - 0.0005f);
            if (seg <= 0) seg = r < 0.035f ? 1 : r < 0.09f ? 2 : 3;
            string key = $"{size.x:F3}_{size.y:F3}_{size.z:F3}_{r:F3}_{seg}";
            if (meshes.TryGetValue(key, out var cached) && cached) return cached;

            var half = size * 0.5f;
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            Vector3[] faces = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var n in faces)
            {
                var v = Mathf.Abs(n.y) > 0.5f ? Vector3.forward : Vector3.up;
                var u = Vector3.Cross(v, -n); // "right" when looking at the face from outside
                float hu = Mathf.Abs(Vector3.Dot(u, half)), hv = Mathf.Abs(Vector3.Dot(v, half)), hn = Mathf.Abs(Vector3.Dot(n, half));
                var pu = AxisPoints(hu, r, seg);
                var pv = AxisPoints(hv, r, seg);
                int start = verts.Count;
                for (int j = 0; j < pv.Length; j++)
                for (int i = 0; i < pu.Length; i++)
                {
                    var p = n * hn + u * pu[i] + v * pv[j];
                    var inner = new Vector3(
                        Mathf.Clamp(p.x, -half.x + r, half.x - r),
                        Mathf.Clamp(p.y, -half.y + r, half.y - r),
                        Mathf.Clamp(p.z, -half.z + r, half.z - r));
                    var d = p - inner;
                    var nn = d.sqrMagnitude > 1e-10f ? d.normalized : n;
                    verts.Add(inner + nn * r);
                    norms.Add(nn);
                    uvs.Add(new Vector2(pu[i] / (2 * hu) + 0.5f, pv[j] / (2 * hv) + 0.5f));
                }
                for (int j = 0; j < pv.Length - 1; j++)
                for (int i = 0; i < pu.Length - 1; i++)
                {
                    int a = start + j * pu.Length + i, b = a + pu.Length;
                    tris.Add(a); tris.Add(b); tris.Add(b + 1);
                    tris.Add(a); tris.Add(b + 1); tris.Add(a + 1);
                }
            }
            var m = new Mesh { name = "RoundedBox " + key };
            m.SetVertices(verts);
            m.SetNormals(norms);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            meshes[key] = m;
            return m;
        }

        static float[] AxisPoints(float half, float r, int seg)
        {
            // -half .. -half+r (denser near the edge), mirrored to +half-r .. +half.
            var p = new float[(seg + 1) * 2];
            for (int i = 0; i <= seg; i++)
            {
                p[i] = -half + r * (1f - Mathf.Cos(i / (float)seg * Mathf.PI * 0.5f));
                p[p.Length - 1 - i] = -p[i];
            }
            return p;
        }

        /// <summary>
        /// Smooth cylinder (40 sides, slightly rounded rims) with the same size as Unity's primitive:
        /// diameter 1, height 2, centred on the origin.
        /// </summary>
        public static Mesh SmoothCylinder()
        {
            const string key = "SmoothCylinder";
            if (meshes.TryGetValue(key, out var cached) && cached) return cached;
            const int sides = 40;
            const float r = 0.5f, bevel = 0.04f;
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            // Side rings: bottom rim, bottom edge, top edge, top rim (the rims bend the normals for a soft edge).
            float[] ys = { -1f, -1f + bevel, 1f - bevel, 1f };
            float[] rs = { r - bevel, r, r, r - bevel };
            float[] ny = { -0.7f, 0f, 0f, 0.7f };
            for (int ring = 0; ring < ys.Length; ring++)
            for (int i = 0; i <= sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                verts.Add(dir * rs[ring] + Vector3.up * ys[ring]);
                norms.Add((dir + Vector3.up * ny[ring]).normalized);
                uvs.Add(new Vector2(i / (float)sides, (ys[ring] + 1f) * 0.5f));
            }
            for (int ring = 0; ring < ys.Length - 1; ring++)
            for (int i = 0; i < sides; i++)
            {
                int a = ring * (sides + 1) + i, b = a + sides + 1;
                tris.Add(a); tris.Add(b); tris.Add(a + 1);
                tris.Add(a + 1); tris.Add(b); tris.Add(b + 1);
            }
            // Caps
            for (int cap = 0; cap < 2; cap++)
            {
                float y = cap == 0 ? -1f : 1f;
                var n = cap == 0 ? Vector3.down : Vector3.up;
                int center = verts.Count;
                verts.Add(Vector3.up * y);
                norms.Add(n);
                uvs.Add(new Vector2(0.5f, 0.5f));
                for (int i = 0; i <= sides; i++)
                {
                    float a = i / (float)sides * Mathf.PI * 2f;
                    verts.Add(new Vector3(Mathf.Cos(a) * (r - bevel), y, Mathf.Sin(a) * (r - bevel)));
                    norms.Add(n);
                    uvs.Add(new Vector2(Mathf.Cos(a) * 0.5f + 0.5f, Mathf.Sin(a) * 0.5f + 0.5f));
                }
                for (int i = 0; i < sides; i++)
                {
                    if (cap == 0) { tris.Add(center); tris.Add(center + 1 + i); tris.Add(center + 2 + i); }
                    else { tris.Add(center); tris.Add(center + 2 + i); tris.Add(center + 1 + i); }
                }
            }
            var m = new Mesh { name = key };
            m.SetVertices(verts);
            m.SetNormals(norms);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            meshes[key] = m;
            return m;
        }

        public static Mesh Primitive(PrimitiveType type)
        {
            if (prims.TryGetValue(type, out var m) && m) return m;
            var go = GameObject.CreatePrimitive(type);
            m = go.GetComponent<MeshFilter>().sharedMesh;
            if (Application.isPlaying) Object.Destroy(go);
            else Object.DestroyImmediate(go);
            prims[type] = m;
            return m;
        }

        static Material Base(string name, string shader)
        {
            var reference = Resources.Load<Material>("Materials/" + name);
            if (reference) return new Material(reference);
            return new Material(Shader.Find(shader));
        }

        public static Material Mat(Color c, float smooth = 0.25f, float emission = 0f)
        {
            string key = $"{ColorUtility.ToHtmlStringRGBA(c)}_{smooth:F2}_{emission:F2}";
            if (mats.TryGetValue(key, out var m) && m) return m;
            m = Base("LitRef", "Universal Render Pipeline/Lit");
            m.name = $"Lit {ColorUtility.ToHtmlStringRGB(c)}" + (emission > 0f ? " Glow" : "");
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", 0f);
            if (emission > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * emission);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            mats[key] = m;
            return m;
        }

        public static Material Unlit(Texture tex, bool transparent, Color? tint = null)
        {
            var m = Base("UnlitRef", "Universal Render Pipeline/Unlit");
            m.name = "Unlit " + (tex ? tex.name : "Color");
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", tint ?? Color.white);
            if (transparent)
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)RenderQueue.Transparent;
            }
            return m;
        }

        public static GameObject Part(string name, Transform parent, Mesh mesh, Material mat, Vector3 pos, Vector3 scale,
            Vector3 euler = default, bool shadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.transform.localEulerAngles = euler;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }

        public static GameObject Box(Transform parent, Vector3 pos, Vector3 size, Color c, float r = 0.06f,
            float smooth = 0.2f, Vector3 euler = default, string name = "Box", float emission = 0f)
            => Part(name, parent, RoundedBox(size, r), Mat(c, smooth, emission), pos, Vector3.one, euler);

        public static GameObject Sphere(Transform parent, Vector3 pos, Vector3 scale, Color c, float smooth = 0.3f,
            float emission = 0f, string name = "Sphere")
            => Part(name, parent, Primitive(PrimitiveType.Sphere), Mat(c, smooth, emission), pos, scale);

        /// <summary>Cylinder of the given diameter and height, standing on pos.</summary>
        public static GameObject Cylinder(Transform parent, Vector3 pos, float diameter, float height, Color c,
            float smooth = 0.25f, string name = "Cylinder", float emission = 0f)
            => Part(name, parent, SmoothCylinder(), Mat(c, smooth, emission),
                pos + Vector3.up * height * 0.5f, new Vector3(diameter, height * 0.5f, diameter));

        public static GameObject Capsule(Transform parent, Vector3 pos, Vector3 scale, Color c, float smooth = 0.3f, string name = "Capsule")
            => Part(name, parent, Primitive(PrimitiveType.Capsule), Mat(c, smooth), pos, scale);
    }
}
