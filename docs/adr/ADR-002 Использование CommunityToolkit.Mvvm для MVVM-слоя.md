# ADR-002: Использование CommunityToolkit.Mvvm для MVVM-слоя

## Статус

**Принято** — 2026-09-13

## Контекст

Reactive Pipeline Editor — WPF-приложение, построенное на MVVM. Слой представления должен поддерживать:

- **Много наблюдаемых свойств** — позиции нод, статусы, метрики, счётчики. У каждой ноды 5–10 свойств, у каждой ViewModel — 5–15
- **Много команд** — `Run`, `Pause`, `Stop`, `AddNode`, `DeleteNode`, `Connect`, `Undo`, `Redo` и т.д.
- **Async-команды** — запуск pipeline, загрузка файлов, длительные операции с отменой
- **Dependent properties** — `StatusColor` зависит от `Status`, `IsValid` — от нескольких полей
- **`CanExecute`-уведомления** — когда `IsRunning` меняется, кнопки `Run`/`Stop` должны обновить доступность
- **Messaging между ViewModel’ями** — например, уведомление о смене выбранной ноды

Требования к библиотеке:

1. Минимальный boilerplate для свойств и команд
2. Совместимость с нативным `Microsoft.Extensions.DependencyInjection` (мы уже используем `IHost`)
3. Не навязывать архитектурные концепции (модульность, навигация) — они не нужны для одноконного canvas-приложения
4. Активная поддержка, желательно официальная от Microsoft
5. Хорошая отлаживаемость. Сгенерированный код должен быть видимым
6. Возможность посмотреть что именно генерируется

### Рассмотренные варианты

**Вариант 1: Ручной `INotifyPropertyChanged`**

```csharp
public sealed class NodeViewModel : INotifyPropertyChanged
{
    private string _title = string.Empty;
    public string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new(name));
}
```

**Вариант 2: Fody.PropertyChanged (IL-weaving)**

```csharp
public class NodeViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public string Title { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
}
// Fody вплетает код уведомления в IL после компиляции
```

**Вариант 3: Prism**

Полноценный application framework с модульностью, Region-навигацией, диалоговыми сервисами, event aggregator’ом.

**Вариант 4: ReactiveUI**

Реактивный MVVM на базе Rx.NET. Свойства через `ReactiveCommand`, `WhenAnyValue`, `ObservableAsPropertyHelper`.

**Вариант 5: CommunityToolkit.Mvvm (Source Generators)**

```csharp
public sealed partial class NodeViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync(CancellationToken ct) { /* ... */ }
}
```

## Решение

Принят **вариант 5 — CommunityToolkit.Mvvm**.

Библиотека используется как основной инструмент MVVM-слоя:

- `ObservableObject` как базовый класс ViewModel’ей
- `[ObservableProperty]` для наблюдаемых свойств
- `[RelayCommand]` для команд (синхронных и async)
- `[NotifyPropertyChangedFor]` для зависимых свойств
- `[NotifyCanExecuteChangedFor]` для автоматического обновления `CanExecute`
- `WeakReferenceMessenger` для слабосвязанного обмена сообщениями между ViewModel’ями
- `ObservableValidator` (при необходимости) для валидации

### Правила использования

1. **Все ViewModel’и наследуют `ObservableObject`** (или `ObservableValidator`, если нужна валидация)
2. **Класс ViewModel’и должен быть `partial`** — Source Generator дописывает код в тот же класс
3. **`[ObservableProperty]` ставится на приватные поля** с префиксом `_`. Генерируется публичное свойство с `PascalCase` именем
4. **`[RelayCommand]` ставится на методы**, а не на свойства. Генерируется свойство `XxxCommand`
5. **`CanExecute` определяется через `[RelayCommand(CanExecute = nameof(CanXxx))]`** — отдельный метод без параметров, возвращающий `bool`
6. **Dependent properties связываются через `[NotifyPropertyChangedFor(nameof(Other))]`**, а не вручную в сеттере
7. **`CanExecute`-уведомления связываются через `[NotifyCanExecuteChangedFor(nameof(XxxCommand))]`** — Source Generator сам вызовет `NotifyCanExecuteChanged()`
8. **Не использовать `[ObservableProperty]` на публичных полях** — только приватные. Публичные поля — плохая практика в C#
9. **Не смешивать ручной `INotifyPropertyChanged` и `[ObservableProperty]`** в одном классе без причины

## Обоснование

### 1. Source Generators дают видимый код

