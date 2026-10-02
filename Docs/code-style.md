# Как писать и оформлять код VATyakov

Образец — игровой проект `D:\client`, куда пойдёт пакет. Правила оттуда:

- `D:\client\AGENTS.md` — правила команды;
- `D:\client\.agents\skills\truegta-client-architecture`, `truegta-reactive-lifecycle`, `truegta-unity-assets` —
  архитектура, жизненный цикл контроллеров, работа с ассетами;
- `D:\client\.editorconfig` — форматирование и именование.

Наш `.editorconfig` в корне репозитория — их выжимка, Rider проверяет по нему. Где код клиента расходится со своими
же правилами, берём правило, а не большинство (раздел 11). Пишем так же или строже, но не свободнее.

## 1. Архитектура

| Клиент | VATyakov |
|---|---|
| Model — обычный C#-класс с `Property<T>`, `Trigger` | `VatProfileModel` (`Editor/Profile`), `Property<T>`, `Trigger` в `Editor/Framework` |
| Controller (`IController`) связывает модель и вид | `*Controller` в `Editor/*/Controllers` |
| Container — пассивный MonoBehaviour со ссылками | `*Container : EditorContainer` только строит элементы UI |
| Generator / `ControllersGroupController.CreateControllers()` регистрирует контроллеры | `ControllerInspector.CreateControllers()` в инспекторах |
| Context — общее состояние | `VatProfileContext` |

Правила контроллеров (из `truegta-reactive-lifecycle`):

- конструктор только сохраняет зависимости: никаких подписок, запросов и изменений вида;
- в параметрах конструктора сначала контекст, потом модели, последним — контейнер или родительский `VisualElement`;
- контроллер получает контекст, даже если пока его не использует;
- в файле `Deactivate()` стоит перед `Activate()`;
- каждый `+=` делается в `Activate()`, парный `-=` — в `Deactivate()`; подписка в `Activate()` безусловная;
- одна ответственность на контроллер, публичных методов кроме `Activate`/`Deactivate` нет; общаются через модель;
- для `Property<T>.Changed` не нужна проверка «старое == новое»: событие и так приходит только при изменении.

Пример клиента (`Game/DiggingEvents/Dialog/DiggingEventDialogTimerController.cs`):

```csharp
public class DiggingEventDialogTimerController : IController
{
    private const long UpdatePeriod = 1000;

    private readonly ILocationContext _context;
    private readonly DiggingEventDialogComponent _component;

    private ITimerTextModel _timerModel;

    public DiggingEventDialogTimerController(ILocationContext context, DiggingEventDialogComponent component)
    {
        _context = context;
        _component = component;
    }

    public void Deactivate()
    {
        _timerModel.Stop();
    }

    public void Activate()
    {
        _timerModel = _context.TimerModel.AddCountdownText(...);
    }
}
```

У нас так же (`Editor/Profile/Controllers/VatEstimateController.cs`):

```csharp
internal sealed class VatEstimateController : IController
{
    private readonly VatProfileContext _context;
    private readonly VatEstimateContainer _container;

    public VatEstimateController(VatProfileContext context, VisualElement parent)
    {
        _context = context;
        _container = parent.CreateContainer<VatEstimateContainer>();
    }

    public void Deactivate()
    {
        _context.Model.Changed.OnCall -= Refresh;
        _container.DestroyView();
    }

    public void Activate()
    {
        _context.Model.Changed.OnCall += Refresh;
        Refresh();
    }

    private void Refresh()
    {
        ...
    }
}
```

Модели — обычные классы, не MonoBehaviour. Неизменяемые данные — `readonly` поля или свойства только для чтения, все
зависимости — через один конструктор. В рантайме (`VatAnimator`, `VatPlayer`, `VatPlayback`) — маленькие классы с одной
задачей, время передаётся параметром, чтобы их можно было тестировать в EditMode.

## 2. Файл

- Один тип на файл, имя файла совпадает с именем типа. Вложенные приватные типы допустимы (в тестах — `Run` в
  `VatDriftTests`).
- Namespace блочный и повторяет сборку: `VATyakov`, `VATyakov.Editor`, `VATyakov.Tests`, `VATyakov.Dev`.
- `using` — вне namespace: сначала `System*`, потом остальные по алфавиту, алиасы (`using Object = UnityEngine.Object;`)
  в конце. Вместо полных имён — `using`. Исключение — `UnityEditor.Editor`: внутри `VATyakov.Editor` короткое имя
  конфликтует с namespace.
