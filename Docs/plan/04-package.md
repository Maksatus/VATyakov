## 4. Структура пакета
```
Packages/com.vatyakov/
  package.json    "unity": "6000.3"
  Runtime/  VATyakov.asmdef — VatAsset.cs (formatVersion, раскладка, клипы), VatClip.cs, VatPlayback.cs, VatAnimator.cs, VatShaderIds.cs
  Shaders/  VatCore.hlsl (адресация), VatShaderGraph.hlsl (обёртки _float, include guard)
            SubGraphs/ VAT_Vertex (1.1), VAT_Bone (1.11, он же для rigid)
  Editor/   VATyakov.Editor.asmdef — VatBakeProfile(+Editor).cs, VatAssetEditor.cs, VatBuildValidator.cs
            Baking/  IVatFrameSource, SkinnedFrameSource, VatLayout, VatAssetWriter,
                     VertexEncoder (1.1), BoneEncoder (1.11, для кусков — 1.15)
            Alembic/ VATyakov.Editor.Alembic.asmdef — AlembicFrameSource (1.4), RigidPieceExtractor (1.15: xform-ноды, 1.16: острова + Kabsch)
  Tests/Editor/
  Samples~/ VAT_Lit_Bone, VAT_Lit_Vertex, VAT_Lit_Vertex_Triplanar (1.4), VAT_Lit_Rigid; сцены Compare и Stress
```
- **Шейдер шаблона по умолчанию** — `VATyakov/VAT_Lit_Vertex` (с 1.3). Пока примеры лежат в `Samples/` без тильды: шейдер по умолчанию должен импортироваться вместе с пакетом. При переезде в `Samples~` шейдер по умолчанию остаётся в пакете.
- **Конвейер бейка:** `VatBakeProfile` → `IVatFrameSource` → `VatLayout` → энкодер → `VatAssetWriter`. Результат — `VatAsset` с sub-assets (Mesh, Texture2D) и таблицей клипов; опционально материал-шаблон и префаб (MeshFilter + MeshRenderer + VatAnimator). `VatBakeProfile` лежит в Editor-сборке и в билд не попадает.
- **Зависимость от Alembic.** У `VATyakov.Editor.Alembic` стоят `defineConstraints: ["VAT_ALEMBIC"]` и `versionDefines`: `com.unity.formats.alembic` ≥ 2.4.5 → `VAT_ALEMBIC`. Без Alembic эта сборка не компилируется, остальной пакет работает.
  `VATyakov.Editor` типов Alembic не видит: поле профиля — `GameObject` (.abc), источник открывается через `IVatAlembicSupport`, реализацию находит `VatAlembic` через `TypeCache`. Тестовая сборка ссылается на Alembic по имени, задаёт тот же `VAT_ALEMBIC` через `versionDefines`, Alembic-тесты под `#if VAT_ALEMBIC`.
