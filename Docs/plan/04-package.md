## 4. Структура пакета
```
Packages/com.vatyakov/
  package.json    "unity": "6000.3"
  Runtime/  VATyakov.asmdef — VatAsset.cs (formatVersion, раскладка, клипы), VatClip.cs, VatPlayback.cs, VatAnimator.cs, VatShaderIds.cs
  Shaders/  VatCore.hlsl (адресация), VatShaderGraph.hlsl (обёртки _float, include guard)
            SubGraphs/ vat_vertex (1.1), vat_vertex_blend (1.8.2), vat_bone (1.11, он же для rigid)
  Editor/   VATyakov.Editor.asmdef — VatBakeProfile(+Editor).cs, VatAssetEditor.cs, VatBuildValidator.cs
            Baking/  IVatFrameSource, VatSkinnedFrameSource, VatLayout, VatAssetWriter,
                     VatVertexEncoder (1.1), BoneEncoder (1.11, для кусков — 1.15)
            Baking/Sources/Alembic/ — VatAlembicFrameSource (1.4), RigidPieceExtractor (1.15: xform-ноды, 1.16: острова + Kabsch), под #if VAT_ALEMBIC
  Tests/Editor/
  Samples~/ vat_lit_bone, vat_lit_vertex, vat_lit_vertex_blend (1.8.2), vat_lit_vertex_triplanar (1.4), vat_lit_rigid; сцены `compare` и `stress`
```
- **Шейдер шаблона по умолчанию** — `VATyakov/vat_lit_vertex` (с 1.3). Пока примеры лежат в `Samples/` без тильды: шейдер по умолчанию должен импортироваться вместе с пакетом. При переезде в `Samples~` шейдер по умолчанию остаётся в пакете.
- **Конвейер бейка:** `VatBakeProfile` → `IVatFrameSource` → `VatLayout` → энкодер → `VatAssetWriter`. Результат — `VatAsset` с sub-assets (Mesh, Texture2D) и таблицей клипов; опционально материал-шаблон и префаб (MeshFilter + MeshRenderer + VatAnimator). `VatBakeProfile` лежит в Editor-сборке и в билд не попадает.
- **Зависимость от Alembic.** Отдельной сборки нет: `VATyakov.Editor` ссылается на `Unity.Formats.Alembic.Runtime` по имени, `versionDefines`: `com.unity.formats.alembic` ≥ 2.4.5 → `VAT_ALEMBIC`. Без пакета ссылка пропускается, файлы с типами Alembic (`VatAlembicFrameSource`, `VatAlembicCopy`) стоят под `#if VAT_ALEMBIC`, `VatAlembic.IsInstalled` — false, остальной пакет работает. Поле профиля — `GameObject` (.abc).
  Так же устроены тестовая сборка (Alembic-тесты под `#if VAT_ALEMBIC`) и VatDev.