- Отступ — 4 пробела, скобки на новой строке (Allman), строка не длиннее 160 символов (у клиента 200).
- Пробелы в конце строк и двойные пустые строки не допускаются. В существующем файле не меняем BOM, переводы строк и
  наличие перевода строки в конце.

## 3. Порядок членов класса

1. константы — сначала публичные, потом приватные;
2. статические поля;
3. поля экземпляра; группы (сериализуемые, `readonly`, изменяемые) разделяются пустой строкой;
4. события;
5. свойства — сначала публичные, потом приватные;
6. конструкторы;
7. методы: публичные, затем колбэки Unity (`Awake`, `OnEnable`, `Update`, `LateUpdate`, `OnDisable`, `OnDestroy`),
   затем приватные; в контроллерах — `Deactivate()`, потом `Activate()`;
8. вложенные типы.

Пустая строка ставится между методами и после закрывающей `}` блока. Однострочные свойства одного доступа идут подряд,
без пустых строк. В тестах публичные `[Test]`-методы идут первыми, хелперы — после них.

## 4. Именование

| Что | Стиль | Пример |
|---|---|---|
| типы, методы, свойства, события, константы | PascalCase | `VatClipRequest`, `MaxTextureSize` |
| публичные и internal поля, `public static readonly` | PascalCase | `public readonly VatLayoutInfo Info;`, `VatShaderIds.Frame` |
| приватные поля, в том числе `static readonly` | `_camelCase` | `_context`, `private static readonly ProfilerMarker _writeMarker` |
| локальные переменные и параметры | camelCase | `clipIndex` |
| интерфейсы | префикс `I` | `IVatFrameSource` |
| bool | `Is`/`Has`/`Can` | `IsBaked`, `_isDestroyed`, `HasProblems` |
| обработчики | `On` + событие | `OnStateChanged` |
| асинхронные методы | без суффикса `Async`, как у клиента | — |

Префикс `Vat` — у всех типов пакета. Имена — термины Unity и предметной области (`Clip`, `Frame`, `Drift`), без
сокращений. Идентификаторы только латиницей: у клиента есть `BusinessСompleted` с кириллической «С».

Файлы контента — маленькими буквами через `_`: `bow_default_vat.prefab`, `vat_lit_vertex.shadergraph`. Папки — в
PascalCase.

## 5. Синтаксис

- Модификатор доступа пишется всегда, кроме членов интерфейсов. Порядок: доступ, `static`, `readonly`
  (`private static readonly`), `internal sealed class`.
- Фигурные скобки ставятся у `if`/`else`/`for`/`foreach`/`while`/`using` всегда, даже для одной строки. У клиента
  так в 78% случаев, у нас — в 100%.

  ```csharp
  if (_asset == null)
  {
      return;
  }
  ```
- Одна строка — один оператор и одна переменная: никаких `int a = 0, b = 1;` и `x = 1; y = 2;` в одной строке.
- `var` для всех локальных переменных, включая `for` и `foreach`. Явный тип пишется, только если иначе тип станет
  другим (`var rows = 0L;`, а не `long rows = 0;`) или его нельзя вывести.
- `new()` для полей, когда тип уже написан слева: `private readonly List<Renderer> _renderers = new();`. Для
  локальных — `var x = new T(...)`.
- Методы и конструкторы — с блочным телом, свойства и аксессоры — через `=>`. Skill клиента разрешает однострочные
  методы через `=>`, кроме `Activate`/`Deactivate`. Мы так не делаем, чтобы форма методов была одна; это же задаёт
  `resharper_method_or_operator_body = block_body` в их `.editorconfig`.
- Простые вызовы и инициализаторы пишутся в одну строку. Переносы — только когда так реально легче читать (4 и больше
  присваиваний, длинные условия). Перенесённые аргументы — с отступом +4.
- Атрибуты типа и поля — каждый на своей строке, `[SerializeField]` последним:

  ```csharp
  [Tooltip("Baked frames per second of animation, for every clip.")]
  [Min(0.001f)]
  [SerializeField]
  private float _fps = 30f;
  ```
- Сравнение с null — `!= null` / `== null`, не `is not null`. Unity-объекты сравниваются только через `==`, у них
  `?.` не работает.
- Строки собираются интерполяцией `$"..."`. Числа в строки — через `CultureInfo.InvariantCulture`: культура
  редактора — ru-RU.
- Ранний выход (guard) вместо вложенных `if`. Маленькие методы: если блок нужно объяснять — это отдельный метод с
  говорящим именем.

## 6. Комментарии

