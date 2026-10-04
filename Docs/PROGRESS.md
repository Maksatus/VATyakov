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
| 1.8 Переходы: CrossFade и вес из кода | готово, устройства не проверены | 0.8.0 |
| 1.8.1 Дрейф на CPU | готово, устройства не проверены | 0.8.1 |
| 1.8.2 Шейдер без бленда и кватернион целиком | готово, устройства не проверены | 0.8.2 |
| 1.8.3 Позиции 8 бит по габаритам ассета | готово, устройства не проверены | 0.8.3 |
| 1.9 Motion vectors | перенесена в патч 2 (§6) | — |
| 1.10 Инструменты: память и «бейк устарел» | готово, валидатора сборки нет (решение пользователя) | 0.10.0 |
| 1.11 Bone с одной костью на вертекс | готово, устройства не проверены | 0.11.0 |
| 1.12 Bone с двумя influences | готово, устройства не проверены | 0.12.0 |
| Аудит: пакет как конструктор | готово | 0.12.1 |
| 1.13–1.16, 1.18 | не начаты | — |
| 1.17 Тени | убрана, рекомендация — в §3 | — |
| 1.19 Финальные бюджеты | слита с 1.14 | — |

Проверки на устройствах (iPhone 12, Adreno, Mali): результаты не записаны — дописать сюда при следующем прогоне.
Слабое целевое устройство — Redmi 9A (Helio G25, PowerVR GE8320, 2–3 ГБ): под него шейдер экономит выборки (1.8.1);
PowerVR — отдельный вендор. Сцены `rot_decode` больше нет (1.8.2): поворот декодируется одним `mad`, точность
байтов проверять не нужно.

## Как устроено сейчас

- **Бейк:** `VatBakeProfile` (меню Create → VATyakov → VAT Bake Profile) → `VatBaker.Bake` → `VatBakePipeline.Run` →
  `VatFrameSources.Open` (`VatSkinnedFrameSource` или `VatAlembicFrameSource` по `VatSourceKind`) → `VatVertexEncoder` →
  `VatAssetWriter`. Проверки до бейка — `VatBakeValidator` (список клипов — `VatClipListProblems`). Предупреждения
  источника (`IVatFrameSource.Warnings`) — в лог бейка.
- **Клипы (1.6):** профиль — `Clips` (массив, общие Loop и fps), Alembic — один клип. `VatLayout.ForVertex` кладёт
  клипы друг под другом в порядке списка и проверяет высоту по всем сразу до сэмплинга (та же ошибка в оценке
  инспектора, `VatBakeEstimate`). `VatSkinnedFrameSource` при смене клипа: `VatClipPlayer.Stop` (граф) →
  `VatPoseSnapshot.Restore` (локальные трансформы всей копии и веса блендшейпов, снимок после `PrepareAnimator`).
  Клипа по умолчанию нет (0.7.1): клип шаблона выбирается в инспекторе материала — `VatClipField` (выпадающий
  список, `ApplyTo` кадр 0), клип материала ищется по строке `_VatFrame.x` (`VatClipLookup.TryFind`). Перебейк сохраняет его
  по имени: `VatTemplateMaterial.ShownClip` читает имя из старого ассета до записи, `Apply` ищет его в новом (нет —
  первый клип). `VatAnimator` с пустым Clip играет первый клип.
- **Alembic:** код в `VATyakov.Editor` (`Editor/Baking/Sources/Alembic/`), сборка ссылается на
  `Unity.Formats.Alembic.Runtime` по имени и получает `VAT_ALEMBIC` через `versionDefines` (0.7.1). Под `#if VAT_ALEMBIC` —
  только `VatAlembicFrameSource` и `VatAlembicCopy`; `VatAlembic.IsInstalled` и `Open` — по тому же define. `VatAlembicFrameSource`: скрытая
  копия .abc с корнем в identity (`VatAlembicCopy`), все меш-ноды — один меш (`VatAlembicReader`, `VatSourceMeshes.Combine`),
  на каждом кадре `VatAlembicTopology` (ошибка «патч 2») и `VatVertexJumps` (предупреждение). Инспектор профиля
  берёт число вертексов, длину и разрыв петли из `VatAlembicProbe` (кэш до переимпорта .abc), подсказка loop —
  `VatLoopHintController`.
  `VatVertexEncoder` за один проход пишет `_VatRotTex` (`VatRotationTexels`, кодек `VatRotationCodec`, знак —
  `VatRotationSigns`, 1.8.2) и дрейф по строкам (`VatDriftRows`, float32, в ассет — `Vector3[]`, 1.8.1); N и T кадра —
  `VatTangentFrames`, знак бинормали — `VatChirality`. Позиции — `VatPositions`: Δ = pos − rest − d, d = центроид
  кадра − центроид покоя, дрейф включён всегда (0.7.1). С 1.8.3 `VatPositions` копит Δ всех кадров во float, а
  `VatPositionEncoding` (лениво, при первом обращении) считает габариты и ошибку 8 бит (`VatBytePositions`, fp16-выборка
  учтена), выбирает формат по Max Position Error профиля и пишет текстуру (`VatBytePositionTexels` или
  `VatHalfPositionTexels`, общий `IVatPositionTexels`), ошибку и баунды. `VatPrecision` в ассете — ошибка формата,
  ошибка 8 бит и путь центроида.
  `VatAssetWriter` при перебейке удаляет сабассеты, кроме меша и двух текстур (`_drift` ассетов формата 3).
