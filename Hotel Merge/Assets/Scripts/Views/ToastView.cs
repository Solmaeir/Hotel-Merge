using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>The message pill that slides in from the top of the screen.</summary>
    public class ToastView : MonoBehaviour
    {
        public Text Label;

        /// <summary>Editor: creates the prefab's objects.</summary>
        public static GameObject Build()
        {
            var bg = UIKit.Img(null, "UI/pill", new Vector2(900, 110), new Color(1, 1, 1, 0.95f), "Toast");
            var v = bg.gameObject.AddComponent<ToastView>();
            v.Label = UIKit.Label(bg.transform, "Mesaj", 38, Color.white, TextAnchor.MiddleCenter, 2.5f);
            v.Label.Wrapped(840).Rt().Stretch(30, 0, 30, 0);
            bg.Rt().Place(new Vector2(0.5f, 1f), new Vector2(0, 160), new Vector2(900, 110), new Vector2(0.5f, 1f));
            return bg.gameObject;
        }
    }
}
