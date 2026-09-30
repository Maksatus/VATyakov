# Changelog

## [0.1.0] — подверсия 1.1

### Добавлено
- Пакет и сборки `Kefir.Vat`, `Kefir.Vat.Editor`, `Kefir.Vat.Editor.Tests`.
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
