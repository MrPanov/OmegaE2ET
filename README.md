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

- `Test` — `https://test.omega.page/`, используется по умолчанию локально;
- `Production` — `https://my.omega.page/`, запускается только после явного подтверждения.

Для production требуется отдельный технический пользователь и отдельный набор
эталонных поисковых данных. Тестовые значения по умолчанию в production не
подставляются. Перед локальным production-запуском заполните весь профиль
`Production`, выберите среду и дополнительно установите
`ALLOW_PRODUCTION_TESTS=true`. Чтобы вернуться на тестовый сервер, укажите `Test`.
Переменные
`BASE_URL`, `OMEGA_EMAIL`, `OMEGA_PASSWORD`,
`OMEGA_ENVIRONMENT`, `ALLOW_PRODUCTION_TESTS`, `REQUIRE_AUTHENTICATION`,
`SEARCH_MIN_INTERVAL_SECONDS` и переменные `SEARCH_*` имеют приоритет над локальным
файлом. `BASE_URL` обязан использовать HTTPS и домен выбранной среды, поэтому
профиль `Test` нельзя направить на production.

```powershell
$env:OMEGA_PASSWORD = "<пароль тестового пользователя>"
dotnet restore
dotnet test --filter "TestCategory=P0"
```

P0 содержит отдельный быстрый набор критических поисковых проверок. Полный P1-набор
запускается по требованию:

```powershell
dotnet test --filter "TestCategory=P1"
```

Замените текст внутри кавычек на реальный пароль: значение `<пароль>` является
только примером и не подходит для авторизации.

Настройки по умолчанию:

- `OMEGA_ENVIRONMENT=Test`
- `BASE_URL=https://test.omega.page/`
- `OMEGA_EMAIL=web@omega-auto.biz`
- `ALLOW_PRODUCTION_TESTS=false`
- `REQUIRE_AUTHENTICATION=false` локально и `true` в TeamCity
- `BROWSER=chrome`
- `HEADLESS=false` — локально браузер открывается в видимом режиме
- `EXPLICIT_WAIT_SECONDS=20`
- `SEARCH_MIN_INTERVAL_SECONDS=5` для Test
- `SEARCH_MIN_INTERVAL_SECONDS=10` для Production

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

Автоматические проверки фильтров сгруппированы в компоненте
`tests/UiAutomation.Tests/Tests/Catalogs`. Каждый каталог представлен отдельным
классом, поэтому в Test Explorer и отчетах CI сразу видно проверяемую сущность.
Общее открытие каталога и проверки бренда, наличия, сужения и сброса фильтров
реализованы один раз в `CatalogFilterTestBase`.

Автоматические smoke-тесты главного `Меню` находятся в
`tests/UiAutomation.Tests/Tests/MainMenuTests.cs`. Для всего класса также
используется одна авторизация и один браузерный сеанс. UUID в ссылках не
проверяются, поскольку зависят от аккаунта; проверяется стабильная часть маршрута.

Автоматические smoke-тесты глобального поиска находятся в
`tests/UiAutomation.Tests/Tests/SearchTests.cs`. Набор содержит 24 сценария
`SEARCH-BAR-001`–`SEARCH-BAR-009`, `SEARCH-BAR-011`–`SEARCH-BAR-018` и
`SEARCH-BAR-020`–`SEARCH-BAR-026`: поиск по
коду, карточке и названию, обработку введённого значения, быстрые запросы,
очистку, Ctrl+A, вставку, режим «починається з» и историю. Цены и остатки
намеренно не фиксируются, поскольку являются динамическими. Между обычными
поисками автотесты выдерживают настраиваемый интервал, необходимый из-за ограничения
частоты запросов сервера.

Перед каждым поисковым тестом восстанавливаются исходный маршрут, пустая строка
поиска, закрытая история и выключенный режим «починається з». Сценарии истории
вынесены в отдельный `SearchHistoryTests`, чтобы они не зависели от остальных
поисковых проверок.

Basket-сценарии разделены по влиянию на данные. Read-only проверка открытия корзины
находится в `BasketReadOnlySmokeTests`. Тесты добавления, удаления и изменения
количества находятся в `BasketSmokeTests`, помечены `MutatesUserState` и получают
новую браузерную сессию для каждого теста.

В production кодовый policy разрешает только тесты категории `ProductionSafe` и
блокирует любой тест с категорией `MutatesUserState`, даже если production-запуск
был явно подтверждён.

### Запуск из Visual Studio с видимым браузером

Если заполнен `testsettings.local.json`, Visual Studio можно открыть обычным способом:
пароль, активная среда и эталонные поисковые данные будут прочитаны из локального файла.

Для переключения среды непосредственно в Visual Studio выберите:

1. `Test → Configure Run Settings → Select Solution Wide runsettings File`.
2. `runsettings/Test.runsettings` для тестового сайта или
   `runsettings/Production.runsettings` для боевого сайта.
3. Запустите тесты через Test Explorer.

Выбранный `.runsettings` имеет приоритет над `activeEnvironment` из локального JSON.
Production-файл также задаёт `ALLOW_PRODUCTION_TESTS=true`: его явный выбор считается
подтверждением запуска на боевом сайте. URL определяется автоматически. Пароль в
`.runsettings` намеренно не хранится и по-прежнему читается из локального файла либо
`OMEGA_PASSWORD`.

Если локального файла нет, закройте уже открытую Visual Studio и запустите скрипт:

```powershell
.\scripts\start-visual-studio.ps1
```

При наличии локального файла скрипт просто откроет решение. Без локального файла он
запросит пароль скрыто, установит `OMEGA_PASSWORD` и `HEADLESS=false` только для
запускаемой Visual Studio и удалит переменные из PowerShell после запуска.

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
параметры можно переопределить в конфигурации или при ручном запуске. Versioned
Settings добавляют выпадающий параметр `env.OMEGA_ENVIRONMENT`: при ручном
запуске можно выбрать `Test` или `Production`. По умолчанию всегда выбран `Test`.
`BASE_URL` в TeamCity намеренно не задаётся: проект автоматически использует
`https://my.omega.page/` для `Production` и `https://test.omega.page/` для `Test`.
`env.REQUIRE_AUTHENTICATION=true` не позволяет получить зелёный CI при отсутствии
пароля. Обычная конфигурация также задаёт `env.ALLOW_PRODUCTION_TESTS=false`.

Для явного локального выбора среды без редактирования JSON:

```powershell
# Test (по умолчанию)
$env:OMEGA_ENVIRONMENT = "Test"
$env:OMEGA_PASSWORD = "<пароль>"
dotnet test

# Production
$env:OMEGA_ENVIRONMENT = "Production"
$env:ALLOW_PRODUCTION_TESTS = "true"
$env:OMEGA_EMAIL = "<production technical login>"
$env:OMEGA_PASSWORD = "<production password>"
dotnet test
```

