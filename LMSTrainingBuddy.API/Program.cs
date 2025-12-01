using LMSTrainingBuddy.API.BackgroundJobs;
using LMSTrainingBuddy.API.BackgroundJobs.Schedulers;
using LMSTrainingBuddy.API.Infrastructure.Data;
using LMSTrainingBuddy.API.Infrastructure.Middleware;
using LMSTrainingBuddy.API.Infrastructure.OpenAI;
using LMSTrainingBuddy.API.Infrastructure.VectorStore;
using LMSTrainingBuddy.API.Models;
using LMSTrainingBuddy.API.Repositories;
using LMSTrainingBuddy.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();

builder.Services.AddSingleton<IOpenAiClient, FakeOpenAiClient>();
builder.Services.AddSingleton<IVectorStore, InMemoryVectorStore>();

builder.Services.Configure<AppSettings>(builder.Configuration);

var appSettings = builder.Configuration.Get<AppSettings>() ?? new AppSettings();

var enlightDbConnectionString = appSettings.ConnectionStrings.EnlightDatabase;
var courseDbConnectionString = appSettings.ConnectionStrings.CourseDatabase;

builder.Services.AddDbContextFactory<EnlightDbContext>(options =>
    options.UseSqlServer(enlightDbConnectionString));

builder.Services.AddDbContextFactory<CourseDbContext>(options =>
    options.UseSqlServer(courseDbConnectionString));

builder.Services.AddSingleton<IEnlightCourseRepository>(serviceProvider =>
{
    var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<EnlightDbContext>>();
    return new EnlightCourseRepository(dbContextFactory);
});

builder.Services.AddSingleton<ITrainingBuddyCourseRepository>(serviceProvider =>
{
    var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<CourseDbContext>>();
    return new TrainingBuddyCourseRepository(dbContextFactory);
});

//builder.Services.AddHostedService<CourseMetadataSyncScheduler>();
//builder.Services.AddHostedService<OpenAIVectorStoreSyncScheduler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.Run();

