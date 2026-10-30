using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>
    /// Translates a fixed label of the scene or a prefab (its Turkish text is the key in Strings.txt).
    /// Labels whose text the game changes at runtime are left alone.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class LocText : MonoBehaviour
    {
        [Tooltip("Turkish source text (key in Strings.txt). Empty = use the label's text.")]
        public string Key;

        void Awake()
        {
            var t = GetComponent<Text>();
            if (string.IsNullOrEmpty(Key)) Key = t.text;
            if (t.text == Key) t.text = Loc.T(Key);
        }
    }
}
