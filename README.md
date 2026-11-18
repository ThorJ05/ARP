# bun-react-template

To install dependencies:

```bash
bun install
```

To start a development server:

```bash
bun dev
```

To run for production:

```bash
bun start
```

This project was created using `bun init` in bun v1.3.14. [Bun](https://bun.com) is a fast all-in-one JavaScript runtime.

## API client

The TypeScript client in `src/api/api.ts` is generated from the backend's OpenAPI
specification with [swagger-typescript-api](https://github.com/acacode/swagger-typescript-api),
so the frontend and backend cannot drift apart. Do not edit the file by hand.

To regenerate it:

1. Start the database: `docker compose up -d`
2. Start the API (profile `http`, listens on `http://localhost:5288`)
3. Run, from the repository root:

```bash
bun run generate:api
```

Commit the updated `src/api/api.ts` in the same pull request as the backend change
(new or changed endpoints and DTOs).

## Tests

Run all tests with `dotnet test ARP.slnx` (GitHub Actions runs the same command on every pull request).

- **Unit tests** (`Tests/Unit`): the holiday rules, which are plain code without a database:
  monthly accrual (full time, part time, start or end in the middle of a month, a new schedule in the
  middle of a month, a rule change from a date), expiry dates, which holiday is used first, week 6 not
  being usable before 1 May, conversion when the schedule changes, and what a holiday day costs.
  Holiday requests: weekends and Danish public holidays are free, notice periods, the main holiday window,
  at most 5 days of holiday debt, a balance of 0 or more on 31 August, week 6 only when usable, overlapping
  requests and cancelling.
  Decisions: who and when is recorded, declining needs a reason, approved holiday writes one ledger entry per
  working day, an approved cancellation gives back exactly the hours it used, and a transfer moves hours to
  the next period.

## Security

### Login

- Users log in with their company email (`name@businessesbjerg.com`) and password on `POST /api/auth/login`.
- The API answers with a JWT that is valid for 60 minutes. The client sends it as `Authorization: Bearer <token>`.
  In Swagger you can paste the token with the **Authorize** button.
- Passwords are stored as a salted PBKDF2 hash (HMAC-SHA512, 210,000 iterations) using ASP.NET Core's
  `PasswordHasher`. Older hashes with fewer iterations are upgraded the next time the user logs in.
- After 5 failed logins in a row the account is locked for 15 minutes. While it is locked the password is not checked.
- A failed login always gives the same answer, whether the email doesn't exist, the password is wrong
  or the account is locked. That way the login can't be used to find out which emails exist.

### Roles

| Role | Who | How many |
|---|---|---|
| Employee | Every person in the organisation has their own employee login | One per employee |
| Accountant | Approves and rejects requests | Exactly one (enforced by the database) |
| System owner | Creates users and sets global rules | Exactly one (enforced by the database) |

The accountant and the system owner are separate logins with their own email. If the accountant is also
an employee, they use their employee login for their own holiday.

### Access matrix

Every endpoint needs a valid token unless it is marked as public.

| What | Public | Employee | Accountant | System owner |
|---|:---:|:---:|:---:|:---:|
| Health check and Swagger | Yes | Yes | Yes | Yes |
| Log in (`POST /api/auth/login`) | Yes | Yes | Yes | Yes |
| Own login info (`GET /api/auth/me`) | No | Yes | Yes | Yes |
| Own balances, also on a future date (`GET /api/me/balances?date=`) | No | Only their own | No | No |
| Own holiday requests: see, check the cost, send, withdraw or ask to cancel (`/api/me/requests`) | No | Only their own | No | No |
| Create employees with a login and work schedule (`POST /api/employees`) | No | No | No | Yes |
| See all employees and their schedule history (`GET /api/employees`, `GET /api/employees/{id}/schedules`) | No | No | Yes | Yes |
| See an employee's balances with expiry dates (`GET /api/employees/{id}/balances?date=`) | No | No | Yes | Yes |
| Give an employee a new work schedule (`POST /api/employees/{id}/schedules`) | No | No | Yes | Yes |
| Set an employee's end date (`PUT /api/employees/{id}/termination`) | No | No | No | Yes |
| Own work schedule, read only (`GET /api/me/schedules`) | No | Only their own | No | No |
| Queue of waiting requests, approve or decline holiday, cancellation and transfer requests (`/api/requests`) | No | No | Yes | No |
| Global rules and company days off *(planned)* | No | No | No | Yes |
| See everyone's planned holiday in the calendar *(planned)* | No | Yes | Yes | Yes |

An employee's id is always taken from the token, never from the URL, so an employee can't ask for
another employee's balances or requests.

### Secrets

No secrets for the deployed system are in git.

- **Development:** `appsettings.Development.json` has a JWT key that is only used locally.
- **Production:** set the secrets on fly.io before deploying. The API will not start without a JWT key.

```bash
fly secrets set -a arp-api \
  Jwt__Key="$(openssl rand -base64 48)" \
  Bootstrap__Accountant__Email="regnskab@businessesbjerg.com" \
  Bootstrap__Accountant__Password="<strong password>" \
  Bootstrap__SystemOwner__Email="systemejer@businessesbjerg.com" \
  Bootstrap__SystemOwner__Password="<strong password>"
```

On an empty database the API creates the accountant and system owner logins from the `Bootstrap` secrets.
The system owner can then log in and create the employees.
