# My FCPL lab capstone

*The FCPL lab runs as **SharpQuest**, the quest to learn C#. That's why the tooling and some code say SharpQuest.*

**Avatar - Brown Otter**

Café POS
The till for a small café — orders in, totals right, nothing lost.

You build the point-of-sale system for The Cozy Cup: a menu, orders that add up correctly, and a sales log the owner can finally ask questions of — "what sells best before 10?", "which pastries have never been ordered?", "what did we make today?"

Your items: menu items — name, category, price, size, allergens, available or not
Where the data comes from: a menu you write yourself. Optionally, prices in euros from Frankfurter (free, no key)
Something happens when: an item joins the menu, an order is completed, or something sells out
Parent and children: a Category has many menu items
What you'll look at: the menu, searchable, and an order being built
Good for you if you'd rather build something that looks like a real job than a game — and you like getting money exactly right.


## Session 2 — Types & modelling
- **Classes:** `Category`, `Money`, `MenuItem`, `Drink`, `Food`, `Combo`, `CafeStore`, `MenuItemMapper`, and `Lab02Wiring`.
- **What they do:** Define validated café menu types, item kinds, allergens, prices, and categories; store items; and map JSON to domain objects while rejecting invalid input.
- **Where to see them:** Run the app to load the starter menu from `src/Capstone.App/menu.json` and print the items. Run `sq test` for the Lab 02 contract checks.

## Session 3 — Testing & the contract
- **Classes:** `Lab03Wiring`, `SearchMatcher`, and the tests in `tests/Capstone.Tests/`.
- **What they do:** Implement and test the specified menu-search behavior, including matching rules and preserving store order. The project tests also undergo mutation checks against deliberately broken implementations.
- **Where to see them:** Run `sq test`; the latest run caught all 8/8 Lab 03 mutations and passed the Lab 03 contract checks.

## Session 4 — OOP & pattern matching
- **Classes:** `MenuItem`, `Drink`, `Food`, `Combo`, `Order`, `OrderLine`, pricing/VAT types, and `Lab04Wiring`.
- **What they do:** Use polymorphic menu items and kind-specific descriptions, model combo parts, and calculate order totals with quantity, availability, currency, time-based pricing, discounts, rounding, and service-mode VAT rules.
- **Where to see them:** Run the app for item descriptions and order/report examples. Run `sq test` for the polymorphism, pattern-matching, and contract checks.

## Session 5 — Collections & generics
- **Classes:** `CafeStore`, `RecentlyUsed<T>`, `CompletedOrderHistory`, and `Lab05Wiring`.
- **What they do:** Provide an ordered iterable store with integer, ID, and category indexers; lazy matching and LINQ slicing; category lookups/counts; and snapshots of available items. Generic recent-item collections validate capacity, preserve newest-first order, and back recent completed-order history.
- **Where to see them:** Run the app to see the menu iterated in order. Run `sq test` for collection indexing, category behavior, snapshots, slicing, and recent-history tests.

## Session 6 — Functions & events
- **Classes:** `CafeStore`, `MenuSubscription`, `StoreFunctions`, `MenuBoard`, `CategoryBoard`, `RecentLog`, `TodayTakings`, `SalesLog`, `AvailabilityTransitionLog`, `FeaturedMenuCache`, `CategorySalesCounter`, `ReceiptMemory`, and `Lab06Wiring`.
- **What they do:** Publish add, update, removal, availability, price, sold-out, order-completed, and general menu-change notifications. Disposable subscriptions unsubscribe safely; subscribers maintain takings, sales, restock logs, featured-item cache, category totals, and in-memory receipt lines. Store updates happen independently of subscribers, and unavailable items cannot be added to orders.
- **Where to see them:** Run the app to see event and subscriber output; run `sq test` for event ordering, unsubscribe, subscriber, and function tests.

## Session 7 — LINQ
- **Classes:** `ItemQueries`, `MenuReports`, `CafeDataGenerator`, `Order`, and `Lab07Wiring`.
- **What they do:** Query, sort, page, and summarize menu items; filter available, vegan, and category-specific items; and format receipt lines. Reports use grouping and joins for category counts, sales, payment methods, and order lines, plus best sellers, hourly revenue, never-ordered items, average/biggest orders, and pastry sales. The app generates 50 deterministic sample orders across five or more categories, with discounts and unavailable menu items. Frankfurter supplies a live EUR conversion for displayed menu prices without changing the saved RON prices.
- **Where to see them:** Run the app to see the query results and café reports printed from `Program.cs`. Run `sq test` for LINQ and report behavior.