- **Bone (1.11, две кости — 1.12):** профиль — поле Mode (`VatMode`, только у Skinned; `VatBakeProfile.IsBone`).
  `VatBakePipeline.Run` при IsBone зовёт `VatBonePipeline.Run` (`Editor/Baking/Bone/`): `VatBoneRig` читает из копии
  бейка кости, bindposes, A = renderer→root, bind-позу в пространстве корня (`VatRestPose` через `VatTangentFrames`),
  влияния вертекса (`VatBoneInfluence`: первые два слота `BoneWeight` с перенормировкой, вес первой кости в 16 бит) и
  пивоты в half; `VatSkinnedFrameSource.Pose` ставит клип без `BakeMesh`, `VatBoneRig.ReadSkin` — матрицы
  `M = root⁻¹·L2W·bindpose·A⁻¹`. `VatBoneEncoder.AddFrame`: `VatSimilarity` (det, s = cbrt, полярное разложение в
  double — `VatMatrix3d`, остаток), `VatBoneCheck` для костей с вертексами, знак q — `VatRotationSigns` (элемент
  «bone»), запись — `VatBoneTexels` (возвращает half-значения), ошибка восстановления — по вертексам после кодирования всех костей кадра: `lerp` двух `VatMath.BonePoint` по
  сохранённому весу против `lerp(M1·v, M0·v)` по точному; капсулы — `VatBoneBounds` (на вертекс, обе кости). Проблема (кость или `VatBlendShapeCheck`) →
  строка-причина, `source.Rewind()`, Vertex-путь с `VatBakeResult.Fallback`. Меш — `VatBoneMeshBuilder`
  (`VatBoneStream0`: позиция + 4 байта `(i0, i1, вес_hi, вес_lo)` в TexCoord6, поток 1 как у Vertex), раскладка — `VatLayout.ForBone`
  (один блок, W = 2N, клипы с 0, строка пивотов последняя), формат — `VatBoneFormat`.
  `VatAsset`: `Mode`, `BoneTexture`, `Fallback`, `BoneCount`, `HasDrift`; `SetBoneData` / `SetData` (Vertex) /
  `SetFallback`; `ApplyTo` пишет `_VatLayout` обоим режимам, `_VatPosTex`/`_VatRotTex`/`_VatPosScale` — Vertex,
  `_VatBoneTex` — Bone; `ApplyFrame` и `VatAnimator.Write` пишут `_VatDrift` только при `HasDrift`. `VatAssetWriter`
  приживляет сабассеты обоих режимов и удаляет лишние при смене режима. `VatTemplateShader` подбирает шаблону шейдер
  режима (с блендом, если был `_VatFrameB`). Хэш: режим добавляется только для Bone (хэши Vertex-бейков не изменились).
  Шейдер: `VatCore.hlsl` — `VatBoneTexels` (два индекса из байтов), `VatBoneWeight` (без промежуточного 65535),
  `VatRotate` (однородный), `VatBonePoint`, `VatBoneDirection` (`rot / |q|²`); `VatShaderGraph.hlsl` — `VatBoneSkin`
  (одна кость, пивот — строка `Layout.y − 1`), `VatBonePose` (`lerp` двух костей), `VatBoneVertex_float` и
  `VatBoneVertexBlend_float` (одна функция отдаёт Position, Normal, Tangent). SubGraph `vat_bone`/`vat_bone_blend`: UV-нода канала 6, Position/Normal/Tangent Vector в Object,
  свойства `_VatBoneTex`, `_VatLayout` (Per Material), `_VatFrame` (и `_VatFrameB`).
- **Инспекторы (1.10):** память — `VatAssetMemory` (по самим `Texture2D`: байты текстур, padding `blocks·W − E`,
  доля клипа `blocks·W·F`), сводка `VatAssetSummary` (общая для инспектора ассета и Result профиля) пишет всего и долю
  клипа в строку клипа (`VatInfoRow`), карточка Memory ассета — `VatAssetMemory*`. «Бейк устарел» —
  `VatSourceHash.IsOutdated` (ассет валиден, профиль без проблем, хэш не совпал): HelpBox в `VatAssetProfile*` и в
  `VatResult*` профиля.
- **Рантайм:** время, loop/one-shot и скорость считает CPU (`VatPlayback`, `VatClip.Frame`, `VatTiming`) и пишет
  `_VatFrame = (row0, row1, frac, 0)` в материал. Драйвер — `VatAnimator` (1.7): `VatMaterialCopies` (копия на
  уникальный VAT-шаблон, VAT = есть `_VatFrame`, только в Play mode, `Dispose` в `OnDestroy`), `VatPlayer` (клип,
  скорость, пауза, нормализованное время поверх `VatPlayback`; время передаётся снаружи — тестируется в EditMode),
  `VatEndLatch` (событие one-shot). Dev-проверок и обёрток `Set*` нет (0.12.1): эффекты — через `Materials`.
  Запись — в `LateUpdate`, событие `ClipFinished` после записи. `VatCompare` в Compare берёт копию `VatAnimator`
  из префаба и выключает его.
  Переходы (1.8): `VatAnimator` держит `VatMixer` — два `VatPlayer` (исходный A → `_VatFrame`, целевой B →
  `_VatFrameB = (row0, row1, frac, w)`) и `VatWeightRamp` (рампа веса, пауза её замораживает). Без B
  `_VatFrameB = 0`, `SetWeight` ничего не делает. Оба вектора пишутся вместе, если изменился любой.
  Дрейф (1.8.1): вместе с ними пишется `_VatDrift = VatAsset.Drift(frame, frameB)` — d, смешанный по frac обоих
  клипов и по w. Превью и свои драйверы пишут кадр через `VatAsset.ApplyFrame` (кадр + дрейф): `ApplyTo`, поле Frame
  инспектора материала, `VatCompare`.
  Без бленда (1.8.2): `VatMaterialCopies.CanBlend` — у всех VAT-копий есть `_VatFrameB`. Иначе `CrossFade` = `Play`,
  `_VatFrameB` не пишется, в лог ничего не пишется (0.12.1).
  8-битные позиции (1.8.3): `VatAsset.PositionFormat` и `PositionRange` (min, size); `Drift` возвращает d + min,
  `ApplyTo` пишет `_VatPosScale = (size, 0)`. Инспектор материала считает материал устаревшим, если `_VatPosScale` не
  совпадает с ассетом.
