# Kefir VAT (`com.kefir.vat`)

Vertex Animation Textures для Unity 6000.3+ и URP: анимация SkinnedMeshRenderer запекается в текстуры
и проигрывается в Shader Graph. Полный план — `Assets/vat-plan-v2.md` проекта-разработки.

Текущее состояние — **подверсия 1.1**: Vertex-режим, только позиции, один loop-клип, ближайший кадр.

## Бейк

1. `Create → Kefir → VAT Bake Profile`.
2. **Source** — SkinnedMeshRenderer внутри префаба или модели (перетащите дочерний объект модели из Project).
   Позиции пишутся в пространстве корня префаба.
3. **Clip** и **Fps**. Loop-клип получает `F = max(1, round(L·fps))` кадров.
4. **Bake**. Результат лежит рядом с профилем:
   - `<Профиль>_Vat.asset` — `VatAsset` (бинарная сериализация) с sub-assets: меш и `_VatPosTex` (RGBAHalf);
   - `<Профиль>_Vat.mat` — материал-шаблон (по умолчанию шейдер `Kefir/VAT/VAT_Unlit_Vertex`);
   - `<Профиль>_Vat.prefab` — MeshFilter + MeshRenderer (создаётся один раз).

Повторный бейк обновляет те же объекты: ссылки из материалов и префабов сохраняются, даже если меняется размер
текстуры.

## Свой шейдер

Добавьте в Shader Graph SubGraph `VAT_Vertex` (`Packages/com.kefir.vat/Shaders/SubGraphs`) и подключите его выход
к Position в Vertex-контексте. Свойства `_VatPosTex`, `_VatLayout`, `_VatClipA` SubGraph сам поднимает в
итоговый шейдер (Promote to final Shader).

Ограничения, которые важно не нарушать (§7 плана):
- VAT-меш не должен быть Static и не должен попадать в static/dynamic batching: индекс берётся из Vertex ID,
  `baseVertex = 0` на всех сабмешах;
- `MaterialPropertyBlock` запрещён, `renderer.material` не использовать;
- Enable GPU Instancing на VAT-материалах выключен;
- после бейка вершины меша не переставлять (никакого `Mesh.Optimize`).

## Структура

```
Runtime/   VatAsset, VatClip, VatLayoutInfo, VatMath (CPU-зеркало VatCore.hlsl), VatShaderIds
Shaders/   VatCore.hlsl (адресация, клип, кадр), VatShaderGraph.hlsl (обёртки _float), SubGraphs/VAT_Vertex
Editor/    VatBakeProfile(+Editor), Baking/: IVatFrameSource, SkinnedFrameSource, VatLayout, VertexEncoder,
           VatAssetWriter, VatBaker
Tests/     EditMode-тесты (Kefir.Vat.Editor.Tests)
Samples/   VAT_Unlit_Vertex — пример шейдера
```

`Samples/` на время разработки видима Unity. Перед релизом папку нужно переименовать в `Samples~`
и прописать сэмплы в `package.json`.
