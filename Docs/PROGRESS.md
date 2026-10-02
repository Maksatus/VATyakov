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
| 1.7 VatAnimator: материал на юнита и управление | готово | 0.7.0 |
| Аудит: упрощение после 1.7 | готово | 0.7.1 |
| 1.8 | следующая | — |
| 1.9 Motion vectors | перенесена в патч 2 (§6) | — |
| 1.10–1.16, 1.18 | не начаты | — |
| 1.17 Тени | убрана, рекомендация — в §3 | — |
| 1.19 Финальные бюджеты | слита с 1.14 | — |

Проверки на устройствах (iPhone 12, Adreno, Mali): результаты не записаны — дописать сюда при следующем прогоне.
1.3 на устройствах: сцена `rot_decode` (кнопка RGBA8 в `compare`) — «RGBA8: OK» или число неверных текселей и API.

## Как устроено сейчас

- **Бейк:** `VatBakeProfile` (меню Create → VATyakov → VAT Bake Profile) → `VatBaker.Bake` → `VatBakePipeline.Run` →
  `VatFrameSources.Open` (`SkinnedFrameSource` или `AlembicFrameSource` по `VatSourceKind`) → `VertexEncoder` →
  `VatAssetWriter`. Проверки до бейка — `VatBakeValidator` (список клипов — `VatClipListProblems`). Предупреждения
  источника (`IVatFrameSource.Warnings`) — в лог бейка.
- **Клипы (1.6):** профиль — `Clips` (массив, общие Loop и fps), Alembic — один клип. `VatLayout.ForVertex` кладёт
  клипы друг под другом в порядке списка и проверяет высоту по всем сразу до сэмплинга (та же ошибка в оценке
  инспектора, `VatBakeEstimate`). `SkinnedFrameSource` при смене клипа: `VatClipPlayer.Stop` (граф) →
  `VatPoseSnapshot.Restore` (локальные трансформы всей копии и веса блендшейпов, снимок после `PrepareAnimator`).
  Клипа по умолчанию нет (0.7.1): клип шаблона выбирается в инспекторе материала — `VatClipField` (выпадающий
  список, `ApplyTo` кадр 0), клип материала ищется по строке `_VatFrame.x` (`VatClipLookup`). Перебейк сохраняет его
  по имени: `VatTemplateMaterial.ShownClip` читает имя из старого ассета до записи, `Apply` ищет его в новом (нет —
  первый клип). `VatAnimator` с пустым Clip играет первый клип.
- **Alembic:** код в `VATyakov.Editor` (`Editor/Baking/Sources/Alembic/`), сборка ссылается на
  `Unity.Formats.Alembic.Runtime` по имени и получает `VAT_ALEMBIC` через `versionDefines` (0.7.1). Под `#if VAT_ALEMBIC` —
  только `AlembicFrameSource` и `VatAlembicCopy`; `VatAlembic.IsInstalled` и `Open` — по тому же define. `AlembicFrameSource`: скрытая
  копия .abc с корнем в identity (`VatAlembicCopy`), все меш-ноды — один меш (`VatAlembicReader`, `VatSourceMeshes.Combine`),
  на каждом кадре `VatAlembicTopology` (ошибка «патч 2») и `VatVertexJumps` (предупреждение). Инспектор профиля
  берёт число вертексов, длину и разрыв петли из `VatAlembicProbe` (кэш до переимпорта .abc), подсказка loop —
  `VatLoopHintController`.
  `VertexEncoder` за один проход пишет `_VatPosTex` (`VatPositionTexels`), `_VatRotTex` (`VatRotationTexels`,
  кодек `VatSmallestThree`) и `_VatDriftTex` (`VatDriftTexels`, кодек `VatDriftCodec`); N и T кадра — `VatTangentFrames`,
  знак бинормали — `VatChirality`. Позиции — `VatPositions`: Δ = pos − rest − d, d = центроид кадра − центроид покоя,
  дрейф включён всегда (0.7.1). `VatPrecision` в ассете — ошибка после fp16 и путь центроида.