- **Шейдер:** `VatCore.hlsl` — адрес тексела, декод поворота (`t·255/127 − 128/127`) и однородные оси кадра;
  `VatMath.cs` — его CPU-зеркало. В HLSL нет `?:` и `if` (1.8.2). `VatShaderGraph.hlsl`: клип целиком —
  `VatClipOffset` и `VatClipRotation` (`lerp` текселей, потом декод); без бленда — `VatVertexPosition_float`
  (входы `Drift`, затем `PosScale` — последними) и `VatVertexNormalTangent_float`; с блендом — `VatVertexPositionBlend_float`
  и `VatVertexNormalTangentBlend_float` (вход `FrameB`, у позиции затем `Drift` и `PosScale`), смесь клипов — один `lerp`,
  позиция — `rest + Drift + смесь · PosScale`.
  SubGraph `vat_vertex` (4 выборки на вертекс) и `vat_vertex_blend` (8) отдают Position, Normal, Tangent. Шаблон по
  умолчанию — `vat_lit_vertex` (BaseMap, NormalMap `_BumpMap`, Metallic, Smoothness), с блендом — `vat_lit_vertex_blend`.
- **Тестовый контент** (файлы — маленькими буквами через `_`, 0.7.1): `Assets/VatDev/Content/Bow`, результаты бейка —
  `Assets/VatDev/Bakes`, сцена `Assets/VatDev/Scenes/compare.unity`.
  Толпа (1.7): профиль `Bakes/bow_one_shot` — те же три клипа лука, но one-shot (VAT 26, Fire 79, BakeSave 98 кадров),
  префаб `bow_one_shot_vat` (шаблон на `vat_lit_vertex_blend`, 1.8.2); сцена `Scenes/animator.unity` (вторая в сборке) — 100 юнитов `VatCrowd` в сетке 10×10,
  каждый по `ClipFinished` играет следующий клип. Кнопки и клавиши: Pool (P, `SetActive` всех), Hit (H, `_BaseColor`
  на 0.15 с), Reverse (R), Pause (Space); счётчики Finished, Doubled (повторное событие за один Play, должно быть 0) и
  Materials (все загруженные `Material`, не должно расти при пуле).
  1.8: Fade (F) — длительность CrossFade к следующему клипу 0 / 0.25 / 1 с (0 — `Play`); Manual (M) — каждый юнит
  встаёт в переход «текущий → следующий», ползунок ведёт вес через `SetWeight(w, 0.1)`, авто-смена клипов выключена;
  в статусе — вес юнита #0. Fade 1 с длиннее клипа VAT (0.83 с), поэтому даёт предупреждения CrossFade посреди перехода.
  У `weapon_landing_bow.fbx` (legacy) три клипа: `VAT` (кадры 0–25 take Fire), `Fire` (take целиком, 2.6 с), `BakeSave`
  (take целиком, 3.23 с); профили `bow_default` и `bow_upgrade` запекают все три (loop, 30 fps): 200 строк, у Upgrade
  2 блока — текстура 2265×400.
  Alembic: `Assets/VatDev/Content/Alembic/water.abc` (5853 вертекса, 80 кадров по 24 fps, с UV, не петля — конец
  отличается от начала на 218 мм), профиль `Bakes/water.asset`, сцена `Scenes/compare_alembic.unity` (не в сборке: .abc
  в мобильный билд не идёт) — сплит-скрин, у каждой половины своя камера с одинаковым ракурсом. `columns.fbx` — на 1.15.
  У лука нет своей normal map: `bow_test_normal.png` — процедурный рельеф (sin·sin, 24 периода), на SMR — `bow_source_lit.mat`
  (URP Lit), на VAT — `vat_lit_vertex` с той же картой и smoothness 0.6. Сцена `rot_decode` удалена в 1.8.2.
  Сжатие (между 1.8.2 и 1.8.3, сцены `compression` и `compression_water` удалены в 1.8.3): 8 бит по габаритам ассета
  на крупных планах лука и воды не отличались от half; ASTC 4×4 на Fire кадр 10 превращал тетиву в полосу в сантиметры
  (до 50 мм), нормали у верхнего конца — до 21°, воду мял (до 223 мм у колонны). ASTC в 2079×200 и в 4096×256
  (степень двойки) декодировался побайтно одинаково — степень двойки только добавляла память. Подвох dev-скриптов:
  `CopyPropertiesFromMaterial` стирает свойства, которых нет у источника, — их выставлять после него и сохранять отдельно.
  Замер способов ужать VAT (после 1.8.2, лук / вода, ошибка позиции max; сейчас 0.12 / 0.5 мм и 0.9°):
  8 бит по габаритам ассета — 1.1 / 5.8 мм, по габаритам клипа — то же, по габаритам вертекса — 1.0 / 3.7 мм (в
  среднем 0.12 / 1.2 мм); RGB565 по габаритам вертекса — 4 / 15 мм; только нормаль в RG8 в осях покоя вертекса — нормаль
  до 0.33° (у воды 0.012% нормалей уходят дальше 90° от покоя — до 48°), тангент дугой от нормали покоя — p99 17° / 18°;
  ключевые кадры по ошибке 1 мм и 2° — у лука 49% строк, у воды 100%; fps вдвое — p99 91 / 94 мм, не годится;
  уникальных траекторий позиций у лука 62% (швы UV и жёсткие рёбра), у воды 100%; deflate исходных текстур (размер
  сборки, не память) — лук 4.76 → 1.69 МБ, вода 5.29 → 2.95 МБ; у лука 4 кости — в Bone около 13 КБ.
  Дрейф (1.5): `Content/Alembic/jelly.abc` — желе 1225 вертексов, 121 кадр по 30 fps, за 1.5 с улетает на 40 м и потом
  медленно колышется (генератор — меню VATyakov → Dev → Regenerate Drift Content, `VatJellyContent`). Профиль
  `Bakes/jelly` (ошибка 0.059 мм, без дрейфа было 15.6 мм), сцена `Scenes/drift.unity` (не в сборке): Alembic и VAT
  (x = 40), камера вплотную к силуэтам.
  Разрушение (на 1.15): `Content/RBDDestroy/rbd_test_rig.fbx` — меши-куски с анимацией трансформов, legacy-клип, без
  скиннинга; сейчас его не печёт ни один источник. `rbd_test_cell.fbx` побайтно совпадает с ним — похоже, не тот файл.
  Bone (1.11): персонажа для толпы в проекте нет, проверка на луке (решение пользователя). Профиль `Bakes/bow_bone` —
  копия `bow_default` с Mode = Bone (три клипа, loop, 30 fps): 4 кости, текстура 8×201 (12.6 КБ), ошибка 0.235 мм против Skin Weights = 2 Bones (1.12; у 2061 из 2079
  вертексов два веса и больше, у 349 — три, третий отбрасывается, как у Unity);
  шаблон `bow_bone_vat.mat` на `vat_lit_bone_blend` с картами и smoothness `bow_default_vat.mat`, префаб `bow_bone_vat`.
  Сцена `compare`: третья пара «SMR Bone» (экземпляр FBX, у обоих SMR `quality = Bone2` с 1.12) на z = 3.34 и «VAT Bone» на
  z = 4.54, `Compare Bone` в `Controls`; камера отодвинута на (4.6, 0.05, 1.57), чтобы видны были все шесть луков.
  Откат в Vertex: клип `Content/Bow/bow_squash.anim` (legacy, loop 1 с) неравномерно растягивает `BowUp_jnt` до
  (2, 1, 0.5) к середине; профиль `Bakes/bow_squash` (Mode = Bone, клипы VAT и bow_squash) оставлен неиспечённым —
  его Bake даёт Vertex-ассет и предупреждение «Clip 'bow_squash', frame 1: bone 'BowUp_jnt' scales non-uniformly
  (residual 0.0137, error 5.074 mm)».

