using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

public class GitHubCommitsContext : DbContext
{
    public DbSet<Commit> GitHubCommit { get; set; }
    
    public string DbPath { get; }

    public GitHubCommitsContext(DbContextOptions<GitHubCommitsContext> options) :base (options)
    {     
    }
    
   
}

public class Commit
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ID { get; private set; }

    public required string UserName { get; set; }

    public required string RepoName { get; set; }

    public required string Sha { get; set; }

    public required string Message { get; set; }

    public string Committer { get; set; }

}
