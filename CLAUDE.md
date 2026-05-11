# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BNTU Applicants is a full-stack web application for managing university applicants and admissions. It consists of a React SPA frontend, an ASP.NET Core 8 Web API backend, and a PostgreSQL database managed via Liquibase migrations.

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
# Start PostgreSQL (run from /infra/)
docker compose up

# Run Liquibase database migrations (run from /liquibase/)
docker compose up
```

Both Docker Compose setups require a `.env` file — copy `.env.example` to `.env` and fill in values.

## Architecture

### Frontend (`/frontend/src`)

- **Entry**: `main.jsx` → `AppRouter.jsx` handles all routes
- **Providers**: `AuthContext` (JWT token, user role) and `ThemeContext` (dark/light mode) wrap the app. Ant Design is configured in `GlobalProvider.jsx` with Russian locale (`ru_RU`) and primary color `#008a5e`.
- **API layer**: `api/index.js` creates an Axios instance targeting `http://localhost:5059/api/v1`. Each API module (`applicantsApi.js`, `authApi.js`, etc.) is a thin wrapper around this instance. The Axios interceptor attaches the JWT token from localStorage (`auth`) to every request.
- **Auth storage**: Token payload includes `token`, `username`, `role`.
- **Route protection**: `ProtectedRoute.jsx` checks the user's role against allowed roles defined in `constants/routes.js`.
- **Permission hook**: `usePermissions()` from `src/hooks/usePermissions.js` exposes `canWriteStructure`, `canWriteApplicants`, `canWriteAudit`, `canManageUsers` booleans — use these in components instead of raw role checks.

### Backend (`/backend/bntuapplicants-backend`)

- **Entry**: `Program.cs` registers all services and repositories, configures JWT authentication, CORS, and Swagger, and seeds an initial SuperAdmin user on first run.
- **Pattern**: Controller → Repository (no ORM — all database access is raw Npgsql queries). No service layer except for `JwtService.cs`, `SelectionService.cs` (admission selection algorithm), and `ExcelExportService.cs`.
- **DTOs**: `Dtos/Requests/` for inbound data, `Dtos/Responses/` for outbound (including `PagedResponse<T>` for paginated endpoints).
- **Roles**: Defined in `Constants/UserRoles.cs` — `SuperAdmin`, `DataAdministrator`, `Auditor`, `AdmissionsOperator`, `DataViewer`. Composite role strings `WriteStructure`, `WriteApplicants`, `WriteAudit` are used in `[Authorize(Roles = ...)]` attributes. The Users management page is restricted to `SuperAdmin` only.

### Database (`/liquibase`)

Schema is managed exclusively through Liquibase YAML changesets in `db.changelog-master.yaml`, with corresponding SQL files in `sql/`. There are 19+ migrations. Never modify the database schema directly — always add a new Liquibase changeset.

### Data Flow

1. Login via `POST /api/v1/auth/login` → JWT returned and stored in localStorage
2. All subsequent API calls attach the JWT via Axios interceptor
3. Backend validates JWT, extracts role and faculty access, and enforces authorization
4. Repositories execute direct SQL queries against PostgreSQL via Npgsql

## Domain Model

### Organizational hierarchy

```
faculties → departments → specialties
```

- **faculties** — верхний уровень: факультеты университета.
- **departments** — кафедры внутри факультета.
- **specialties** — специальности, привязанные к кафедре (до миграции 0005 были привязаны напрямую к факультету).

### Конкурс и приём

- **competitionlists** — конкурсные списки: привязан к конкретной специальности и содержит план приёма (`plan`). Например: "бюджет", "платное", "целевое направление")
- **admissioncategories** — категории приёма внутри конкурсного списка (например: "олимпиадники", "цт/цэ", "золотая медаль", "выпускник технопарка", "выпускник лицея"). Каждая категория имеет:
  - `quota` — количество мест в этой категории,
  - `priority` — приоритет категории внутри конкурсного списка (меньшее число = выше приоритет),
  - `evaluationcriteriagroupid` — группа оценочных параметров, по которым ранжируются абитуриенты в этой категории.

### Критерии оценки

- **evaluationcriteria** — оценочный параметр, конкретный измеримый показатель (например: "ЦТ Математика", "ЦТ Физика", "Средний балл аттестата"). Имеет `minvalue`, `maxvalue` и `type` (`higher_is_better` / `lower_is_better`). Тип учитывается алгоритмом отбора: для `lower_is_better` значение инвертируется (`-v`) при построении вектора оценок, что позволяет использовать единый компаратор (всегда по убыванию).
- **evaluationcriteriagroups** — группа оценочных параметров, именованный набор оценочных параметров. Используется как конфигурация ранжирования для `admissioncategories`.
- **evaluationcriteriagroupitems** — элемент связи группы оценочных параметров с оценочным параметром; поле `priority` задаёт порядок оценочных параметров при лексикографической сортировке (сначала самый важный критерий).

