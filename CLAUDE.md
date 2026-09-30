# VATyakov — заметки для агента

Проект-разработка пакета `Packages/com.kefir.vat` (VAT для Unity 6 и URP). План — `Assets/vat-plan-v2.md`.
Работа идёт по подверсиям §5 («реализуй подверсию 1.N, следующие не трогай»). Подверсия принимается по её
разделам «Как проверить» и «Тесты».

## Управление редактором: Unity CLI и com.unity.pipeline

- Команда: `unity command <name> --project-path 'D:\Kefir\VATyakov' --no-banner --format json -- --param value`.
  Параметры команды идут только после `--`. `--timeout N` до `--` — таймаут самого CLI в секундах,
  после `--` — параметр команды.
- C#: `eval --code` (тело метода, без `using`), `eval_file --file`, `run_script --file X.cs --entry Type.Method`
  (компилирует файл целиком, без domain reload, файл может лежать вне Assets).
- Тесты: `run_tests -- --mode editor --filter Kefir.Vat.Editor.Tests --filter_type assembly` (около 15 с).
- Пакеты ставить через `package_add` / `package_remove` / `package_resolve`: после ручной правки
  `manifest.json` Unity ждёт фокуса окна.
- Без фокуса редактор может не тикать — `set_autotick`. Play mode без фокуса стоит на месте, пока в рантайме
  не выставить `Application.runInBackground = true` (в настройки проекта не писать).
- Перед прогоном тестов активная сцена должна быть сохранённым ассетом, иначе Test Runner падает на ReloadScene.
- `capture_game_view --save_path` пишет относительно `Assets/`; снимки удалять за собой.
- GPU-скиннинг SMR пересчитывается раз за кадр игрового цикла, не на каждый `Camera.Render()`: для сравнения
  с VAT сначала выставить кадр, дождаться реального кадра, потом рендерить.
- `com.unity.pipeline` 0.8.0-exp.1 не компилируется без `com.unity.inputsystem`
  (Active Input Handling = Input System).

## Соглашения

- Комментарии в коде на английском, сообщения пользователю (ошибки бейка, инспектор, лог) на русском.
- Приватные поля `_camelCase`, ссылки на план в комментариях — `§1.3`.
- Культура редактора ru-RU: числа в строки только через `CultureInfo.InvariantCulture`.
- Пакет обязан компилироваться на 6000.3: не использовать API из 6000.4+ (например `GetEntityId`,
  `FindObjectsByType` без `FindObjectsSortMode`).
- `VatMath.cs` — CPU-зеркало `VatCore.hlsl`: менять оба файла вместе.
- Shader Graph ассеты (`VAT_Vertex`, `VAT_Unlit_Vertex`, фикстура `VAT_HalfParent`) сгенерированы скриптом,
  дальше их правят в редакторе SG. Векторные свойства SubGraph — Precision Single: с Inherit в Half-графе они
  объявляются `half4` и портят упакованный клип и W > 2048 (это проверяет тест).
- MaterialPropertyBlock запрещён, копии материалов — только в Play mode (§1.6).

## Где что

- `Assets/VatDev/Content/Bow` — тестовый лук: `Bow_default_main` на 2079 вертексов, `Bow_upgrade_main` на 4529
  (два блока), legacy-клип.
- `Assets/VatDev/Bakes` — профили бейка и результаты. `Assets/VatDev/Scenes/Compare.unity` — сравнение SMR и VAT,
  она же единственная сцена сборки.
