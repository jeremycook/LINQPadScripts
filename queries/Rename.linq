<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Windows.Forms.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Security.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Configuration.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\Accessibility.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Deployment.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Runtime.Serialization.Formatters.Soap.dll</Reference>
  <Namespace>System.Windows.Forms</Namespace>
</Query>

void Main()
{
	string rootDir = Util.ReadLine("Path to Root Directory").Dump("Root Directory");

	if (!Directory.Exists(rootDir))
	{
		throw new DirectoryNotFoundException($"Directory not found: '{rootDir}'.");
	}

	var tokens = new Dictionary<string, string>();
	while (true)
	{
		var searchToken = Util.ReadLine("Search Token");

		if (string.IsNullOrWhiteSpace(searchToken))
		{
			break;
		}

		tokens.Add(searchToken, Util.ReadLine("Replacement Token"));
	}
	tokens.Dump("Tokens");

	var folderPaths = new[] { rootDir }
		.Union(Directory.GetDirectories(rootDir, @"*", SearchOption.AllDirectories))
		.Where(d => !Regex.IsMatch(d, @"\\(bin|obj|\.git|\.vs)(\\|$)"))
		.Select(d => new
		{
			CurrentPath = d.Replace(rootDir, ""),
			NewPath = ReplaceTokens(d.Replace(rootDir, ""), tokens),
		});

	var filePaths = folderPaths
		.SelectMany(d => Directory.GetFiles(rootDir + d.CurrentPath)
		.Select(p => new
		{
			CurrentFile = rootDir + d.CurrentPath + @"\" + Path.GetFileName(p),
			NewFile = rootDir + d.CurrentPath + @"\" + ReplaceTokens(Path.GetFileName(p), tokens),
		}))
		.OrderBy(p => p.CurrentFile);
	filePaths
		.Where(p => p.CurrentFile != p.NewFile)
		.Dump("Renamed Files");

	var renamedFolders = folderPaths
		.Where(p => p.CurrentPath != p.NewPath)
		.OrderBy(f => f.CurrentPath)
		.Dump("Renamed Folders");

	if (Util.ReadLine("Proceed with renaming files?", true) == false)
	{
		"Rename cancelled.".Dump("Aborted");
		return;
	}

	var errors = new List<Exception>();

	// Rename all the files and contents
	foreach (var path in filePaths)
	{
		if (path.CurrentFile != path.NewFile)
		{
			File.Move(path.CurrentFile, path.NewFile);
		}

		bool neededToReplace = false;
		string content = File.ReadAllText(path.NewFile);
		foreach (var item in tokens)
		{
			if (content.Contains(item.Key))
			{
				neededToReplace = true;
				content = content.Replace(item.Key, item.Value);
			}
		}

		if (neededToReplace)
		{
			try
			{
				File.WriteAllText(path.NewFile, content);
			}
			catch (Exception ex)
			{
				errors.Add(ex);
			}
		}
	}

	// Move directories.
	foreach (var folder in renamedFolders)
	{
		try
		{
			// Don't need to move folders if a parent folder was already moved.
			if (Directory.Exists(rootDir + folder.CurrentPath))
			{
				Directory.Move(rootDir + folder.CurrentPath, rootDir + folder.NewPath);
			}
		}
		catch (Exception ex)
		{
			errors.Add(ex);
		}
	}

	errors.Dump("Errors");
}

string ReplaceTokens(string text, IDictionary<string, string> tokens)
{
	foreach (var item in tokens)
	{
		text = text.Replace(item.Key, item.Value);
	}
	return text;
}
