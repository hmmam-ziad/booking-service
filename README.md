# Booking Management Service

A backend service for managing bookings on shared resources (meeting rooms, equipment, etc.), built with .NET 10 Web API using Clean Architecture, plus a Next.js frontend to exercise it.

**Repo:** https://github.com/hmmam-ziad/booking-service

## Contents

- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Getting Started](#getting-started)
- [API Docs](#api-docs)
- [Design Write-up](#design-write-up)
- [Extension Task: Concurrency](#extension-task-concurrency)
- [Assumptions](#assumptions)
- [Testing](#testing)

## Architecture

I went with Clean Architecture, split into separate class libraries rather than just folders in one project. The reason is mostly about enforcement, not aesthetics if Domain and Infrastructure are separate projects, Domain literally *can't* reference EF Core, because there's no project reference to it. If it were all one project with folders, nothing stops that boundary from getting crossed by accident six months in.

```
backend/
├── src/
│   ├── BookingService.Domain/          -> Booking entity, enums, exceptions. No dependencies at all.
│   ├── BookingService.Application/     -> DTOs, interfaces, the BookingService orchestration class.
│   ├── BookingService.Infrastructure/  -> EF Core, DbContext, repositories, migrations.
│   └── BookingService.Api/             -> Controllers, middleware, DI wiring.
└── tests/
    └── BookingService.Tests/

frontend/    -> Next.js (App Router), shadcn/ui + Tailwind.
```

Dependencies flow one way: Api → Infrastructure → Application → Domain. Domain has zero framework dependencies, which is also what makes the overlap logic trivial to unit test no database, no mocking, just plain C# you call directly.

### What a Booking looks like

```csharp
Id            string  (GUID-formatted, via Guid.NewGuid().ToString())
ResourceId    string
UserId        string
StartDateTime DateTime (UTC)
EndDateTime   DateTime (UTC)
Status        Confirmed | Cancelled
CreatedAt     DateTime (UTC)
CancelledAt   DateTime? (UTC)
```

Bookings are only ever created through `Booking.Create(...)` there's no public constructor. That factory method is where all the invariants live (required fields, UTC-only timestamps, End must be after Start), so there's no way to end up with a `Booking` object in a broken state anywhere in the codebase.

## Tech Stack

| Layer | Tech |
|---|---|
| Backend | .NET 10 Web API |
| ORM | EF Core 10 |
| Database | SQL Server (Express/Developer) |
| Testing | xUnit + Moq |
| Frontend | Next.js (App Router) + TypeScript |
| UI | shadcn/ui + Tailwind |

## Getting Started

### Prerequisites

- .NET 10 SDK
- SQL Server — Express, Developer, LocalDB, whatever you've got
- Node 18+

### Backend

Update the connection string in `backend/src/BookingService.Api/appsettings.json` for your local setup:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=.\\SQLEXPRESS;Initial Catalog=BookingServiceDb;Integrated Security=True;MultipleActiveResultSets=True;TrustServerCertificate=True"
}
```

Then:

```bash
cd backend/src/BookingService.Api
dotnet run
```

Migrations apply automatically on startup in Development (see `ApplyMigrationsAsync()` in `Program.cs`), so there's no separate `dotnet ef database update` step for local dev just run it and the DB gets created/updated.

The API listens on `https://localhost:7221` by default, but check your console output since the port can shift depending on your machine.

### Frontend

```bash
cd frontend
npm install
```

`.env`:
```
NEXT_PUBLIC_API_URL=https://localhost:7221
```

One thing worth flagging here: the ASP.NET Core dev HTTPS certificate is self-signed, and Node doesn't trust it by default even after `dotnet dev-certs https --trusted` (that command only updates the OS/browser trust store, not Node's). So in development, `NODE_TLS_REJECT_UNAUTHORIZED = "0"` is set to let Next.js's server-side fetches through. This is a dev-only hack — it turns off TLS certificate checking for the whole Node process, so it should never make it into anything resembling production. A real deployment would sit behind a properly issued cert and this line would just go away.

```bash
npm run dev
```

Then open `http://localhost:3000`.

### Tests

```bash
cd backend/tests/BookingService.Tests
dotnet test
```

## API Docs

Base URL: `https://localhost:7221/api`

### `POST /api/bookings`

```json
{
  "resourceId": "room-101",
  "userId": "user-1",
  "startDateTime": "2026-07-10T10:00:00Z",
  "endDateTime": "2026-07-10T11:00:00Z"
}
```

- `201 Created` — booking created, `Location` header points at `GET /api/bookings/{id}`
- `400 Bad Request` — bad window (End <= Start), missing fields
- `409 Conflict` — overlaps an existing confirmed booking on that resource

### `GET /api/bookings/{id}`

- `200 OK` with the booking
- `404 Not Found` otherwise

### `GET /api/bookings`

Query params:

| Param | Type | Required | Default |
|---|---|---|---|
| `resourceId` | string | yes | — |
| `from` | datetime | no | — |
| `to` | datetime | no | — |
| `page` | int | no | 1 |
| `pageSize` | int | no | 20 |

```
GET /api/bookings?resourceId=room-101&from=2026-07-01T00:00:00Z&to=2026-07-31T23:59:59Z
```

```json
{
  "items": [ { "id": "...", "resourceId": "room-101" } ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 2,
  "totalPages": 1
}
```

Quick note on `from`/`to`: it returns anything that *overlaps* the given range (`booking.End > from && booking.Start < to`), not just bookings fully contained inside it same logic as the conflict check, just reused here. It's fully wired up and testable through the API/Postman. The demo frontend only exercises the plain `resourceId` search though the assignment scoped the UI to create/list/cancel, so I didn't build a date-range picker for it.

### `DELETE /api/bookings/{id}`

- `204 No Content`
- `404 Not Found` if it doesn't exist
- Cancelling something already cancelled is a no-op, not an error

## Design Write-up

### A. How did you define and enforce overlapping bookings, and why?

Two bookings overlap if:

```
StartA < EndB  &&  StartB < EndA
```

This is a half-open interval, `[Start, End)`. A booking ending at 11:00 and one starting at 11:00 don't overlap. I went with this because it's how basically every calendar tool behaves, and because back-to-back bookings are the normal case for a shared resource, not some rare edge case treating them as conflicting would make the resource annoying to actually use.

The rule lives in two places on purpose:
- `Booking.OverlapsWith()` in Domain plain C#, no DB involved, so it's easy to unit test every boundary case directly.
- `BookingRepository.HasOverlapAsync()` in Infrastructure same logic, just translated to SQL so it runs as one indexed query instead of pulling rows into memory to check them in C#.

Only `Confirmed` bookings count toward the check a cancelled one frees the slot immediately.

### B. What did you assume about concurrency?

The naive "check overlap, then insert" flow has a race: two requests for the same resource can both pass the overlap check before either has actually committed, and you end up double-booked. Neither EF Core's defaults nor the index by itself stop this an index doesn't enforce exclusivity, it just makes lookups fast.

I handle this directly in the extension task below. Outside of that specific race, I'm assuming a single API instance talking to a single SQL Server database no distributed writers, no multi-region anything.

### C. What would break in your design at scale, and where's the first bottleneck?

Realistically, the first bottleneck is the lock on whatever resource happens to be popular. If everyone's fighting over the same meeting room, all those writes serialize through `sp_getapplock`, even though writes to other rooms go through fine in parallel. Reads aren't affected no lock there, and the composite index (`ResourceId, Status, StartDateTime, EndDateTime`) covers the query pattern.

Past that:
- One SQL Server instance is eventually a ceiling if write volume grows across *many* resources, since every lock and transaction goes through the same database.
- `Skip/Take` paging gets slow at high offsets on large tables keyset pagination would be the next move.
- Auto-migrating on startup is convenient for local dev but wouldn't fly with multiple instances you'd want migrations applied as a deliberate deploy step instead, so two instances don't race to apply the same migration.

### D. How would you evolve this into a distributed system?

- Swap `sp_getapplock` for a distributed lock (Redis + something like RedLock) keyed on `ResourceId`, so multiple API instances behind a load balancer still serialize correctly per resource.
- Move toward an outbox/event pattern publish a `BookingCreated` event after commit so things like notifications or audit logging don't need to sit in the write path.
- Shard the database by `ResourceId` if a single instance becomes the bottleneck bookings on different resources never need to be transactionally consistent with each other anyway.
- Add a read replica for GETs, since reads massively outnumber writes here and don't need the same consistency guarantees the overlap check does.

### E. Which tradeoff did you prioritize — simplicity, correctness, or performance?

Correctness, then simplicity, then performance roughly in that order.

Correctness wasn't really optional. The entire value of a booking system is the guarantee "this slot is yours, nobody else can take it." A fast system that occasionally double-books is worse than a slower one that doesn't, so the concurrency guard exists even though it adds a bit of latency to every write.

Where I leaned toward simplicity over performance:
- Pessimistic locking (`sp_getapplock`) instead of optimistic concurrency with retries. Fewer moving parts, easier to reason about, at the cost of writers queueing up instead of racing and retrying.
- No caching, no CQRS, no event sourcing. None of that is warranted at this scale, and adding it would just bury the logic this exercise is actually about.

I didn't ignore performance entirely though the index and scoping the lock to a single `ResourceId` (instead of locking the whole table) both exist specifically so correctness doesn't come at the cost of unrelated resources blocking each other.

## Extension Task: Concurrency

**Went with Option 1.**

Mostly because it's not really a separate problem from the core overlap requirement defining overlap correctly and enforcing it under concurrent writes are two sides of the same thing, so building them together kept the design coherent instead of bolting on something unrelated.

### The race, concretely

1. Request A and Request B both hit `room-101` for overlapping windows at roughly the same moment.
2. Both call `HasOverlapAsync()`. Neither booking exists in the DB yet, so both checks come back `false`.
3. Both insert. Both succeed. Room's double-booked.

### The fix

Booking creation runs inside a transaction that grabs an exclusive, transaction-scoped lock on the `ResourceId` first, via `sp_getapplock`:

```csharp
await _unitOfWork.BeginTransactionAsync(ct);
try
{
    await _repository.AcquireResourceLockAsync(booking.ResourceId, ct); // blocks other writers on this ResourceId only
    var overlaps = await _repository.HasOverlapAsync(...);
    if (overlaps) throw new BookingOverlapException(...);
    await _repository.AddAsync(booking, ct);
    await _repository.SaveChangesAsync(ct);
    await _unitOfWork.CommitTransactionAsync(ct);
}
catch { await _unitOfWork.RollbackTransactionAsync(ct); throw; }
```

`@LockOwner = 'Transaction'` means the lock releases automatically on commit or rollback there's no scenario where a failed request leaves a resource stuck locked.

### Why this over optimistic concurrency

| | Pessimistic lock (what I did) | Optimistic (RowVersion) |
|---|---|---|
| Correctness | Solid second request re-checks only after the first has fully committed or rolled back | Needs a retry loop; a naive version can still race on the *first* insert since RowVersion protects updates, not the check-then-insert gap |
| Throughput | Same-resource writers queue; different resources unaffected | Better under contention, but you're paying for retries |
| Complexity | One extra round-trip + explicit transaction handling | Client/service needs retry logic |

Bookings aren't a high-frequency write pattern nobody's hammering the same room hundreds of times a second, it's humans clicking a button. So the throughput cost of locking is basically nothing here, and the correctness guarantee is exactly what matters. I'd flip this decision in a domain with genuinely high write contention on the same key.

## Assumptions

- **No auth.** The brief describes this as an internal API for multiple teams, so `UserId` is just an opaque string from the caller, not backed by a real identity system. JWT at the API (or gateway) would be the obvious next step before this goes anywhere near production.
- **Past dates are allowed.** `Booking.Create()` doesn't check that `StartDateTime` is in the future. Wasn't in the requirements, and it felt more like a business-rule toggle than something to hardcode maybe admins can log retroactively, maybe regular users can't.
- **Soft delete.** Cancelling flips `Status` to `Cancelled` instead of deleting the row. Cancelled bookings drop out of overlap checks immediately but stick around for history.
- **UTC everywhere.** `Booking.Create()` rejects non-UTC input outright. The frontend's `datetime-local` input is treated as UTC directly rather than converting from whatever timezone the browser is in a real UI would detect and convert.
- **Frontend scope.** Per the brief ("not a polished product"), the UI covers create/list/cancel for a resource. It doesn't expose the `from`/`to` filtering even though the API fully supports it.
- **Local HTTPS with a workaround** instead of just switching to HTTP for dev, mostly because I wanted local setup to resemble how the API would actually run. Documented above.

## Testing

`BookingOverlapTests` — the overlap rule across every case I could think of: identical windows, partial overlap on each side, one fully inside the other (both directions), completely before/after, and the two boundary cases where one booking's end lines up exactly with another's start (these are the important ones they're what actually prove the half-open interval decision). Plus `Booking.Create()` validation and `Cancel()` idempotency.

`BookingServiceTests` — orchestration, with `IBookingRepository` mocked. Overlap throws and skips the save. Domain validation fails before the repository is even touched. Not-found paths on `GetById`/`Cancel` throw correctly. Paging maps through as expected.

```bash
dotnet test
```
