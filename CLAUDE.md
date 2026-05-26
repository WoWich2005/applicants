# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BNTU Applicants — полнофункциональное веб-приложение для управления поступлением в БНТУ. Стек: React SPA (frontend), ASP.NET Core 8 Web API (backend), PostgreSQL (база данных), Liquibase (миграции схемы).

Параллельно с кодом в проекте ведётся **расчётно-пояснительная записка (РПЗ) дипломного проекта на LaTeX** в папке `diploma/`. См. секцию [Дипломная записка (LaTeX)](#дипломная-записка-latex) ниже и подробные правила оформления в `diploma/FORMATTING.md`.

## Project Conventions

Эти правила обязательны для всех изменений в проекте.

### Git-коммиты

- Формат: **Conventional Commits** — `type(scope): description` (например, `feat(api): add applicant export`, `fix(auth): handle expired token`).
- **Никогда не добавлять** `Co-Authored-By: Claude` или любую другую атрибуцию Claude в сообщения коммитов.
- Коммитить только когда пользователь явно об этом просит.

### Таблицы (frontend)

**Все** таблицы во всём приложении обязаны использовать **серверную** пагинацию и **серверные** фильтры. Без исключений.

- Frontend: использовать хук `useServerTable`, никогда не передавать `pagination={false}` в `DataTable`.
- Backend: эндпоинт-список должен принимать `page` / `pageSize` параметры и возвращать `PagedResponse<T>`.
- Это касается аудит-вкладок, CRUD-таблиц и любых новых таблиц.

### Язык документации

- **Описания доменной модели, бизнес-логики, сущностей** в `CLAUDE.md` и сопутствующих документах пишутся **на русском**.
- **Технические команды, имена API, идентификаторы, код** — на английском.
- В тексте РПЗ — на русском (это диплом БНТУ).

### Plan mode

В Claude Code plan mode после **каждой** правки plan-файла сразу вызывать `ExitPlanMode`, чтобы пользователь мог видеть и комментировать изменения inline. Не группировать несколько раундов правок в один turn без открытия плана.

## Commands

### Frontend (`/frontend`)

```bash
npm run dev       # Start Vite dev server with HMR
npm run build     # Production build (output: dist/)
npm run preview   # Preview production build
npm run lint      # Run ESLint + Stylelint
```

### Backend (`/backend`)

```bash
dotnet build      # Build the solution
dotnet run --project bntuapplicants-backend/bntuapplicants-backend.csproj  # Run the API
```

The API runs on `http://localhost:5059`. Swagger UI is available at `/swagger` in Development mode.

### Infrastructure

```bash
# Start PostgreSQL 18-alpine (run from /infra/)
docker compose up

# Run Liquibase database migrations (run from /liquibase/)
docker compose up
```

Both Docker Compose setups require a `.env` file — copy `.env.example` to `.env` and fill in values.

## Architecture

### Frontend (`/frontend/src`)

- **Entry**: `main.jsx` → `AppRouter.jsx` handles all routes
- **Providers**: `AuthContext` (JWT token, user role) и `ThemeContext` (dark/light mode). Ant Design настроен в `GlobalProvider.jsx` с динамически переключаемой локалью (`ruRU` / `enUS`) и primary color `#008a5e`.
- **API layer**: `api/index.js` создаёт Axios instance на `http://localhost:5059/api/v1`. Интерцептор запроса прикрепляет JWT из localStorage (`auth`) заголовком `Authorization: Bearer ...` и язык из localStorage (`language`) заголовком `Accept-Language`. Каждый API-модуль — тонкая обёртка над этим instance.
- **Auth storage**: localStorage key `auth` → `{ token, username, role }`.
- **Route protection**: `ProtectedRoute.jsx` проверяет роль из `AuthContext` по списку `roles` маршрута.
- **Permission hook**: `usePermissions()` из `src/hooks/usePermissions.js` — используй в компонентах вместо прямых проверок роли:
  - `canWriteStructure` — SA, DA
  - `canWriteApplicants` — SA, DA, AU, AO
  - `canWriteAudit` — SA, DA, AU
  - `canManageUsers` — SA

#### Маршруты (`AppRouter.jsx` + `constants/routes.js`)

| Путь | Компонент | Доступ |
|------|-----------|--------|
| `/login` | Login | без авторизации |
| `/` | Results | все роли |
| `/results/competition-list/:clId` | CompetitionListResult | все роли |
| `/applicants` | Applicants | все роли |
| `/applicant/:applicantId/edit` | ApplicantEdit | все роли |
| `/faculties` | Faculties | все роли |
| `/departments` | Departments | все роли |
| `/specialties` | Specialties | все роли |
| `/specialty/:specialtyId/edit` | SpecialtyEdit | все роли |
| `/evaluation-criteria` | EvaluationCriteria | все роли |
| `/evaluation-criteria-groups` | EvaluationCriteriaGroups | все роли |
| `/evaluation-criteria-group/:groupId/edit` | EvaluationCriteriaGroupEdit | все роли |
| `/competition-list/:listId/edit` | CompetitionListEdit | все роли |
| `/users` | Users | все роли (мутации только SA на бэкенде) |
| `/audit` | AuditPage | все роли |
| `/audit-log` | AuditLogPage | все роли |

Все аутентифицированные роли видят все страницы; разграничение — только на уровне кнопок (через `usePermissions`) и бэкенда.

#### API-модули (`src/api/`)

| Файл | Основные вызовы |
|------|----------------|
| `authApi.js` | POST /auth/login, POST /auth/change-password |
| `applicantsApi.js` | GET /applicants, GET /applicants/paged, GET /applicants/{id}, POST, PUT, DELETE |
| `facultyApi.js` | GET /faculties, GET /faculties/paged, GET /faculties/{id}, POST, PUT, DELETE |
| `departmentsApi.js` | CRUD + GET /departments/by-faculty/{id} |
| `specialtiesApi.js` | CRUD + GET /specialties/by-department/{id} |
| `evaluationCriteriaApi.js` | CRUD + GET /{id}/delete-check, GET /{id}/range-check |
| `evaluationCriteriaGroupsApi.js` | CRUD + GET /{id}/delete-check |
| `evaluationCriteriaGroupItemsApi.js` | CRUD |
| `competitionListsApi.js` | CRUD + GET /by-specialty/{id} |
| `admissionCategoriesApi.js` | CRUD + GET /by-competition-list/{id}, GET /by-specialty/{id} |
| `applicantAdmissionCategoriesApi.js` | CRUD + GET /by-category/{id} |
| `applicantEvaluationValuesApi.js` | CRUD |
| `selectionApi.js` | POST /selection/recalculate, GET /competition-lists, GET /competition-lists/{id}/result, GET /competition-lists/{id}/header, GET /competition-lists/{clId}/categories/{catId}/applicants, GET /competition-lists/{id}/excel |
| `auditApi.js` | GET /audit/entity-history, GET /audit/log, GET /audit/auth-log, GET/POST/DELETE /audit/validation/{id}, GET /audit/alerts-summary, GET /audit/unvalidated, GET /audit/incomplete, GET /audit/incomplete/{id}, GET /audit/invalid-criteria-groups, GET /audit/invalid-admission-categories, GET /audit/pending-deletions, POST /pending-deletions/{id}/confirm, POST /pending-deletions/{id}/reject |
| `usersApi.js` | GET /users, GET /users/{id}, POST, PUT, PATCH /{id}/toggle-active, DELETE |

### Backend (`/backend/bntuapplicants-backend`)

- **Pattern**: Controller → Repository. Нет ORM — только raw Npgsql. Нет service layer для бизнес-логики CRUD; сервисы только для специализированных задач.
- **DTOs**: `Dtos/Requests/` для входящих данных, `Dtos/Responses/` для исходящих. `PagedResponse<T>` используется для всех постраничных эндпоинтов.

#### Роли и составные строки авторизации (`Constants/UserRoles.cs`)

| Константа | Роли |
|-----------|------|
| `SuperAdmin` | SuperAdmin |
| `WriteStructure` | SuperAdmin, DataAdministrator |
| `WriteApplicants` | SuperAdmin, DataAdministrator, Auditor, AdmissionsOperator |
| `WriteAudit` | SuperAdmin, DataAdministrator, Auditor |

Все роли имеют доступ на чтение ко всем данным. Разграничение только по записи.

#### Сервисы (`Services/`)

| Файл | Назначение |
|------|-----------|
| `JwtService.cs` | Генерация и валидация JWT (HS256, SymmetricSecurityKey из `Jwt:SecretKey`) |
| `SelectionService.cs` | Алгоритм полного пересчёта отбора абитуриентов |
| `ExcelExportService.cs` | Экспорт результатов отбора в Excel (ClosedXML или EPPlus) |
| `AuditLogger.cs` (`IAuditLogger`) | Единая точка записи в `audit_log`; инжектируется во все репозитории |
| `AuthLogger.cs` (`IAuthLogger`) | Запись попыток входа в `auth_log` |
| `CurrentUserService.cs` (`ICurrentUserService`) | Получение текущего пользователя (id, username, role) из JWT claims через `IHttpContextAccessor` |
| `Selection/FullRecalculationCore.cs` | Чистая in-memory логика алгоритма отбора (без зависимостей от БД) |
| `Selection/CategoryComparer.cs` | Компаратор кандидатов для `SortedSet` |

#### Контроллеры (`Controllers/`)

**`AuthController`** `/api/v1/auth`
- POST `/login` — `[AllowAnonymous]`; записывает в `auth_log`
- POST `/change-password` — `[Authorize]`

**`FacultyController`** `/api/v1/faculties`
- GET (all), GET `/paged`, GET `/{id}` — `[Authorize]`
- POST, PUT `/{id}`, DELETE `/{id}` — `[WriteStructure]`

**`DepartmentController`** `/api/v1/departments`
- GET (all), GET `/paged`, GET `/by-faculty/{id}`, GET `/{id}` — `[Authorize]`
- POST, PUT `/{id}`, DELETE `/{id}` — `[WriteStructure]`

**`SpecialtyController`** `/api/v1/specialties`
- GET (all), GET `/paged`, GET `/by-department/{id}`, GET `/{id}` — `[Authorize]`
- POST, PUT `/{id}`, DELETE `/{id}` — `[WriteStructure]`

**`EvaluationCriteriaController`** `/api/v1/evaluation_criteria`
- GET (all), GET `/paged`, GET `/{id}`, GET `/{id}/delete-check`, GET `/{id}/range-check` — `[Authorize]`
- POST, PUT `/{id}`, DELETE `/{id}` — `[WriteStructure]`

**`EvaluationCriteriaGroupController`** `/api/v1/evaluation_criteria_groups`
- GET (all), GET `/paged`, GET `/{id}`, GET `/{id}/delete-check` — `[Authorize]`
- POST, PUT `/{id}`, DELETE `/{id}` — `[WriteStructure]`

**`EvaluationCriteriaGroupItemController`** `/api/v1/evaluation_criteria_group_items`
- GET (all), GET `/paged`, GET `/{id}` — `[Authorize]`
- POST, PUT `/{id}`, DELETE `/{id}` — `[WriteStructure]`; вызывает `RecalculateAllAsync`

**`CompetitionListController`** `/api/v1/competition_lists`
- GET (all), GET `/by-specialty/{id}`, GET `/by-specialty/{id}/paged`, GET `/{id}` — `[Authorize]`
- POST, PUT `/{id}`, DELETE `/{id}` — `[WriteStructure]`

**`AdmissionCategoryController`** `/api/v1/admission_categories`
- GET (all), GET `/paged`, GET `/by-competition-list/{id}`, GET `/by-specialty/{id}`, GET `/{id}` — `[Authorize]`
- POST, PUT `/{id}`, DELETE `/{id}` — `[WriteStructure]`

**`ApplicantController`** `/api/v1/applicants`
- GET (all), GET `/paged`, GET `/{id}` — `[Authorize]`
- POST, PUT `/{id}` — `[WriteApplicants]`; вызывает `RecalculateAllAsync`
- DELETE `/{id}` — `[WriteApplicants]`; создаёт запись в `applicant_deletion_requests` (soft-delete), затем `RecalculateAllAsync`

**`ApplicantAdmissionCategoryController`** `/api/v1/applicant_admission_categories`
- GET (all), GET `/paged`, GET `/by-category/{id}`, GET `/{id}` — `[Authorize]`
- POST, PUT `/{id}`, DELETE `/{id}` — `[WriteApplicants]`; вызывают `RecalculateAllAsync`

**`ApplicantEvaluationValueController`** `/api/v1/applicant_evaluation_values`
- GET (all), GET `/paged`, GET `/{id}` — `[Authorize]`
- POST, PUT `/{id}`, DELETE `/{id}` — `[WriteApplicants]`; вызывают `RecalculateAllAsync`

**`SelectionController`** `/api/v1/selection`
- POST `/recalculate` — `[WriteStructure]`; ручной запуск `RecalculateAllAsync`
- GET `/competition-lists` — `[Authorize]`; список конкурсных списков с результатами
- GET `/competition-lists/{id}/result` — `[Authorize]`; постраничный результат конкурсного списка
- GET `/competition-lists/{id}/header` — `[Authorize]`; заголовок (название специальности, план)
- GET `/competition-lists/{clId}/categories/{catId}/applicants` — `[Authorize]`
- GET `/competition-lists/{id}/excel` — `[Authorize]`; возвращает файл Excel (Content-Disposition в заголовке)

**`AuditController`** `/api/v1/audit`

| Метод | Путь | Роли | Описание |
|-------|------|------|----------|
| GET | `/entity-history` | `[Authorize]` | История изменений конкретной сущности |
| GET | `/log` | `[Authorize]` | Журнал всех действий |
| GET | `/auth-log` | `[Authorize]` | Журнал авторизации |
| GET | `/validation/{id}` | `[Authorize]` | Статус валидации абитуриента |
| POST | `/validation/{id}` | `[WriteAudit]` | Поставить валидацию |
| DELETE | `/validation/{id}` | `[WriteAudit]` | Снять валидацию |
| GET | `/alerts-summary` | `[Authorize]` | Сводка: кол-во невалидированных, незавершённых и т.д. |
| GET | `/unvalidated` | `[Authorize]` | Список абитуриентов по статусу валидации |
| GET | `/incomplete` | `[Authorize]` | Абитуриенты с неполными данными (постранично) |
| GET | `/incomplete/{id}` | `[Authorize]` | Детали неполных данных конкретного абитуриента |
| GET | `/invalid-criteria-groups` | `[Authorize]` | Некорректные группы оценочных параметров |
| GET | `/invalid-admission-categories` | `[Authorize]` | Некорректные конкурсные списки |
| GET | `/pending-deletions` | `[Authorize]` | Запросы на удаление абитуриентов |
| POST | `/pending-deletions/{id}/confirm` | `[WriteAudit]` | Подтвердить удаление |
| POST | `/pending-deletions/{id}/reject` | `[WriteAudit]` | Отклонить удаление |

**`UserController`** `/api/v1/users`
- GET (all), GET `/{id}` — `[Authorize]` (все аутентифицированные)
- POST, PUT `/{id}`, PATCH `/{id}/toggle-active`, DELETE `/{id}` — `[SuperAdmin]`
- DELETE защищён от удаления последнего SuperAdmin

### Database (`/liquibase`)

Схема управляется исключительно через Liquibase YAML changesets в `db.changelog-master.yaml`, SQL-файлы в `sql/`. Никогда не изменяй схему напрямую — только новый changeset.

#### Все 27 миграций

| № | SQL-файл | Что делает |
|---|----------|-----------|
| 0001 | `0001_init_schema.sql` | Базовые таблицы: faculties, specialties, applicants, applicantadmissioncategories, applicantevaluationvalues |
| 0002 | `0002_add_evaluation_criteria.sql` | Таблица evaluationcriteria |
| 0003 | `0003_add_evaluation_criteria_groups.sql` | Таблицы evaluationcriteriagroups, evaluationcriteriagroupitems |
| 0004 | `0004_add_departments.sql` | Таблица departments |
| 0005 | `0005_specialty_link_to_department.sql` | specialties привязывается к departments (вместо faculties) |
| 0006 | `0006_add_competition_lists.sql` | Таблица competitionlists с полем plan |
| 0007 | `0007_add_applicant_categories_and_values.sql` | applicantadmissioncategories, applicantevaluationvalues (переработка) |
| 0008 | `0008_drop_applicant_documents_and_specialties.sql` | Удаление устаревших таблиц applicant_documents и applicant_specialties |
| 0009 | `0009_add_selected_applicants.sql` | Таблица selectedapplicants |
| 0010 | `0010_add_users.sql` | Таблица users (учётные записи операторов) |
| 0011 | `0011_rename_result_viewer_role.sql` | Роль ResultViewer → DataViewer |
| 0012 | `0012_add_type_to_evaluation_criteria.sql` | Поле type (`higher_is_better` / `lower_is_better`) в evaluationcriteria |
| 0013 | `0013_add_applicant_notes.sql` | Поле notes в applicants |
| 0014 | `0014_add_externalid_to_applicants.sql` | Поле externalid (уникальный идентификатор) в applicants |
| 0015 | `0015_add_audit_log.sql` | Таблица audit_log |
| 0016 | `0016_add_auth_log.sql` | Таблица auth_log |
| 0017 | `0017_add_applicant_validation.sql` | Таблица applicant_validation (позднее упразднена в 0024) |
| 0018 | `0018_drop_audit_log_context.sql` | Удаление поля context из audit_log |
| 0019 | `0019_add_applicant_soft_delete.sql` | Таблица applicant_deletion_requests |
| 0020 | `0020_remove_deletion_request_comments.sql` | Удаление поля comments из applicant_deletion_requests |
| 0021 | `0021_drop_validated_at_from_deletion_requests.sql` | Удаление validated_at из applicant_deletion_requests |
| 0022 | `0022_drop_requested_at_and_validated_by_from_deletion_requests.sql` | Удаление requested_at, validated_by из applicant_deletion_requests |
| 0023 | `0023_drop_validated_by_and_at_from_applicant_validation.sql` | Удаление validated_by, validated_at из applicant_validation |
| 0024 | `0024_merge_applicant_validation_into_applicants.sql` | Поле `validated BOOLEAN` добавляется в applicants; таблица applicant_validation УДАЛЕНА |
| 0025 | `0025_drop_role_from_audit_log.sql` | Удаление поля role из audit_log |
| 0026 | `0026_simplify_roles.sql` | Роль FacultyManager → DataAdministrator; удалены таблицы user_faculty_access, user_specialty_access; удалён faculty_id из users |
| 0027 | `0027_unique_applicant_evaluation_value.sql` | UNIQUE(applicant_id, evaluation_criteria_id) в applicantevaluationvalues |

### Data Flow

1. Login via `POST /api/v1/auth/login` → JWT возвращается и сохраняется в localStorage (`auth`)
2. Все последующие запросы: Axios-интерцептор добавляет `Authorization: Bearer <token>` и `Accept-Language: <ru|en>`
3. Backend валидирует JWT, извлекает роль, применяет `[Authorize(Roles = ...)]`
4. Репозитории выполняют прямые SQL-запросы через Npgsql
5. При мутациях данных абитуриентов: `RecalculateAllAsync` пересчитывает selectedapplicants

### Program.cs — ключевая конфигурация

- **CORS**: origins из `Cors:AllowedOrigins`, `AllowAnyHeader`, `AllowAnyMethod`, `WithExposedHeaders("Content-Disposition")` — нужен для скачивания Excel
- **JWT**: алгоритм HS256, ключ из `Jwt:SecretKey`, валидируются Issuer, Audience, Lifetime, IssuerSigningKey
- **Локализация**: культуры `ru` (default), `en`; определение через `Accept-Language` заголовок
- **Seed**: при первом запуске (нет пользователей) создаётся SuperAdmin из env vars `InitialAdmin__Username` / `InitialAdmin__Password` (defaults: `admin` / `Admin@123`)

## Domain Model

### Организационная иерархия

```
faculties → departments → specialties → competitionlists → admissioncategories
```

- **faculties** — факультеты университета.
- **departments** — кафедры внутри факультета.
- **specialties** — специальности, привязанные к кафедре (с миграции 0005; до этого — к факультету).

### Конкурс и приём

- **competitionlists** — конкурсный список: привязан к специальности, содержит `plan` (общее число мест). Примеры: "бюджет", "платное", "целевое направление".
- **admissioncategories** — категория приёма внутри конкурсного списка. Поля:
  - `quota` — мест в категории,
  - `priority` — защита категории (меньше = защищённее; `priority=1` вытесняется последним),
  - `evaluationcriteriagroupid` — группа критериев для ранжирования абитуриентов.

### Критерии оценки

- **evaluationcriteria** — конкретный показатель (например, "ЦТ Математика", "Средний балл аттестата"). Поля: `minvalue`, `maxvalue`, `type` (`higher_is_better` / `lower_is_better`).
- **evaluationcriteriagroups** — именованный набор критериев; конфигурирует ранжирование для категории.
- **evaluationcriteriagroupitems** — связь группы и критерия; `priority` задаёт порядок в лексикографической сортировке.

### Абитуриенты

- **applicants** — запись абитуриента. Поля: `name`, `externalid` (уникальный идентификатор документа), `notes` (заметки оператора), `validated` (булевый статус валидации данных).
- **applicantevaluationvalues** — оценки абитуриента: `(applicantid, evaluationcriteriaid) → value`. Уникальное ограничение с миграции 0027.
- **applicantadmissioncategories** — заявки абитуриента: в каких категориях участвует и в каком порядке предпочтения (`selectionpriority`). Один абитуриент может подавать заявки в категории разных специальностей.

### Результаты отбора

- **selectedapplicants** — итоговая таблица: кто и в какую категорию зачислен. Пересчитывается полностью при каждом изменении данных абитуриента (`SelectionService.RecalculateAllAsync`). Вызывается из `ApplicantController`, `ApplicantAdmissionCategoryController`, `ApplicantEvaluationValueController`, `EvaluationCriteriaGroupItemController`, `AuditController` (при подтверждении/отклонении удаления) и `SelectionController` (ручной запуск).

  **Алгоритм отбора** (`SelectionService.RecalculateAllAsync` → `FullRecalculationCore.Run`):
  1. Берёт глобальный `pg_advisory_xact_lock(0)` на транзакцию (исключает параллельный пересчёт).
  2. Загружает снимок БД: заявки (только активных абитуриентов через `SoftDelete.NotSoftDeleted`), категории, планы конкурсных списков, критерии по группам, оценки, направления критериев.
  3. Запускает `FullRecalculationCore.Run(snapshot)` — чистая in-memory логика без обращений к БД.
  4. Удаляет все записи из `selectedapplicants` и bulk-вставляет результат через `COPY BINARY`.

  **`FullRecalculationCore.Run`** — очередь с вытеснением (аналог deferred acceptance):
  1. **Предфильтр**: из заявок исключаются те, где у абитуриента не заполнен хотя бы один обязательный критерий группы категории.
  2. **Инициализация**: все абитуриенты попадают в очередь; для каждого строится упорядоченный список предпочтений (`selectionpriority ASC`).
  3. **Основной цикл**: абитуриент из очереди берёт следующее предпочтение → добавляется в пул категории (`SortedSet<Candidate>` по силе DESC).
  4. **Фаза 1 — квота категории**: если пул > `quota`, слабейший (`SortedSet.Max`) вытесняется и возвращается в очередь.
  5. **Фаза 2 — план конкурсного списка**: если суммарное число зачисленных в конкурсном списке > `plan`, вытесняется слабейший из наименее защищённой непустой категории (наибольший `priority`).

  **Ранжирование кандидатов** (`CategoryComparer`): лексикографически по вектору оценок DESC → `selectionpriority` ASC → `applicantId` ASC (для уникальности в `SortedSet`). Для `lower_is_better` критериев значение инвертируется (`-v`), чтобы единый компаратор «больше = лучше» работал корректно.

### Пользователи и доступ

- **users** — учётные записи операторов. Поля: `username`, `passwordhash`, `role`, `isactive`, `mustchangepassword`.
- Факультетный доступ (user_faculty_access) был удалён в миграции 0026.

| Роль | Права записи |
|------|-------------|
| `SuperAdmin` | всё, включая управление пользователями |
| `DataAdministrator` | структура + абитуриенты + аудит (всё кроме управления пользователями) |
| `Auditor` | абитуриенты + аудит (валидация, подтверждение/отклонение удалений) |
| `AdmissionsOperator` | только абитуриенты и их заявки |
| `DataViewer` | только чтение |

## Аудит, валидация и soft-delete

### Журнал действий (`audit_log`)

Таблица `audit_log` (миграции 0015, 0018, 0025) хранит все изменения системы. Сервис `AuditLogger` (`Services/AuditLogger.cs`) — единая точка записи; инжектируется во все репозитории.

- Действия: `create`, `update`, `delete`, `validate`, `invalidate`, `delete_confirmed`, `delete_rejected`, `recalculation`.
- Страница `/audit-log` — журнал с фильтрами по дате, действию, типу сущности, пользователю. Доступна всем аутентифицированным пользователям.
- Эндпоинт `GET /audit/entity-history` — история изменений конкретной сущности.

### Журнал авторизации (`auth_log`)

Таблица `auth_log` (миграция 0016). Сервис `AuthLogger` (`Services/AuthLogger.cs`) записывает каждый вход. Страница `/audit-log` (вкладка "Авторизация"), доступна всем аутентифицированным пользователям (бэкенд `[Authorize]`, без ограничений по роли).

### Валидация данных абитуриентов

Поле `validated BOOLEAN` в таблице `applicants` (добавлено в миграции 0024, до этого была отдельная таблица `applicant_validation`). Двойной контроль: оператор вносит, другой пользователь валидирует.

- При любом изменении данных абитуриента `AuditLogger.ResetValidationIfNeededAsync` автоматически сбрасывает `validated = false`.
- `GET /audit/alerts-summary` — сводка: число невалидированных абитуриентов, незавершённых данных, pending-удалений.
- `GET /audit/unvalidated` — список абитуриентов с фильтром по статусу валидации.
- Страница `/audit` — 4 вкладки: невалидированные абитуриенты, абитуриенты с неполными данными, некорректные группы оценочных параметров, некорректные конкурсные списки.

### Soft-delete абитуриентов (`applicant_deletion_requests`)

Таблица `applicant_deletion_requests` (миграция 0019): двухступенчатое удаление.

- **Шаг 1:** Пользователь с `WriteApplicants` нажимает "Удалить" → создаётся запись `status='pending'` и запускается `RecalculateAllAsync`.
- **Скрытие:** Абитуриент со статусом `pending` или `confirmed` исключается из всех SELECT в `ApplicantRepository` и из снимка `SelectionService` (через `SoftDelete.NotSoftDeleted`).
- **Шаг 2** (вкладка "Удаления на валидации" на `/audit`, только `WriteAudit`):
  - **Подтвердить** (`status='confirmed'`) — абитуриент скрыт навсегда; `RecalculateAllAsync` не нужен (уже исключён). Лог: `delete_confirmed`.
  - **Отклонить** (`status='rejected'`) — абитуриент возвращается в активные, `validated` сбрасывается, запускается `RecalculateAllAsync`. Лог: `delete_rejected`.
- После `rejected` можно снова создать запрос на удаление.

## Internationalization (i18n)

Приложение поддерживает русский (default) и английский. Язык хранится в localStorage key `language`.

### Frontend

**Stack:** `react-i18next` + `i18next-browser-languagedetector`. Init file: `src/i18n/index.js`.

**Translation files:**
- `src/i18n/locales/ru/translation.json` — Russian strings
- `src/i18n/locales/en/translation.json` — English strings

**Key naming convention:** `domain.context.key`, например `faculty.form.nameLabel`, `users.roles.SuperAdmin`.

**Usage in components:**
```jsx
import { useTranslation } from "react-i18next"

function MyComponent() {
  const { t } = useTranslation()
  return <label>{t('faculty.form.nameLabel')}</label>
}
```

**Dynamic strings:** `t('key', { name: value })` — шаблон использует `{{name}}` в JSON.

**Language switcher:** `Segmented` (RU/EN) в `src/components/ContentHeader/index.jsx`; `Radio.Group` над формой входа в `src/pages/Login.jsx`. Оба используют `useLanguage()` из `src/contexts/LanguageContext.jsx`.

**Ant Design locale:** переключается динамически в `src/providers/GlobalProvider.jsx` через `useLanguage()` — `ruRU` / `enUS`.

**Axios:** интерцептор в `src/api/index.js` читает `localStorage.getItem('language')` напрямую (React-хуки недоступны в интерцепторах) и передаёт как `Accept-Language`.

### Backend

**Stack:** встроенный `Microsoft.Extensions.Localization`. Дополнительных NuGet-пакетов не нужно.

**Resource files** (`Resources/`):
- `SharedResources.cs` — пустой маркерный класс в namespace `bntuapplicants_backend`
- `SharedResources.ru.resx` — Russian strings (default)
- `SharedResources.en.resx` — English strings

**Key naming convention:** `Domain.Context` (PascalCase), например `Faculty.Name.Required`, `Faculty.NameExists`, `Faculty.NotFound`.

**Usage in controllers** — инжектируй `IStringLocalizer<SharedResources> _localizer`, при встраивании в анонимные объекты явно приводи к `string`:
```csharp
private readonly IStringLocalizer<SharedResources> _localizer;

public MyController(IStringLocalizer<SharedResources> localizer) { _localizer = localizer; }

// Plain string:
return NotFound(new { message = (string)_localizer["Faculty.NotFound", id] });

// With format args (resx value uses {0}, {1}, ...):
return BadRequest(new { message = (string)_localizer["ApplicantEvaluationValue.ValueOutOfRange", value, min, max, name] });
```

**DTO validation:** `ErrorMessage` — ключи ресурсов, не русский текст. `DataAnnotationsLocalization` в `Program.cs` маршрутизирует их через `SharedResources`:
```csharp
[Required(ErrorMessage = "Faculty.Name.Required")]
[StringLength(100, ErrorMessage = "Faculty.Name.MaxLength100")]
```

**All error responses** must use `new { message = "..." }` shape so the frontend can read `err.response?.data?.message`.

**Добавление новой строки:**
1. Добавь ключ в оба `.resx` файла
2. Используй `(string)_localizer["Key"]` в контроллере

**Добавление новой строки на frontend:**
1. Добавь ключ в оба `translation.json`
2. Используй `t('key')` в компоненте

## Дипломная записка (LaTeX)

Расчётно-пояснительная записка (РПЗ) дипломного проекта ведётся параллельно с кодом в папке `diploma/`. Подробные правила оформления по методичке БНТУ 2025 — в файле **`diploma/FORMATTING.md`**.

### Тема и научное руководство

- **Тема ДП:** «Система компьютеризации ввода и мониторинга информации об абитуриентах при поступлении в БНТУ»
- **Студент:** Лемяшевич Владимир Александрович (группа 10701222)
- **Руководитель и консультант по разделу «Компьютерное проектирование»:** старший преподаватель кафедры «ПОИСиТ» ФИТР Станкевич С. Н.
- **Кафедра:** ПОИСиТ, ФИТР, БНТУ

### Сборка

Компилятор — **XeLaTeX** (не pdflatex), требуется из-за `\setmainfont{Times New Roman}` через `fontspec`.

```bash
cd diploma
xelatex -output-directory=build main.tex
xelatex -output-directory=build main.tex  # повторно для оглавления и ссылок
```

### Структура `diploma/`

```
diploma/
├── main.tex                # корневой документ — \input на все секции
├── preamble.tex            # шрифты, поля, заголовки, подписи, макросы
├── FORMATTING.md           # полные правила оформления БНТУ 2025
├── images/                 # рисунки (.png, .pdf)
├── styles/                 # .bst-файлы (не используются)
├── reference/              # методичка, нормоконтроль, чужие дипломы как образцы (не в сборке)
├── build/                  # вывод компиляции
└── sections/
    ├── toc.tex             # оглавление
    ├── referat.tex         # реферат
    ├── intro.tex           # ВВЕДЕНИЕ (без жирного/курсива!)
    ├── chapter1.tex        # 1 Обзор состояния вопроса
    ├── chapter2.tex        # 2 Выбор программно-технических средств
    ├── chapter3.tex        # 3 Проектирование приложения
    ├── chapter4.tex        # 4 Программная реализация
    ├── chapter5.tex        # 5 Руководство пользователя
    ├── chapter6.tex        # 6 Тестирование приложения
    ├── conclusion.tex      # ЗАКЛЮЧЕНИЕ
    ├── bibliography.tex    # СПИСОК ИСПОЛЬЗОВАННЫХ ИСТОЧНИКОВ
    └── appendix.tex        # ПРИЛОЖЕНИЯ
```

### Список источников и цитирование

**BibTeX/BibLaTeX/biber не используются** — список оформлен как обычный `enumerate` с кастомным макросом `\src` в `bibliography.tex`. Это сделано чтобы иметь полный контроль над форматированием по ГОСТ 7.1-2003 без правки `.bst`-стилей.

#### Макрос `\src[key]`

Определён в `preamble.tex`:

```latex
\newcommand{\src}[1][]{\stepcounter{totreferences}\item\ifx\relax#1\relax\else\label{src:#1}\fi}
```

- Аргумент `key` необязательный, но **всегда добавлять** для новых источников
- Автоматически инкрементирует счётчик `totreferences` (используется в реферате)
- Ставит `\label{src:key}` для ссылок из текста

#### Цитирование в тексте

**Всегда** через `[\ref{src:key}]`, никогда через жёсткие числа `[5]`:

```latex
...применяется более чем в 1500 учреждениях [\ref{src:banner}].
```

При перекомпиляции номера пересчитываются автоматически после удаления/перестановки источников.

#### Соглашение об именах ключей

- Префикс `laws:` — законы и нормативные акты РБ (`laws:education`, `laws:admission`)
- Остальное — без префикса, kebab-case: `aspnetcore`, `react-i18next`, `gost2105`, `bntu-instruction`

#### Порядок источников (п. 3.16 методички)

Допустимо: **по порядку появления в тексте** или **по алфавиту**. Сейчас сгруппированы тематически — это ВРЕМЕННО, перестановка по правильному порядку будет когда все главы будут готовы.

#### При добавлении источника

1. Дописать `\src[key] ...` в `bibliography.tex`
2. В тексте ссылаться как `[\ref{src:key}]`
3. Электронные ресурсы — обязательно `Режим доступа: \url{...}` и `Дата доступа: DD.MM.YYYY` (требование нормоконтроля)
4. Формат описания — по ГОСТ 7.1-2003 (примеры в `diploma/FORMATTING.md`)

### Полезные макросы

Определены в `preamble.tex`:

- `\code{...}` — inline-код (внутри таблиц переопределяется на `seqsplit` для переноса длинных URL)
- `\csharp`, `\dotnet`, `\netcore`, `\aspnet`, `\reactjs`, `\postgresql` — названия технологий
- `\unnumberedsection{Заголовок}` — для ВВЕДЕНИЕ/ЗАКЛЮЧЕНИЕ/СПИСОК и т.п.
- `\hyph` — мягкий перенос через дефис (`аппаратно\hyph программный`)
- `\src[key]` — элемент списка источников (см. выше)
- `\No` — устойчивый знак №

### Типографские конвенции

- Кавычки русские: `<<...>>` → «...», **не** английские `"..."`
- Длинное тире: `~--~` (с неразрывными пробелами с обеих сторон)
- Число + единица — через неразрывный пробел `~`: `14~пт`, `30~мм`, `2025~г.`, `№~105`
- Каждое предложение — с **новой строки** в `.tex` (для удобства diff и правок)
- Во ВВЕДЕНИИ — **никакого жирного и курсива** (требование нормоконтроля)
- Точка в конце заголовков **не ставится**

### Материалы от руководителя

В `diploma/reference/` лежат:
- `Методичка по ДП.pdf` — официальная методичка БНТУ 2025
- `Сообщения от нормоконтроля.txt` — замечания от нормоконтролёра
- `Диплом Трухов.docx`, `Диплом Костин/` — чужие дипломы как образцы
- `Дипломное пректирование БНТУ.doc`, `Оформление pdf-файла.docx`, `дополнения по диплому.docx`, `записка_1.docx` — дополнительные материалы
- `ThesisMagistr/`, `bsuir-diploma-latex/` — чужие LaTeX-шаблоны для справки

Эта папка **не входит** в сборку pdf — только для чтения.