## Карта кода (`Packages/com.vatyakov/`)

- `Runtime/` — AssemblyInfo, VATyakov.asmdef, VatAnimator, VatAsset, VatClip, VatEndLatch, VatLayoutInfo, VatMaterialCopies, VatMath, VatMixer, VatMode, VatPlayback, VatPlayer, VatPositionFormat, VatPositionRange, VatPrecision, VatShaderIds, VatTiming, VatWeightRamp
- `Shaders/` — VatCore.hlsl, VatShaderGraph.hlsl; `SubGraphs/` — vat_vertex.shadersubgraph, vat_vertex_blend.shadersubgraph, vat_bone.shadersubgraph, vat_bone_blend.shadersubgraph (1.11)
- `Samples/UnlitVertex/` — vat_unlit_vertex.shadergraph; `Samples/LitVertex/` — vat_lit_vertex.shadergraph (шаблон по умолчанию);
  `Samples/LitVertexBlend/` — vat_lit_vertex_blend.shadergraph (переходы);
  `Samples/LitVertexTriplanar/` — vat_lit_vertex_triplanar.shadergraph (меши без UV);
  `Samples/LitBone/` — vat_lit_bone.shadergraph, `Samples/LitBoneBlend/` — vat_lit_bone_blend.shadergraph (1.11)
