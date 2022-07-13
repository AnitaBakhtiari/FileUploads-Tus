using Context.Actions.Interfaces;
using Context.Tasks.Interface;
using Core;
using Core.Middleware;
using DataCore.Tasks.Interface;
using Media.Api.Controllers;
using Media.Application.Action;
using Media.Application.Task;
using Media.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using tusdotnet.ExternalMiddleware.EndpointRouting;
using tusdotnet.Helpers;
using tusdotnet.Models;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<Media.Domain.Context>(option =>
    option.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSql")));

builder.Services.AddJwt(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();


builder.Services.AddScoped<IMediaRepository, MediaRepository>();

builder.Services.AddScoped(typeof(IAction2<Task<bool>, HttpContext, string>), typeof(DownloadFileAction));
builder.Services.AddScoped(typeof(IAction2<Task<bool>, HttpContext, string>), typeof(DownloadFileActionByUserId));
builder.Services.AddScoped(typeof(IAction2<Task<bool>, CreateContext, CancellationToken>), typeof(UploadMediaAction));

builder.Services.AddScoped(typeof(IRepositoryTask2<IMediaRepository, bool, string, string>), typeof(DownloadFileTask));
builder.Services.AddScoped(
    typeof(IRepositoryTask6<IMediaRepository, bool, string, string, string, string, string, string>),
    typeof(AddMediaRepositoryTask));

builder.Services.AddScoped(typeof(ITask1<List<string>, IDictionary<string, Metadata>>),
    typeof(ValidateMetadataMediaTask));


builder.Services.AddScoped(typeof(StorageService<ITusConfigurator>));


builder.Services.AddScoped<ITusConfigurator, MyTusConfigurator>();


builder.Services.AddTus()
    .AddConfigurator<MyTusConfigurator>()
    .AddController<MyTusController, MyTusConfigurator>();

builder.Services.AddAuthorization(opt =>
    opt.AddPolicy("create-file-policy", builder => builder.RequireRole("create-file")));


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();

app.UseCors(builder => builder
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowAnyOrigin()
    .WithExposedHeaders(CorsHelper.GetExposedHeaders()));


app.UseHttpsRedirection();


app.UseMiddleware<ContainerMiddleware>();

app.UseRouting();

app.UseAuthorization();


app.UseEndpoints(
    endpoints => endpoints.MapTus<MyTusController, MyTusConfigurator>("/files") /*.RequireAuthorization()*/);

app.MapControllers();


app.Run();
