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
| 1.5 Дрейф | готово | 0.5.0 |
| 1.6 Несколько клипов в одном ассете | готово | 0.6.0 |
| 1.7 | следующая | — |
| 1.8–1.19 | не начаты | — |

Проверки на устройствах (iPhone 12, Adreno, Mali): результаты не записаны — дописать сюда при следующем прогоне.
1.3 на устройствах: сцена `RotDecode` (кнопка RGBA8 в Compare) — «RGBA8: OK» или число неверных текселей и API.

## Как устроено сейчас

- **Бейк:** `VatBakeProfile` (меню Create → VATyakov → VAT Bake Profile) → `VatBaker.Bake` → `VatBakePipeline.Run` →
  `VatFrameSources.Open` (`SkinnedFrameSource` или `AlembicFrameSource` по `VatSourceKind`) → `VertexEncoder` →
  `VatAssetWriter`. Проверки до бейка — `VatBakeValidator` (список клипов — `VatClipListProblems`). Предупреждения
  источника (`IVatFrameSource.Warnings`) — в лог бейка.
- **Клипы (1.6):** профиль — `Clips` (массив, общие Loop и fps), Alembic — один клип. `VatLayout.ForVertex` кладёт
  клипы друг под другом в порядке списка и проверяет высоту по всем сразу до сэмплинга (та же ошибка в оценке
  инспектора, `VatBakeEstimate`). `SkinnedFrameSource` при смене клипа: `VatClipPlayer.Stop` (граф) →
  `VatPoseSnapshot.Restore` (локальные трансформы всей копии и веса блендшейпов, снимок после `PrepareAnimator`).
  Клип по умолчанию — `VatAsset._defaultClip` (имя), `DefaultClipIndex`; выбор — `VatDefaultClip.Set` (инспектор
  ассета, пишет в шаблон профиля через `VatProfileLookup`), при бейке — `VatTemplateMaterial.Apply`. Инспектор
  материала: `VatClipField` (выпадающий список, `ApplyTo` кадр 0), клип материала ищется по строке `_VatFrame.x`
  (`VatClipLookup`).
- **Alembic:** `VATyakov.Editor` типов Alembic не видит; `VatAlembic` находит через `TypeCache` реализацию
  `IVatAlembicSupport` из сборки `VATyakov.Editor.Alembic` (только с `VAT_ALEMBIC`). `AlembicFrameSource`: скрытая
  копия .abc с корнем в identity (`VatAlembicCopy`), все меш-ноды — один меш (`VatAlembicReader`, `VatSourceMeshes.Combine`),
  на каждом кадре `VatAlembicTopology` (ошибка «патч 2») и `VatVertexJumps` (предупреждение). Инспектор профиля
  берёт число вертексов, длину и разрыв петли из `VatAlembicProbe` (кэш до переимпорта .abc), подсказка loop —
  `VatLoopHintController`.
  `VertexEncoder` за один проход пишет `_VatPosTex` (`VatPositionTexels`), `_VatRotTex` (`VatRotationTexels`,
  кодек `VatSmallestThree`) и `_VatDriftTex` (`VatDriftTexels`, кодек `VatDriftCodec`); N и T кадра — `VatTangentFrames`,
  знак бинормали — `VatChirality`. Позиции пишутся в два `VatPositionVariant` (d = 0 и d = центроид кадра − центроид
  покоя); после последнего кадра `VatDriftPolicy` выбирает один, `encoder.Layout` — раскладка с решением о дрейфе.
  Ошибки обоих вариантов — `VatPrecision` в ассете.
- **Рантайм:** время, loop/one-shot и скорость считает CPU (`VatPlayback`, `VatClip.Frame`, `VatTiming`) и пишет
  `_VatFrame = (row0, row1, frac, 0)` в материал. Драйвер пока — `Assets/VatDev/Scripts/VatCompare`.
- **Шейдер:** `VatCore.hlsl` — адрес тексела, тексел и сумма дрейфа, декод smallest-three, nlerp и оси кадра;
  `VatMath.cs` — его CPU-зеркало. Дрейф читается всегда (без него в текстуре нули).
  `VatShaderGraph.hlsl`: `VatVertexPosition_float` и `VatVertexNormalTangent_float`; SubGraph `VAT_Vertex` отдаёт
  Position, Normal, Tangent. Шаблон по умолчанию — `VAT_Lit_Vertex` (BaseMap, NormalMap `_BumpMap`, Metallic, Smoothness).