- `Editor/Baking/` — VatAssetPath, VatAssetWriter, VatBakeEstimate, VatBakeException, VatBakeLog, VatBakePipeline, VatBakeProgress, VatBakeResult, VatBakeTextures, VatBakeValidator, VatBaker, VatClipListProblems, VatMemory, VatSourceHash, VatTemplateMaterial, VatTemplateShader
- `Editor/Baking/Bone/` (1.11) — VatBlendShapeCheck, VatBoneBounds, VatBoneCheck, VatBoneEncoder, VatBoneFormat, VatBoneInfluence (1.12), VatBoneMeshBuilder, VatBonePipeline, VatBoneRig, VatBoneStream0, VatBoneTexels, VatMatrix3d, VatSimilarity
- `Editor/Baking/Layout/` — VatClipRequest, VatLayout, VatVertexFormat
- `Editor/Baking/Sources/` — IVatFrameSource, VatFrame, VatFrameSources, VatLoopGap, VatSourceClip, VatSourceMesh, VatSourceMeshes, VatSourceSubMesh
- `Editor/Baking/Sources/Alembic/` — VatAlembicFrameSource и VatAlembicCopy (под `#if VAT_ALEMBIC`), VatAlembic, VatAlembicProbe, VatAlembicReader, VatAlembicTopology, VatVertexJumps
- `Editor/Baking/Sources/Skinned/` — VatSkinnedFrameSource, VatBakeCopy, VatClipPlayer, VatFrameReader, VatPoseSnapshot, VatRootSpace
- `Editor/Baking/Vertex/` — VatBoundsBuilder, VatCentroid, VatBytePositionTexels, VatBytePositions, VatChirality, VatDriftRows, VatHalf3, VatHalfPositionTexels, VatIndexBuffer, IVatPositionTexels, VatPositionEncoding, VatPositions, VatQuantizationStats, VatRestPose, VatRotationCodec, VatRotationSigns, VatRotationTexels, VatSubMeshes, VatTangentFrames, VatTexture, VatVertexMeshBuilder, VatVertexStream1, VatVertexEncoder
- `Editor/Profile/` — VatBakeDialog, VatBakeProfile, VatBakeProfileEditor, VatProfileContext, VatProfileModel, VatSourceKind; `Containers/` и `Controllers/` — части инспектора профиля (раскладка и секции — VatProfileLayout*, источник — VatSourceFields*, подсказка loop — VatLoopHint*)
- `Editor/Asset/` — VatAssetContext, VatAssetEditor, VatAssetMemory, VatProfileLookup; `Containers/` и `Controllers/` — части инспектора VatAsset (память текстур и padding — VatAssetMemory*, ошибка и путь центроида — VatAssetPrecision*, профиль и «Out of date» — VatAssetProfile*)
- `Editor/Animator/` — VatAnimatorContext, VatAnimatorEditor; `Containers/` и `Controllers/` — VatAnimatorClip* (VAT Asset и выпадающий Clip, пусто = First — первый клип), VatAnimatorFields* (Play On Enable, Speed)
- `Editor/Material/` — VatShaderGUI (по порядку Surface — VatSurfaceFields, Animation — VatAnimationFields, Render Queue), VatClipField, VatClipLookup, VatFrameField, VatMaterialBinding, VatMaterialStatus, VatObjectLinkField, VatUndo
- `Editor/Framework/` — IVatController, VatControllerExtensions, VatControllerInspector, VatEditorContainer, VatProperty, VatTrackerContainer, VatTrigger, VatVisualElementExtensions
- `Editor/Ui/` — VatAssetSummary, VatAssetSummaryContainer, VatEditor.uss, VatInfoRow (строка «имя — сведения», бывший VatClipRow), VatObjectLink, VatStat, VatText, VatUi
- `Tests/Editor/` — VatAlembicTests (под `#if VAT_ALEMBIC`), VatAssetMemoryTests, VatBakeTests, VatBoneDecoder, VatBoneTests, VatClipFrameTests, VatClipsTests, VatDriftTests, VatInMemoryBake, VatMaterialCopiesTests, VatMathTests, VatMixerTests, VatPlaybackTests, VatPlayerTests, VatPositionEncodingTests, VatRotationCodecTests, VatRotationSignsTests, VatShaderGraphTests, VatSwingRig, VatTangentFramesTests, VatTestRig, VatTestUtil, VatTriadRig, VatTimingTests, VatVertexEncoderTests; `Fixtures/` — vat_half_parent.shadergraph, vat_half_parent_blend.shadergraph, vat_cloth.abc, vat_topology.abc, vat_shuffled.abc
- Вне пакета: `Assets/VatDev/Editor/VatAlembicFixtures` (сборка `VATyakov.Dev.Editor`, меню VATyakov → Dev → Regenerate Alembic Fixtures) — генератор .abc-фикстур, `VatJellyContent` — желе для дрейфа; `Assets/VatDev/Scripts/VatDevGui` — общие размеры и стили IMGUI сцен VatDev; `Assets/VatDev/Scripts/VatCompare` умеет `AlembicStreamPlayer` (под `VAT_ALEMBIC`) и несколько клипов (`_clips`, `_clipIndex`, кнопка Clip и клавиша C в `VatCompareControls`; SMR играет клип с тем же именем; если на VAT-объекте есть `VatAnimator`, берёт его копию и выключает его); `VatCrowd` + `VatCrowdControls` — толпа для проверок 1.7

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
- **1.8:** продуктовые решения (вопросы к пользователю): `ClipFinished`, `CurrentClip`, `GetNormalizedTime` — целевого
  клипа, исходный доигрывает молча; скорость и пауза общие; `SetWeight` без целевого клипа — ничего и предупреждение;
  ветвления по весу нет, замер стоимости второго клипа — за пользователем. Шейдер (требование пользователя): никаких
  `?:` и `if` — N и T смешиваются одним `lerp` без нормализации и fallback по доминантному клипу из прежнего §1.5;
  нормализует URP (`SafeNormalize` в `TransformObjectToWorldNormal`/`Dir`), противоположные N при w = 0.5 дают ноль,
  не NaN. Записано в §1.5, `code-style.md` §9 и CLAUDE.md. `?:` в `VatDecodeRotation` и `VatNlerp` (1.3) не трогались.
  Сам решил: «переход незакончен» — 0 < w < 1 (при w = 0 рывка нет, предупреждения тоже); CrossFade, когда ничего не
  играет, — это `Play`; пауза замораживает рампу веса; вес приводится к [0, 1], NaN → 0; `d ≤ 0` или NaN — сразу.
  `VatCore.hlsl` и `VatMath.cs` не менялись: бленд — обычный `lerp` в `VatShaderGraph.hlsl`. Тест «противоположные N и T
  без NaN» проверяет `lerp` + формулу URP `SafeNormalize` на CPU. Свойство `_VatFrameB` добавлено в SubGraph правкой
  JSON (как `DriftTex`): родительские графы получают его сами, `vat_half_parent` объявляет `float4 _VatFrameB`.
  Проверено в Play mode (Animator): Fade 1 с — у 43 из 100 юнитов вес в (0, 1), NaN нет, предупреждения с именами
  клипов; Manual 0.5 — все 100 в переходе. Глазом плавность и замер `VatAnimator.Write` не смотрел — это проверяет
  пользователь. 217 тестов зелёные.
