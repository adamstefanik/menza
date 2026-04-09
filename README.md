# UTB.Minute

Objednavaci system pre menzu. Semestralny projekt pre predmet **Aplikacne frameworky** na UTB.

## Popis

System umoznuje objednavanie minutek (jedal pripravenych na objednavku) v menze. Student si objedna jedlo vo webovej aplikacii, kucharky menia stav objednavky a student je o stave informovany.

## Technologie

- .NET 10
- .NET Aspire (orchestracia, service discovery)
- Minimal Web API s TypedResults
- Entity Framework Core + PostgreSQL
- xUnit + Aspire.Hosting.Testing

## Struktura projektu

| Projekt | Popis |
|---------|-------|
| `UTB.Minute.AppHost` | Aspire orchestracia (PostgreSQL, service discovery) |
| `UTB.Minute.ServiceDefaults` | Zdielana konfiguracia (health checks, telemetria) |
| `UTB.Minute.Db` | Entity a `CanteenContext` (DbContext) |
| `UTB.Minute.Contracts` | DTO definovane ako `record` |
| `UTB.Minute.WebApi` | REST API endpointy |
| `UTB.Minute.DbManager` | Http Command pre reset a seed databazy |
| `UTB.Minute.WebApi.Tests` | Integracne testy |

## Datovy model

- **Meal** — jedlo (popis, cena, aktivne/neaktivne)
- **MenuItem** — polozka menu (datum, pocet porcii, vaazba na Meal)
- **Order** — objednavka (stav, cas vytvorenia, vaazba na MenuItem)
- **OrderStatus** — enum: `Preparing`, `Ready`, `Cancelled`, `Completed`

Relacie: `Meal` 1:N `MenuItem` 1:N `Order`

## API endpointy

| Metoda | URL | Popis |
|--------|-----|-------|
| GET | `/api/meals` | Vsetky jedla |
| GET | `/api/meals/{id}` | Jedlo podla Id |
| POST | `/api/meals` | Vytvorenie jedla |
| PUT | `/api/meals/{id}` | Uprava jedla |
| GET | `/api/menu` | Vsetky polozky menu |
| GET | `/api/menu/today` | Dnesne menu |
| POST | `/api/menu` | Vytvorenie polozky menu |
| PUT | `/api/menu/{id}` | Uprava polozky menu |
| DELETE | `/api/menu/{id}` | Zmazanie polozky menu |
| GET | `/api/orders` | Vsetky objednavky |
| POST | `/api/orders` | Vytvorenie objednavky |
| PUT | `/api/orders/{id}/status` | Zmena stavu objednavky |

## Stavy objednavky

```
Preparing --> Ready --> Completed
Preparing --> Cancelled --> Completed
```

Neplatne prechody su blokovane (napr. z `Ready` na `Cancelled`).

## Architektonicke rozhodnutia

- **Minimal API** namiesto controllerov — menej boilerplate kodu, vsetky endpointy su v jednom subore `Program.cs` ako pomenovane staticke metody.
- **TypedResults** — navratove typy endpointov su explicitne definovane (napr. `Results<Ok<MealDto>, NotFound>`), co zlepuje dokumentaciu API.
- **Record pre DTO** — immutable, struktualny equality, stiahlly zapis.
- **Enum pre stav objednavky** — typ-bezpecne, nemozno zadat neplatny stav.
- **Aspire.Hosting.Testing** — testy bzia oproti realnej PostgreSQL databaze spustenej cez Aspire, nie InMemory.

## Spustenie

Poziadavky: .NET 10 SDK, Docker

```
dotnet run --project UTB.Minute.AppHost
```

Aspire dashboard zobrazi vsetky sluzby. Pre reset databazy pouzite Http Command "Reset Database" v dashboarde alebo:

```
POST https://localhost:{port}/reset-db
```

## Spustenie testov

```
dotnet test
```

Testy automaticky spustia PostgreSQL kontajner cez Aspire.

## Pomer prace v time

1:1:1
