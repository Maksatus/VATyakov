# Задача: исправить оставшиеся проблемы из аудита VATyakov

Это бриф для нового чата. Пакет — `Packages/com.vatyakov`, сейчас версия 0.16.4 (разделы 1–4 сделаны). Полный аудит с доказательствами —
`Docs/audit_2026-10-07.md` (номера в скобках — его пункты: RT, SH, BK, VX, AB, BN, RG, UI, DC, DT, UX, SM, TS, G, SRC, ED).
Что уже сделано в 0.16.2 — последняя заметка в `Docs/PROGRESS.md`.

## Как работать

- Прочитай `CLAUDE.md`, `Docs/code-style.md`, последнюю заметку `Docs/PROGRESS.md` и статус в начале
  `Docs/audit_2026-10-07.md`. Детали пункта ищи в аудите по его номеру. Перед правкой открой код и проверь, что проблема
  ещё есть: номера строк в аудите могли сдвинуться.
- Unity CLI на этой машине: `--project-path 'Z:\Kefir\VATyakov'` (не `D:\`, как в CLAUDE.md).
- После правки кода: `eval -- --code 'UnityEditor.AssetDatabase.Refresh(); return 1;'`, подождать ~30 с,
  `console_status` (compilationFailed = false), потом `run_tests -- --mode editor --filter VATyakov.Editor.Tests
  --filter_type assembly`. Сейчас 284 теста, все зелёные.
- Отрицательный контроль для каждого нового теста: временно откатить исправление → тест краснеет → вернуть.
  **После возврата файлов обязательно снова Refresh и перекомпиляция**, иначе Play mode и бейки идут на старом коде.
- На каждое исправление — тест, если его можно написать в EditMode. Стиль — строго по CLAUDE.md (без комментариев,
  скобки всегда, `var`, строки ≤ 160, строки через интерполяцию, bool — `Is/Has/Can`, API только из 6000.3).
- В конце: версия `0.16.3` в `package.json`, запись в `CHANGELOG.md`, заметка и карта кода в `PROGRESS.md`, статус в
  начале `Docs/audit_2026-10-07.md`. Если менялся результат бейка — перепечь затронутые профили VatDev
  (`Assets/VatDev/Bakes`) и проверить, что у всех профилей нет проблем и «Out of date».
- Пункты с пометкой **[решение]** — сначала спросить пользователя, не делать молча.
- Не коммитить без просьбы.

## Не трогать без ответа пользователя

- **Loop и шов петли (3.1, 3.2, SH-1, DT-2).** Предложение: убрать поле Loop из профиля; все клипы печь с конечным
  кадром (+1 строка на петлю); петля или нет — из Loop Time клипа (legacy — Wrap Mode, Alembic — совпадение начала и
  конца); `VatClip.Frame` без переноса на кадр 0; удалить `VatRotationSigns.CloseLoop/SeamCount`, `IsSeamShown`,
  `VatBakeLog.Seams`; поднять `formatVersion`, перепечь всё. Это отдельная задача, ждёт решения.
- Крепление доп. мешей к костям (вместо удалённых Extra Renderers) — пользователь придумает сам.
- Сборка префаба юнита в пакете — не делать (решение пользователя).

## 1. Баги (делать первыми)

- [x] **«Out of date» не видит переэкспорт FBX, если Source лежит в префабе (BK-4).** `VatSourceHash.AppendSkinned`
  хэширует только файл, где лежит Source. Добавить `DependencyHash(profile.Source.sharedMesh)` и аватар ближайшего
  Animator над SMR (если есть). Заодно индекс SMR среди `root.GetComponentsInChildren<SkinnedMeshRenderer>(true)`
  (SRC-4: одноимённые соседи). Хэши всех Skinned-бейков один раз сменятся — перепечь VatDev. Тест: сменить
  `sharedMesh` у Source → `IsOutdated`.
- [x] **Speed в инспекторе `VatAnimator` в Play mode не действует (RT-6, UI-5).** `private void OnValidate() {
  _mixer?.SetSpeed(Now, _speed); }`. Поля VAT Asset и Clip в инспекторе выключать в Play mode
  (`VatAnimatorClipController`, `SetEnabled(!EditorApplication.isPlaying)`).
- [x] **Утечка копий материалов у ни разу не активированного объекта (RT-5).** В `VatMaterialCopies.CopyOf`
  `HideFlags.DontSave` → `HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild`. Поправить §7 `00-decisions.md` и
  §1.6 `01-core.md`.
- [x] **[решение] Пауза переживает `Play` и возврат из пула (RT-8).** Решено: так задумано, записано в README и §1.6. Вариант: `Mixer.Resume(Now)` в `OnEnable` перед
  Play On Enable. Либо записать в README, что так задумано.
- [x] **Кость со scale 0 откатывает весь ассет в Vertex (BN-5).** В `VatSimilarity.Of` при почти нулевой матрице
  отдавать Scale = 0, Residual = 0; в `VatBoneEncoder.EncodeBone` брать поворот прошлого кадра этой кости. Тест: кадр
  со scale 0 остаётся в Bone и декодируется в точку.
- [x] **Лишняя проверка остатка в `VatBoneCheck` (BN-4).** Убрать условие `similarity.Residual <= MaxResidual`, решать
  только по ошибке на вертексах (0.5 мм). Это касается и Rigid — прогнать rbd-тесты и перепечь `rbd_*`.
- [x] **«Клип не двигает меш» без предупреждения (VX-4, UX-5).** Если у клипа с FrameCount > 1 все кадры совпадают с
  кадром 0, предупреждение: Humanoid-клип без Humanoid-аватара, чужие пути костей. В Vertex и Bone.
- [x] **Rigid получает текст ошибки топологии от Vertex с «patch 2» (AB-6).** Одна общая подсказка в
  `VatAlembicTopology` для обоих источников («не удаляйте куски — прячьте видимостью»); «UV changed» — отдельный текст
  про анимированные UV. Поправить тест `ChangingTopology_*` в `VatAlembicTests`.
- [x] **Сообщения бейка ведут в тупик (UX-10).** Совет «triplanar template» у Rigid (такого шаблона нет); повтор имени
  клипа — подсказать Mixamo `mixamo.com` и где переименовать; подсказка высоты 4096 — «or Mode = Bone».
- [x] **Констрейнты и Animation Rigging не запекаются молча; Bake в Play mode не заблокирован (G3).** Предупреждение
  валидатора, если под корнем Source есть `IConstraint` или компонент с именем типа `RigBuilder`;
  `VatBaker.Bake` в Play mode → `VatBakeException("Exit Play mode to bake.")`; строка в README.
- [x] **Optimize Game Objects (BK-7, BN-7).** Сначала проверить на FBX с Optimize Game Objects, что `bones` исходника
  пуст. Если да — в `VatBoneRig.Problem` текст «turn off Optimize Game Objects in the Rig tab of the model».
- [x] **Rigid: лимит 2048 недостижим из профиля (RG-4).** В `VatBakeEstimate` для Rigid `Math.Min(PieceCount,
  VatRigidFormat.MaxPieces)`.
- [x] **Rigid: повторное появление куска «прилетает» за интервал (RG-2).** Проход с конца в `VatRigidVisibility`:
  скрытый кадр перед видимым берёт позу следующего видимого. Шов петли — отложить до решения по Loop.
- [x] **Мелочи бейка:** откат Bone → Vertex с ошибкой высоты не называет причину отката (BK-8, обернуть `RunVertex`);
  отмена в прогресс-баре показывает диалог ошибки (BK-9, `OperationCanceledException` и тихий catch в
  `VatBakeDialog`); `profile.Asset` выставлять сразу после записи, `SaveAssets` → `SaveAssetIfDirty` (BK-12).
- [x] **Unlit-сэмпл без Normal и Tangent (SH-4).** В `vat_unlit_vertex.shadergraph` подключить выходы 2 и 3 сабграфа к
  блокам Normal и Tangent (тени с normal bias, DepthNormals).
- [x] **Цвета вертексов и UV1+ теряются молча (BK-6).** Предупреждение в `source.Warnings`, если у меша есть Color или
  TexCoord1–3; строка в README.
- [x] **Alembic (AB-5).** В asmdef `versionDefines` `"[2.4.5,3.0.0)"`; в `VatAlembicSampleTimes.Read` возвращать
  null, если в [0, Duration] нет ни одного времени.
- [x] **Нет тестов на исправления 0.16.2 (SRC-12):** зеркальная нода Alembic (`VatSourceWinding.Oriented` в
  `VatAlembicReader`), Animator на ребёнке-модели под корнем без Animator.

## 2. Инспекторы и удобство

- [x] Инспектор материала: при нескольких материалах ложный текст «shown for one»; на копиях в Play mode во время
  перехода мигает «stale» — ранний выход для `!EditorUtility.IsPersistent(material)` (UI-12).
- [x] Текст NotAssigned в инспекторе материала советует сделать материал шаблоном профиля (украдёт шаблон) —
  «Duplicate a baked template material…» (UI-11).
- [x] `VatAnimator`: `[CanEditMultipleObjects]` и popup Clip с `bindingPath = "_clip"` (Undo, prefab override) (UI-6).
- [x] Инспектор ассета: строка Padding у Bone и Rigid всегда 0 — прятать при 0 (UI-14).
- [x] Кэш `VatAlembicProbe` на одну запись — словарь по `GameObject` с той же проверкой хэша (UI-4).
- [x] **[решение]** Память в инспекторе считает только текстуры, у Bone и Rigid меш — 58–92% ассета (G8): строка Mesh
  (`VatMemory.Bytes(Mesh)`), поправить «в сотни раз» в README и `budgets.md`.
- [x] **[решение]** Mode по умолчанию для Skinned — Bone (UX-7). Отклонено: остаётся Vertex.
- [x] **[решение]** Поле Shader профиля действует только на первом бейке (BK-10, UX-8): удалить, новый шаблон —
  `DefaultName(mode, false)`.
- [x] **[решение]** Пакетный бейк выделенных профилей через пункт меню (UI-7).

## 3. Возможности — только по решению пользователя

- [x] **[решение]** События `AnimationClip` теряются при бейке (G5): `VatClipEvent[]` в `VatClip` и событие рядом с
  `VatEndLatch`. Минимум — предупреждение бейка и строка в README.
- [x] **[решение]** Толпа, включённая в одном кадре, шагает в ногу (G4): поле Random Start у `VatAnimator` или Решено: не делаем, на пользователях.
  строка в README. Решено: не делаем, на пользователях (минимальный функционал).
- [x] **[решение]** У Bone нет проверки ошибки между кадрами (BN-6): проход по серединам интервалов, или поправить §2.1.
- [x] **[решение]** Rigid Deform без масштаба (RG-1): минимум — подсказка «Packed Transform = Transform Geometry»
  вместо «use Mode = Vertex».

## 4. Упрощения (поведение не меняется)

- [x] Мёртвое: `VatBakeProfile.IsBaked`, `VatUi.SecondaryButton` и `.vat-secondary-button`, `VatBoneFormat.Strides`,
  `internal set` у `VatAnimator.Asset`, `VatPlayback.Frame`, вторая перегрузка `VatTexture.Create`, мёртвый
  `catch (ArgumentOutOfRangeException)` и обёртка `VatLayout.FrameCount` (BK-11, SM-5, SM-6, RT-11).
- [x] `VatRigidMeshBuilder` → generic `VatBoneMeshBuilder.Build<T>` (SM-3).
- [x] `VatAnimatorFieldsController` → `VatBoundFieldsController<T>(SerializedObject)` (SM-4). Не нужно: с 0.16.3 в нём Speed в Play mode.
- [x] `VatPositions` без своих копий rest и drift (VX-9).
- [x] `VatRigidEncoder` без `_offsetScales`/`_rotations` и публичного `Point` (SM-7).
- [x] Дубли Bounds и Centroid (SM-10); общий каркас открытия .abc в двух источниках (AB-7).
- [x] 8-битная ошибка: таблица `Sample` на 256 значений вместо `Mathf.FloatToHalf` (VX-6).
- [ ] Не делать: слияние писателей текселей (SM-9), удаление слоёв каркаса UI (правило CLAUDE.md).

## 5. Тесты

- [ ] HLSL ни разу не исполняется (TS-1): compute-зонд в `Tests/Editor/Fixtures`, который зовёт функции
  `VatShaderGraph.hlsl` на текстурах in-memory бейков и сверяет с `VatMath`/`VatBoneDecoder` при frac ≠ 0, w ≠ 0.
- [ ] Shader Graph: `Assert.Inconclusive` → `Assert.Fail` в `RequireGeneratedCode` (TS-4); JSON-тест пяти
  `.shadersubgraph` на `m_Precision` и `hlslDeclarationOverride` (SH-2); тип `VertexDescription.Position` в Half-родителе
  (SH-3); компиляция под GLES3, Vulkan и Metal через `ShaderData.Pass.CompileVariant` (SH-6).
- [ ] Константы `VatCore.hlsl` против `VatMath` регэкспом; `PieceTexel` в half-арифметике (SH-7, DT-5).
- [ ] Абсолютная граница ошибки 8 бит `|Size|·(0.5/255 + 1/4096)` (TS-3); граница 4096/4097 вместо дубля теста (TS-7);
  пустые проверки (TS-8); баунды Rigid (TS-9); блендшейп в Vertex против `BakeMesh` (TS-6); foot IK (TS-11);
  `CreateFolder` без `IsValidFolder` (TS-10).
- [ ] **[решение]** `VatAnimator` в Play mode (TS-2): `[UnityTest]` с `EnterPlayMode` — в проекте включена перезагрузка
  домена, тест может быть хрупким.

## 6. Документы

- [x] `01-core.md` §1.8: «рантайм отказывается играть чужую formatVersion» — не так, проверяют только инспекторы
  (SH-8, DT-7).
- [ ] `02-modes.md` §2.1: убрать `_VAT_BONES_4`; §2.3: убрать `+1e-6·n`; §2.1: у Bone нет FPS-предупреждения (DC-11).
- [ ] `00-decisions.md` §0: масштаб «до ~100» → 100–500, цель Redmi 9A (DC-11).
- [ ] README: быстрый старт (git URL с `?path=/Packages/com.vatyakov`, профиль, Bake, сборка юнита, `*_blend` для
  переходов, пример кода); минимум «Adreno 650 / Mali-G78» против целевого Redmi 9A; «невалидный ассет падает
  исключением» — не так; запрет Static объяснён через Vertex ID, а Bone и Rigid его не используют; раздел «Свой шейдер»
  (сабграф не трогать, свойства без префикса `_Vat`, Custom Editor `VatShaderGUI`) (UX-6, G6).
- [x] Ошибка Rigid ссылается на «the package README», которого нет — «README of the repository».

## Вне чата

- Замер `stress` на Redmi 9A в Release (render thread при копии материала на юнита) — делает пользователь. С 0.16.2 у
  VAT-воина одна копия и нет LOD, сравнение с SMR неравное (у SMR LOD и топор).
