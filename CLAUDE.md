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
  (компилирует файл целиком, без domain reload, файл может лежать вне Assets). Из PowerShell 5.1 код с двойными
  кавычками ломает разбор аргументов `eval --code` — такой код класть в файл и звать `eval_file`.
- Тесты: `run_tests -- --mode editor --filter VATyakov.Editor.Tests --filter_type assembly` (около 15 с).
- Пакеты ставить через `package_add -- --identifier name@version --confirm true` / `package_remove -- --name name
  --confirm true` / `package_resolve`: после ручной правки `manifest.json` Unity ждёт фокуса окна. На перезагрузке
  домена CLI теряет связь («Network error») — итог смотреть в `package_status`, ошибки сборки — в `console_status`.
- Без фокуса редактор может не тикать — `set_autotick`. Play mode без фокуса стоит на месте, пока в рантайме
  не выставить `Application.runInBackground = true` (в настройки проекта не писать: выставлять уже в Play mode,
  в edit mode это `PlayerSettings.runInBackground`).
- Перед прогоном тестов активная сцена должна быть сохранённым ассетом, иначе Test Runner падает на ReloadScene.
- `capture_game_view --save_path` пишет относительно `Assets/`; снимки удалять за собой.
- GPU-скиннинг SMR пересчитывается раз за кадр игрового цикла, не на каждый `Camera.Render()`: для сравнения
  с VAT сначала выставить кадр, дождаться реального кадра, потом рендерить.
- `com.unity.pipeline` 0.8.0-exp.1 не компилируется без `com.unity.inputsystem`
  (Active Input Handling = Input System).

## Соглашения

- Полное руководство по коду с примерами из `D:\client` — `Docs/code-style.md`; перед написанием кода читать его.
- Стиль кода — как в `D:\client\Assets\Scripts\Game` и его `D:\client\AGENTS.md`, правила для Rider — `.editorconfig`
  в корне репозитория:
  - явные модификаторы доступа у всех типов и членов, кроме членов интерфейсов; порядок `private static readonly`;
  - приватные поля, в том числе `static readonly`, — `_camelCase`; константы и не приватные члены — PascalCase; bool — `Is/Has`;
  - фигурные скобки у `if`/`else`/`for`/`foreach`/`while`/`using` всегда, даже для одной строки; одна строка — один оператор;
  - `var` для локальных; `new()` для полей, когда тип уже написан слева;
  - методы и конструкторы — с блочным телом, свойства — через `=>`; простые вызовы и инициализаторы — в одну строку;
  - атрибуты полей — каждый на своей строке, `[SerializeField]` последним;
  - порядок в классе: константы, статические поля, поля, события, свойства, конструктор, методы (сначала публичные), вложенные
    типы; в контроллерах `Deactivate()` перед `Activate()`;
  - пустая строка после закрывающей `}` блока; однострочные свойства одного доступа идут без пустых строк;
  - один тип на файл; `using` вне namespace — сначала `System`, потом по алфавиту, алиасы в конце; короткие имена типов
    вместо полных (`UnityEditor.Editor` — исключение: конфликтует с namespace `VATyakov.Editor`).
- Комментариев в коде нет: ни пересказа, ни ссылок на план, ни XML-summary. Неочевидные факты — в `Docs/plan` и `PROGRESS.md`.
- Классы и методы маленькие, логика разнесена по классам.
- Редакторный UI — контроллеры по образцу `D:\client\Assets\Editor\AssetsIntegrations\Vehicle`: `IVatController`
  (Activate/Deactivate) на каждую часть, `VatEditorContainer` только строит элементы, общее состояние — Context с
  Model (`VatProperty<T>`, `VatTrigger`), инспектор (`VatControllerInspector.CreateControllers`) только регистрирует
  контроллеры. Каркас свой, в `Editor/Framework`, без зависимостей от реестра Kefir. IMGUI `ShaderGUI` — `VatShaderGUI`
  рисует по порядку Surface, Animation и Render Queue (`VatSurfaceFields`, `VatAnimationFields`), без реестра секций.
- Весь текст в пакете только на английском: комментарии, инспекторы, тултипы, ошибки бейка, лог. Подписи — термины Unity
  (Skinned Mesh Renderer, Clip, Loop), а не пересказ («Персонаж»). Документация в `Docs/` — на русском.
- Культура редактора ru-RU: числа в строки только через `CultureInfo.InvariantCulture`.
- Пакет обязан компилироваться на 6000.3: не использовать API из 6000.4+ (например `GetEntityId`,
  `FindObjectsByType` без `FindObjectsSortMode`).
- `VatMath.cs` — CPU-зеркало `VatCore.hlsl`: менять оба файла вместе.
- Шейдер для слабых телефонов, максимально простой: без времени, ветвлений, клампов и проверок, без `if` и `?:`
  (только арифметика). Время, loop/one-shot, скорость и
  переходы считает CPU (`VatPlayback`, `VatClip.Frame`) и пишет готовые строки в `_VatFrame`; валидность
  данных обеспечивает бейкер.
- Shader Graph ассеты (`vat_vertex`, `vat_unlit_vertex`, `vat_lit_vertex`, фикстура `vat_half_parent`) сгенерированы скриптом,
  дальше их правят в редакторе SG. Бленд-версии (`vat_vertex_blend`, `vat_lit_vertex_blend`, `vat_half_parent_blend`, 1.8.2) —
  копии через `AssetDatabase.CopyAsset` (новый GUID) с правкой JSON: имена функций `*Blend`, GUID сабграфа и
  `promotedFromAssetID`. Шейдер с блендом и без — разные шейдеры, keyword нет. Векторные свойства SubGraph — Precision
  Single: с Inherit в Half-графе они объявляются `half4` и портят строки и W > 2048 (это проверяет тест). В HLSL —
  только `_float`-обёртки. Новый вход Custom Function добавлять последним (как `Drift`): Shader Graph сопоставляет слоты по id.
- MaterialPropertyBlock запрещён, копии материалов — только в Play mode (§1.6).
- Контент — файлы ассетов (модели, текстуры, материалы, префабы, сцены, профили и результаты бейка) — маленькими
  буквами через `_`: `bow_default_vat.prefab`. Папки и код (скрипты, шейдеры, asmdef) — PascalCase, как в `D:\client`.
  Бейкер называет результат `<профиль>_vat` (ассет, материал, префаб), сабассеты — `_mesh`, `_pos`, `_rot` (дрейф — массив в самом ассете, 1.8.1).
  Переименовывать через Unity (`AssetDatabase.RenameAsset`), а в git смену одного регистра фиксировать заново
  (`git rm --cached` + `git add`): `core.ignorecase = true` её не видит.

## Где что

- `Assets/VatDev/Content/Bow/weapon_landing_bow.fbx` — тестовый лук: меши `Bow_default_main` на 2079 вертексов,
  `Bow_upgrade_main` на 4529 (два блока), три legacy-клипа: VAT, Fire, BakeSave.
- `Assets/VatDev/Content/Alembic/water.abc` — жидкость с постоянной топологией (1.4), сравнение — сцена
  `compare_alembic` (не в сборке). Без `com.unity.formats.alembic` Alembic-код пакета и VatDev выключен (`VAT_ALEMBIC`).
- `Assets/VatDev/Bakes` — профили бейка и результаты. `Assets/VatDev/Scenes/compare.unity` — сравнение SMR и VAT,
  она же первая сцена сборки; вторая — `animator`, толпа из 100 `VatAnimator` (1.7) на шаблоне с блендом
  (`vat_lit_vertex_blend`, 1.8.2).
