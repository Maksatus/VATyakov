# Changelog

## [0.5.0] — подверсия 1.5

### Добавлено
- `_VatDriftTex` RGBAHalf 2 × ΣF без блоков: смещение центроида кадра от покоя парой half, `d = hi + lo` (§1.2, §1.9).
  Шейдер: `pos = rest + lerp(d0, d1) + lerp(Δ0, Δ1)`, в Vertex-режиме 4 выборки дрейфа на вертекс.
- Автовключение дрейфа: энкодер за один проход пишет позиции с дрейфом и без и после последнего кадра выбирает по
  `VatDriftPolicy` — центроид ушёл дальше 2 м или ошибка half без дрейфа больше 1 мм. Флаг — `driftOn` в `_VatLayout.z`.
- `VatPrecision` в `VatAsset`: насколько уходит центроид, ошибка с дрейфом и без. Инспектор VAT-ассета — карточка
  Precision; лог бейка — обе ошибки.
- Ссылка Drift Texture в инспекторе ассета; `_VatDriftTex` в стейл-проверке материала.

### Изменено
- `formatVersion` 3: ассеты 0.4 нужно перезапечь (рантайм откажется их играть с понятной ошибкой).
- `_VatDriftTex` есть в каждом Vertex-ассете: без дрейфа там нули (16 Б на кадр), у шейдера нет ветки driftOn.
- `VatLayoutInfo`: формат текстуры дрейфа; память в инспекторе и оценке учитывает её.
- `VatVertexPosition_float`: новый последний вход `DriftTex`, у SubGraph `VAT_Vertex` — свойство `_VatDriftTex`.

## [0.4.0] — подверсия 1.4

### Добавлено
- Alembic-источник: в профиле бейка поле Source — Skinned Mesh Renderer или Alembic (.abc из проекта). Запекается весь
  Time Range импортёра; кадр k — `UpdateImmediately(k / fps)` от StartTime, корень плеера в identity, все меш-ноды
  .abc — один меш в пространстве корня, анимация xform-нод учитывается.
- Сборка `VATyakov.Editor.Alembic`: компилируется только с `com.unity.formats.alembic` ≥ 2.4.5 (`VAT_ALEMBIC`);
  без пакета Alembic-источник выключен с понятным сообщением, остальное работает.
- Проверки Alembic на каждом кадре бейка: число вертексов, хэши индексов и UV, разбиение на сабмеши. Расхождение —
  ошибка «меняющаяся топология — патч 2». `Duration ≤ 0` или нет мешей — ошибка бейка.
- Предупреждение, если вертекс за кадр сдвинулся больше чем на половину размера меша (сдвиг всего меша вычитается):
  так выглядит нестабильный порядок точек в экспорте. Меш без UV — подсказка про триплэнарный шаблон.
- Подсказка loop в инспекторе профиля: насколько конец .abc отличается от начала и подходит ли флаг Loop.
- Alembic без нормалей — кадр от `(0, 0, 1)`; без тангентов (меш без UV) — тангент по правилу вырожденного (§2.2):
  базис от N в кадре 0 и перенос по кадрам.
- Пример `VAT_Lit_Vertex_Triplanar` (`VATyakov/VAT_Lit_Vertex_Triplanar`): BaseMap и normal map триплэнарно в
  мировых координатах, свойство `_Tile`.

### Изменено
- Весь текст пакета на английском: инспекторы профиля, ассета и материала, тултипы, ошибки и лог бейка.
- `IVatFrameSource.Warnings`, `VatBakeResult.Warnings`: предупреждения источника выводятся в лог бейка.

## [0.3.0] — подверсия 1.3

### Добавлено
- `_VatRotTex` RGBA8: кадр (T, N×T, N) каждого вертекса кватернионом smallest-three 2 + 10 + 10 + 10 бит (§1.9).
  Шейдер декодирует байты, выравнивает знак и делает nlerp между двумя строками кадров.
