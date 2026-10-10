# Лабораторная работа 04 по разработке веб-сервисов

ASP.NET Core Web API для управления товарами по заданию CSE5032, модуль 04. Реализованы EF Core, база SQLite, Repository, DTO, AutoMapper, Code First миграция и единый формат ответа ReturnResult. Проект предназначен для локального запуска и демонстрации через Swagger.

## Быстрый запуск

Нужен .NET SDK 8 или 9 и установленная среда выполнения ASP.NET Core 8. Для первого восстановления пакетов нужен интернет. На компьютере, где подготовлен проект, необходимые SDK и среда выполнения уже есть.

Откройте терминал в папке, где находится `WebServicesLab4.sln`, и выполните:

```powershell
dotnet restore
dotnet run --project src/ProductsApi --launch-profile http
```

Откройте Swagger: **http://localhost:5084/swagger**.

При первом запуске в режиме Development сохранённая миграция `InitialCreate` применяется автоматически. База `products.db` создаётся в папке `src/ProductsApi`. Товары сохраняются между запусками. Начальная база пустая — сначала выполните POST. Чтобы остановить сервер, нажмите Ctrl+C в терминале.

В Visual Studio откройте `WebServicesLab4.sln`, выберите `ProductsApi` как запускаемый проект и профиль `http`.

## Проверка в Swagger

1. Откройте `POST /api/products`, нажмите **Try it out**, вставьте тело запроса и нажмите **Execute**:

   ```json
   {
     "name": "Ноутбук",
     "price": 350000,
     "category": "Электроника"
   }
   ```

2. Запомните `id` из ответа. Выполните `GET /api/products` и `GET /api/products/{id}`.
3. Выполните `PUT /api/products/{id}` с новым названием, ценой и категорией:

   ```json
   {
     "name": "Игровой ноутбук",
     "price": 420000,
     "category": "Компьютеры"
   }
   ```

4. Выполните `DELETE /api/products/{id}`. Повторный GET по этому ID вернёт 404.
5. Проверьте POST с отрицательной ценой или пустым названием — получите 400 и массив ошибок.

Категория принимается при POST и PUT и сохраняется в базе. Ответ содержит только `id`, `name`, `price`, поскольку в `ProductDto` нет `Category`. Отдельный `ProductRequestDto` используется для входных данных; ID назначает база, а PUT берёт ID из адреса.

## Endpoints и ответы

| Метод | Адрес | Результат |
| --- | --- | --- |
| GET | `/api/products` | 200, список DTO; при пустой базе `result: []` |
| GET | `/api/products/{id}` | 200, DTO товара; 404, если товар отсутствует |
| POST | `/api/products` | 201, DTO нового товара и заголовок Location; 400 при неверных данных |
| PUT | `/api/products/{id}` | 200, DTO изменённого товара; 400 или 404 при ошибке |
| DELETE | `/api/products/{id}` | 200, `result: true`; 404, если товар отсутствует |

DELETE возвращает 200, чтобы сохранить тело ReturnResult. Успешный ответ:

```json
{
  "isSuccess": true,
  "result": {
    "id": 1,
    "name": "Ноутбук",
    "price": 350000
  },
  "errorMessage": []
}
```

Ошибка поиска возвращается с HTTP 404:

```json
{
  "isSuccess": false,
  "result": null,
  "errorMessage": ["Товар с ID 1 не найден."]
}
```

Валидация и ошибки адреса/HTTP-метода также используют ReturnResult. HTTP-коды сохраняют своё значение и не заменяются полем `isSuccess`.

Название обязательно и ограничено 200 символами, категория — 100 символами. Цена обязательна, допускает ноль и должна быть не больше 999999999999.99. PUT заменяет все три поля, поэтому их нужно передавать вместе.

## Структура и объяснение для защиты

```text
WebServicesLab4.sln
src/ProductsApi/
  Controllers/ProductsController.cs
  Data/AppDbContext.cs
  DTOs/ProductDto.cs
  DTOs/ProductRequestDto.cs
  Mappings/AutoMapperProfile.cs
  Migrations/
  Models/Product.cs
  Models/ReturnResult.cs
  Repositories/IProductRepository.cs
  Repositories/ProductRepository.cs
  Program.cs
  appsettings.json
scripts/Test-Api.ps1
ProductsApi.http
```

