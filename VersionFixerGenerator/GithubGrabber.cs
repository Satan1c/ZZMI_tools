using Octokit;

namespace VersionFixerGenerator;

public static class GithubGrabber
{
	private const string GhUsername = "Satan1c";
	private const string GhRepoUsername = "Satan1c";
	private const string GhRepoName = "ZZ-Model-Importer-Assets";
	private const string GhRepoFolder = "PlayerCharacterData";

	private const string BaseAddress = $"https://raw.githubusercontent.com/{GhRepoUsername}/{GhRepoName}/";
	public static async ValueTask<(DateTimeOffset date, string name, string data)[]> Run()
	{
		using var httpClient = new HttpClient();
		httpClient.BaseAddress = new Uri(BaseAddress);
		
		var gh = new GitHubClient(new ProductHeaderValue(GhUsername))
		{
			Credentials = new Credentials(Environment.GetEnvironmentVariable("GH_TOKEN"))
		};
		
		var repo = await gh.Repository.Content.GetAllContents(GhRepoUsername, GhRepoName, GhRepoFolder).ConfigureAwait(false);
		var content = repo.AsParallel().Select(x => x.Path).ToAsyncEnumerable();

		Task<(string name, IReadOnlyList<GitHubCommit> commits)>[] rawTasks = await content
			.Select(async x => (x, await gh.Repository.Commit.GetAll(GhRepoUsername, GhRepoName, new CommitRequest { Path = string.Concat(x.AsSpan(), "/hash.json") }).ConfigureAwait(false)))
			.ToArrayAsync();
		await Task.WhenAll(rawTasks).ConfigureAwait(false);

		
		var raw = rawTasks
			.AsParallel()
			.Select(x => x.Result)
			.SelectMany(x=> x.commits.OrderBy(y => y.Commit.Committer.Date).Select(y => (y.Commit.Committer.Date, string.Concat(y.Sha.AsSpan(), "/", x.name, "/hash.json"))))
			.Select(async x => (x.Date, string.Join('/', x.Item2.Split('/')[1..^1]), await httpClient.GetStringAsync(x.Item2).ConfigureAwait(false)))
			.ToArray();
		await Task.WhenAll(raw).ConfigureAwait(false);

		var hashes = raw.AsParallel().Select(x => x.Result).Select(x => (x.Date, x.Item2, x.Item3)).ToArray();

		return hashes;
	}
}
