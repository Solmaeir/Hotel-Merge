using UnityEngine;

namespace HotelMerge
{
    /// <summary>
    /// Cute bean-shaped hotel guest. One prefab holds every hair style; <see cref="Setup"/> picks the style and
    /// tints shirt, skin, hair and suitcase per guest (MaterialPropertyBlock, so all guests share the same materials).
    /// </summary>
    [ExecuteAlways]
    public class Guest : MonoBehaviour, IPooled
    {
        static readonly string[] Shirts = { "ff6f91", "4fa3ea", "ffb21f", "52c46a", "9b6bf0", "ff7d4d", "2fc4c4", "f25f5c" };
        static readonly string[] Skins = { "ffe0c2", "f5c6a0", "d99c6c", "a8704a", "ffd6b8" };
        static readonly string[] Hairs = { "3a2438", "8a4a2a", "f2c94c", "d65a31", "6b4f3a", "1f1f2e" };
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static MaterialPropertyBlock block;

        [SerializeField] Transform body;
        [SerializeField] Vector3 basePos;
        [SerializeField] float phase;
        [SerializeField] int look;
        [SerializeField] Renderer shirt, skin, bag;
        [SerializeField] GameObject[] hairStyles;
        [SerializeField] GameObject luggage;
        float mood = 1f;
        bool leaving;

        /// <summary>Takes a guest from the pool and puts it into a room.</summary>
        public static Guest Spawn(Transform parent, Vector3 localPos, int look)
        {
            var g = Pool.Get(GamePrefabs.I.Guest, parent);
            g.transform.localPosition = localPos;
            g.transform.localRotation = Quaternion.Euler(0, Random.Range(-15f, 15f), 0);
            g.basePos = localPos;
            g.phase = Random.value * 10f;
            g.Setup(look);
            return g;
        }

        public void Setup(int guestLook)
        {
            look = guestLook;
            Apply();
        }

        void OnEnable() => Apply();

        void Apply()
        {
            if (!shirt || hairStyles == null || hairStyles.Length == 0) return;
            var rnd = new System.Random(look);
            var shirtColor = Pal.Hex(Shirts[rnd.Next(Shirts.Length)]);
            var skinColor = Pal.Hex(Skins[rnd.Next(Skins.Length)]);
            var hairColor = Pal.Hex(Hairs[rnd.Next(Hairs.Length)]);
            int style = Mathf.Min(rnd.Next(4), hairStyles.Length - 1);
            var bagColor = Pal.Hex(Shirts[(rnd.Next(Shirts.Length) + 3) % Shirts.Length]);

            Tint(shirt, shirtColor);
            Tint(skin, skinColor);
            Tint(bag, bagColor);
            for (int i = 0; i < hairStyles.Length; i++)
            {
                hairStyles[i].SetActive(i == style);
                foreach (var r in hairStyles[i].GetComponentsInChildren<Renderer>(true)) Tint(r, hairColor);
            }
        }

        static void Tint(Renderer r, Color c)
        {
            if (!r) return;
            block ??= new MaterialPropertyBlock();
            block.SetColor(BaseColor, c);
            r.SetPropertyBlock(block);
        }

