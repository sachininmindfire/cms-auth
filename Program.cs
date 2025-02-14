using Azure.Identity;
using CMS.Auth.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.EnvironmentName == "Production")
{
    // Add Azure Key Vault
    var keyVaultEndpoint = new Uri(builder.Configuration["KeyVault:Endpoint"]);
    builder.Configuration.AddAzureKeyVault(keyVaultEndpoint, new DefaultAzureCredential());
}

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.Production.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddControllers();
// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        builder =>
        {
            builder.WithOrigins("http://localhost:4200")
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

var dbServer = builder.Configuration["CMSAuth-DbServer"];
var dbName = builder.Configuration["CMSAuth-DbName"];
var dbUsername = builder.Configuration["CMSAuth-DbUserName"];
var dbPassword = builder.Configuration["CMSAuth-DbPassword"];

Console.WriteLine($"dbServer: {dbServer}");
Console.WriteLine($"dbName: {dbName}");
Console.WriteLine($"dbUsername: {dbUsername}");
Console.WriteLine($"dbPassword: {dbPassword}");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    .Replace("{DbServer}", dbServer)
    .Replace("{DbName}", dbName)
    .Replace("{DbUsername}", dbUsername)
    .Replace("{DbPassword}", dbPassword);

// Configure Entity Framework and Identity
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 1;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme);
    //.AddJwtBearer(options =>
    //{
    //    options.TokenValidationParameters = new TokenValidationParameters
    //    {
    //        ValidateIssuer = true,
    //        ValidateAudience = true,
    //        ValidateLifetime = true,
    //        ValidateIssuerSigningKey = true,
    //        ValidIssuer = "yourIssuer",
    //        ValidAudience = "yourAudience",
    //        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ThisIsMyAndMyFriendAlsoYourSecretKeyDoWeNeedMoreLetters"))
    //    };
    //});

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors("AllowAngularApp");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