Главное отличие от Fody.PropertyChanged. Сгенерированный код — это **обычный C#**, добавленный в компиляцию. Он виден в Visual Studio: `Dependencies → Analyzers → CommunityToolkit.Mvvm.SourceGenerators → ObservablePropertyGenerator`

С Fody такого нет: код вплетается в IL, и его не видно ни в отладчике, ни в дереве проекта. Чтобы понять, что произошло, нужно декомпилировать сборку.

### 2. Отладка обычная, без сюрпризов

Source Generator генерирует код **до** компиляции. В отладчике stack trace чистый, breakpoint’ы работают предсказуемо.

ReactiveUI мигрировал с Fody на Source Generators.

### 3. Полный набор MVVM-инструментов, а не только свойства

Fody.PropertyChanged решает **только задачу свойств**. Для команд нужен дополнительный инструмент (MVVM Helpers, свой `RelayCommand`). CommunityToolkit.Mvvm даёт всё в одном пакете:

| Задача | CommunityToolkit.Mvvm |
|---|---|
| Наблюдаемые свойства | `[ObservableProperty]` |
| Команды | `[RelayCommand]` |
| Async-команды с отменой | `[RelayCommand]` на `async Task` |
| CanExecute | `[RelayCommand(CanExecute = ...)]` |
| Обновление CanExecute | `[NotifyCanExecuteChangedFor]` |
| Dependent properties | `[NotifyPropertyChangedFor]` |
| Валидация | `ObservableValidator` |
| Messaging | `WeakReferenceMessenger` |
| Observable Property → IObservable | `ObservableObject.PropertyChanged` через `Observable.FromEvent` |

### 4. Не навязывает архитектуру

В отличие от Prism, CommunityToolkit.Mvvm — **не application framework**. Это набор инструментов MVVM. Нет модульности, нет Region-навигации, нет диалоговых сервисов. Для одноконного canvas-приложения это идеально: не тащим лишнее.

### 5. Официальная поддержка Microsoft

CommunityToolkit.Mvvm — проект .NET Foundation, поддерживается Microsoft. Используется в приложениях Microsoft Store. Это значит:

- Долгосрочная поддержка
- Быстрые фиксы багов
- Хорошая документация

### 6. Совместимость с нативным DI

CommunityToolkit.Mvvm не привязан к конкретному DI-контейнеру. Он естественно работает с `Microsoft.Extensions.DependencyInjection`, который уже используется через `IHost`.

Prism, например, тащит свой DI-контейнер (или адаптер к чужому). У нас — одна точка конфигурации.

### 7. Производительность

Source Generators **инкрементальные** — перегенерируется только то, что изменилось. Fody пересобирает IL для всей сборки.

### 8. Работает с современным C#

- Records — поддерживаются
- File-scoped namespaces — поддерживаются
- `required`-члены — поддерживаются
- Nullable reference types — поддерживаются

Fody долго не поддерживал records.

### 9. Нулевой конфликт с анализаторами

Source Generators — часть стандартного компилятора Roslyn. Они не конфликтуют с другими анализаторами. Fody модифицирует IL после компиляции и может конфликтовать с инструментами, которые ожидают, что IL соответствует исходному коду.

## Последствия

### Положительные

- **Boilerplate свойств сокращается в 5–6 раз.** 10 свойств — 20 строк вместо 100
- **Команды с `CanExecute` из коробки.** Async-команды с `CancellationToken` и `IsRunning`
- **Автоматические dependent properties и CanExecute-уведомления** через атрибуты — нельзя забыть обновить
- **Видимый сгенерированный код** — можно учиться, отлаживать, объяснять на интервью
- **Официальная поддержка Microsoft**, активная разработка
- **Совместимость с нативным DI** — никаких дополнительных контейнеров
- **Нет конфликтов с анализаторами и инструментами** — Source Generator, а не IL-weaver

### Отрицательные

- **Класс ViewModel’и должен быть `partial`**.
- **Поля с `_` префиксом**.
- **Зависимость на NuGet-пакет.**
- **Имя генерируемого свойства зависит от имени поля.** `_title` → `Title`, `_isRunning` → `IsRunning`. Если назвать поле `title` — получишь свойство `Title` без подчёркивания, что не соответствует соглашениям. Всегда `_camelCase`

### Нейтральные

- **Source Generator работает только в SDK-style проектах.**
- **Совместимость с .NET Framework.** CommunityToolkit.Mvvm 8.x требует .NET Standard 2.0+.

## Альтернативы и почему они отклонены

### Ручной `INotifyPropertyChanged`

**Отклонено.** Работает, даёт полный контроль, но:

