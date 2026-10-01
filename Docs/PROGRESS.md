# Прогресс VATyakov

Память между чатами. Исполнитель читает этот файл вместо обхода кода и обновляет его в конце каждой подверсии:
статус, карта кода (если появились или переехали файлы), заметки.

## Статус

| Подверсия | Статус | Версия пакета |
|---|---|---|
| 1.1 Позиции, один клип, ближайший кадр | готово | 0.1.0 |
| 1.2 Интерполяция и правила времени | готово | 0.2.0 |
| 1.3 Нормали и тангенты | готово в редакторе, устройства не проверены | 0.3.0 |
| 1.4 Alembic | готово | 0.4.0 |
| 1.5 Дрейф | следующая | — |
| 1.6–1.19 | не начаты | — |

Проверки на устройствах (iPhone 12, Adreno, Mali): результаты не записаны — дописать сюда при следующем прогоне.
1.3 на устройствах: сцена `RotDecode` (кнопка RGBA8 в Compare) — «RGBA8: OK» или число неверных текселей и API.

## Как устроено сейчас

- **Бейк:** `VatBakeProfile` (меню Create → VATyakov → VAT Bake Profile) → `VatBaker.Bake` → `VatBakePipeline.Run` →
  `VatFrameSources.Open` (`SkinnedFrameSource` или `AlembicFrameSource` по `VatSourceKind`) → `VertexEncoder` →
  `VatAssetWriter`. Проверки до бейка — `VatBakeValidator`. Предупреждения источника (`IVatFrameSource.Warnings`) — в лог бейка.
- **Alembic:** `VATyakov.Editor` типов Alembic не видит; `VatAlembic` находит через `TypeCache` реализацию
  `IVatAlembicSupport` из сборки `VATyakov.Editor.Alembic` (только с `VAT_ALEMBIC`). `AlembicFrameSource`: скрытая
  копия .abc с корнем в identity (`VatAlembicCopy`), все меш-ноды — один меш (`VatAlembicReader`, `VatSourceMeshes.Combine`),
  на каждом кадре `VatAlembicTopology` (ошибка «патч 2») и `VatVertexJumps` (предупреждение). Инспектор профиля
  берёт число вертексов, длину и разрыв петли из `VatAlembicProbe` (кэш до переимпорта .abc), подсказка loop —
  `VatLoopHintController`.
  `VertexEncoder` за один проход пишет `_VatPosTex` (`VatPositionTexels`) и `_VatRotTex` (`VatRotationTexels`,
  кодек `VatSmallestThree`); N и T кадра — `VatTangentFrames`, знак бинормали — `VatChirality`.
- **Рантайм:** время, loop/one-shot и скорость считает CPU (`VatPlayback`, `VatClip.Frame`, `VatTiming`) и пишет
  `_VatFrame = (row0, row1, frac, 0)` в материал. Драйвер пока — `Assets/VatDev/Scripts/VatCompare`.
- **Шейдер:** `VatCore.hlsl` — адрес тексела, декод smallest-three, nlerp и оси кадра; `VatMath.cs` — его CPU-зеркало.
  `VatShaderGraph.hlsl`: `VatVertexPosition_float` и `VatVertexNormalTangent_float`; SubGraph `VAT_Vertex` отдаёт
  Position, Normal, Tangent. Шаблон по умолчанию — `VAT_Lit_Vertex` (BaseMap, NormalMap `_BumpMap`, Metallic, Smoothness).
