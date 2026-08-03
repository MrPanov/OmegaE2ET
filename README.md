# Omega UI Automation

Проект автоматизации и ручного тестирования авторизации на
`https://test.omega.page/`. Автотесты реализованы на .NET 8, Selenium
WebDriver и NUnit; запуск в CI настроен через TeamCity.

## Требования

- .NET 8 SDK или новее
- Chrome, Edge или Firefox
- TeamCity-агент с выбранным браузером

Selenium Manager автоматически подбирает драйвер установленного браузера.

## Локальный запуск

Для запуска из Visual Studio без повторного ввода пароля скопируйте
`testsettings.local.example.json` в `testsettings.local.json` и заполните локальные
профили. Файл `testsettings.local.json` исключён из Git и не попадёт в коммит.

Поле `activeEnvironment` выбирает профиль:

- `Test` — тестовый сервер и тестовый пользователь;
- `Production` — production-сервер и production-клиент.

Перед первым production-запуском заполните в профиле `Production` поля `baseUrl`,
`loginEmail` и `loginPassword`, затем измените `activeEnvironment` на `Production`.
Чтобы вернуться на тестовый сервер, укажите `Test`. Переменные `BASE_URL`,
`OMEGA_EMAIL`, `OMEGA_PASSWORD` и `OMEGA_ENVIRONMENT` имеют приоритет над локальным
файлом, поэтому TeamCity может по-прежнему передавать настройки безопасными параметрами.

```powershell
$env:OMEGA_PASSWORD = "<пароль тестового пользователя>"
dotnet restore
dotnet test --filter "TestCategory=Smoke"
```

Замените текст внутри кавычек на реальный пароль: значение `<пароль>` является
только примером и не подходит для авторизации.

Настройки по умолчанию:

- `BASE_URL=https://test.omega.page/`
- `OMEGA_EMAIL=web@omega-auto.biz`
- `BROWSER=chrome`
- `HEADLESS=false` — локально браузер открывается в видимом режиме
- `EXPLICIT_WAIT_SECONDS=20`

Пароль намеренно не хранится в репозитории. Поддерживаемые браузеры:
`chrome`, `edge`, `firefox`. При падении теста снимок экрана прикрепляется к
результату NUnit.

## Ручное тестирование

Ручные сценарии страницы авторизации находятся в
[`docs/manual-tests/login-page.md`](docs/manual-tests/login-page.md).

Ручные сценарии каталогов находятся в
[`docs/manual-tests/catalogs.md`](docs/manual-tests/catalogs.md).

Ручные сценарии глобального поиска и поисковой выдачи находятся в
[`docs/manual-tests/search-results.md`](docs/manual-tests/search-results.md).

Отдельный набор критических и P1-сценариев непосредственно для глобальной
поисковой строки находится в
[`docs/manual-tests/search-bar.md`](docs/manual-tests/search-bar.md).

Автоматические smoke-тесты меню каталогов находятся в
`tests/UiAutomation.Tests/Tests/CatalogMenuTests.cs`. Каждый пункт каталога
отображается в Test Explorer как отдельный параметризованный тест.
Для всего класса используется один браузерный сеанс: вход выполняется в
`OneTimeSetUp`, а браузер закрывается в `OneTimeTearDown` после всех проверок.

Автоматические smoke-тесты главного `Меню` находятся в
`tests/UiAutomation.Tests/Tests/MainMenuTests.cs`. Для всего класса также
используется одна авторизация и один браузерный сеанс. UUID в ссылках не
проверяются, поскольку зависят от аккаунта; проверяется стабильная часть маршрута.

Автоматические smoke-тесты глобального поиска находятся в
`tests/UiAutomation.Tests/Tests/SearchTests.cs`. Набор содержит 25 сценариев
`SEARCH-BAR-001`–`SEARCH-BAR-009` и `SEARCH-BAR-011`–`SEARCH-BAR-026`: поиск по
коду, карточке и названию, обработку введённого значения, быстрые запросы,
очистку, Ctrl+A, вставку, режим «починається з» и историю. Цены и остатки
намеренно не фиксируются, поскольку являются динамическими. Между обычными
поисками автотесты выдерживают интервал, необходимый из-за ограничения частоты
запросов тестового сервера.

### Запуск из Visual Studio с видимым браузером

Visual Studio должна получить переменную `OMEGA_PASSWORD`. Самый безопасный
вариант — закрыть уже открытую Visual Studio и запустить подготовленный скрипт:

```powershell
.\scripts\start-visual-studio.ps1
```

Скрипт запросит пароль скрыто, установит `OMEGA_PASSWORD` и `HEADLESS=false`
только для запускаемой Visual Studio и удалит переменные из PowerShell после
запуска. Пароль не записывается в файл.

То же самое можно сделать вручную:

```powershell
$env:OMEGA_PASSWORD = "<пароль тестового пользователя>"
$env:HEADLESS = "false"
& "C:\Program Files\Microsoft Visual Studio\18\Insiders\Common7\IDE\devenv.exe" "UiAutomation.sln"
```

После запуска откройте `Test → Test Explorer` и выполните нужные тесты. Если
`OMEGA_PASSWORD` не передан, позитивные тесты входа и тесты каталогов будут
отмечены как пропущенные. Для TeamCity `HEADLESS=true` уже задан в Kotlin DSL.

Перед релизом обязательно выполняются проверки с приоритетом P0. Проверки P1
рекомендуется выполнять при изменениях формы авторизации, управления сессией
или серверной валидации.

## TeamCity

Kotlin DSL находится в `.teamcity/settings.kts`. Подключите репозиторий в
TeamCity как Versioned Settings и убедитесь, что на агенте установлены .NET SDK
и браузер.

Создайте в TeamCity параметр `env.OMEGA_PASSWORD` типа **Password**. Остальные
параметры можно переопределить в конфигурации или при ручном запуске.