- **Тестовый контент:** `Assets/VatDev/Content/Bow`, результаты бейка — `Assets/VatDev/Bakes`, сцена `Assets/VatDev/Scenes/Compare.unity`.
  У `WeaponLandingBow.fbx` (legacy) три клипа: `VAT` (кадры 0–25 take Fire), `Fire` (take целиком, 2.6 с), `BakeSave`
  (take целиком, 3.23 с); профили `Bow_Default` и `Bow_Upgrade` запекают все три (loop, 30 fps): 200 строк, у Upgrade
  2 блока — текстура 2265×400.
  Alembic: `Assets/VatDev/Content/Alembic/Water.abc` (5853 вертекса, 80 кадров по 24 fps, с UV, не петля — конец
  отличается от начала на 218 мм), профиль `Bakes/Water.asset`, сцена `Scenes/CompareAlembic.unity` (не в сборке: .abc
  в мобильный билд не идёт) — сплит-скрин, у каждой половины своя камера с одинаковым ракурсом. `Columns.fbx` — на 1.15.
  У лука нет своей normal map: `Bow_TestNormal.png` — процедурный рельеф (sin·sin, 24 периода), на SMR — `Bow_Source_Lit.mat`
  (URP Lit), на VAT — `VAT_Lit_Vertex` с той же картой и smoothness 0.6. Сцена `RotDecode` — проверка RGBA8 на устройстве
  (`VatRotDecodeCheck`, шейдер `Assets/VatDev/Shaders/VatRotDecodeTest.shader`), вторая сцена сборки.
  Дрейф (1.5): `Content/Alembic/Jelly.abc` — желе 1225 вертексов, 121 кадр по 30 fps, за 1.5 с улетает на 40 м и потом
  медленно колышется (генератор — меню VATyakov → Dev → Regenerate Drift Content, `VatJellyContent`). Профили
  `Bakes/Jelly` (дрейф включился сам: ошибка 0.059 мм) и `Bakes/Jelly_NoDrift` (скрытое `_noDrift`, 15.6 мм), сцена
  `Scenes/Drift.unity` (не в сборке): Alembic, VAT с дрейфом (x = 40) и без (x = 41.5), камера вплотную к их силуэтам.

## Карта кода (`Packages/com.vatyakov/`)

- `Runtime/` — AssemblyInfo, VATyakov.asmdef, VatAsset, VatClip, VatLayoutInfo, VatMath, VatPlayback, VatPrecision, VatShaderIds, VatTiming
- `Shaders/` — VatCore.hlsl, VatShaderGraph.hlsl; `SubGraphs/` — VAT_Vertex.shadersubgraph
- `Samples/UnlitVertex/` — VAT_Unlit_Vertex.shadergraph; `Samples/LitVertex/` — VAT_Lit_Vertex.shadergraph (шаблон по умолчанию);
  `Samples/LitVertexTriplanar/` — VAT_Lit_Vertex_Triplanar.shadergraph (меши без UV)
