# Changelog

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
