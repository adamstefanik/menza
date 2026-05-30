using UTB.Minute.Db;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<CanteenContext>("database");

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapPost("/reset-db", async (CanteenContext context) =>
{
    await context.Database.EnsureDeletedAsync();
    await context.Database.EnsureCreatedAsync();

    // 50 MAIN COURSES
    var m1 = new Meal { Description = "Viedenský hovädzí guláš s viedenskou cibuľkou", Allergens = "1", Price = 7.50m, IsActive = true };
    var m2 = new Meal { Description = "Pečené bravčové koleno na pive, horčica, chren", Allergens = "1, 10", Price = 9.20m, IsActive = true };
    var m3 = new Meal { Description = "Sviečková na smotane s karlovarskou knedľou", Allergens = "1, 3, 7, 9, 10", Price = 8.80m, IsActive = true };
    var m4 = new Meal { Description = "Bryndzové halušky so slaninkou a pažítkou", Allergens = "1, 3, 7", Price = 6.40m, IsActive = true };
    var m5 = new Meal { Description = "Kuracie soté v zemiakovej placke", Allergens = "1, 3, 7", Price = 7.20m, IsActive = true };
    var m6 = new Meal { Description = "Vyprážaný karfiol, varené zemiaky, tatárka", Allergens = "1, 3, 7", Price = 5.90m, IsActive = true };
    var m7 = new Meal { Description = "Hovädzí burger s cheddarom a batátovými hranolkami", Allergens = "1, 7, 11", Price = 10.50m, IsActive = true };
    var m8 = new Meal { Description = "Pstruh na masle s bylinkami a pečenými zemiakmi", Allergens = "4, 7", Price = 8.50m, IsActive = true };
    var m9 = new Meal { Description = "Rizoto s lesnými hubami a parmezánom", Allergens = "7", Price = 7.80m, IsActive = true };
    var m10 = new Meal { Description = "Lasagne Bolognese so syrovou kôrkou", Allergens = "1, 3, 7, 9", Price = 8.20m, IsActive = true };
    var m11 = new Meal { Description = "Segedínsky guláš Special s domácou knedľou", Allergens = "1, 3, 7", Price = 7.10m, IsActive = true };
    var m12 = new Meal { Description = "Pečené kačacie stehno, červená kapusta, lokše", Allergens = "1", Price = 12.50m, IsActive = true };
    var m13 = new Meal { Description = "Vyprážaný kurací rezeň XXL, majonézový šalát", Allergens = "1, 3, 7, 10", Price = 8.90m, IsActive = true };
    var m14 = new Meal { Description = "Penne s pestom, paradajkami a mozzarellou", Allergens = "1, 7, 8", Price = 6.80m, IsActive = true };
    var m15 = new Meal { Description = "Jelení guláš na lesných plodoch, žemľová knedľa", Allergens = "1, 3, 7", Price = 9.50m, IsActive = true };
    var m16 = new Meal { Description = "Caesar šalát s grilovaným kuracím mäsom", Allergens = "1, 3, 4, 7", Price = 7.90m, IsActive = true };
    var m17 = new Meal { Description = "Dukátové buchtičky s vanilkovým krémom", Allergens = "1, 3, 7", Price = 5.50m, IsActive = true };
    var m18 = new Meal { Description = "Grilovaný encián s brusnicami a zemiakmi", Allergens = "7", Price = 6.90m, IsActive = true };
    var m19 = new Meal { Description = "Bravčová panenka, dubáková omáčka", Allergens = "7", Price = 11.20m, IsActive = true };
    var m20 = new Meal { Description = "Thajské zelené kari s tofu a jazmínovou ryžou", Allergens = "6", Price = 8.40m, IsActive = true };
    var m21 = new Meal { Description = "Bravčové pečené, kapusta, knedľa", Allergens = "1, 3", Price = 7.30m, IsActive = true };
    var m22 = new Meal { Description = "Španielsky vtáčik s ryžou", Allergens = "1, 3, 10", Price = 8.60m, IsActive = true };
    var m23 = new Meal { Description = "Kuracie prsia s broskyňou a syrom", Allergens = "7", Price = 7.40m, IsActive = true };
    var m24 = new Meal { Description = "Vyprážaný syr so šunkou, hranolky", Allergens = "1, 3, 7", Price = 6.80m, IsActive = true };
    var m25 = new Meal { Description = "Zemiakové placky s kyslou smotanou", Allergens = "1, 3, 7", Price = 5.20m, IsActive = true };
    var m26 = new Meal { Description = "Hovädzí steak s korenenou omáčkou", Allergens = "7", Price = 15.90m, IsActive = true };
    var m27 = new Meal { Description = "Grilované kuracie krídelká, bbq omáčka", Allergens = "10", Price = 7.80m, IsActive = true };
    var m28 = new Meal { Description = "Zeleninové curry s cícerom", Allergens = "-", Price = 6.50m, IsActive = true };
    var m29 = new Meal { Description = "Rybie filé na masle so zemiakmi", Allergens = "4, 7", Price = 7.10m, IsActive = true };
    var m30 = new Meal { Description = "Tvarohové knedličky s jahodami", Allergens = "1, 3, 7", Price = 6.20m, IsActive = true };
    var m31 = new Meal { Description = "Morčací rezeň v cornflakes obale", Allergens = "1, 3, 7", Price = 8.40m, IsActive = true };
    var m32 = new Meal { Description = "Halušky s kapustou a údeným mäsom", Allergens = "1, 3", Price = 6.30m, IsActive = true };
    var m33 = new Meal { Description = "Kuracie stehno na smotane, kolienka", Allergens = "1, 3, 7", Price = 6.90m, IsActive = true };
    var m34 = new Meal { Description = "Bravčový rezeň na prírodno s ryžou", Allergens = "1", Price = 7.20m, IsActive = true };
    var m35 = new Meal { Description = "Zapekané cestoviny s kuracím mäsom", Allergens = "1, 3, 7", Price = 6.70m, IsActive = true };
    var m36 = new Meal { Description = "Mexické chilli con carne s ryžou", Allergens = "-", Price = 8.10m, IsActive = true };
    var m37 = new Meal { Description = "Pečený hejk s chlebom a uhorkou", Allergens = "1, 4", Price = 7.50m, IsActive = true };
    var m38 = new Meal { Description = "Grenadír s kyslou uhorkou", Allergens = "1", Price = 5.40m, IsActive = true };
    var m39 = new Meal { Description = "Bravčové karé s volským okom", Allergens = "3", Price = 7.90m, IsActive = true };
    var m40 = new Meal { Description = "Domáce buchty s lekvárom a makom", Allergens = "1, 3, 7", Price = 5.80m, IsActive = true };
    var m41 = new Meal { Description = "Bravčový rezeň v trojobale, zemiaky", Allergens = "1, 3, 7", Price = 7.40m, IsActive = true };
    var m42 = new Meal { Description = "Kurací perkelt s haluškami", Allergens = "1, 3, 7", Price = 6.80m, IsActive = true };
    var m43 = new Meal { Description = "Znojemská hovädzia pečienka s ryžou", Allergens = "1, 10", Price = 8.10m, IsActive = true };
    var m44 = new Meal { Description = "Vyprážaný oštiepok, hranolky, brusnice", Allergens = "1, 3, 7", Price = 6.90m, IsActive = true };
    var m45 = new Meal { Description = "Hubové rizoto so sušenými paradajkami", Allergens = "7", Price = 7.60m, IsActive = true };
    var m46 = new Meal { Description = "Bravčová krkovička na grile, horčica", Allergens = "10", Price = 8.90m, IsActive = true };
    var m47 = new Meal { Description = "Kurací gyros s pita chlebom a tzatziki", Allergens = "1, 7", Price = 7.20m, IsActive = true };
    var m48 = new Meal { Description = "Lekvárové pirohy s maslom a strúhankou", Allergens = "1, 3, 7", Price = 5.90m, IsActive = true };
    var m49 = new Meal { Description = "Hovädzie na divoko s brusnicami a knedľou", Allergens = "1, 3, 7", Price = 9.40m, IsActive = true };
    var m50 = new Meal { Description = "Grilovaná zelenina s tofu a kuskusom", Allergens = "1, 6", Price = 6.70m, IsActive = true };

    context.Meals.AddRange(m1, m2, m3, m4, m5, m6, m7, m8, m9, m10);
    context.Meals.AddRange(m11, m12, m13, m14, m15, m16, m17, m18, m19, m20);
    context.Meals.AddRange(m21, m22, m23, m24, m25, m26, m27, m28, m29, m30);
    context.Meals.AddRange(m31, m32, m33, m34, m35, m36, m37, m38, m39, m40);
    context.Meals.AddRange(m41, m42, m43, m44, m45, m46, m47, m48, m49, m50);
    
    await context.SaveChangesAsync();

    // MENU ITEMS
    var today = DateOnly.FromDateTime(DateTime.Today);
    var tomorrow = today.AddDays(1);
    var monday = new DateOnly(2026, 6, 1);
    var tuesday = new DateOnly(2026, 6, 2);
    var wednesday = new DateOnly(2026, 6, 3);
    var thursday = new DateOnly(2026, 6, 4);
    var friday = new DateOnly(2026, 6, 5);

    var menu = new List<MenuItem>
    {
        // Today (Saturday 30.5.)
        new MenuItem { Date = today, MealId = m1.Id, AvailablePortions = 20 },
        new MenuItem { Date = today, MealId = m2.Id, AvailablePortions = 20 },
        new MenuItem { Date = today, MealId = m3.Id, AvailablePortions = 20 },
        new MenuItem { Date = today, MealId = m4.Id, AvailablePortions = 20 },
        new MenuItem { Date = today, MealId = m5.Id, AvailablePortions = 20 },
        // Tomorrow (Sunday 31.5.)
        new MenuItem { Date = tomorrow, MealId = m6.Id, AvailablePortions = 20 },
        new MenuItem { Date = tomorrow, MealId = m7.Id, AvailablePortions = 20 },
        new MenuItem { Date = tomorrow, MealId = m8.Id, AvailablePortions = 20 },
        new MenuItem { Date = tomorrow, MealId = m9.Id, AvailablePortions = 20 },
        new MenuItem { Date = tomorrow, MealId = m10.Id, AvailablePortions = 20 },
        // Next Week
        new MenuItem { Date = monday, MealId = m1.Id, AvailablePortions = 30 },
        new MenuItem { Date = monday, MealId = m4.Id, AvailablePortions = 25 },
        new MenuItem { Date = monday, MealId = m41.Id, AvailablePortions = 40 },
        new MenuItem { Date = monday, MealId = m5.Id, AvailablePortions = 15 },
        new MenuItem { Date = monday, MealId = m6.Id, AvailablePortions = 20 },
        new MenuItem { Date = tuesday, MealId = m3.Id, AvailablePortions = 20 },
        new MenuItem { Date = tuesday, MealId = m13.Id, AvailablePortions = 35 },
        new MenuItem { Date = tuesday, MealId = m25.Id, AvailablePortions = 30 },
        new MenuItem { Date = tuesday, MealId = m7.Id, AvailablePortions = 25 },
        new MenuItem { Date = tuesday, MealId = m8.Id, AvailablePortions = 10 },
        new MenuItem { Date = wednesday, MealId = m10.Id, AvailablePortions = 50 },
        new MenuItem { Date = wednesday, MealId = m9.Id, AvailablePortions = 20 },
        new MenuItem { Date = wednesday, MealId = m42.Id, AvailablePortions = 25 },
        new MenuItem { Date = wednesday, MealId = m11.Id, AvailablePortions = 30 },
        new MenuItem { Date = wednesday, MealId = m14.Id, AvailablePortions = 15 },
        new MenuItem { Date = thursday, MealId = m12.Id, AvailablePortions = 15 },
        new MenuItem { Date = thursday, MealId = m24.Id, AvailablePortions = 30 },
        new MenuItem { Date = thursday, MealId = m31.Id, AvailablePortions = 25 },
        new MenuItem { Date = thursday, MealId = m15.Id, AvailablePortions = 20 },
        new MenuItem { Date = thursday, MealId = m16.Id, AvailablePortions = 40 },
        new MenuItem { Date = friday, MealId = m17.Id, AvailablePortions = 60 },
        new MenuItem { Date = friday, MealId = m19.Id, AvailablePortions = 20 },
        new MenuItem { Date = friday, MealId = m48.Id, AvailablePortions = 45 },
        new MenuItem { Date = friday, MealId = m20.Id, AvailablePortions = 30 },
        new MenuItem { Date = friday, MealId = m21.Id, AvailablePortions = 25 }
    };

    context.MenuItems.AddRange(menu);
    await context.SaveChangesAsync();

    return Results.Ok("Database reset: Menu items added for today, tomorrow and next week.");
});

app.UseHttpsRedirection();
app.Run();
