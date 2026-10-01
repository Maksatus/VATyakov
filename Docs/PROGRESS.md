# Прогресс VATyakov

Память между чатами. Исполнитель читает этот файл вместо обхода кода и обновляет его в конце каждой подверсии:
статус, карта кода (если появились или переехали файлы), заметки.

## Статус

| Подверсия | Статус | Версия пакета |
|---|---|---|
| 1.1 Позиции, один клип, ближайший кадр | готово | 0.1.0 |
| 1.2 Интерполяция и правила времени | готово | 0.2.0 |
| 1.3 Нормали и тангенты | следующая | — |
| 1.4–1.19 | не начаты | — |

Проверки на устройствах (iPhone 12, Adreno, Mali): результаты не записаны — дописать сюда при следующем прогоне.

## Как устроено сейчас

- **Бейк:** `VatBakeProfile` (меню Create → VATyakov → VAT Bake Profile) → `VatBaker.Bake` → `VatBakePipeline.Run` →
  `SkinnedFrameSource` → `VertexEncoder` → `VatAssetWriter`. Проверки до бейка — `VatBakeValidator`.
- **Рантайм:** время, loop/one-shot и скорость считает CPU (`VatPlayback`, `VatClip.Frame`, `VatTiming`) и пишет
  `_VatFrame = (row0, row1, frac, 0)` в материал. Драйвер пока — `Assets/VatDev/Scripts/VatCompare`.
- **Шейдер:** `VatCore.hlsl` — только адрес тексела, две выборки и lerp; `VatMath.cs` — его CPU-зеркало.
- **Тестовый контент:** `Assets/VatDev/Content/Bow`, результаты бейка — `Assets/VatDev/Bakes`, сцена `Assets/VatDev/Scenes/Compare.unity`.

## Карта кода (`Packages/com.vatyakov/`)

- `Runtime/` — AssemblyInfo, VATyakov.asmdef, VatAsset, VatClip, VatLayoutInfo, VatMath, VatPlayback, VatShaderIds, VatTiming
- `Shaders/` — VatCore.hlsl, VatShaderGraph.hlsl; `SubGraphs/` — VAT_Vertex.shadersubgraph
- `Samples/UnlitVertex/` — VAT_Unlit_Vertex.shadergraph
- `Editor/Baking/` — VatAssetPath, VatAssetWriter, VatBakeEstimate, VatBakeException, VatBakeLog, VatBakePipeline, VatBakeProgress, VatBakeResult, VatBakeValidator, VatBaker, VatMemory, VatSourceHash, VatTemplateMaterial, VatTestPrefab
- `Editor/Baking/Layout/` — VatClipRequest, VatLayout, VatLayoutVerifier, VatVertexFormat
- `Editor/Baking/Sources/` — IVatFrameSource, VatFrame, VatSourceClip, VatSourceMesh, VatSourceSubMesh
- `Editor/Baking/Sources/Skinned/` — SkinnedFrameSource, VatBakeCopy, VatClipPlayer, VatFrameReader, VatRootSpace
- `Editor/Baking/Vertex/` — VatBoundsBuilder, VatHalf3, VatIndexBuffer, VatPositionTexels, VatQuantizationStats, VatRestPose, VatSubMeshes, VatVertexMeshBuilder, VatVertexStream1, VertexEncoder
- `Editor/Profile/` — VatBakeDialog, VatBakeProfile, VatBakeProfileEditor, VatProfileContext, VatProfileModel; `Containers/` и `Controllers/` — части инспектора профиля
- `Editor/Asset/` — VatAssetEditor, VatProfileLookup; `Containers/` и `Controllers/` — части инспектора VatAsset
- `Editor/Material/` — VatShaderGUI, IVatMaterialSection, VatClipLookup, VatFoldoutHeader, VatFrameField, VatMaterialBinding, VatObjectLinkField; `Sections/` — VatAdvancedSection, VatAnimationSection, VatSurfaceSection
- `Editor/Framework/` — ControllerExtensions, ControllerInspector, EditorContainer, IController, Property, Trigger, VisualElementExtensions
- `Editor/Ui/` — VatAssetSummaryContainer, VatClipRow, VatEditor.uss, VatObjectLink, VatStat, VatText, VatUi
- `Tests/Editor/` — VatBakeTests, VatClipFrameTests, VatInMemoryBake, VatMathTests, VatPlaybackTests, VatShaderGraphTests, VatTestRig, VatTestUtil, VatTimingTests; `Fixtures/` — VAT_HalfParent.shadergraph

## Заметки по подверсиям

- **1.2:** время ушло из шейдера на CPU — решение зафиксировано в плане (§0, §1.3, §1.4). Time-нода SG не используется,
  поэтому motion vectors в 1.9 делаются через `_VatFramePrev` (§3).