- **Рантайм:** время, loop/one-shot и скорость считает CPU (`VatPlayback`, `VatClip.Frame`, `VatTiming`) и пишет
  `_VatFrame = (row0, row1, frac, 0)` в материал. Драйвер — `VatAnimator` (1.7): `VatMaterialCopies` (копия на
  уникальный VAT-шаблон, VAT = есть `_VatFrame`, только в Play mode, `Dispose` в `OnDestroy`), `VatPlayer` (клип,
  скорость, пауза, нормализованное время поверх `VatPlayback`; время передаётся снаружи — тестируется в EditMode),
  `VatEndLatch` (событие one-shot), `VatPropertyBlockCheck` (`[Conditional]` UNITY_EDITOR/DEVELOPMENT_BUILD).
  Запись — в `LateUpdate`, событие `ClipFinished` после записи. `VatCompare` в Compare берёт копию `VatAnimator`
  из префаба и выключает его.
- **Шейдер:** `VatCore.hlsl` — адрес тексела, тексел и сумма дрейфа, декод smallest-three, nlerp и оси кадра;
  `VatMath.cs` — его CPU-зеркало. Дрейф пишется и читается всегда.
  `VatShaderGraph.hlsl`: `VatVertexPosition_float` и `VatVertexNormalTangent_float`; SubGraph `vat_vertex` отдаёт
  Position, Normal, Tangent. Шаблон по умолчанию — `vat_lit_vertex` (BaseMap, NormalMap `_BumpMap`, Metallic, Smoothness).
- **Тестовый контент** (файлы — маленькими буквами через `_`, 0.7.1): `Assets/VatDev/Content/Bow`, результаты бейка —
  `Assets/VatDev/Bakes`, сцена `Assets/VatDev/Scenes/compare.unity`.
  Толпа (1.7): профиль `Bakes/bow_one_shot` — те же три клипа лука, но one-shot (VAT 26, Fire 79, BakeSave 98 кадров),
  префаб `bow_one_shot_vat`; сцена `Scenes/animator.unity` (третья в сборке) — 100 юнитов `VatCrowd` в сетке 10×10,
  каждый по `ClipFinished` играет следующий клип. Кнопки и клавиши: Pool (P, `SetActive` всех), Hit (H, `_BaseColor`
  на 0.15 с), Reverse (R), Pause (Space); счётчики Finished, Doubled (повторное событие за один Play, должно быть 0) и
  Materials (все загруженные `Material`, не должно расти при пуле).
  У `weapon_landing_bow.fbx` (legacy) три клипа: `VAT` (кадры 0–25 take Fire), `Fire` (take целиком, 2.6 с), `BakeSave`
  (take целиком, 3.23 с); профили `bow_default` и `bow_upgrade` запекают все три (loop, 30 fps): 200 строк, у Upgrade
  2 блока — текстура 2265×400.
  Alembic: `Assets/VatDev/Content/Alembic/water.abc` (5853 вертекса, 80 кадров по 24 fps, с UV, не петля — конец
  отличается от начала на 218 мм), профиль `Bakes/water.asset`, сцена `Scenes/compare_alembic.unity` (не в сборке: .abc
  в мобильный билд не идёт) — сплит-скрин, у каждой половины своя камера с одинаковым ракурсом. `columns.fbx` — на 1.15.
  У лука нет своей normal map: `bow_test_normal.png` — процедурный рельеф (sin·sin, 24 периода), на SMR — `bow_source_lit.mat`
  (URP Lit), на VAT — `vat_lit_vertex` с той же картой и smoothness 0.6. Сцена `rot_decode` — проверка RGBA8 на устройстве
  (`VatRotDecodeCheck`, шейдер `Assets/VatDev/Shaders/VatRotDecodeTest.shader`), вторая сцена сборки.
  Дрейф (1.5): `Content/Alembic/jelly.abc` — желе 1225 вертексов, 121 кадр по 30 fps, за 1.5 с улетает на 40 м и потом
  медленно колышется (генератор — меню VATyakov → Dev → Regenerate Drift Content, `VatJellyContent`). Профиль
  `Bakes/jelly` (ошибка 0.059 мм, без дрейфа было 15.6 мм), сцена `Scenes/drift.unity` (не в сборке): Alembic и VAT
  (x = 40), камера вплотную к силуэтам.
  Разрушение (на 1.15): `Content/RBDDestroy/rbd_test_rig.fbx` — меши-куски с анимацией трансформов, legacy-клип, без
  скиннинга; сейчас его не печёт ни один источник. `rbd_test_cell.fbx` побайтно совпадает с ним — похоже, не тот файл.

