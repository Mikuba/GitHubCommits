usage: GitHubCommits.exe username repositoryname
The program will connect to github api for specified user and repo, and collect all commits,
Commits will be then  saved into SQLite database called githubcommits.db.
Database is located at Appdata\Local folder.
After sucessfull database creation all commits will be displayed to console.