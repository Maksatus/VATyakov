# VATyakov — заметки для агента

Проект-разработка пакета `Packages/com.vatyakov` (VAT для Unity 6 и URP). План — `Docs/plan/` (оглавление —
`Docs/plan/README.md`), что сделано и где лежит код — `Docs/PROGRESS.md`.
Работа идёт по подверсиям: команда `/vat 1.N` или «реализуй подверсию 1.N, следующие не трогай». Подверсия
принимается по её разделам «Как проверить» и «Тесты».

## Экономия контекста

- В начале читать только `Docs/PROGRESS.md`, `Docs/plan/00-decisions.md` и `Docs/plan/subversions/1.N.md`;
  из справочника — разделы, на которые ссылается подверсия. Весь план и весь пакет не читать.
- Код искать по карте из `PROGRESS.md`, открывать точечно. `Library`, `Temp`, `Logs`, csproj и sln закрыты в
  `.claude/settings.json` — не обходить их.
- В конце подверсии обновить `PROGRESS.md` (статус, карта кода, заметки), чтобы следующему чату не пришлось
  разбирать код заново.

## Управление редактором: Unity CLI и com.unity.pipeline

- Команда: `unity command <name> --project-path 'D:\Kefir\VATyakov' --no-banner --format json -- --param value`.
  Параметры команды идут только после `--`. `--timeout N` до `--` — таймаут самого CLI в секундах,
  после `--` — параметр команды.
- C#: `eval --code` (тело метода, без `using`), `eval_file --file`, `run_script --file X.cs --entry Type.Method`
  (компилирует файл целиком, без domain reload, файл может лежать вне Assets).
- Тесты: `run_tests -- --mode editor --filter VATyakov.Editor.Tests --filter_type assembly` (около 15 с).
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

- Классы и методы максимально маленькие, логика разнесена по классам. XML-summary не пишем; комментарии — только
  неочевидное «почему» и ссылки на план, нужные для следующих подверсий.
- Редакторный UI — контроллеры по образцу `D:\client\Assets\Editor\AssetsIntegrations\Vehicle`: `IController`
  (Activate/Deactivate) на каждую часть, `EditorContainer` только строит элементы, общее состояние — Context с
  Model (`Property<T>`, `Trigger`), инспектор (`ControllerInspector.CreateControllers`) только регистрирует
  контроллеры. Каркас свой, в `Editor/Framework`, без зависимостей от реестра Kefir. IMGUI `ShaderGUI` — реестр
  секций `IVatMaterialSection`.
- Комментарии в коде на английском, сообщения пользователю (ошибки бейка, инспектор, лог) на русском.
- Приватные поля `_camelCase`, ссылки на план в комментариях — `§1.3`.
- Культура редактора ru-RU: числа в строки только через `CultureInfo.InvariantCulture`.
- Пакет обязан компилироваться на 6000.3: не использовать API из 6000.4+ (например `GetEntityId`,
  `FindObjectsByType` без `FindObjectsSortMode`).
- `VatMath.cs` — CPU-зеркало `VatCore.hlsl`: менять оба файла вместе.
- Шейдер максимально простой: без времени, ветвлений, клампов и проверок. Время, loop/one-shot, скорость и
  переходы считает CPU (`VatPlayback`, `VatClip.Frame`) и пишет готовые строки в `_VatFrame`; валидность
  данных обеспечивает бейкер.
- Shader Graph ассеты (`VAT_Vertex`, `VAT_Unlit_Vertex`, фикстура `VAT_HalfParent`) сгенерированы скриптом,
  дальше их правят в редакторе SG. Векторные свойства SubGraph — Precision Single: с Inherit в Half-графе они
  объявляются `half4` и портят строки и W > 2048 (это проверяет тест).
- MaterialPropertyBlock запрещён, копии материалов — только в Play mode (§1.6).

## Где что

- `Assets/VatDev/Content/Bow` — тестовый лук: `Bow_default_main` на 2079 вертексов, `Bow_upgrade_main` на 4529
  (два блока), legacy-клип.
- `Assets/VatDev/Bakes` — профили бейка и результаты. `Assets/VatDev/Scenes/Compare.unity` — сравнение SMR и VAT,
  она же единственная сцена сборки.
