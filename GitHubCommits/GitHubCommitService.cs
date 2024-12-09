using System.Net.Http.Json;
using System.Reflection.Metadata;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GitHubCommits
{
    public class GitHubCommitService
    {
        private IHttpClientFactory _httpClientFactory;
        private ILogger _logger;
        private GitHubCommitsContext _context;
        private string _gitHubApi;
       
       
        public GitHubCommitService(IHttpClientFactory httpClientFactory, ILogger<GitHubCommit> logger,GitHubCommitsContext context, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _context = context;
            _gitHubApi = config.GetSection("GitHubApi").Value;

        }

        public async Task ExecuteAsync(string userName, string repositoryName)
        {
            var commits =  await RetrieveCommits(userName, repositoryName);

            if (commits != null)
            {
                await DbSave(userName, repositoryName, commits);
            }

            PrintCommits(commits, userName, repositoryName);
        }

        private void PrintCommits(GitHubCommit.Root[]? commits, string userName, string repositoryName)
        {
            _logger.LogWarning("===================================================");
            _logger.LogWarning("|Following commits have been retrieved :           |");
            _logger.LogWarning("===================================================");
            foreach(var commit in commits)
            {
                string committer, message;
                HandleNullValues(commit, out committer, out message);
                _logger.LogWarning($@"[{repositoryName}]/[{commit.sha}]: {message} [{committer}]");
            }
            _logger.LogInformation("Those will be saved in database, unless they already exist");
        }

        //PLEASE NOTE THAT IN ENTERPRISE ENVIRONMENT THE DB CALLS WOULD BE HANDLED BY DEDICATED API
        //THE SCOPE OF THIS EXERCISE IS HOWEVER TO HAVE CONSOLE APP SAVING IT
        private async Task DbSave(string userName, string RepositoryName, GitHubCommit.Root[]? commits)
        {
            _context.Database.EnsureCreated();
            var transaction = _context.Database.BeginTransaction();
            try
            {
                List<string> shas = _context.GitHubCommit.Select(c => c.Sha).ToList();
                foreach (var commit in commits)
                {
                    string commiter, message;
                    HandleNullValues(commit, out commiter, out message);

                    //sha is unique 
                    if (!shas.Contains(commit.sha))
                    {

                        _context.GitHubCommit.Add(new Commit()
                        {
                            Committer = commiter,
                            Message = message,
                            RepoName = RepositoryName,
                            Sha = commit.sha,
                            UserName = userName
                        });
                    }
                }
                await _context.SaveChangesAsync();
                transaction.Commit();
            }
            catch (Exception e)
            {
                //It is a need to have all commits, so if one fails it is rolled back
                transaction.Rollback();
                throw;
            }

        }

        static private  void HandleNullValues(GitHubCommit.Root commit, out string committer, out string message)
        {
            committer = commit.author != null && !String.IsNullOrEmpty(commit.author.login) ? commit.author.login : "unknown";
            message = commit.commit != null && !String.IsNullOrEmpty(commit.commit.message) ? commit.commit.message : String.Empty;
        }

   
        private async Task<GitHubCommit.Root[]?> RetrieveCommits(string userName , string repositoryName)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(_gitHubApi);

                client.DefaultRequestHeaders.Add("User-Agent", "request");

                return await client.GetFromJsonAsync<GitHubCommit.Root[]>(
                $"{userName}/{repositoryName}/commits",
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            }
            catch (HttpRequestException hre)
            {
                if (hre.StatusCode.Value == System.Net.HttpStatusCode.Conflict)
                {
                    _logger.LogWarning($"The repository {repositoryName} for user {userName} has no commits");               
                }
                if (hre.StatusCode.Value == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning($"The repository {repositoryName} for user {userName} does not exists");
                                      
                }

                throw;
            }

            catch (Exception e)
            {
                _logger.LogWarning(e.Message);
                throw;
            }
        }
    }
}
