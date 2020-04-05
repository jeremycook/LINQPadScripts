<Query Kind="Program">
  <NuGetReference>AngleSharp</NuGetReference>
  <NuGetReference>HtmlSanitizer</NuGetReference>
  <NuGetReference>TidyNetPortable</NuGetReference>
  <Namespace>AngleSharp</Namespace>
  <Namespace>AngleSharp.Dom</Namespace>
  <Namespace>Ganss.XSS</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

async Task Main()
{
	configuration = AngleSharp.Configuration.Default.WithDefaultLoader();
	context = BrowsingContext.New(configuration);

	sanitizer.OutputFormatter = new AngleSharp.Html.PrettyMarkupFormatter { };
	sanitizer.AllowedAttributes.Clear();
	sanitizer.AllowedSchemes.Add("mailto");

	await GetPageAsync(host.AbsoluteUri);

	results.Values.OrderBy(v => !v.ErroredWhileScraping).Dump();
}

Uri host = new Uri(Util.ReadLine("Starting URL"));
IConfiguration configuration;
IBrowsingContext context;
readonly HtmlSanitizer sanitizer = new HtmlSanitizer();
readonly Dictionary<string, Page> results = new Dictionary<string, UserQuery.Page>(StringComparer.InvariantCultureIgnoreCase);

async Task GetPageAsync(string url)
{
	var uri = new Uri(host, url);
	if (results.ContainsKey(uri.PathAndQuery))
	{
		return;
	}

	var page = new Page(uri.PathAndQuery);
	try
	{
		await context.OpenAsync(url);
		var contentSearch = context.Active.Body;

		page.OriginalHtml = contentSearch.Html();
		page.SimplifedHtml = sanitizer.Sanitize(page.OriginalHtml);
		page.Text = contentSearch.Text();
		page.Title = context.Active.QuerySelector("h1")?.Text() ?? context.Active.QuerySelector("title")?.Text();
		if (page.Title != null)
		{
			page.Title = Regex.Match(page.Title, "$[^-]+").Value.Trim();
		}
	}
	catch (Exception ex)
	{
		page.ErroredWhileScraping = true;
		page.Exception = ex;
	}
	finally
	{
		results.Add(page.Url, page);
	}

	var localUrls = context.Active.QuerySelectorAll($"a[href^='{host.AbsoluteUri}'], a[href^='/'], a[href^='.'], img[src^='{host.AbsoluteUri}'], img[src^='/'], img[src^='.']")
		.Select(s => s.GetAttribute("href") ?? s.GetAttribute("src"));

	foreach (var href in localUrls)
	{
		await GetPageAsync(href);
	}
}

string GoTidy(string input)
{
	var tidy = new Tidy.Core.Tidy();

	using (var mem = new MemoryStream())
	using (var sr = new StreamReader(mem))
	{
		tidy.Parse(input, mem);
		mem.Position = 0;
		return sr.ReadToEnd();
	}
}

class Page
{
	public Page(string url)
	{
		Url = url;
	}

	public bool ErroredWhileScraping { get; set; }
	public Exception Exception { get; set; }
	
	public string Url { get; set; }
	public string Title { get; set; }
	public string SimplifedHtml { get; set; }
	public string Text { get; set; }
	public string OriginalHtml { get; set; }
}