### Абитуриенты

- **applicants** — запись об абитуриенте. `externalid` — идентификатор для однозначного различения абитуриентов с одинаковыми ФИО; планируется хранить серию и номер паспорта или иного документа. `notes` — произвольные заметки оператора.
- **applicantevaluationvalues** — оценки абитуриента по каждому оценочному параметру (`evaluationcriteriaid → value`). Именно эти значения используются алгоритмом отбора.
- **applicantadmissioncategories** — заявки абитуриента: в каких категориях приёма он участвует и в каком порядке предпочтения (`selectionpriority`). Абитуриент может участвовать в категориях разных специальностей и разных факультетов одновременно.

### Результаты отбора

- **selectedapplicants** — итоговая таблица: кто и в какую категорию зачислен. Заполняется автоматически `SelectionService` при каждом изменении данных абитуриента.

  **Алгоритм отбора** (`SelectionService.RecalculateForApplicantAsync`):
  1. Определяет все конкурсные списки, которых касается изменение (собственные + списки всех абитуриентов, которые с ним пересекаются).
  2. Берёт PostgreSQL advisory locks на эти конкурсные списки (в порядке возрастания id, чтобы избежать deadlock).
  3. Удаляет текущие записи абитуриента из `selectedapplicants`.
  4. Пытается поместить абитуриента в его наиболее приоритетную доступную категорию приёма.
  5. Если в категории превышена квота (`quota`) — вытесняет "худшего" (последнего по рейтингу) и запускает для него пересчёт со следующего по приоритету варианта.
  6. Если в конкурсном списке превышен общий план (`plan`) — вытесняет худшего из наименее приоритетной непустой категории (наибольшее значение `priority`). Категории с `priority = 1` вытесняются последними.

  **Ранжирование внутри категории**: лексикографически по вектору оценок (в порядке `priority` критериев в группе, по убыванию значения), при равенстве — по `selectionpriority` заявки (меньше = лучше).

### Пользователи и доступ

- **users** — учётные записи операторов. Роли: `SuperAdmin`, `DataAdministrator`, `Auditor`, `AdmissionsOperator`, `DataViewer`. Все роли читают все данные; роли отличаются только правами записи.

  | Роль | Права записи |
  |------|-------------|
  | `SuperAdmin` | всё, включая управление пользователями |
  | `DataAdministrator` | всё кроме управления пользователями |
  | `Auditor` | абитуриенты + аудит (валидация, подтверждение/отклонение удалений) |
  | `AdmissionsOperator` | только абитуриенты и их заявки |
  | `DataViewer` | только чтение |

## Аудит, валидация и soft-delete

### Журнал действий (`audit_log`)

Таблица `audit_log` (миграции 0015, 0018) хранит все изменения системы. Сервис `AuditLogger` (`Services/AuditLogger.cs`) — единая точка записи; инжектируется во все репозитории.

- Действия: `create`, `update`, `delete`, `validate`, `invalidate`, `delete_confirmed`, `delete_rejected`.
- Страница `/audit-log` — журнал с фильтрами по дате, действию, типу сущности, пользователю. Доступна всем аутентифицированным пользователям.
- Журнал авторизации: таблица `auth_log` (миграция 0016), страница `/audit-log` (вкладка "Авторизация"), только `SuperAdmin`.

### Валидация данных абитуриентов (`applicant_validation`)

Таблица `applicant_validation` (миграция 0017): двойной контроль данных — оператор вносит, другой пользователь валидирует.

- При любом изменении данных абитуриента `AuditLogger.ResetValidationIfNeededAsync` автоматически сбрасывает `validated = false`.
- Страница `/audit` — 4 вкладки: валидация данных абитуриентов, некорректные заявки, некорректные группы оценочных параметров, некорректные конкурсные списки.
- Доступно `SuperAdmin`, `DataAdministrator`, `Auditor`.

### Soft-delete абитуриентов (`applicant_deletion_requests`)

Таблица `applicant_deletion_requests` (миграция 0019): двухступенчатое удаление абитуриентов.

- **Шаг 1:** Любой пользователь с правом удаления (`SuperAdmin`, `DataAdministrator`, `Auditor`, `AdmissionsOperator`) нажимает "Удалить" в `/applicants`. Вместо физического удаления создаётся запись `status='pending'`.
- **Скрытие:** Абитуриент с `status` = `pending` или `confirmed` скрыт из всех обычных выборок (`ApplicantRepository` добавляет `NOT EXISTS` фильтр во все SELECT).
- **Шаг 2 (на `/audit`, вкладка "Удаления на валидации"):** `SuperAdmin`, `DataAdministrator`, `Auditor` видят таблицу pending-удалений и могут:
  - **Подтвердить** (`status='confirmed'`) — абитуриент остаётся скрыт навсегда. Лог: `delete_confirmed`.
  - **Отклонить** (`status='rejected'`) — абитуриент возвращается в активные, `applicant_validation` сбрасывается. Лог: `delete_rejected`. После `rejected` можно снова создать запрос на удаление.