## Session 8 — Persistence & async
- **Classes:** `IMenuStorage`, `JsonMenuStorage`, `CsvMenuStorage`, `MenuFile`, `AtomicFile`, `CompletedOrderHistoryFile`, and `Lab08Wiring`.
- **What they do:** Save and reload menu items asynchronously as versioned JSON or CSV, selected by file extension. They preserve item kinds and fields, validate malformed data, write through a temporary file, and keep a `.bak` backup after replacing an existing save. Completed orders are persisted separately from the menu.
- **Where to see them:** Run the app to load and save `menu-state.json` beside the executable and inspect the persistence demo in `Program.cs`. Completed-order history uses its own `completed-orders.json` file. Run `sq test` for the Lab 08 contract and persistence tests.
- **Verification:** Latest `sq test` passed Labs 02–08, including Lab 08 contract checks (38/38), Lab 03 mutation checks (8/8), and the project test suite (241/241).
---

## Getting your own copy (session 1, browser only)

1. On the course template page (<https://github.com/x86bogdan/sharpquest-2026>), click **Use this template → Create a new repository**. Not *Fork*: a fork of a public repository is public.
2. Owner: you. Name: anything, e.g. `fcpl-lab`. Visibility: **Private**.
3. In your new repository: **Settings → Collaborators → Add people** → `x86bogdan`.
4. Open `README.md`, click the pencil, fill in the section above, **Commit changes**.
5. Paste your repository's URL into the claim form, along with your avatar.

That's all for session 1. Cloning and pushing start in session 2.

## Every lab, the same loop

```
sq update      # at the start of the lab: fetch new contracts, reference solutions and catch-up kits
sq test        # as often as you like: build + run everything, see what's left
git push       # when it's green: CI runs the same tests and records your result
```

On Windows, run `sq` from the repository folder (in PowerShell: `.\sq test`). On Linux or macOS: `./sq.sh test`.
`sq` is a small C# program (`sq.cs`), run by `dotnet run --file sq.cs`. You can read it — session 12 explains how it works.

**Labs you haven't started show as "not started", not as failures.** Contracts can appear before their session. A lab starts when you add its wiring class (see `src/Capstone.Core/Wiring/`).

**Missed a lab, or didn't finish it?** After each session, `sq update` brings one of two things for that lab:

- **A catch-up kit, in `catchup/LabNN/`**, for the labs that later labs build on (02, 05, 06 and 08). It's the smallest working piece that lets you carry on, written for no particular theme. Copy its files into `src/Capstone.Core/CatchUp/` and next week's lab works. **It earns nothing for the lab you missed:** the tests ignore it, so that lab stays *not started*, and a wiring class of your own that hands back the kit's objects fails by name. To get the points, write your own version for your own theme, stop using the kit, and recover the lab in session 13 or 14. `catchup/README.md` has the details.
- **A reference solution, in `reference/LabNN/`**, for every other lab: one theme's solution. If it's your theme you may copy it (say so in this README). Otherwise read it for the shape and write your own.

Late work and recovery (each worth up to 8; at most 2 recoveries per recovery session, 4 in total) are explained in the [course contract](https://github.com/x86bogdan/fcpl-2026/blob/main/CONTRACT.md).

## What's in here

| Path | What it is | Yours? |
|---|---|---|
| `src/Capstone.Core/` | Your domain: types, logic, storage. The tests look here. | ✅ |
| `src/Capstone.Core/Wiring/` | One small class per lab that tells the tests how to create your objects | ✅ |
| `src/Capstone.App/` | The program you run. Keep it thin. | ✅ |
| `tests/Capstone.Tests/` | Your own tests (from session 3) | ✅ |
| `Directory.Packages.local.props` | Extra NuGet packages you want (create it if you need it) | ✅ |
| `contracts/` | Each lab's interfaces and the tests that grade them | 🔒 read, don't edit |
| `reference/` | One theme's solution for each finished lab that isn't a kit lab | 🔒 read, don't edit |
| `catchup/` | Catch-up kits for Labs 02, 05, 06 and 08, once published | 🔒 copy from, don't edit |
| `Directory.Build.*`, `Directory.Packages.props`, `global.json` | Shared build settings | 🔒 |
| `sq.cs`, `sq.cmd`, `sq.sh`, `.github/workflows/ci.yml` | Tooling | 🔒 |

🔒 files are replaced with the official copies by `sq update`, and again by CI before it grades. Editing them changes nothing that counts. It only makes your local results disagree with the recorded ones.

## How this is graded

Each lab is worth 10 points: **Contract 40%**, **Change 30%**, **Comprehension 30%**.

- **Contract** comes from the contract tests. The number that counts is the one CI records when you push, not the one on your screen.
- **Change** and **Comprehension** happen in the room: a small live modification to your code, and two "why" questions about it. AI and tutorials are allowed if you say so. What's graded is whether you can work with the code you submitted.

The full rules are in the [course contract](https://github.com/x86bogdan/fcpl-2026/blob/main/CONTRACT.md).

## Rules the build enforces

- **Zero nullable warnings.** In this repository, a nullable warning is a build error. `#nullable disable` compiles, but it gets flagged to your instructor.
- **.NET 10.** `global.json` pins the SDK so every machine in the room builds the same way.