## Карта кода (`Packages/com.vatyakov/`)

- `Runtime/` — AssemblyInfo, VATyakov.asmdef, VatAnimator, VatAsset, VatClip, VatEndLatch, VatLayoutInfo, VatMaterialCopies, VatMath, VatPlayback, VatPlayer, VatPrecision, VatPropertyBlockCheck, VatShaderIds, VatTiming
- `Shaders/` — VatCore.hlsl, VatShaderGraph.hlsl; `SubGraphs/` — vat_vertex.shadersubgraph
- `Samples/UnlitVertex/` — vat_unlit_vertex.shadergraph; `Samples/LitVertex/` — vat_lit_vertex.shadergraph (шаблон по умолчанию);
  `Samples/LitVertexTriplanar/` — vat_lit_vertex_triplanar.shadergraph (меши без UV)
- `Editor/Baking/` — VatAssetPath, VatAssetWriter, VatBakeEstimate, VatBakeException, VatBakeLog, VatBakePipeline, VatBakeProgress, VatBakeResult, VatBakeTextures, VatBakeValidator, VatBaker, VatClipListProblems, VatMemory, VatSourceHash, VatTemplateMaterial, VatTestPrefab
- `Editor/Baking/Layout/` — VatClipRequest, VatLayout, VatVertexFormat
- `Editor/Baking/Sources/` — IVatFrameSource, VatFrame, VatFrameSources, VatLoopGap, VatSourceClip, VatSourceMesh, VatSourceMeshes, VatSourceSubMesh
- `Editor/Baking/Sources/Alembic/` — AlembicFrameSource и VatAlembicCopy (под `#if VAT_ALEMBIC`), VatAlembic, VatAlembicProbe, VatAlembicReader, VatAlembicTopology, VatVertexJumps
- `Editor/Baking/Sources/Skinned/` — SkinnedFrameSource, VatBakeCopy, VatClipPlayer, VatFrameReader, VatPoseSnapshot, VatRootSpace
- `Editor/Baking/Vertex/` — VatBoundsBuilder, VatCentroid, VatChirality, VatDriftCodec, VatDriftTexels, VatHalf3, VatIndexBuffer, VatPositionTexels, VatPositions, VatQuantizationStats, VatRestPose, VatRotationTexels, VatSmallestThree, VatSubMeshes, VatTangentFrames, VatTexture, VatVertexMeshBuilder, VatVertexStream1, VertexEncoder
- `Editor/Profile/` — VatBakeDialog, VatBakeProfile, VatBakeProfileEditor, VatProfileContext, VatProfileModel, VatSourceKind; `Containers/` и `Controllers/` — части инспектора профиля (источник — VatSourceFields*, подсказка loop — VatLoopHint*)
- `Editor/Asset/` — VatAssetEditor, VatProfileLookup; `Containers/` и `Controllers/` — части инспектора VatAsset (ошибка и путь центроида — VatAssetPrecision*)
- `Editor/Animator/` — VatAnimatorEditor; `Containers/` и `Controllers/` — VatAnimatorClip* (VAT Asset и выпадающий Clip, пусто = First — первый клип), VatAnimatorFields* (Play On Enable, Speed)
- `Editor/Material/` — VatShaderGUI (по порядку Surface — VatSurfaceFields, Animation — VatAnimationFields, Render Queue), VatClipField, VatClipLookup, VatFrameField, VatMaterialBinding, VatMaterialStatus, VatObjectLinkField
- `Editor/Framework/` — ControllerExtensions, ControllerInspector, EditorContainer, IController, Property, Trigger, VisualElementExtensions
- `Editor/Ui/` — VatAssetSummaryContainer, VatClipRow, VatEditor.uss, VatObjectLink, VatStat, VatText, VatUi
- `Tests/Editor/` — VatAlembicTests (под `#if VAT_ALEMBIC`), VatBakeTests, VatClipFrameTests, VatClipsTests, VatDriftTests, VatInMemoryBake, VatMaterialCopiesTests, VatMathTests, VatPlaybackTests, VatPlayerTests, VatRotationCodecTests, VatShaderGraphTests, VatTangentFramesTests, VatTestRig, VatTestUtil, VatTimingTests, VatVertexEncoderTests; `Fixtures/` — vat_half_parent.shadergraph, VatCloth.abc, VatTopology.abc, VatShuffled.abc
- Вне пакета: `Assets/VatDev/Editor/VatAlembicFixtures` (сборка `VATyakov.Dev.Editor`, меню VATyakov → Dev → Regenerate Alembic Fixtures) — генератор .abc-фикстур, `VatJellyContent` — желе для дрейфа; `Assets/VatDev/Scripts/VatCompare` умеет `AlembicStreamPlayer` (под `VAT_ALEMBIC`) и несколько клипов (`_clips`, `_clipIndex`, кнопка Clip и клавиша C в `VatCompareControls`; SMR играет клип с тем же именем; если на VAT-объекте есть `VatAnimator`, берёт его копию и выключает его); `VatCrowd` + `VatCrowdControls` — толпа для проверок 1.7

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
- **1.7:** продуктовые решения (вопросы к пользователю): `Play` всегда с начала; `GetNormalizedTime` у loop — фаза
  в [0, 1); Play On Enable при каждом `OnEnable` перезапускает клип из поля Clip (пусто — клип по умолчанию ассета);
  hit-flash — через `_BaseColor`, без эмиссии в графах. Записано в §1.3 и §1.6. Сам решил (план не уточнял): у
  компонента одно поле `VatAsset`, таблица клипов — из него; loop события не даёт; ошибка MPB — один раз на рендерер;
  `_VatFrame` не пишется, если кадр не изменился.
  Событие: `VatEndLatch` помнит «стоим у конца в сторону движения» и обновляется только при ненулевой скорости, поэтому
  пауза или смена скорости у конца не дают повтор, а разворот даёт его на другом конце.
  VAT-шаблон — материал со свойством `_VatFrame`. Рендереры — `GetComponentsInChildren<Renderer>(true)` (в том числе
  неактивные LOD), трогаются только те, у кого есть VAT-слот. Копии создаются лениво (`OnEnable` или первое
  обращение к `Materials`/`Set*`), поэтому эффекты можно писать из чужого `Awake`.
  Тестовые префабы `Bow_Default_Vat`/`Bow_Upgrade_Vat` пересобраны с `VatAnimator`. Префабы Jelly и Water не
  пересобирались: `VatAnimator` появится у них при следующем Create Prefab, а `VatCompare` к этому готов.
  Проверено в Play mode (Animator, 100 юнитов): за 30 с ~1400 событий, Doubled 0; 20 переключений пула — Materials
  201 до и после; Hit красит `_BaseColor` и возвращает его; MPB на одном юните — одна ошибка с именем объекта. Compare
  на Fire кадр 20 — SMR и VAT совпадают через копию `VatAnimator`. Шаблонные `.mat` после Play mode в git не
  изменились. Замер стоимости записи (маркер `VatAnimator.Write`) не делал — его проверяет пользователь. 203 теста зелёные.
