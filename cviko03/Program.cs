using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.VisualBasic;

var options = new DbContextOptionsBuilder<CourseContext>()
                .UseSqlite("Data Source=courses.db")
                .Options;

using var context = new CourseContext(options);
context.Database.EnsureDeleted();
context.Database.EnsureCreated();

var frameworky =  new Course() {Title = "Aplikacne Frameworky", Credits = 4};
var fyzika =  new Course() {Title = "Fyzika", Credits = 5};
var matika =  new Course() {Title = "Matika", Credits = 2};

context.AddRange(fyzika, matika, frameworky);
context.SaveChanges();

var vybraneKurzy = context.Courses.Where(k => k.Credits > 3); 

foreach(var kurz in vybraneKurzy)
{
  System.Console.WriteLine(kurz.Title);
}

Course? zmenaKurzu = context.Courses.Find(3);

if(zmenaKurzu is not null)
{
  zmenaKurzu.Credits = 10;
  context.SaveChanges();
}

Course? vymazanieKurzu = context.Courses.FirstOrDefault(k => k.Credits == 2);

if(vymazanieKurzu is not null)
{
  context.Courses.Remove(vymazanieKurzu);
  context.SaveChanges(); 
}

var zoradeneKurzy = context.Courses.OrderBy(k => k.Title);

foreach(var kurz in zoradeneKurzy)
{
  System.Console.WriteLine($"{kurz.Id}: {kurz.Title} credits: {kurz.Credits}");
}

public class Course
{
    public int Id { get; set; } // Primární klíč
    public required string Title { get; set; } // Musíme zadať pri vytváraní
    public required int Credits { get; set; }
}

public class CourseContext(DbContextOptions<CourseContext> options) : DbContext(options)
{
    public DbSet<Course> Courses { get; set; }
}