


using System.Globalization;
using LarpMovies.Data;

// Fiksirame kulturata, za da se razbirat drobnite chisla s tochka (8.3) na vsichki kompyutri haeh
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("MoviesDb")
    ?? "Data Source=App_Data/movies.db";
var dataSource = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString).DataSource;
if (!Path.IsPathRooted(dataSource))
{
    connectionString = $"Data Source={Path.Combine(builder.Environment.ContentRootPath, dataSource)}";
}

// Singleton: cialoto prilozhenie polzva edin i sushti Database prez IDatabase.
builder.Services.AddSingleton<IDatabase>(_ => Database.Initialize(connectionString));

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