- **1.8.1:** запрос пользователя — облегчить шейдер для слабых телефонов (Redmi 9A). Дрейф одинаков для всех вертексов
  draw, а стоил 4 выборки на вертекс на клип (8 с переходом) — половину выборок и 2/3 выборок позиции. Теперь d —
  `Vector3[]` во float32 в `VatAsset`, CPU пишет готовый `_VatDrift`; `formatVersion` 4. Ветвление по w не делалось:
  `if` и `?:` в шейдере запрещены решением 1.8. Сам решил: `ApplyFrame` (кадр + дрейф) — один путь записи для превью и
  `VatCompare`; `_VatDrift` пишется в том же блоке, что `_VatFrame*`, у всех ассетов (у реальных d ≠ 0: дрейф включён
  всегда, у лука центроид ходит на 3–8 см), то есть +1 `SetVector` на материал за изменившийся кадр. Инспектор
  материала сравнивает `_VatDrift` с дрейфом кадра (строки берутся из клипа по `_VatFrame.x`, а не из `.y` — так
  индекс всегда в массиве) и предлагает Update from Asset. Вход SubGraph `DriftTex` (слот 7) удалён, `Drift` (слот 9)
  добавлен после `FrameB`, как в 1.8 — правкой JSON; свойство переиспользует объект `_VatDriftTex`, сменив тип.
  В старых `.mat` VatDev осталась пустая запись `_VatDriftTex` (как и `_VatClipA`) — безвредна.
  VatDev перезапечён: лук 0.122 мм, желе 0.059 мм, вода 0.503 мм — как до 1.8.1; в ассетах по 3 сабассета.
  Проверено в Play mode: `drift` кадр 80 — VAT-желе на x ≈ 40 (без дрейфа было бы у нуля), `_VatDrift` = (40, 0, 0);
  `animator` — у 100 юнитов `_VatDrift` совпадает с `VatAsset.Drift(frame, frameB)` (расхождение 0), в том числе у
  16 юнитов в переходе. Сравнение с SMR в `compare` и плавность глазом не смотрел — это проверяет пользователь.
  216 тестов зелёные (удалены 6 тестов кодека hi/lo, добавлены 4 теста `Drift`/`ApplyFrame` и тест перебейка формата 3).
- **1.8.2:** запрос пользователя — шейдер для Redmi 9A: не платить за клип B без перехода и убрать все `?:`.
  Продуктовые решения (вопросы к пользователю): два вида шейдера без keyword, шейдер выбирает художник в шаблоне (без
  бленда `CrossFade` мгновенный); бленд-граф только у Lit; новый формат поворота RGBA8 — кватернион целиком; сцену
  `rot_decode` удалить. Записано в §0, §1.2, §1.3, §1.5, §1.6, §1.9, §2.1, §2.2, §5, `code-style.md` §9 и CLAUDE.md.
  Сам решил: квантование симметричное (`round(c·127) + 128`), чтобы 0 и ±1 были точными; знак выравнивается по
  сырому q прошлого кадра того же клипа, кадр 0 клипа не зависит от прошлого клипа; `lerp` делается по текселям до
  декода (декод линейный — на один `mad` меньше); N и T однородными формулами без `normalize`. Шов петли с нечётным
  числом оборотов кадра — предупреждение бейка (`VatRotationSigns.SeamCount`), у VatDev его нет. `CanBlend` — все
  VAT-копии юнита с `_VatFrameB`: смешанный юнит (тело с блендом, оружие без) переходов не делает, предупреждение
  называет материал без бленда. Предупреждение — один раз на компонент, а не на вызов: толпа зовёт `CrossFade` часто.
  Сабграф и графы с блендом скопированы `AssetDatabase.CopyAsset` (новые GUID) и поправлены в JSON; у `vat_vertex`
  удалены свойство `_VatFrameB`, его нода и вход `FrameB` у обеих Custom Function (слоты 8 и 6).
  VatDev перезапечён: позиции как до 1.8.2 (лук 0.122 мм, вода 0.503 мм, желе 0.059 мм), ошибка поворота 0.87–0.89°
  (теоретический предел RGBA8 — 0.9°, было около 0.2°); `bow_one_shot_vat.mat` переключён на `vat_lit_vertex_blend`,
  остальные шаблоны сохранили клип. Build Settings: `compare`, `animator`.
  Проверено в Play mode: `animator` — 100 бленд-копий, юниты в переходе, NaN нет; юнит на `vat_lit_vertex` — два
  `CrossFade` подряд мгновенные, одно предупреждение, `_VatFrameB` у копии нет; `compare` на Fire кадр 20 — SMR и VAT
  обоих луков совпадают на снимке. Плавность глазом и замер на устройстве — за пользователем. 209 тестов зелёные
  (удалены тесты smallest-three и nlerp, добавлены кодек, `VatRotationSigns`, `CanBlend` и Half-родитель бленд-графа).
- **1.8.3:** запрос пользователя — уменьшить память текстур. Продуктовые решения (вопросы к пользователю): один
  габарит на ассет (не на вертекс, не на клип: на клип замер не дал выигрыша); формат выбирает бейкер по полю профиля
  Max Position Error, по умолчанию 8 мм; размах — отдельное свойство `_VatPosScale` (не упаковка в свободные
  компоненты); эксперименты со сжатием удалить. Записано в §0, §1.2, §1.4, §1.9, §2.2, план 1.8.3.
  Сам решил: минимум прибавляется к дрейфу на CPU (`VatAsset.Drift`), поэтому в шейдере сложение заменилось на
  умножение-сложение и лишних операций нет; сравнение строгое — 0 значит всегда half; ошибка 8 бит считается с
  fp16-выборкой (`half(b / 255)`), поэтому она чуть больше замеров до 1.8.3 (лук 1.24 мм против 1.12); ось без
  движения — size 0 и байт 0; Δ всех кадров копятся во float до конца сэмплинга (12 Б на вертекс-кадр на время
  бейка), выбор и запись — в `VatPositionEncoding` при первом обращении к позициям; поле в хэше исходника.
  Свойство `_VatPosScale` (Per Material, Single, по умолчанию 1) и вход `PosScale` (слот 10, последним) добавлены в оба
  SubGraph правкой JSON. В логе бейка — формат, ошибка и ошибка 8 бит; в инспекторе ассета — стат Positions с подсказкой.
  VatDev перезапечён, всё ушло в 8 бит: лук 1.24 мм (4.76 → 3.17 МБ), Upgrade 1.38 мм (10.37 → 6.91 МБ), вода
  6.31 мм (5.29 → 3.53 МБ), желе 0.70 мм (1.70 → 1.13 МБ). Проверено в Play mode: `compare` на Fire кадр 20 — SMR и
  VAT обоих луков совпадают; `animator` — 100 копий с размахом ассета, переходы идут; `drift` — `_VatDrift` на кадре 80
  (39.92, −0.13, −0.06) = 40 м + min. Удалены сцены `compression`, `compression_water`, папки `Compression`,
  `Shaders` и `VatDevLabel`. 214 тестов зелёные (тесты восстановления сравнивают с заявленной точностью ассета,
  добавлены `VatPositionEncodingTests` и тест min в `Drift`). Плавность глазом и устройства — за пользователем.
