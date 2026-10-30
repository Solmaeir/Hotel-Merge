using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>Player level badge: round avatar, "Lv. 5" on a blue pill and the XP bar under it.</summary>
    public class KitLevelBadge : MonoBehaviour
    {
        [SerializeField] Text level;
        [SerializeField] KitBar xp;

        public static KitLevelBadge Create(Transform parent, string portrait, float height)
        {
            var root = UIKit.Node("LevelBadge", parent);
            float avatar = height, w = height * 2.55f;
            root.sizeDelta = new Vector2(w, height);
            var b = root.gameObject.AddComponent<KitLevelBadge>();
            var pill = Kit.Frame(root, "pill_blue", new Vector2(w - avatar * 0.55f, height * 0.78f), height * 0.3f, "Pill").Rt();
            pill.Place(new Vector2(0f, 0.5f), new Vector2(avatar * 0.55f, 0), pill.sizeDelta, new Vector2(0f, 0.5f));
            b.level = Kit.Bold(pill, "Lv. 1", Mathf.RoundToInt(height * 0.26f), Pal.Hex("22468e"), TextAnchor.MiddleLeft, "Level");
            b.level.Rt().Place(new Vector2(0f, 1f), new Vector2(avatar * 0.52f, -height * 0.2f), new Vector2(200, height * 0.32f), new Vector2(0f, 0.5f));
            var track = UIKit.Img(pill, "Kit/pill_dark", Vector2.zero, null, "XpBack").Round(height * 0.14f).Rt();
            track.Place(new Vector2(0f, 0f), new Vector2(avatar * 0.5f, height * 0.08f),
                new Vector2(pill.sizeDelta.x - avatar * 0.5f - height * 0.1f, height * 0.3f), new Vector2(0f, 0f));
            b.xp = Kit.Bar(track, track.sizeDelta - new Vector2(8, 8), "green", "0/100");
            b.xp.Rt().Stretch(4, 4, 4, 4);
            var face = Kit.PortraitFrame(root, portrait, avatar, true);
            face.Place(new Vector2(0f, 0.5f), new Vector2(avatar / 2, 0), new Vector2(avatar, avatar));
            return b;
        }

        int shownLv = -1, shownXp = -1, shownNeed = -1;

        public void Set(int lv, int current, int needed, bool animate = true)
        {
            if (lv == shownLv && current == shownXp && needed == shownNeed) return;
            shownLv = lv;
            shownXp = current;
            shownNeed = needed;
            level.text = "Lv. " + lv;
            xp.Set(needed > 0 ? current / (float)needed : 1f, $"{current}/{needed}", animate);
        }
    }
}
