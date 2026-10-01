# VATyakov

Проект-разработка VAT-системы для Unity 6 и URP — пакета [`com.vatyakov`](Packages/com.vatyakov/README.md).
План реализации: [`Assets/vat-plan-v2.md`](Assets/vat-plan-v2.md). Работа идёт по подверсиям §5 плана,
каждая принимается по своему чек-листу «Как проверить».

| | |
|---|---|
| Редактор | Unity 6000.4.1f1 (минимум пакета — 6000.3) |
| Рендер | URP 17.4, Linear, SRP Batcher, dynamic batching выключен |
| Платформы | iOS Metal, Android Vulkan + GLES3, IL2CPP ARM64 |
| Состояние | подверсия 1.1 — Vertex-режим, позиции, один клип |

## Структура

```
Packages/com.vatyakov/   пакет (встроенный): Runtime, Editor, Shaders, Tests, Samples
Assets/VatDev/            всё для разработки и проверки, в пакет не входит
  Content/Bow/            тестовый контент: лук (SMR 2079 и 4529 вертексов, legacy-клип Fire)
  Bakes/                  профили бейка и результаты (VatAsset, материалы-шаблоны, префабы)
  Scenes/Compare.unity    исходник и VAT бок о бок; эта же сцена собирается на устройства
  Scripts/                VATyakov.Dev: драйвер сцены Compare
Assets/Settings/          URP-ассеты
```

## Сцена Compare

Play. Слева исходный SkinnedMeshRenderer, справа VAT. Режим Step показывает запечённый кадр `k` на обоих:
исходник семплится во время `t_k`, VAT получает `rate = 0, offset = k`. Управление: кнопки на экране, в редакторе
ещё Space (Step/Play) и ←/→ (кадр).

## Тесты

`Window → General → Test Runner → EditMode`, сборка `VATyakov.Editor.Tests`. Тесты создают временные ассеты
в `Assets/__VatTestTemp` и удаляют их за собой.

## Управление редактором из CLI

В проект установлен `com.unity.pipeline` — через него Unity CLI (`unity command …`) управляет открытым
редактором. Пакету нужен `com.unity.inputsystem`: без него версия 0.8.0-exp.1 не компилируется при
Active Input Handling = Input System.
