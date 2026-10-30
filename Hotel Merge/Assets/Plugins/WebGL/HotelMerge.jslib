// Browser helpers for the WebGL build.
mergeInto(LibraryManager.library, {
  // Remembers the game language so the web loader page (Assets/WebGLTemplates/HotelMerge) shows its texts in it.
  HotelMerge_SaveLanguage: function (code) {
    try { localStorage.setItem("hotelmerge_lang", UTF8ToString(code)); } catch (e) {}
  },
});
