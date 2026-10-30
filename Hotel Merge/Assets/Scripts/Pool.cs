using System.Collections.Generic;
using UnityEngine;

namespace HotelMerge
{
    /// <summary>
    /// Reuses prefab instances instead of creating and destroying them (merge items, order cards, effects, popups,
    /// guests…). <see cref="Get{T}"/> takes an instance from the pool or instantiates the prefab;
    /// <see cref="Release(GameObject)"/> stops its tweens, hides it and keeps it for the next Get.
    /// </summary>
    public static class Pool
    {
        static readonly Dictionary<int, Stack<GameObject>> free = new Dictionary<int, Stack<GameObject>>();
        static readonly Dictionary<int, int> prefabOf = new Dictionary<int, int>();
        static readonly HashSet<int> released = new HashSet<int>();
        static Transform store;

        static Transform Store
        {
            get
            {
                if (!store)
                {
                    var go = new GameObject("[Pool]");
                    go.SetActive(false);
                    Object.DontDestroyOnLoad(go);
                    store = go.transform;
                }
                return store;
            }
        }

        public static T Get<T>(T prefab, Transform parent) where T : Component
        {
            int key = prefab.gameObject.GetInstanceID();
            GameObject go = null;
            if (free.TryGetValue(key, out var stack))
                while (!go && stack.Count > 0) go = stack.Pop();

            if (go)
            {
                released.Remove(go.GetInstanceID());
                go.transform.SetParent(parent, false);
                go.transform.SetAsLastSibling();
                go.SetActive(true);
            }
            else
            {
                go = Object.Instantiate(prefab.gameObject, parent, false);
                go.name = prefab.name;
                prefabOf[go.GetInstanceID()] = key;
            }
            ResetTransform(go.transform, prefab.transform);
            return go.GetComponent<T>();
        }

        public static void Release(Component c)
        {
            if (c) Release(c.gameObject);
        }

        public static void Release(GameObject go)
        {
            if (!go) return;
            int id = go.GetInstanceID();
            if (!prefabOf.TryGetValue(id, out int key))
            {
                Object.Destroy(go); // not pooled
                return;
            }
            if (!released.Add(id)) return;
            Tween.KillAll(go);
            foreach (var p in go.GetComponentsInChildren<IPooled>(true)) p.OnRelease();
            go.SetActive(false);
            go.transform.SetParent(Store, false);
            if (!free.TryGetValue(key, out var stack)) free[key] = stack = new Stack<GameObject>();
            stack.Push(go);
        }

        /// <summary>Releases every pooled child of a container (other children are destroyed).</summary>
        public static void ReleaseChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--) Release(parent.GetChild(i).gameObject);
        }

        /// <summary>Forgets every pooled object (before the scene is reloaded).</summary>
        public static void Clear()
        {
            if (store) Object.Destroy(store.gameObject);
            store = null;
            free.Clear();
            prefabOf.Clear();
            released.Clear();
        }

        /// <summary>Creates instances ahead of time so the first use does not hitch.</summary>
        public static void Prewarm<T>(T prefab, int count) where T : Component
        {
            if (!prefab) return;
            var list = new List<T>();
            for (int i = 0; i < count; i++) list.Add(Get(prefab, Store));
            foreach (var t in list) Release(t);
        }

        static void ResetTransform(Transform t, Transform prefab)
        {
            t.localScale = prefab.localScale;
            t.localRotation = prefab.localRotation;
            if (t is RectTransform rt && prefab is RectTransform src)
            {
                rt.anchorMin = src.anchorMin;
                rt.anchorMax = src.anchorMax;
                rt.pivot = src.pivot;
                rt.sizeDelta = src.sizeDelta;
                rt.anchoredPosition = src.anchoredPosition;
            }
            else t.localPosition = prefab.localPosition;
        }
    }

    /// <summary>Components that need to clean up when their object goes back to the pool.</summary>
    public interface IPooled
    {
        void OnRelease();
    }
}
