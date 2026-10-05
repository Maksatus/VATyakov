## 4. Структура пакета
```
Packages/com.vatyakov/
  package.json    "unity": "6000.3"
  Runtime/  VATyakov.asmdef — VatAsset.cs (formatVersion, раскладка, клипы), VatClip.cs, VatPlayback.cs, VatAnimator.cs, VatShaderIds.cs
  Shaders/  VatCore.hlsl (адресация), VatShaderGraph.hlsl (обёртки _float, include guard)
            SubGraphs/ vat_vertex (1.1), vat_vertex_blend (1.8.2), vat_bone и vat_bone_blend (1.11), vat_rigid (1.15)
  Editor/   VATyakov.Editor.asmdef — VatBakeProfile(+Editor).cs, VatAssetEditor.cs
            Baking/  IVatFrameSource, VatSkinnedFrameSource, VatLayout, VatAssetWriter,
                     VatVertexEncoder (1.1); Bone/ — VatBoneRig, VatBoneEncoder, VatBonePipeline (1.11); Rigid/ — VatRigidPipeline, VatRigidEncoder, VatRigidPivot (1.15)
            Baking/Sources/Alembic/ — VatAlembicFrameSource (1.4), VatRigidPieceExtractor (1.15: xform-ноды, 1.16: острова + Kabsch), под #if VAT_ALEMBIC
  Tests/Editor/
  Samples~/ vat_lit_bone и vat_lit_bone_blend (1.11), vat_lit_vertex, vat_lit_vertex_blend (1.8.2), vat_lit_vertex_triplanar (1.4), vat_lit_rigid; сцены `compare` и `stress`
```
- **Шейдер шаблона по умолчанию** — `VATyakov/vat_lit_vertex` (с 1.3). Пока примеры лежат в `Samples/` без тильды: шейдер по умолчанию должен импортироваться вместе с пакетом. При переезде в `Samples~` шейдер по умолчанию остаётся в пакете.
- **Конвейер бейка:** `VatBakeProfile` → `IVatFrameSource` → `VatLayout` → энкодер → `VatAssetWriter`. Результат — `VatAsset` с sub-assets (Mesh, Texture2D) и таблицей клипов; материал-шаблон (создаётся рядом, если в профиле пусто; текстуры исходного материала в него не переносятся). Префаб юнита собирает пользователь (MeshFilter + MeshRenderer + VatAnimator), бейкер его не создаёт (0.12.1). `VatBakeProfile` лежит в Editor-сборке и в билд не попадает.
- **Зависимость от Alembic.** Отдельной сборки нет: `VATyakov.Editor` ссылается на `Unity.Formats.Alembic.Runtime` по имени, `versionDefines`: `com.unity.formats.alembic` ≥ 2.4.5 → `VAT_ALEMBIC`. Без пакета ссылка пропускается, файлы с типами Alembic (`VatAlembicFrameSource`, `VatAlembicCopy`) стоят под `#if VAT_ALEMBIC`, `VatAlembic.IsInstalled` — false, остальной пакет работает. Поле профиля — `GameObject` (.abc).
  Так же устроены тестовая сборка (Alembic-тесты под `#if VAT_ALEMBIC`) и VatDev.
