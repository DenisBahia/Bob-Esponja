## Plan: "My Goal" Feature — Save & Edit a Target Portfolio

A new **My Goal** tab sits between Actual Portfolio and Projections. Users click **"Save as goal"** on any version row to seed a `user_goals` DB table with that version's year-by-year `totalAmount` values. The Goal tab shows a single-line chart plus an editable table; editing any value live-refreshes the chart. One goal per user (upsert).

---

### Steps

1. **Add `UserGoal` model** in [Models/](ETFTracker.Api/Models/) — new `UserGoal.cs` with `Id`, `UserId` (unique FK), `SourceVersionId` (nullable), `SavedAt`, and `GoalPointsJson` (text column for a `[{year, targetValue}]` JSON array).

2. **Register model & create migration** — add `DbSet<UserGoal>` + `OnModelCreating` mapping to [`AppDbContext.cs`](ETFTracker.Api/Data/AppDbContext.cs), then generate migration `AddUserGoals`.

3. **Backend service + controller** — add `IGoalService` / `GoalService` with `GetGoalAsync` and `UpsertGoalAsync` (creates or replaces). Add `GoalController` with two endpoints: `GET /api/goal` (returns `UserGoalDto` or 404) and `PUT /api/goal` (accepts `{ sourceVersionId?, dataPoints: [{year, targetValue}] }`).

4. **Extend `api.service.ts`** — add `GoalDataPointDto` and `UserGoalDto` interfaces, plus `getGoal()` and `upsertGoal(dto)` methods pointing at the new endpoints; also wire `saveAsGoal` call used by the versions table button.

5. **Update `dashboard.component.ts`** — extend `activeMainSection` to `'portfolio' | 'goal' | 'projections'`; add goal state (`userGoal`, `goalDataPoints[]`, `goalLoading`, goal chart instance + `@ViewChild`); add `loadGoal()`, `saveAsGoal(version)`, `onGoalValueChange(index, value)` (updates array + re-renders chart), and `saveGoalEdits()` methods; hook chart lifecycle into `ngAfterViewChecked` / `ngOnDestroy`.

6. **Update HTML & SCSS** — add "My Goal" nav tab in [`dashboard.component.html`](ETFTracker.Web/src/app/pages/dashboard/dashboard.component.html) between the two existing tabs; add **"Save as goal" 🎯** button to each row in the versions table; add the Goal section with a Chart.js canvas (single `Projected Portfolio Value` line) and an editable year/target-value table that triggers live chart refresh on `(input)` change, plus a "Save changes" button.

---

### Further Considerations

1. **One goal per user (upsert) vs. multiple goals** — the plan uses a single upsert; clicking "Save as goal" on a different version replaces the existing goal. Should there be a confirmation prompt if a goal already exists? Worth confirming.
2. **What "target value" column shows** — the plan seeds from `totalAmount` (Projected Portfolio Value) only. Should users also see/edit the inflation-corrected or after-tax columns, or is raw projected value sufficient for now?
3. **Chart refresh on edit** — the plan re-renders the Chart.js instance on every `(input)` event (debounce optional). Since this is a client-side-only redraw with no API call, it should be snappy; a debounce of ~300 ms can be added if needed.

