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
- **Providers**: `AuthContext` (JWT token, user role, faculty/specialty access) and `ThemeContext` (dark/light mode) wrap the app. Ant Design is configured in `GlobalProvider.jsx` with Russian locale (`ru_RU`) and primary color `#008a5e`.
- **API layer**: `api/index.js` creates an Axios instance targeting `http://localhost:5059/api/v1`. Each API module (`applicantsApi.js`, `authApi.js`, etc.) is a thin wrapper around this instance. The Axios interceptor attaches the JWT token from localStorage (`bntu_auth`) to every request.
- **Auth storage**: Token payload includes `token`, `username`, `role`, `facultyId`, `specialtyIds`, and `facultyAccessIds`.
- **Route protection**: `ProtectedRoute.jsx` checks the user's role against allowed roles defined in `constants/routes.js`.

### Backend (`/backend/bntuapplicants-backend`)

- **Entry**: `Program.cs` registers all services and repositories, configures JWT authentication, CORS, and Swagger, and seeds an initial SuperAdmin user on first run.
- **Pattern**: Controller → Repository (no ORM — all database access is raw Npgsql queries). No service layer except for `JwtService.cs`, `SelectionService.cs` (admission selection algorithm), and `ExcelExportService.cs`.
- **DTOs**: `Dtos/Requests/` for inbound data, `Dtos/Responses/` for outbound (including `PagedResponse<T>` for paginated endpoints).
- **Roles**: Defined in `Constants/UserRoles.cs` — `SuperAdmin`, `FacultyManager`, `AdmissionsOperator`, `DataViewer`. The Users management page is restricted to `SuperAdmin` only.

### Database (`/liquibase`)

Schema is managed exclusively through Liquibase YAML changesets in `db.changelog-master.yaml`, with corresponding SQL files in `sql/`. There are 14+ migrations. Never modify the database schema directly — always add a new Liquibase changeset.

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

- **evaluationcriteria** — оценочный параметр, конкретный измеримый показатель (например: "ЦТ Математика", "ЦТ Физика", "Средний балл аттестата"). Имеет `minvalue`, `maxvalue` и `type` (`higher_is_better` / `lower_is_better`). Тип определяет направление сортировки при ранжировании — запланированная фича, в текущей реализации `SelectionService` не учитывается (сортировка всегда по убыванию).
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

- **users** — учётные записи операторов. Роли: `SuperAdmin`, `FacultyManager`, `AdmissionsOperator`, `DataViewer`. Поле `faculty_id` — необязательная привязка пользователя к конкретному факультету.
- **user_faculty_access** — дополнительный список факультетов, к данным которых пользователь имеет доступ.
- **user_specialty_access** — список специальностей, к которым пользователь имеет доступ (более гранулярный контроль, чем на уровне факультета).

  Оба списка передаются в JWT-токене (`facultyAccessIds`, `specialtyIds`) и используются фронтендом для фильтрации отображаемых данных.