- Нормали и тангенты по кадрам: скиннутый тангент из `BakeMesh`, Грам–Шмидт к нормали, перенос с прошлого кадра
  при вырожденных UV, детерминированный базис от N в кадре 0, нормализация нормалей после `BakeMesh`.
- Предупреждение бейка, если знак бинормали в кадрах не совпадает с покоем; в логе — max ошибка поворота и число
  вырожденных тангентов.
- `VatVertexNormalTangent_float`, выходы Normal и Tangent у SubGraph `VAT_Vertex`.
- Пример `VAT_Lit_Vertex` с normal map — теперь шейдер материала-шаблона по умолчанию; в новый шаблон копируются
  `_BaseMap` и `_BumpMap` исходного материала.
- `VatMath`: CPU-зеркало декодера (`RotationBytes`, `RotationFields`, `DecodeRotation`, `Nlerp`, `FrameNormal`, `FrameTangent`).

### Изменено
- `VatAsset.CurrentFormatVersion` = 2: ассеты 0.2.0 нужно перезапечь. `VatAsset.RotationTexture`,
  `VatLayoutInfo.RotationFormat`, `VatShaderIds.RotTex`.
- Нормаль и тангент в меше — ортонормированный кадр 0; знак бинормали по-прежнему из исходного `tangent.w`.
- Память в инспекторе и логе — сумма обеих текстур.

## [0.2.0] — подверсия 1.2

### Изменено
- Шейдер больше не считает время: CPU каждый кадр пишет `_VatFrame = (row0, row1, frac, 0)`, а
  `VatVertexPosition` делает только адрес тексела, две выборки `_VatPosTex` и lerp. Time-нода, упаковка клипа,
  ветвления и клампы из шейдера убраны; вход `Time` SubGraph `VAT_Vertex` удалён, `_VatClipA` заменён на `_VatFrame`.
- `VatMath` — только адресация (зеркало `VatCore.hlsl`); `PackClip`, `UnpackClip`, `LoopFrame` удалены.
  `LoopFrameCount`, `LoopFrameRate`, `LoopFrameTime` перенесены в `VatTiming`.

### Добавлено
- Интерполяция кадров и плавный шов цикла.
- One-shot клипы: флаг «Цикл» в профиле бейка, `F = round(L·fps) + 1`, последний кадр — конец клипа.
- `VatClip.Frame(position)` и `VatClip.Wrap` — loop и one-shot на CPU в double.
- `VatPlayback`: якорь времени, скорость, пауза (0), обратное проигрывание, смена скорости без рывка.
- Инспектор VAT-материала: поле «Кадр» — статичный кадр шаблона в edit mode.

### Исправлено
- Последний кадр one-shot у зацикленного исходного клипа (loopTime, legacy `WrapMode.Loop`) больше не
  заворачивается в позу кадра 0.
- `L·fps` за пределами int больше не превращается в один кадр, а падает на лимите высоты.

## [0.1.0] — подверсия 1.1

### Добавлено
- Пакет и сборки `VATyakov`, `VATyakov.Editor`, `VATyakov.Editor.Tests`.
- `VatBakeProfile`: SkinnedMeshRenderer из префаба или модели, один клип, fps.
- `SkinnedFrameSource`: копия для бейка в preview-сцене, PlayableGraph без foot IK и root motion
  (Generic, Humanoid), `SampleAnimation` для legacy-клипов, `BakeMesh`, перевод в пространство корня префаба.
- `VertexEncoder`: `_VatPosTex` RGBAHalf со смещениями от покоя; меш с финальной раскладкой
  (поток 0 — позиция, поток 1 — нормаль, тангент, UV в Float16), `baseVertex = 0`, баунды по всем кадрам.
- `VatAssetWriter`: `.asset` с `[PreferBinarySerialization]`, повторный бейк на месте через `CopySerialized`,
  `formatVersion`, хэш исходника.
- `VatCore.hlsl`: адресация с блоками ширины, упаковка клипа, кламп кадра loop; `VatShaderGraph.hlsl`
  с обёртками `_float`; SubGraph `VAT_Vertex` (Precision Single).
- Пример `VAT_Unlit_Vertex`.
