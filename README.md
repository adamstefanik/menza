# UTB.Minute – Canteen Management System

Objednávací systém pre našu menzu.

## Popis

Systém je navrhnutý na efektívnu správu a objednávanie jedál na objednávku – minútiek.

1. **Študent** si vyberie jedlo z aktuálneho menu cez webovú aplikáciu (CanteenClient).
2. **Kuchyňa** príjme objednávku a mení jej stav v reálnom čase (AdminClient).
3. **Študent** je informovaný o stave svojej objednávky.

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


## Funkcionalita

### AdminClient (Správa)
- ✅ Správa jedál – vytvorenie, úprava, deaktivácia, mazanie
- ✅ Správa menu – pridávanie, úprava, mazanie položiek menu
- ✅ Správa objednávok – zmena stavu, zobrazenie všetkých objednávok

### CanteenClient (Študent)
- ✅ Zobrazenie dnešného menu
- ✅ Objednanie jedla so znížením počtu dostupných porcií
- ✅ Zobrazenie histórie objednávok so stavom
- ✅ Vizualizácia vypredaných jedál (disabled tlačidlo, šedá farba)

## Dátový model & Stavový stroj

Entity:
- **Meal** – Jedlá v databáze (názov, cena, aktívne)
- **MenuItem** – Ponuka v daný deň (dátum, dostupné porcie, odkaz na Meal)
- **Order** – Objednávky študentov (stav, čas vytvorenia, odkaz na MenuItem)

Životný cyklus objednávky:
Systém prísne stráži prechody medzi stavmi:
- Preparing ➔ Ready ➔ Completed
- Preparing ➔ Cancelled
- Cancelled ➔ Completed (výnimka – administrátor môže dokončiť aj zrušenú)

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

## Spustenie

### Požiadavky
- .NET 10+
- Docker

### Spustenie aplikácie
```bash
- dotnet run --project UTB.Minute.AppHost

## Pomer práce v tíme
1:1