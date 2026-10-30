using UnityEngine;

namespace HotelMerge
{
    /// <summary>
    /// Holder of one upgrade's models inside a room or the lobby (e.g. "Room 3/Furniture/bed").
    /// Remembers which level is shown so only changed upgrades are rebuilt.
    /// </summary>
    public class UpgradeSlot : MonoBehaviour
    {
        public string Id;
        public int Level = -1;
        [Tooltip("Fingerprint of the level's models/positions: a catalog edit rebuilds the slot too.")]
        public int Signature;

        /// <summary>Shows the def's models for a level under parent (creates the slot the first time).</summary>
        public static void Apply(Transform parent, UpgradeDef def, int level, bool animate)
        {
            var t = parent.Find(def.Id);
            var slot = t ? t.GetComponent<UpgradeSlot>() : null;
            if (!slot)
            {
                slot = new GameObject(def.Id).AddComponent<UpgradeSlot>();
                slot.transform.SetParent(parent, false);
                slot.Id = def.Id;
            }
            var lv = def.Level(level);
            int signature = Sign(def, lv);
            if (slot.Level == level && slot.Signature == signature) return;
            slot.Level = level;
            slot.Signature = signature;
            for (int i = slot.transform.childCount - 1; i >= 0; i--)
            {
                var c = slot.transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(c);
                else DestroyImmediate(c);
            }
            if (lv == null) return;
            int n = 0;
            foreach (var anchor in def.Anchors)
            foreach (var piece in lv.Pieces)
            {
                if (!piece.Prefab) continue;
                // Pieces at a left-hand anchor are mirrored, so lobby pairs stay symmetric.
                var offset = anchor.x < 0 && def.Anchors.Length > 1 ? new Vector3(-piece.Offset.x, piece.Offset.y, piece.Offset.z) : piece.Offset;
                var go = Spawn(piece.Prefab, slot.transform);
                go.transform.localPosition = anchor + offset;
                go.transform.localRotation = Quaternion.Euler(0, piece.Yaw, 0);
                go.transform.localScale = Vector3.one * piece.Scale;
                if (animate)
                {
                    var target = go.transform.localScale;
                    go.transform.localScale = Vector3.zero;
                    Tween.Scale(go.transform, target, 0.5f, Ease.OutBack, 0.05f * n++);
                }
            }
        }

        static int Sign(UpgradeDef def, UpgradeLevel lv)
        {
            unchecked
            {
                int h = 17;
                foreach (var a in def.Anchors) h = h * 31 + a.GetHashCode();
                if (lv != null)
                    foreach (var p in lv.Pieces)
                        h = h * 31 + (p.Prefab ? p.Prefab.name.GetHashCode() : 0) + p.Offset.GetHashCode() * 7 + p.Yaw.GetHashCode() * 13 + p.Scale.GetHashCode() * 17;
                return h;
            }
        }

        static GameObject Spawn(GameObject prefab, Transform parent)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
#endif
            return Instantiate(prefab, parent);
        }
    }
}