- 100 строк boilerplate на 10 свойств → 500+ строк на проект
- Опечатки в имени свойсва на обновление — binding не обновляется, ловится только глазами
- Команды — отдельный boilerplate (`RelayCommand`, `CanExecute`, `RaiseCanExecuteChanged`)
- Async-команды — ещё больше boilerplate с обработкой исключений и отмены

Для проекта с 5–6 ViewModel’ями и сотней свойств ручной INPC превращается в спагетти.

### Fody.PropertyChanged

**Отклонено.** Решает только задачу свойств, но:

- **Сгенерированный код невидим**.
- **Конфликты с другими анализаторами**
- **Не работает с командами**
- **Не официальная поддержка Microsoft**
- **ReactiveUI мигрировал с Fody на Source Generators** и официально рекомендует Source Generators

### Prism

**Отклонено.** Полноценный application framework с:

- Модульностью (`IModule`, `ModuleCatalog`)
- Region-навигацией
- Диалоговыми сервисами
- Event aggregator’ом
- Своим DI-контейнером или адаптером

Для одноконного canvas-приложения с нативным DI это **избыточно**. Мы бы тащили 20 концепций, из которых используем 3.

### ReactiveUI

**Отклонено.** Реактивный MVVM на Rx.NET — мощный, но:

- **Крутая кривая обучения.** `ReactiveCommand`, `WhenAnyValue`, `ObservableAsPropertyHelper`, `WhenActivated` — всё требует понимания Rx
- **Концептуальное пересечение с System.Reactive**, который мы используем в UI-слое. Два разных подхода к реактивности — путаница
- **Сейчас избыточно для нашей задачи.**

## Ссылки

- **CommunityToolkit.Mvvm на GitHub** — https://github.com/CommunityToolkit/dotnet
- **Официальная документация** — https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/
- **Introduction to the MVVM Toolkit** — https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/introduction
- **ObservablePropertyGenerator: как работает Source Generator** — https://github.com/CommunityToolkit/dotnet/tree/main/src/CommunityToolkit.Mvvm.SourceGenerators
- **ReactiveUI migration from Fody to Source Generators** — https://www.reactiveui.net/docs/handbook/message-bus/ (в разделе про code generation)

## Примеры в коде

### Базовая ViewModel с наблюдаемыми свойствами

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace ReactivePipelineEditor.App.ViewModels;

public partial class NodeViewModel : ObservableObject
{
    public string Id { get; }

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    public NodeViewModel(string id, string title, double x, double y)
    {
        Id = id;
        _title = title;
        _x = x;
        _y = y;
    }
}
```

Source Generator сгенерирует:

```csharp
public string Title
{
    get => _title;
    set
    {
        if (!EqualityComparer<string>.Default.Equals(_title, value))
        {
            OnPropertyChanging(nameof(Title));
            _title = value;
            OnPropertyChanged(nameof(Title));
        }
    }
}
// аналогично для X и Y
```

### ViewModel с командами и CanExecute

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ReactivePipelineEditor.App.ViewModels;

public partial class PipelineViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    private bool _isRunning;

    [ObservableProperty]
    private long _processedCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusColor))]
    private NodeStatus _status;

    public Color StatusColor => Status switch
    {
        NodeStatus.Idle => Colors.Gray,
        NodeStatus.Running => Colors.Blue,
        NodeStatus.Completed => Colors.Green,
        NodeStatus.Failed => Colors.Red,
        _ => Colors.Gray
    };

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync(CancellationToken ct)
    {
        IsRunning = true;
        try
        {
            await ExecutePipelineAsync(ct);
        }
        finally
        {
            IsRunning = false;
        }
    }

    private bool CanRun() => !IsRunning;

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop() => _cancellationTokenSource.Cancel();

    private bool CanStop() => IsRunning;
}
```

### Messenger между ViewModel’ями

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

public sealed class NodeSelectedMessage : ValueChangedMessage<NodeViewModel>
{
    public NodeSelectedMessage(NodeViewModel node) : base(node) { }
}

public partial class MainViewModel : ObservableObject
{
    public MainViewModel(IMessenger messenger)
    {
        messenger.Register<MainViewModel, NodeSelectedMessage>(this, (r, m) =>
        {
            r.SelectedNode = m.Value;
        });
    }

    [ObservableProperty]
    private NodeViewModel? _selectedNode;
}
```

### Регистрация в DI

```csharp
// В App.xaml.cs
var builder = Host.CreateApplicationBuilder();

builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
builder.Services.AddSingleton<MainViewModel>();
builder.Services.AddSingleton<MainWindow>();
```