В коде их нет: ни пересказа, ни ссылок на план (`§1.3`), ни XML-summary, ни закомментированного кода. У клиента
разрешён только `// TODO:` со ссылкой на Jira — для временных обходных решений. Неочевидные факты (подвохи API,
решения) живут в `Docs/plan` и `Docs/PROGRESS.md`, а если без них легко ошибиться при правке — в CLAUDE.md.
Имя метода или переменной должно объяснять «что»; «почему» — в документации.

## 7. Защитный код

- Нет проверок на null для зависимостей, которые гарантирует архитектура: правится связка, а не ставится заглушка.
- Нет клампов для значений, которые система уже гарантирует. Данные проверяет бейкер (`VatBakeValidator`), шейдер
  ничего не проверяет.
- Проверка нужна там, где null — законное состояние: копий материалов нет вне Play mode, текстуры собраны
  частично при ошибке, Unity-объект уничтожен.
- Ошибки для пользователя — `VatBakeException` с понятным английским текстом; в рантайме — `Debug.LogError` с именем
  объекта и контекстом (`this`). Ошибка не должна глушиться молча.
- Никаких «магических» сигналов вроде `int.MinValue` как признака «уже залогировали»: используется
  `TryGet...(out ...)` и явное имя.

## 8. Unity

- Сериализуемые данные — `[SerializeField] private` плюс свойство. У клиента в контейнерах публичные поля; у нас
  общий пакет, и изменяемое состояние наружу не отдаём. Контейнеры UI Toolkit (`EditorContainer`) отдают элементы
  через `public readonly`.
- Колбэки Unity — `private void OnEnable()` и т. п.
- `.meta` не правятся вручную. Переименование и перенос — через Unity (`AssetDatabase.RenameAsset`/`MoveAsset`); при
  переносе файла снаружи его `.meta` переносится вместе с ним.
- Пакет компилируется на 6000.3: API из 6000.4+ не используется.
- MaterialPropertyBlock и `renderer.material` запрещены (§1.6 плана).
- Весь текст в пакете — на английском, подписи — термины Unity.

## 9. Шейдеры (HLSL)

- Код максимально простой: без времени, ветвлений, клампов и проверок; выбор — через `?:`.
- В Shader Graph — только обёртки `_float`; SubGraph и Custom Function — Precision Single.
- `VatCore.hlsl` и `VatMath.cs` меняются вместе.
- Новый вход Custom Function добавляется последним.
- Комментариев нет, как и в C#.

## 10. Тесты

- Сборка `VATyakov.Editor.Tests`, namespace `VATyakov.Tests`, класс `Vat<Что>Tests`.
- Имя теста — `Что_Условие_Результат`: `Rebake_KeepsTheTemplateClipByName`, `FlyingBody_DecodesWithinATenthOfAMillimeter`.
- Каждое утверждение проверяет поведение, а не реализацию. Для числовых проверок — явный допуск
  (`Is.LessThan(1e-3f)`) и сообщение на английском.
- Временные ассеты — в `Assets/__Vat*Temp`, удаляются в `[TearDown]`; сцены — preview-сцены.
- Тот же стиль, что в основном коде: модификаторы, скобки, `var`, без комментариев.

## 11. Что из `D:\client` не берём

| В клиенте | Почему не берём |
|---|---|
| около 22% `if`/`foreach` без скобок (`CharacterBaker.MeshCombine.cs`) | `AGENTS.md` требует скобки |
| `private static readonly` в PascalCase (около половины случаев) | `.editorconfig` требует `_camelCase` |
| несортированные `using` в 44% файлов | правило — сортировка |
| строки по 200–690 символов | наш предел — 160 |
| публичные изменяемые поля в моделях (`FurnitureShopModel`) | состояние — через `Property<T>` или `readonly` |
| классы-свалки: `GameContext` (около 180 свойств), `AnalyticsModel` (915 строк) | маленькие классы с одной задачей |
| закомментированный код, TODO без Jira, `#region` | нет |
| кириллица в идентификаторах | только латиница |
| магические числа (`StatusCode != 200`, `Schedule(vCount, 64, …)`) | именованные константы |

## 12. Чек-лист перед сдачей

- [ ] Rider не показывает предупреждений по `.editorconfig` в изменённых файлах.
- [ ] У всех членов есть модификатор, у всех блоков — скобки, локальные переменные через `var`.
- [ ] Комментариев нет.
- [ ] Порядок членов как в разделе 3, `Deactivate()` перед `Activate()`.
- [ ] Один тип на файл, `using` отсортированы.
- [ ] Нет лишних null-проверок и клампов.
- [ ] `git diff` без случайных пробельных изменений.
- [ ] EditMode-тесты зелёные (`run_tests` из CLAUDE.md).
