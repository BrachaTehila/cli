using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Text.RegularExpressions;

var outputOption = new Option<FileInfo>(new[] { "--output", "-o" }, "file path and name")
{
    IsRequired = true
};
var languageOption = new Option<string[]>(new[] { "--languages", "-l" }, "Languages to include in the bundle")
{
    IsRequired = true
};
var noteOption = new Option<bool>(new[] { "--note", "-n" }, "add path file");
var sortOption = new Option<string>(new[] { "--sort", "-s" }, "sort by fileName or by file type");
var removeEmptyLinesOption = new Option<bool>(new[] { "--remove-empty-lines", "-rel" }, "remove empty lines");
var authorOption = new Option<string>(new[] { "--author", "-a" }, "add author name");

var bundleCommand = new Command("bundle", "Bundle code files to a single file");

bundleCommand.AddOption(outputOption);
bundleCommand.AddOption(languageOption);
bundleCommand.AddOption(noteOption);
bundleCommand.AddOption(sortOption);
bundleCommand.AddOption(removeEmptyLinesOption);
bundleCommand.AddOption(authorOption);


bundleCommand.SetHandler((output, languages, note, sort, removeEmptyLines, author) =>
{
    try
    {
        using (var stream = File.Create(output.FullName))
        {
            using (var writer = new StreamWriter(stream))
            {
                if (!string.IsNullOrEmpty(author))
                {
                    writer.WriteLine($"//Author: {author}");
                }

                var filesInDirectory = Directory.GetFiles(Environment.CurrentDirectory);
                var filteredFiles = filesInDirectory
            .Where(file => languages.Contains("all") || languages.Contains(Path.GetExtension(file).Substring(1)));

                switch (sort?.ToLower())
                {
                    case "type":
                        filteredFiles = filteredFiles.OrderBy(file => Path.GetExtension(file)).ToArray();
                        break;
                    case "name":
                        filteredFiles = filteredFiles.OrderBy(file => Path.GetFileName(file)).ToArray();
                        break;
                    default:
                        filteredFiles = filteredFiles.OrderBy(file => Path.GetFileName(file)).ToArray();
                        break;
                }
                foreach (var file in filteredFiles)
                {
                    if (file != stream.Name || Path.GetExtension(file).Equals(".rsp"))
                    {
                        if (note)
                        {
                            writer.WriteLine($"// Source: {file}");
                        }
                        string content = File.ReadAllText(file);
                        if (removeEmptyLines)
                        {
                            content = Regex.Replace(content, @"\r?\n\s*\r?\n", Environment.NewLine);
                        }
                        writer.WriteLine(content);
                        writer.WriteLine();
                        Console.WriteLine(file);
                    }
                }
            }
        }
    }

    catch (DirectoryNotFoundException ex)
    {
        Console.WriteLine("erorr: file path is invalid");
    }


}, outputOption, languageOption, noteOption, sortOption, removeEmptyLinesOption, authorOption);


var createRspCommand = new Command("create-rsp", "create a response file with a ready-to-use bundle command");

createRspCommand.SetHandler((command) =>
{

    try
    {
        Console.Write("Enter the desired output file name:");
        string outputFile = Console.ReadLine();
        Console.Write("Enter the language(s) to include (comma-separated or 'all'):");
        string language = Console.ReadLine();
        Console.Write("Do you want to add file paths to the output(y/n)? ");
        bool note = Console.ReadLine().ToLower() == "y";
        Console.Write("Do you want to sort the files by (name/type/n)?");
        string sort = Console.ReadLine();
        Console.Write("Do you want to remove empty lines from code (y/n)?");
        bool removeEmptyLines = Console.ReadLine().ToLower() == "y";
        Console.Write("Enter the author name (optional):");
        string author = Console.ReadLine();
        string rspContent = $"fib bundle -o {outputFile}.txt -l {language}";
        if (note)
        {
            rspContent += " -n";

        }
        if (!string.IsNullOrEmpty(sort) && !sort.ToLower().Equals("n"))
        {
            rspContent += $" -s {sort}";
        }
        if (removeEmptyLines)
        {
            rspContent += " -rel";
        }
        if (!string.IsNullOrEmpty(author))
        {
            rspContent += $" -a {author}";
        }
        using (var stream = File.Create($"{outputFile}.rsp"))
        {
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(rspContent);
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error creating response file: " + ex.Message);
    }
});
var rootCommand = new RootCommand("Root command for File Bundle CLI");
rootCommand.AddCommand(bundleCommand);
rootCommand.AddCommand(createRspCommand);

rootCommand.InvokeAsync(args);
