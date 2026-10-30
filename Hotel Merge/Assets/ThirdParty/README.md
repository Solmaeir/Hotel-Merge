# Üçüncü parti asset'ler

Kaynak klasör: `~/Masaüstü/hotelMergeFreeAssets`

## 3D modeller (126 adet)
| Paket | FBX | Prefab |
|---|---|---|
| Sevimli mobilyalar (47) | `ThirdParty/CuteFurniture/Models` | `HotelMerge/Prefabs/Furniture/CuteFurniture` |
| Kafe seti (39) | `ThirdParty/CartoonInteriors/Models/Cafe` | `HotelMerge/Prefabs/Furniture/CartoonInteriors/Cafe` |
| Oda eşyaları (40) | `ThirdParty/CartoonInteriors/Models/Room` | `HotelMerge/Prefabs/Furniture/CartoonInteriors/Room` |

- `.blend` dosyaları Blender ile her obje ayrı bir FBX olacak şekilde dışa aktarıldı; ölçü metre cinsinden.
- Her prefabın kökü, modelin tabanının ortasında durur. Sahneye bırakılan obje zemine oturur.
- Renkler tek bir palet dokusundan gelir. Paketin ortak URP materyalleri `ThirdParty/<Paket>/Materials` klasöründedir. Inspector'da bu materyaller değiştirilirse paketteki bütün modeller birlikte değişir.
- Prefabları baştan oluşturmak için **Hotel Merge → Mobilya Prefablarını Oluştur**. Bu işlem materyallere dokunmaz.
- Tüm modelleri yan yana görmek için **Hotel Merge → Asset Vitrini Sahnesi** (`Scenes/AssetShowcase.unity`).

## İkonlar (152 adet, 256 px)
- `Resources/Art/Packs/CartoonIcons/<Kategori>`: Simulator, Farm, Winter, Fishing, Mining, Anime, Space (140 ikon)
- `Resources/Art/Packs/ForestMagic`: 12 merge ikonu (6 çiçek seviyesi, 3 üretici, 3 özel)

Koddan kullanımı:
```csharp
Art.Get("Packs/CartoonIcons/Farm/farm_red_apple")
Art.Get("Packs/ForestMagic/white_bloom_lv03_flower")
```

## Lisanslar
İkon lisansları `ThirdParty/Licenses` klasöründedir. İki ikon paketi de ticari oyunda kullanıma izin verir, ama dosyaların kendisinin paylaşılmasını yasaklar. Bu yüzden repo herkese açık olacaksa bu klasörler commit edilmemeli. Blender paketlerinde lisans dosyası yoktu; indirilen sayfadaki lisansı kontrol et.