- **Тестовый контент:** `Assets/VatDev/Content/Bow`, результаты бейка — `Assets/VatDev/Bakes`, сцена `Assets/VatDev/Scenes/Compare.unity`.
  Alembic: `Assets/VatDev/Content/Alembic/Water.abc` (5853 вертекса, 80 кадров по 24 fps, с UV, не петля — конец
  отличается от начала на 218 мм), профиль `Bakes/Water.asset`, сцена `Scenes/CompareAlembic.unity` (не в сборке: .abc
  в мобильный билд не идёт) — сплит-скрин, у каждой половины своя камера с одинаковым ракурсом. `Columns.fbx` — на 1.15.
  У лука нет своей normal map: `Bow_TestNormal.png` — процедурный рельеф (sin·sin, 24 периода), на SMR — `Bow_Source_Lit.mat`
  (URP Lit), на VAT — `VAT_Lit_Vertex` с той же картой и smoothness 0.6. Сцена `RotDecode` — проверка RGBA8 на устройстве
  (`VatRotDecodeCheck`, шейдер `Assets/VatDev/Shaders/VatRotDecodeTest.shader`), вторая сцена сборки.

## Карта кода (`Packages/com.vatyakov/`)

- `Runtime/` — AssemblyInfo, VATyakov.asmdef, VatAsset, VatClip, VatLayoutInfo, VatMath, VatPlayback, VatShaderIds, VatTiming
- `Shaders/` — VatCore.hlsl, VatShaderGraph.hlsl; `SubGraphs/` — VAT_Vertex.shadersubgraph
- `Samples/UnlitVertex/` — VAT_Unlit_Vertex.shadergraph; `Samples/LitVertex/` — VAT_Lit_Vertex.shadergraph (шаблон по умолчанию);
  `Samples/LitVertexTriplanar/` — VAT_Lit_Vertex_Triplanar.shadergraph (меши без UV)
- `Editor/Baking/` — VatAssetPath, VatAssetWriter, VatBakeEstimate, VatBakeException, VatBakeLog, VatBakePipeline, VatBakeProgress, VatBakeResult, VatBakeValidator, VatBaker, VatMemory, VatSourceHash, VatTemplateMaterial, VatTestPrefab
- `Editor/Baking/Layout/` — VatClipRequest, VatLayout, VatLayoutVerifier, VatVertexFormat
- `Editor/Baking/Sources/` — IVatAlembicSupport, IVatFrameSource, VatAlembic, VatAlembicProbe, VatFrame, VatFrameSources, VatLoopGap, VatSourceClip, VatSourceMesh, VatSourceMeshes, VatSourceSubMesh
- `Editor/Alembic/` — VATyakov.Editor.Alembic.asmdef, AlembicFrameSource, VatAlembicCopy, VatAlembicReader, VatAlembicSupport, VatAlembicTopology, VatVertexJumps
- `Editor/Baking/Sources/Skinned/` — SkinnedFrameSource, VatBakeCopy, VatClipPlayer, VatFrameReader, VatRootSpace
- `Editor/Baking/Vertex/` — VatBoundsBuilder, VatChirality, VatHalf3, VatIndexBuffer, VatPositionTexels, VatQuantizationStats, VatRestPose, VatRotationTexels, VatSmallestThree, VatSubMeshes, VatTangentFrames, VatTexture, VatVertexMeshBuilder, VatVertexStream1, VertexEncoder
- `Editor/Profile/` — VatBakeDialog, VatBakeProfile, VatBakeProfileEditor, VatProfileContext, VatProfileModel, VatSourceKind; `Containers/` и `Controllers/` — части инспектора профиля (источник — VatSourceFields*, подсказка loop — VatLoopHint*)
- `Editor/Asset/` — VatAssetEditor, VatProfileLookup; `Containers/` и `Controllers/` — части инспектора VatAsset
- `Editor/Material/` — VatShaderGUI, IVatMaterialSection, VatClipLookup, VatFoldoutHeader, VatFrameField, VatMaterialBinding, VatObjectLinkField; `Sections/` — VatAdvancedSection, VatAnimationSection, VatSurfaceSection
- `Editor/Framework/` — ControllerExtensions, ControllerInspector, EditorContainer, IController, Property, Trigger, VisualElementExtensions
- `Editor/Ui/` — VatAssetSummaryContainer, VatClipRow, VatEditor.uss, VatObjectLink, VatStat, VatText, VatUi
- `Tests/Editor/` — VatAlembicTests (под `#if VAT_ALEMBIC`), VatBakeTests, VatClipFrameTests, VatInMemoryBake, VatMathTests, VatPlaybackTests, VatRotationCodecTests, VatShaderGraphTests, VatTangentFramesTests, VatTestRig, VatTestUtil, VatTimingTests, VatVertexEncoderTests; `Fixtures/` — VAT_HalfParent.shadergraph, VatCloth.abc, VatTopology.abc, VatShuffled.abc
- Вне пакета: `Assets/VatDev/Editor/VatAlembicFixtures` (сборка `VATyakov.Dev.Editor`, меню VATyakov → Dev → Regenerate Alembic Fixtures) — генератор .abc-фикстур; `Assets/VatDev/Scripts/VatCompare` умеет `AlembicStreamPlayer` (под `VAT_ALEMBIC`)

