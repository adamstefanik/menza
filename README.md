![Alt text](assets/ui-screenshot.png)
![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-4169E1?logo=postgresql&logoColor=white)
![Aspire](https://img.shields.io/badge/.NET_Aspire-13-512BD4?logo=dotnet)
![xUnit](https://img.shields.io/badge/xUnit-2.9-green)
# Menza

Objednávací systém pre našu menzu.

## Popis

Systém je navrhnutý na efektívnu správu a objednávanie jedál na objednávku - minútiek.

1. Študent si vyberie jedlo z aktuálneho menu cez webovú aplikáciu (CanteenClient).
2. Kuchyňa príjme objednávku a mení jej stav v reálnom čase (AdminClient).
3. Študent je informovaný o stave svojej objednávky.

## Stack

- Runtime: .NET 10
- Orchestrácia: .NET Aspire
- API: Minimal Web API s využitím TypedResults
- Frontend: Blazor Server (AdminClient + CanteenClient)
- Databáza: Entity Framework Core + PostgreSQL
- Testovanie: xUnit + Aspire.Hosting.Testing

## Štruktúra

```
UTB.Minute/
├── UTB.Minute.AdminClient/      # Blazor Server – správa pre administrátorov/kuchárov
├── UTB.Minute.CanteenClient/    # Blazor Server – objednávanie pre študentov
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

- **Meal** – jedlá v databáze (názov, cena, aktívne)
- **MenuItem** – ponuka v daný deň (dátum, dostupné porcie, odkaz na Meal)
- **Order** – objednávky študentov (stav, čas vytvorenia, odkaz na MenuItem)

Životný cyklus objednávky — systém prísne stráži prechody medzi stavmi (napr. nie je možné zrušiť objednávku, ktorá už bola vydaná):

- Preparing ➔ Ready ➔ Completed
- Preparing ➔ Cancelled
- Cancelled ➔ Completed (administrátor môže dokončiť aj zrušenú)

## API endpointy

### Meals

| Metóda | URL | Popis |
|--------|-----|-------|
| GET | `/api/meals` | Všetky jedlá |
| GET | `/api/meals/{id}` | Jedlo podľa Id |
| POST | `/api/meals` | Vytvorenie jedla |
| PUT | `/api/meals/{id}` | Úprava jedla |
| DELETE | `/api/meals/{id}` | Zmazanie jedla |

### Menu

| Metóda | URL | Popis |
|--------|-----|-------|
| GET | `/api/menu` | Všetky položky menu |
| GET | `/api/menu/today` | Dnešné menu |
| POST | `/api/menu` | Vytvorenie položky menu |
| PUT | `/api/menu/{id}` | Úprava položky menu |
| DELETE | `/api/menu/{id}` | Zmazanie položky menu |

### Orders

| Metóda | URL | Popis |
|--------|-----|-------|
| GET | `/api/orders` | Všetky objednávky |
| POST | `/api/orders` | Vytvorenie objednávky |
| PUT | `/api/orders/{id}/status` | Zmena stavu objednávky |

## Architektúra

- **Minimal API** — všetky endpointy sú v jednom súbore `Program.cs` ako pomenované statické metódy.
- **TypedResults** — návratové typy endpointov sú explicitne definované (napr. `Results<Ok<MealDto>, NotFound>`).
- **Record pre DTO** — immutable, štrukturálna rovnosť, stručný zápis.
- **Enum pre stav objednávky** — typovo bezpečné, nemožno zadať neplatný stav.
- **Blazor Server** — interaktívne komponenty s `@rendermode InteractiveServer`.
- **Aspire Service Discovery** — automatické prepojenie klientov s API cez `http://webapi`.
- **Aspire.Hosting.Testing** — testy bežia oproti PostgreSQL databáze spustenej cez Aspire.

## Requirements and Run

- .NET 10+
- Docker

```bash
dotnet run --project UTB.Minute.AppHost
```

Aspire dashboard zobrazí všetky služby.

## Spustenie testov

```bash
dotnet test
```

Testy automaticky spustia PostgreSQL kontajner cez Aspire.

## Pomer práce v tíme

1:1