- `SelectionService` **не обновлён** под soft-delete — TODO в рамках переписывания алгоритма.

### `AuditController` (`/api/v1/audit`)

| Метод | Путь | Роли | Описание |
|-------|------|------|----------|
| GET | `/audit/log` | все аутентифицированные | Журнал действий |
| GET | `/audit/auth-log` | SA, DA, AU | Журнал авторизации |
| GET | `/audit/validation/{id}` | SA, DA, AU | Статус валидации |
| POST | `/audit/validation/{id}` | SA, DA, AU | Валидировать |
| DELETE | `/audit/validation/{id}` | SA, DA, AU | Отозвать валидацию |
| GET | `/audit/unvalidated` | SA, DA, AU | Список абитуриентов (с фильтром по статусу валидации) |
| GET | `/audit/incomplete` | SA, DA, AU | Абитуриенты с неполными данными |
| GET | `/audit/invalid-criteria-groups` | SA, DA, AU | Некорректные группы |
| GET | `/audit/invalid-admission-categories` | SA, DA, AU | Некорректные конкурсные списки |
| GET | `/audit/pending-deletions` | SA, DA, AU | Запросы на удаление абитуриентов |
| POST | `/audit/pending-deletions/{id}/confirm` | SA, DA, AU | Подтвердить удаление |
| POST | `/audit/pending-deletions/{id}/reject` | SA, DA, AU | Отклонить удаление |

SA = SuperAdmin, DA = DataAdministrator, AU = Auditor.

## Internationalization (i18n)

The app supports Russian (default) and English. Language is stored in localStorage key `language`.

### Frontend

**Stack:** `react-i18next` + `i18next-browser-languagedetector`. Init file: `src/i18n/index.js`.

**Translation files:**
- `src/i18n/locales/ru/translation.json` — Russian strings
- `src/i18n/locales/en/translation.json` — English strings

**Key naming convention:** `domain.context.key`, e.g. `faculty.form.nameLabel`, `users.roles.SuperAdmin`.

**Usage in components:**
```jsx
import { useTranslation } from "react-i18next"

function MyComponent() {
  const { t } = useTranslation()
  return <label>{t('faculty.form.nameLabel')}</label>
}
```

**Dynamic strings:** `t('key', { name: value })` — template uses `{{name}}` in JSON.

**Language switcher:** `Segmented` (RU/EN) in `src/components/ContentHeader/index.jsx` next to the avatar; `Radio.Group` above the login form in `src/pages/Login.jsx`. Both use `useLanguage()` from `src/contexts/LanguageContext.jsx`.

**Ant Design locale:** switched dynamically in `src/providers/GlobalProvider.jsx` via `useLanguage()` — `ruRU` / `enUS`.

**Axios:** the request interceptor in `src/api/index.js` reads `localStorage.getItem('language')` directly (can't use React hooks in interceptors) and sends it as the `Accept-Language` header.

### Backend

**Stack:** built-in `Microsoft.Extensions.Localization`. No extra NuGet packages needed.

**Resource files** (`Resources/`):
- `SharedResources.cs` — empty marker class in namespace `bntuapplicants_backend`
- `SharedResources.ru.resx` — Russian strings (default)
- `SharedResources.en.resx` — English strings

**Key naming convention:** `Domain.Context` (PascalCase), e.g. `Faculty.Name.Required`, `Faculty.NameExists`, `Faculty.NotFound`.

**Usage in controllers** — inject `IStringLocalizer<SharedResources> _localizer` and cast explicitly when embedding in anonymous objects:
```csharp
private readonly IStringLocalizer<SharedResources> _localizer;

public MyController(IStringLocalizer<SharedResources> localizer) { _localizer = localizer; }

// Plain string:
return NotFound(new { message = (string)_localizer["Faculty.NotFound", id] });

// With format args (resx value uses {0}, {1}, ...):
return BadRequest(new { message = (string)_localizer["ApplicantEvaluationValue.ValueOutOfRange", value, min, max, name] });
```

**DTO validation:** `ErrorMessage` attributes use resource keys (not Russian text). `DataAnnotationsLocalization` in `Program.cs` routes them through `SharedResources`:
```csharp
[Required(ErrorMessage = "Faculty.Name.Required")]
[StringLength(100, ErrorMessage = "Faculty.Name.MaxLength100")]
```

**All error responses** must use `new { message = "..." }` shape so the frontend can read `err.response?.data?.message`.

**Adding a new translatable string:**
1. Add the key to both `SharedResources.ru.resx` and `SharedResources.en.resx`
2. Use `(string)_localizer["Key"]` in the controller

**Adding a new translatable frontend string:**
1. Add the key to both `ru/translation.json` and `en/translation.json`
2. Use `t('key')` in the component (import `useTranslation`)
