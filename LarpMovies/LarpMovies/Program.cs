


using System.Globalization;
using LarpMovies.Configuration;
using LarpMovies.Data;
using LarpMovies.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// Fiksirame kulturata, za da se razbirat drobnite chisla s tochka (8.3) na vsichki kompyutri haeh
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("LarpMoviesContext")
    ?? "Data Source=App_Data/movies.db";

var dataSource = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString).DataSource;
if (!Path.IsPathRooted(dataSource))
{
    connectionString = $"Data Source={Path.Combine(builder.Environment.ContentRootPath, dataSource)}";
}

// Singleton: cialoto prilozhenie polzva edin i sushti Database prez IDatabase.
builder.Services.AddSingleton<IDatabase>(_ => Database.Initialize(connectionString));
// Singleton: cialoto prilozhenie polzva edin i sushti Database prez IDatabase.
//builder.Services.AddSingleton<IDatabase>(_ => Database.Initialize(connectionString));

// Repository: vseki kontroler poluchava samo interfeisa, ot koito se nuzhdae, no vsichki sochat kum sushtiya Database
builder.Services.AddSingleton<IMovieRepository>(sp => sp.GetRequiredService<IDatabase>());
builder.Services.AddSingleton<IGenreRepository>(sp => sp.GetRequiredService<IDatabase>());
builder.Services.AddSingleton<IDirectorRepository>(sp => sp.GetRequiredService<IDatabase>());

builder.Services.Configure<ExternalApisOptions>(
    builder.Configuration.GetSection("ExternalApis"));

builder.Services.AddHttpClient<IGeminiQueryService, GeminiQueryService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Movies}/{action=Index}/{id?}");

// Suzdava bazata pri start, a ne pri purvata zaqvka.
app.Services.GetRequiredService<IDatabase>();

app.Run();