- **1.10:** продуктовое решение (вопрос к пользователю): валидатора сборки нет. Ассеты пишет только бейкер, поэтому
  fp32-текстуры и `baseVertex ≠ 0` в ассете не возникают; правила для сцен — в README («Технические требования»).
  Поэтому нет порога вариантов материалов и тестов «правило — фикстура». Записано в §0, §1.2, §1.6, §1.8, §4 и в 1.10.
  Сам решил: память — по фактическим `Texture2D`, а не по раскладке; доля клипа — в строке клипа общей сводки
  (видна и в Result профиля), без отдельного списка; «Out of date» — и в инспекторе ассета, и в Result профиля
  (обновляется по `Model.Changed`, после Bake — через `_context.Refresh`); профиль с проблемами и невалидный ассет
  «устаревшим» не считаются (у ассета старого формата своя ошибка «Rebake it»). `VatClipRow` стал общим
  `VatInfoRow`, CSS `vat-clip*` → `vat-row*`.
  Memory Profiler: `Profiler.GetRuntimeMemorySizeLong` у VAT-текстуры = w×h×байты + 904 Б. В редакторе после любой
  записи в AssetDatabase (`CreateFolder`, `CreateAsset`, `SaveAssets`, в том числе внутри бейка) все загруженные
  текстуры, даже обычный PNG, держат CPU-копию и показывают ×2, пока их не выгрузить (`Resources.UnloadAsset`).
  Выгрузка в конце `VatAssetWriter` не помогает: бейк дальше создаёт материал и сохраняет ассеты, и копия
  появляется снова. Поэтому сверять со сборкой; подсказка об этом — в карточке Memory.
  VatDev: все пять бейков свежие (хэш совпадает в 6000.4.1f1); лук 3.17 МБ, Upgrade 6.91 МБ (padding 1 тексел в
  строке = 1.6 КБ), вода 3.53 МБ (632 Б), желе 1.13 МБ. Смена fps профиля в памяти даёт «Out of date» в обоих
  инспекторах (проверено по дереву UI), возврат убирает. 219 тестов зелёные (добавлены 4 теста памяти и тест «устарел»).
  Сверку с Memory Profiler сборки делает пользователь.
- **1.11:** продуктовые решения (вопросы к пользователю): режим — поле Mode профиля, при неразложимой кости или
  блендшейпе весь ассет печётся в Vertex с сообщением (поклипового отката нет: один меш и один шейдер на ассет);
  ключа `_VAT_BONES_1` нет, выбор 1/2 influences — в 1.12; бленд-граф есть (`vat_lit_bone_blend`), как у Vertex;
  проверка на луке; пивоты — в последней строке bone-текстуры. Записано в §0, §1.4–§1.9, §2.1, §2.3, §4, 1.11, 1.12.
  Отклонения от плана, найденные в работе: (1) индекс кости — TexCoord6 Float16 `(i, 0)`, а не TexCoord4 UNorm16:
  TEXCOORD4 занимает `positionOld` прохода MotionVectors URP (граф с `uv4` не компилировался), а Shader Graph
  объявляет UV в привязках сабграфа `half4` при любой точности — `i/65535` в fp16 путает кости от 1024, целое ≤ 2048
  во Float16 точно (тест `BoneIndex_SurvivesHalfPrecision_ForEveryBone`); (2) пивоты не в строке 0: бленд-шейдер без
  перехода читает `_VatFrameB = 0`, в строке пивотов q = 0, деление на |q|² давало NaN, и меш пропадал (видно было в
  `compare`); теперь клипы со строки 0, пивоты — `_VatLayout.y − 1`, второй тексел строки пивотов — единичный q.
  Сам решил: в HLSL позиция делится на |q|² однородной формулы вместо `normalize(q)` (без ветвлений, то же число
  операций); радиус капсулы — на каждый вертекс; хранятся все `smr.bones` (индексы рига общие, 1.13), кости без
  вертексов не проверяются (при det ≤ 0 пишется q = 1, s = 1); ошибка ассета — восстановление по half-данным против
  скиннинга на одну кость, больше 1 мм — предупреждение в логе; `formatVersion` не менялся (новое поле `_mode` у старых
  ассетов — Vertex), режим в хэше только у Bone, чтобы Vertex-бейки не стали «Out of date»; шаблон с шейдером не того
  режима переключается на шейдер режима с сохранением бленда (лог); сабассет текстуры — `_bone`.
  Сабграфы `vat_bone*` сгенерированы скриптом по образцу `vat_vertex` (UV-нода канала 6, Normal/Tangent Vector в
  Object; скрипт, как и прежние, не сохранён — дальше графы правятся в редакторе SG), `vat_lit_bone*` — копии
  `vat_lit_vertex*` через `CopyAsset` с заменой GUID сабграфа. Unity BakeMesh учитывает `smr.quality` — тест восстановления сверяет с ним напрямую (контроль: Bone4
  отличается больше чем на 10 мм).
  Проверено в Play mode: `compare` на Fire кадр 10 и 20 — «SMR Bone» и «VAT Bone» совпадают на снимке; юнит из
  префаба `bow_bone_vat`: `CrossFade("Fire", 2)` — вес 0.6, `_VatFrameB` пишется, `_VatDrift` нет, меш виден.
  Бейк лука в Bone — 0.3 с. Устройства и плавность глазом — за пользователем. 235 тестов зелёные (16 новых: разложение,
  откат по масштабу и блендшейпу, капсула, восстановление против BakeMesh, раскладка меша и строк, перебейк
  Vertex → Bone, `ApplyFrame` без дрейфа, `VatMath.Rotate`/`BonePoint`/`BoneIndex`, графы `vat_lit_bone*`).
