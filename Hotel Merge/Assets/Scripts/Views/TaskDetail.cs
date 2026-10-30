using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>Bottom panel of the task board for the selected note, with a button to play it.</summary>
    public class TaskDetail : MonoBehaviour, IPooled
    {
        public Image AreaIcon;
        public Text Title, Description;
        OrderData order;

        public void Bind(OrderData o)
        {
            order = o;
            AreaIcon.sprite = Art.Get(Areas.GeneratorSprite[o.area]);
            Title.text = Areas.TaskName[o.area];
            Description.text = Loc.F("Oda {0}: {1}", o.room + 1, string.Join(", ", o.items.ConvertAll(Items.Name)));
            var rt = (RectTransform)transform;
            rt.anchoredPosition = new Vector2(0, -360);
            Tween.Anchored(rt, new Vector2(0, 40), 0.35f, Ease.OutBack);
        }

        // Button target
        public void Play()
        {
            if (order != null) GameRoot.I.ShowMerge(order);
        }

        public void OnRelease() => order = null;

        /// <summary>Editor: creates the prefab's objects.</summary>
        public static GameObject Build()
        {
            var img = UIKit.Img(null, "UI/panel", new Vector2(980, 330), null, "TaskDetail");
            img.raycastTarget = true;
            var detail = img.Rt();
            detail.Place(new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(980, 330), new Vector2(0.5f, 0f));
            var v = img.gameObject.AddComponent<TaskDetail>();
            v.AreaIcon = UIKit.Icon(detail, "Items/generator", 190, new Vector2(140, 10), new Vector2(0, 0.5f));
            v.AreaIcon.name = "AreaIcon";
            v.Title = UIKit.Label(detail, "Görev", 56, Pal.Ink, TextAnchor.MiddleLeft, 0f, "Title");
            v.Title.Rt().Place(new Vector2(0, 1), new Vector2(260, -80), new Vector2(600, 64), new Vector2(0, 0.5f));
            v.Description = UIKit.Label(detail, "Oda 1: ...", 34, Pal.Hex("8a6a7a"), TextAnchor.MiddleLeft, 0f, "Description").Wrapped(420);
            v.Description.Rt().Place(new Vector2(0, 1), new Vector2(260, -170), new Vector2(420, 110), new Vector2(0, 0.5f));
            var play = UIKit.TextButton(detail, "UI/btn_green", new Vector2(260, 150), "OYNA", 60, null);
            UIKit.Bind(play, v.Play);
            play.Rt().Place(new Vector2(1, 0.5f), new Vector2(-160, 0), new Vector2(260, 150));
            play.gameObject.AddComponent<Pulse>().amount = 0.04f;
            Shine.Add(play.Rt(), 2f);
            return img.gameObject;
        }
    }
}
