# Menza
Objednávací systém pre našu menzu.

## Popis
Systém je navrhnutý na efektívnu správu a objednávanie jedál na objednávku - minútiek.

1. Študent si vyberie jedlo z aktuálneho menu cez webovú aplikáciu(todo).
2. Kuchyňa príjme objednávku a mení jej stav v reálnom čase.
3. Študent je informovaný o stave.

## Stack

- Runtime: .NET 10
- Orchestrácia: .NET Aspire
- API: Minimal Web API s využitím TypedResults
- Databáza: Entity Framework Core + PostgreSQL
- Testovanie: xUnit + Aspire.Hosting.Testing

## Štruktúra

```
UTB.Minute/
├── UTB.Minute.WebApi.Tests/
├── UTB.Minute.WebApi/
├── UTB.Minute.DbManager/
├── UTB.Minute.AppHost/
├── UTB.Minute.ServiceDefaults/
├── UTB.Minute.Db/
├── UTB.Minute.Contracts/
├── README.md
├── global.json
└── UTB.Minute.sln
```

## Dátový model & Stavový stroj

Entity:
- Meal
- MenuItem – Ponuka v daný deň
- Order

Životný cyklus objednávky:
Systém prísne stráži prechody medzi stavmi (napr. nie je možné zrušiť objednávku, ktorá už bola vydaná):
- Preparing ➔ Ready ➔ Completed > Preparing ➔ Cancelled ➔ Completed

## API endpointy

| Metóda | URL | Popis |
|--------|-----|-------|
| GET | `/api/meals` | Všetky jedlá |
| GET | `/api/meals/{id}` | Jedlo podľa Id |
| POST | `/api/meals` | Vytvorenie jedla |
| PUT | `/api/meals/{id}` | Úprava jedla |
| GET | `/api/menu` | Všetky položky menu |
| GET | `/api/menu/today` | Dnešné menu |
| POST | `/api/menu` | Vytvorenie položky menu |
| PUT | `/api/menu/{id}` | Úprava položky menu |
| DELETE | `/api/menu/{id}` | Zmazanie položky menu |
| GET | `/api/orders` | Všetky objednávky |
| POST | `/api/orders` | Vytvorenie objednávky |
| PUT | `/api/orders/{id}/status` | Zmena stavu objednávky |

## Architektúra

- **Minimal API** — všetky endpointy sú v jednom súbore `Program.cs` ako pomenované statické metódy.
- **TypedResults** — návratové typy endpointov sú explicitne definované (napr. `Results<Ok<MealDto>, NotFound>`).
- **Record pre DTO** — immutable, štrukturálna rovnosť, stručný zápis.
- **Enum pre stav objednávky** — typovo bezpečné, nemožno zadať neplatný stav.
- **Aspire.Hosting.Testing** — testy bežia oproti PostgreSQL databáze spustenej cez Aspire.

## Requirements and Run

- .NET 10+
- Docker

```
dotnet run --project UTB.Minute.AppHost
```

Aspire dashboard zobrazí všetky služby.

## Spustenie testov

```
dotnet test
```

Testy automaticky spustia PostgreSQL kontajner cez Aspire.

## Pomer prace v time

1:1