- **1.12:** продуктовые решения (вопросы к пользователю): шейдер один — всегда две кости на вертекс (однокостный из
  1.11 переделан; поля Skin Weights, ключа `_VAT_BONES_*` и отдельных графов нет; имена и GUID `vat_bone*`,
  `vat_lit_bone*` прежние, Custom Function и SubGraph не менялись — поменялся только HLSL); индексы — байты, до 256
  костей. Записано в §0, §1.5–§1.9, §2.1, §2.3, 1.12.
  Отклонения от плана, найденные в работе: (1) выбор влияний — не «два наибольших, при равенстве меньший индекс», а как
  Unity: проба `BakeMesh` (у каждой кости смещение по своей оси, позиция = веса) показала, что Unity берёт первые N
  слотов как записаны, не сортирует, при 2 Bones перенормирует, при 4 — нет; при равных весах решает порядок слотов.
  Для импортированных FBX (веса по убыванию) это и есть два наибольших. Тест «равные веса» сверяет обе раскладки слотов
  и неотсортированные слоты с `BakeMesh`. (2) Вес собирается как `dot(bytes, (256, 1)/65535)`, а не
  `(hi·256 + lo)/65535`: в редакторе (D3D11) у вертексов с весом 1 (тетива, концы плеч) `hi·256 + lo = 65535`
  переполнял half и давал бесконечность — GPU считает выражения от UV сабграфа (`half4` в привязках) в half, хотя
  функция объявлена с `float`. Нашлось только на снимке: CPU-декод данных ассета совпадал с SMR (0.021 мм), опыты
  «вес 1 / вес 0 / вес = старший байт» сузили до склейки веса. Тест `BoneWeight_InHalfArithmetic_StaysFiniteAndClose`
  эмулирует half на каждом шаге (и показывает переполнение старой формулы); на устройствах 16-битный вес в half
  точен до ~1e-3 — это и есть пункт «веса декодируются точно» для пользователя.
  Сам решил: нормаль и тангент кости делятся на |q|² (`VatBoneDirection`), иначе вклад двух костей с разной |q|
  перекашивался бы; вертекс с одним весом — обе кости на ней (те же тексели, кэш); капсула вертекса — объединение
  коробок обеих костей (выпуклая комбинация двух точек лежит в их AABB); формат меша 1.11 (Float16-индекс) не
  выпускался, поэтому `formatVersion` не менялся, `bow_bone` перепечён.
  Проверено в Play mode: `compare` на Fire 10 — «SMR Bone» (2 Bones) и «VAT Bone» совпадают, тетива и концы плеч на
  месте; юнит из `bow_bone_vat` — `CrossFade("Fire", 2)`, вес 0.74, поза смешана, меш целый. 238 тестов зелёные
  (новые: две кости против `BakeMesh` на полоске с весом 0.5 и на риге из трёх костей с равными весами в обоих
  порядках, неотсортированными слотами и рычагом ~1 м; 16-битный вес через half — все 65536 значений, в том числе в
  half-арифметике; индекс-байт через half). Устройства — за пользователем.
- **Аудит «пакет как конструктор» (0.12.1):** решение пользователя — в пакете минимум, удобства и оптимизации поверх
  ядра делает тот, кто его использует. Убраны: dev-проверки `VatPropertyBlockCheck`, `VatBlendCheck`,
  `VatTransitionWarnings` (с ними — `VatMixer.IsInTransition`/`DroppedClip`, `SetWeight` теперь `void`,
  `VatMaterialCopies.Renderers`); `VatAnimator.SetFloat/SetColor/SetVector` (VatDev `VatCrowd` красит через
  `Materials`); тестовый префаб (`VatTestPrefab`, `VatPrefab*` инспектора профиля, поле `_prefab`,
  `VatBaker.CreatePrefab` — тест перебейка собирает префаб сам); перенос `_BaseMap`/`_BumpMap` в новый шаблон.
  У старых профилей в YAML осталось поле `_prefab` — Unity выкинет его при следующем сохранении. Подверсии 1.7,
  1.8 и 1.8.2 в `Docs/plan/subversions` описывают состояние на свой момент; справочник (§1.5, §1.6, §4) поправлен.
  Обсуждено и не делалось: переключение юнита на шейдер без бленда вне перехода (`malioc`, Mali-G57: Forward −19%,
  тени −47%) — забота пользователя пакета; упаковка нормали и тангента Bone в один кватернион (−7…9% Forward) —
  мелко, отложено. Замер `malioc` по Bone: на Mali узкое место — загрузка атрибутов и регистры (64 — половинная
  занятость), не ALU; матрицы в шейдере, строки матриц в текстуре и палитра костей в uniform — хуже текущего.