`Product` — сущность базы с четырьмя полями. `AppDbContext` наследуется от `DbContext` и содержит `DbSet<Product> Products`. Строка подключения находится в `appsettings.json`; `Program.cs` регистрирует контекст, репозиторий и AutoMapper через Dependency Injection.

`IProductRepository` задаёт методы `GetAllAsync`, `GetAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`. `ProductRepository` выполняет запросы EF Core и сохраняет изменения через `SaveChangesAsync`. Контроллер получает репозиторий и `IMapper`; напрямую к контексту он не обращается.

Профиль содержит `CreateMap<Product, ProductDto>().ReverseMap()`. Категория исключена из обратного преобразования, поскольку её нет в DTO ответа. Преобразование входного `ProductRequestDto` в `Product` тоже выполняется AutoMapper; при PUT изменяется найденная сущность, её ID сохраняется.

На GET цепочка выглядит так:

```text
Клиент → Controller → Repository → EF Core → Database
Database → Entity → AutoMapper → ProductDto → ReturnResult → Клиент
```

На POST и PUT входные данные идут через `ProductRequestDto → AutoMapper → Product → Repository → EF Core → Database`.

## Code First и миграции

Классы сущности и контекста определяют структуру данных. На их основе инструмент EF Core создал миграцию `InitialCreate`, которая создаёт таблицу Products. В проект включены сама миграция, её Designer и снимок модели.

Чтобы вручную применить существующую миграцию, остановите API и выполните из корня проекта:

```powershell
dotnet tool restore
dotnet ef database update --project src/ProductsApi
```

После изменения модели создать следующую миграцию можно так:

```powershell
dotnet ef migrations add AddProductField --project src/ProductsApi
dotnet ef database update --project src/ProductsApi
```

Версии EF Core и локального инструмента `dotnet-ef` согласованы: 8.0.26. AutoMapper 12.0.1 и Swagger 6.6.2 закреплены в проекте; транзитивные зависимости сохранены в `packages.lock.json`. SQLite выбрана для запуска без установки отдельного сервера БД; её провайдер EF Core поддерживается Microsoft.

## Автоматическая проверка

Запустите API, затем откройте второй терминал в корне проекта:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/Test-Api.ps1
```

Скрипт проверяет CRUD, коды ответов, Location, формат ReturnResult, валидацию, скрытие Category и обращения к удалённому товару. Он создаёт свой тестовый товар и удаляет его. Файл `ProductsApi.http` содержит отдельные запросы для редактора с поддержкой HTTP-файлов.

При подготовке проекта сборка завершилась без ошибок и предупреждений, а все 100 проверок этого скрипта прошли. Отдельно проверены сохранение Category в SQLite, работа русских названий, сохранение данных после перезапуска, ручное применение миграции и доступность Swagger. Тестовые товары удалены.

## Загрузка на GitHub

Создайте пустой репозиторий на GitHub. В терминале в корне проекта выполните:

```powershell
git init
git add .
git commit -m "Implement lab 04 products API"
git branch -M main
```

Затем добавьте адрес вашего репозитория и отправьте проект:

```powershell
git remote add origin https://github.com/YOUR_USERNAME/YOUR_REPOSITORY.git
git push -u origin main
```

В последнем примере замените `YOUR_USERNAME` и `YOUR_REPOSITORY` своими значениями. `.gitignore` исключает базу, сборки и временные файлы. Исходники, миграции, настройки и README нужно загрузить. Можно также загрузить файлы через сайт GitHub.

## Материалы

- Задание: «Модуль 04 Лабораторная работа», предоставленное для CSE5032.
- Пример структуры: https://github.com/Damirmgg/Web-Services-Development-HW4
- Пример предыдущего модуля: https://github.com/Damirmgg/Web-Services-Development-Prac3
- EF Core SQLite: https://learn.microsoft.com/en-us/ef/core/providers/sqlite/

Реализация выполнена для модели Product и контракта ответа из текущего задания.