        /// <summary>Editor: builds the guest's objects (prefab, or the receptionist in the lobby).</summary>
        public static Guest Create(Transform parent, Vector3 localPos, int look)
        {
            var go = new GameObject("Guest");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var g = go.AddComponent<Guest>();
            g.basePos = localPos;
            g.look = look;

            var white = Color.white;
            g.body = new GameObject("Body").transform;
            g.body.SetParent(go.transform, false);
            var b = g.body;
            g.shirt = MeshKit.Capsule(b, new Vector3(0, 0.45f, 0), new Vector3(0.62f, 0.45f, 0.52f), white, 0.3f, "Shirt").GetComponent<Renderer>();
            g.skin = MeshKit.Sphere(b, new Vector3(0, 1.08f, 0), new Vector3(0.6f, 0.58f, 0.58f), white, 0.3f, 0f, "Head").GetComponent<Renderer>();

            var hat = new GameObject("Hair Hat").transform;
            hat.SetParent(b, false);
            MeshKit.Cylinder(hat, new Vector3(0, 1.28f, 0), 0.72f, 0.05f, white);
            MeshKit.Cylinder(hat, new Vector3(0, 1.3f, 0), 0.46f, 0.32f, white);
            var bun = new GameObject("Hair Bun").transform;
            bun.SetParent(b, false);
            MeshKit.Sphere(bun, new Vector3(0, 1.22f, 0.05f), new Vector3(0.64f, 0.42f, 0.62f), white);
            MeshKit.Sphere(bun, new Vector3(0, 1.45f, 0.12f), Vector3.one * 0.26f, white);
            var cap = new GameObject("Hair Short").transform;
            cap.SetParent(b, false);
            MeshKit.Sphere(cap, new Vector3(0, 1.22f, 0.06f), new Vector3(0.63f, 0.4f, 0.62f), white);
            g.hairStyles = new[] { hat.gameObject, bun.gameObject, cap.gameObject };

            MeshKit.Sphere(b, new Vector3(-0.11f, 1.1f, -0.26f), new Vector3(0.08f, 0.11f, 0.06f), Pal.Ink, 0.8f, 0f, "Eye");
            MeshKit.Sphere(b, new Vector3(0.11f, 1.1f, -0.26f), new Vector3(0.08f, 0.11f, 0.06f), Pal.Ink, 0.8f, 0f, "Eye");
            MeshKit.Sphere(b, new Vector3(-0.19f, 1.0f, -0.22f), new Vector3(0.1f, 0.06f, 0.05f), Pal.Hex("ff8fa6"), 0.3f, 0f, "Cheek");
            MeshKit.Sphere(b, new Vector3(0.19f, 1.0f, -0.22f), new Vector3(0.1f, 0.06f, 0.05f), Pal.Hex("ff8fa6"), 0.3f, 0f, "Cheek");

            g.luggage = new GameObject("Suitcase");
            g.luggage.transform.SetParent(go.transform, false);
            g.bag = MeshKit.Box(g.luggage.transform, new Vector3(0.48f, 0.26f, 0.05f), new Vector3(0.22f, 0.5f, 0.4f), white, 0.06f, 0.2f, default, "Case").GetComponent<Renderer>();
            MeshKit.Box(g.luggage.transform, new Vector3(0.48f, 0.56f, 0.05f), new Vector3(0.06f, 0.1f, 0.2f), Pal.Ink, 0.02f, 0.2f, default, "Handle");
            g.Apply();
            return g;
        }

        /// <summary>Hides the suitcase (staff standing behind the desk).</summary>
        public Guest IdleOnly()
        {
            if (luggage) luggage.SetActive(false);
            return this;
        }

        public void Arrive()
        {
            transform.localScale = Vector3.zero;
            Tween.Scale(transform, Vector3.one, 0.55f, Ease.OutBack);
        }

        public void SetWorried(bool worried) => mood = worried ? 2.2f : 1f;

        public void Leave(bool happy)
        {
            if (leaving) return;
            leaving = true;
            if (happy)
            {
                mood = 0f;
                Tween.Run(this, Tween.ChPos, 0.9f, k =>
                {
                    float jump = Mathf.Sin(Mathf.Min(1f, k * 1.6f) * Mathf.PI) * 0.9f;
                    transform.localPosition = basePos + Vector3.up * jump;
                    transform.localRotation = Quaternion.Euler(0, k * 720f, 0);
                    transform.localScale = Vector3.one * (k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f);
                }, () => Pool.Release(this), Ease.Linear);
            }
            else
            {
                Tween.Scale(transform, Vector3.zero, 0.45f, Ease.InBack, 0f, () => Pool.Release(this));
            }
        }

        public void OnRelease()
        {
            leaving = false;
            mood = 1f;
            if (body)
            {
                body.localPosition = Vector3.zero;
                body.localScale = Vector3.one;
            }
        }

        void Update()
        {
            if (!Application.isPlaying || leaving || body == null) return;
            float t = Time.time * 4f * Mathf.Max(0.5f, mood) + phase;
            float hop = Mathf.Abs(Mathf.Sin(t)) * 0.05f * mood;
            body.localPosition = Vector3.up * hop;
            float squash = 1f + Mathf.Sin(t * 2f) * 0.025f;
            body.localScale = new Vector3(1f / squash, squash, 1f / squash);
        }
    }
}
