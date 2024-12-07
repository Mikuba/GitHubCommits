using GitHubCommits;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddHttpClient()
.AddDbContext<GitHubCommitsContext>(options => options.UseSqlite($"Data Source={SetDbPath()}"))
.AddTransient<GitHubCommitService>();

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

using IHost host = builder.Build();

var gitService = host.Services.GetRequiredService<GitHubCommitService>();

if (args.Length != 2 || (args.Length == 2 &&(String.IsNullOrEmpty(args[0]) || String.IsNullOrEmpty(args[1]))))
{
    Console.WriteLine("usage: GitHubCommits.exe username repositoryname");
    Console.ReadKey();
}
else
{
     await gitService.ExecuteAsync(args[0], args[1]); 
}

string SetDbPath()
{
    var folder = Environment.SpecialFolder.LocalApplicationData;
    var path = Environment.GetFolderPath(folder);
    return System.IO.Path.Join(path, "githubcommits.db");
}