# Omega UI Automation

Проект автоматизации и ручного тестирования Omega на Test и Production.
Production-проверки работают только с изолированным тестовым клиентом в боевой
базе. Автотесты реализованы на .NET 8, Selenium WebDriver и NUnit; запуск в CI
настроен через TeamCity.

## Требования

- .NET SDK 8.0.423 или совместимый стабильный feature band .NET 8
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

Для production используется тот же изолированный тестовый клиент. Production-профиль
может получить его логин и пароль из локального профиля `Test` через
`credentialsFromEnvironment: "Test"`, поэтому секрет не дублируется и остаётся только
в игнорируемом файле. Для Basket достаточно заполнить учётные данные профиля `Test`;
для полного прогона Search заполните также весь блок `search`. Затем выберите среду
и дополнительно установите
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
количества находятся в `BasketSmokeTests`, помечены `ProductionTestClient` и
`MutatesUserState`, получают новую браузерную сессию для каждого теста и очищают
эталонный товар до и после проверки.

В production кодовый policy разрешает `ProductionSafe` и контролируемые изменения
тестов, одновременно помеченных `ProductionTestClient` и `MutatesUserState`.
Категория `ProductionBlocked` всегда запрещена и предназначена для будущих сценариев
заказов, оплат и других опасных операций.

### Запуск из Visual Studio с видимым браузером

Если заполнен `testsettings.local.json`, Visual Studio можно открыть обычным способом:
пароль, активная среда и эталонные поисковые данные будут прочитаны из локального файла.

Для переключения среды непосредственно в Visual Studio выберите:

1. `Test → Configure Run Settings → Select Solution Wide runsettings File`.
2. Выберите один профиль:
   - `runsettings/Test.runsettings` — тестовый сайт;
   - `runsettings/Production.runsettings` — полный прогон тестового клиента в Production.
3. Запустите тесты через Test Explorer.

Выбранный `.runsettings` имеет приоритет над `activeEnvironment` из локального JSON.
`Production.runsettings` задаёт `ALLOW_PRODUCTION_TESTS=true` и разрешает
контролируемые изменения только внутри тестового клиента. URL определяется
автоматически. Пароль в
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

Создайте в TeamCity параметр `env.OMEGA_PASSWORD` типа **Password**. В каждой
конфигурации `env.REQUIRE_AUTHENTICATION=true`, поэтому отсутствие секрета завершает
сборку ошибкой, а не пропуском авторизованных тестов. `BASE_URL` намеренно не
задаётся: проект сам выбирает разрешённый HTTPS-домен среды.

Versioned Settings создают пять независимых конфигураций:

- `UI Smoke - Test` — автоматически запускает `Smoke` для всех веток, которые
  VCS root TeamCity публикует как обычные или pull request branches;
- `UI P0 Release - Test` — ручной release gate с категорией `P0`;
- `UI P0 + P1 Nightly - Test` — каждый день в 02:00 запускает категории `P0` и `P1`;
- `UI Read Only - Production` — только ручной production-запуск категории
  `ProductionSafe`, дополнительно исключающий изменения и `ProductionBlocked`;
- `UI Test Client - Production` — ручной полный прогон безопасных тестов и
  контролируемых изменений изолированного тестового клиента.

Production-конфигурации требуют переопределить `env.OMEGA_EMAIL` логином тестового
клиента, безопасно задать `env.OMEGA_PASSWORD` и заполнить все production-переменные
`SEARCH_*`. Basket и Search работают только с данными этого клиента.
`ProductionBlocked` исключается фильтром и дополнительно блокируется кодом.

Каждая конфигурация выводит `dotnet --info`, восстанавливает зависимости и запускает
свой E2E/UI-набор.

`global.json` фиксирует стабильный SDK 8.0.423 с `latestFeature` и запрещает preview.
Такой SDK должен быть установлен и на TeamCity-агенте.

Для явного локального выбора среды без редактирования JSON:

```powershell
# Test (по умолчанию)
$env:OMEGA_ENVIRONMENT = "Test"
$env:OMEGA_PASSWORD = "<пароль>"
dotnet test

# Production
$env:OMEGA_ENVIRONMENT = "Production"
$env:ALLOW_PRODUCTION_TESTS = "true"
$env:OMEGA_EMAIL = "<production test-client login>"
$env:OMEGA_PASSWORD = "<production password>"
dotnet test
```