- **Аудит после 1.7 (0.7.1):** запрос пользователя — убрать лишнее из инструмента и плана. Его решения: дрейф всегда
  включён; клипа по умолчанию в `VatAsset` нет; Alembic-код — в основной Editor-сборке через `#if`; убраны поля
  `VatLayoutInfo` под Bone и Rigid, `VatLayoutVerifier`, миграция `_clip`, реестр секций ShaderGUI. Инспекторы не
  урезались (оценка, подсказка loop, Result, Test Prefab остались). В плане: 1.9 — в патч 2, 1.17 убрана, 1.19 слита с
  1.14, у 1.13 нет `averageSpeed`, в §1.9 нет vertex color и доп. UV. Bone, Rigid и 1.10 остаются как были; RBD
  (1.15–1.16) пользователь назвал архиважным. Записано в §0, §1.1, §1.4, §1.6, §1.8, §1.9, §2.1, §2.2, §3, §4, §6.
  Сам решил: перебейк сохраняет клип шаблона по имени (иначе каждый перебейк сбрасывал бы выбор из инспектора
  материала); `VatPositionVariant` → `VatPositions`; Alembic-файлы переехали к источникам, `VatAlembic` — с `#if`.
  `formatVersion` не менялся: у старых ассетов без дрейфа в текстуре нули, они играют; ошибка в инспекторе у них 0
  до перебейка (поле `VatPrecision._error` новое). VatDev перезапечён: лук 0.122 мм (без дрейфа было 0.21), Jelly
  0.059 мм, вода 0.503 мм (было ≈ 0.5 мм). Профиль `Jelly_NoDrift` и его результаты удалены, из сцены Drift — объект
  без дрейфа. Шаблонные материалы лука при перебейке сохранили клип (`_VatFrame` в git не изменился).
  Проверено: 198 тестов зелёные; без `com.unity.formats.alembic` (удалён и возвращён через `package_remove/add`)
  проект собирается, 192 теста зелёные; `manifest.json`, lock и `.meta` у .abc после возврата не изменились.
  Нейминг (по просьбе пользователя): файлы контента VatDev — маленькими буквами через `_` (`Bow_Default_Vat.prefab` →
  `bow_default_vat.prefab`, `WeaponLandingBow.fbx` → `weapon_landing_bow.fbx`, сцены `compare`, `rot_decode`,
  `animator`, `compare_alembic`, `drift`); папки и код — PascalCase, как в `D:\client`. Бейкер пишет `<профиль>_vat` и
  сабассеты `_mesh`, `_pos`, `_rot`, `_drift`; при перебейке имена сабассетов теперь идут от текущего имени файла
  (`VatAssetWriter.Adopt` больше не держит старое). Клипы Alembic называются по файлу: `water`, `jelly`. Клипы лука
  (`VAT`, `Fire`, `BakeSave`) — имена из импортёра FBX, не файлы, не менялись. Переименовано через `AssetDatabase.RenameAsset`
  (GUID и ссылки целы, Build Settings обновились сами); смену регистра git с `core.ignorecase` не видит — папки
  `Bakes`, `Content`, `Scenes` перевнесены в индекс.
  Стиль кода (по просьбе пользователя): весь C# и HLSL приведён к стилю `D:\client\Assets\Scripts\Game` и его
  `AGENTS.md`. Правила записаны в CLAUDE.md («Соглашения»), для Rider — `.editorconfig` в корне. Основное:
  явные модификаторы, скобки всегда, `var`, методы с блочным телом, порядок членов, `Deactivate()` перед `Activate()`,
  комментариев нет. Сделано утилитой на Roslyn из .NET SDK (синтаксические проходы плюс `var` по семантике только при
  точном совпадении типов), остальное — руками. Из старых комментариев шейдеров в CLAUDE.md перенесено: в HLSL только
  `_float`, новый вход Custom Function — последним. В `VatAnimator` убран костыль с `int.MinValue`
  (`TryGetClip` по имени и по индексу, проверка ассета в `IsAssetPlayable`). Поведение не менялось, 198 тестов зелёные.