- `Editor/Baking/` — VatAssetPath, VatAssetWriter, VatBakeEstimate, VatBakeException, VatBakeLog, VatBakePipeline, VatBakeProgress, VatBakeResult, VatBakeTextures, VatBakeValidator, VatBaker, VatClipListProblems, VatMemory, VatSourceHash, VatTemplateMaterial, VatTestPrefab
- `Editor/Baking/Layout/` — VatClipRequest, VatLayout, VatLayoutVerifier, VatVertexFormat
- `Editor/Baking/Sources/` — IVatAlembicSupport, IVatFrameSource, VatAlembic, VatAlembicProbe, VatFrame, VatFrameSources, VatLoopGap, VatSourceClip, VatSourceMesh, VatSourceMeshes, VatSourceSubMesh
- `Editor/Alembic/` — VATyakov.Editor.Alembic.asmdef, AlembicFrameSource, VatAlembicCopy, VatAlembicReader, VatAlembicSupport, VatAlembicTopology, VatVertexJumps
- `Editor/Baking/Sources/Skinned/` — SkinnedFrameSource, VatBakeCopy, VatClipPlayer, VatFrameReader, VatPoseSnapshot, VatRootSpace
- `Editor/Baking/Vertex/` — VatBoundsBuilder, VatCentroid, VatChirality, VatDriftCodec, VatDriftPolicy, VatDriftTexels, VatHalf3, VatIndexBuffer, VatPositionTexels, VatPositionVariant, VatQuantizationStats, VatRestPose, VatRotationTexels, VatSmallestThree, VatSubMeshes, VatTangentFrames, VatTexture, VatVertexMeshBuilder, VatVertexStream1, VertexEncoder
- `Editor/Profile/` — VatBakeDialog, VatBakeProfile, VatBakeProfileEditor, VatProfileContext, VatProfileModel, VatSourceKind; `Containers/` и `Controllers/` — части инспектора профиля (источник — VatSourceFields*, подсказка loop — VatLoopHint*)
- `Editor/Asset/` — VatAssetEditor, VatDefaultClip, VatProfileLookup; `Containers/` и `Controllers/` — части инспектора VatAsset (ошибка с дрейфом и без — VatAssetPrecision*, клип по умолчанию — VatAssetDefaultClip*)
- `Editor/Material/` — VatShaderGUI, IVatMaterialSection, VatClipField, VatClipLookup, VatFoldoutHeader, VatFrameField, VatMaterialBinding, VatObjectLinkField; `Sections/` — VatAdvancedSection, VatAnimationSection, VatSurfaceSection
- `Editor/Framework/` — ControllerExtensions, ControllerInspector, EditorContainer, IController, Property, Trigger, VisualElementExtensions
- `Editor/Ui/` — VatAssetSummaryContainer, VatClipRow, VatEditor.uss, VatObjectLink, VatStat, VatText, VatUi
- `Tests/Editor/` — VatAlembicTests (под `#if VAT_ALEMBIC`), VatBakeTests, VatClipFrameTests, VatClipsTests, VatDriftTests, VatInMemoryBake, VatMathTests, VatPlaybackTests, VatRotationCodecTests, VatShaderGraphTests, VatTangentFramesTests, VatTestRig, VatTestUtil, VatTimingTests, VatVertexEncoderTests; `Fixtures/` — VAT_HalfParent.shadergraph, VatCloth.abc, VatTopology.abc, VatShuffled.abc
- Вне пакета: `Assets/VatDev/Editor/VatAlembicFixtures` (сборка `VATyakov.Dev.Editor`, меню VATyakov → Dev → Regenerate Alembic Fixtures) — генератор .abc-фикстур, `VatJellyContent` — желе для дрейфа; `Assets/VatDev/Scripts/VatCompare` умеет `AlembicStreamPlayer` (под `VAT_ALEMBIC`) и несколько клипов (`_clips`, `_clipIndex`, кнопка Clip и клавиша C в `VatCompareControls`; SMR играет клип с тем же именем)

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
- **1.5:** продуктовые решения (вопросы к пользователю): `_VatDriftTex` есть в каждом Vertex-ассете (без дрейфа — нули,
  16 Б на кадр), шейдер всегда прибавляет d — без driftOn-ветки, умножения и keyword; ручного поля в профиле нет, только
  авто. Записано в §1.9 и §2.2. `formatVersion` 3, лук и вода перезапечены (дрейф у них не включился: центроид уходит
  на 0.03–0.29 м, ошибка 0.2–0.5 мм). Цена: 4 лишние выборки на вертекс у всех Vertex-ассетов — если упрёмся в
  вертексную стадию на мобилках, вариант на будущее — keyword `_VAT_DRIFT`.
  d — центроид кадра (среднее вертексов в double) минус центроид покоя. Решение принимается после сэмплинга по
  фактической ошибке без дрейфа, а не по прогнозу: энкодер пишет оба варианта позиций за один проход (×2 буфер
  позиций на время бейка), поэтому инспектор знает обе ошибки. Δ считается от уже декодированного d, ошибки не
  складываются. Скрытое поле профиля `_noDrift` (в хэше) — только для сравнения в VatDev.
  Видно ли на глаз: без дрейфа у желе на 40 м ошибка до 15.6 мм, но нормали берутся из `_VatRotTex` и квантование
  позиций не трогают — свет одинаковый, отличается только силуэт. С 5.5 м это 1–2 пикселя и почти не видно, поэтому
  камера сцены Drift стоит в 1.2 м от силуэтов. На лука дрейф дал бы 0.12 мм вместо 0.21, но по правилу он выключен.
  У `Jelly.abc` 21 вертекс на полюсе меняет знак бинормали (вырожденные треугольники полюса) — предупреждение бейка ожидаемое.
- **1.6:** продуктовые решения (вопросы к пользователю): Loop и fps — общие для всех клипов профиля (одного
  поля на клип нет); клип по умолчанию пишется только в шаблон своего профиля, остальные материалы выбирают клип
  в своём инспекторе; для проверки клипы лука нарезаны из take FBX. Записано в §1.1 и §1.6. Имена клипов в ассете
  уникальны (ошибка бейка): по имени хранится клип по умолчанию, по имени же `VatCompare` ищет клип SMR.
  `formatVersion` не менялся (новое поле `_defaultClip` у старых ассетов пустое → первый клип). Старые профили с
  полем `_clip` переносятся в `_clips` в `OnAfterDeserialize`; у лука перенос уже записан на диск.
  Сброс позы: без него тесты порядка расходятся на 564 мм (отрицательный контроль сделан, legacy и Generic), с ним
  кадры источника совпадают побайтно. Побайтно текстуры в двух порядках не совпадают и не должны: покой — кадр 0
  первого клипа (§2.2), смещения считаются от разных поз; декод совпадает в пределах half (< 0.5 мм).
  `VatDefaultClip.Set(asset, i, template)` — перегрузка для тестов: профиль, сохранённый ассетом, теряет ссылку на
  рендерер из preview-сцены, и тест через `VatProfileLookup` был нестабилен.
  Лог бейка: «degenerate tangents» — сумма по кадрам (вертекс-кадры), поэтому у лука с тремя клипами 4800 = 24 × 200.
  Проверено в Play mode в Compare: Fire кадр 20 и BakeSave кадр 50 — SMR и VAT совпадают на обоих луках. 183 теста зелёные.