## Заметки по подверсиям

- **1.2:** время ушло из шейдера на CPU — решение зафиксировано в плане (§0, §1.3, §1.4). Time-нода SG не используется,
  поэтому motion vectors в 1.9 делаются через `_VatFramePrev` (§3).
- **1.3:** `formatVersion` 2 (`_VatRotTex`, `VatLayoutInfo.RotationFormat`): ассеты 0.2 нужно перезапечь, лук в VatDev
  перезапечён. Решения, уточнённые в плане (§2.2, §4): тангент кадра — скиннутый из `BakeMesh`, не из UV; вырожденный —
  порог `|T⊥|² ≤ 1e-6`, перенос внутри клипа, базис от N в кадре 0 каждого клипа; шаблон по умолчанию — Lit.
  Шейдерная часть: выбор idx и выравнивание знака q — select (`?:`), не ветвление; `saturate` под корнем — по §1.9.
  `VatTemplateMaterial` копирует в новый шаблон `_BaseMap` и `_BumpMap` исходного материала.
  Лог бейка: max ошибка поворота (на луке ~0.2°) и число вырожденных тангентов (`Bow_default_main` — 24 вертекса);
  предупреждение о хиральности — отдельным `LogWarning`. В инспектор это выйдет в 1.10.
  Сравнение в редакторе: SMR и VAT из одной камеры на кадре 12 — больше 8 уровней яркости расходятся 3–5 пикселей
  на силуэте. RGBA8-проверка в редакторе (D3D11): OK; отрицательный контроль (испорченные байты) краснеет.
  Lit-граф сгенерирован скриптом по образцу Unlit, как и правки SubGraph (вторая Custom Function, выходы Normal, Tangent).
- **1.4:** продуктовые решения (вопросы к пользователю): в профиле переключатель Source (Skinned Mesh Renderer / Alembic), клип
  Alembic — весь Time Range импортёра; подсказка loop — только «конец ≈ начало» (≤ 1 мм), без поиска периода. Решения
  записаны в §2.2 и §4. Несколько меш-нод .abc склеиваются в один меш. Порог прыжка вертекса — половина наибольшей
  стороны баунда прошлого кадра после вычитания сдвига центроида (иначе быстрый soft body из 1.5 давал бы ложные срабатывания).
  Фикстуры .abc записаны `AlembicRecorder` в edit mode: рекордер подменяет меш через `MeshFilter.mesh`, поэтому кадр
  пишется в `filter.sharedMesh`; импортёр хранит Time Range первого импорта в .meta — перед перезаписью ассет удаляется.
  Тест сравнения с Alembic гоняется и с обрезанным StartTime: время от StartTime, не абсолютное (негативный контроль —
  176 мм). Проверено: без `com.unity.formats.alembic` проект компилируется, 158 тестов зелёные (Alembic-тесты выключены);
  с ним — 164. `Water.abc` в редакторе: ошибка half 0.5 мм, сплит-скрин на кадре 40 совпадает с плеером.
  Сравнение с одной камерой на разнесённых объектах обманчиво: разная перспектива прячет разные дыры за гребнем.
  Весь UI, ошибки и лог пакета переведены на английский по просьбе пользователя (правило в CLAUDE.md); тесты проверяют английские строки.
