using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Spectre.Console;

namespace LocalFileHosting.App
{
    public class DirChoice
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool IsAction { get; set; }
    }

    public static class ConsoleExplorer
    {
        public static string BrowseDirectory(string title, string? startPath = null)
        {
            var currentPath = startPath ?? Directory.GetCurrentDirectory();
            
            while (true)
            {
                var choices = new List<DirChoice>
                {
                    new() { DisplayName = "[Confirm this folder]", Path = currentPath, IsAction = true },
                    new() { DisplayName = "[Go Up]", Path = "UP", IsAction = true },
                    new() { DisplayName = "[Create New Folder]", Path = "NEW", IsAction = true }
                };

                try 
                {
                    var dirs = Directory.GetDirectories(currentPath);
                    foreach(var d in dirs)
                    {
                        choices.Add(new DirChoice { DisplayName = Path.GetFileName(d), Path = d, IsAction = false });
                    }
                }
                catch (UnauthorizedAccessException) { /* Ignore inaccessible folders */ }
                catch (Exception ex) { AnsiConsole.MarkupLine($"[red]Error reading directory: {Markup.Escape(ex.Message)}[/]"); }

                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<DirChoice>()
                        .Title($"[bold yellow]{Markup.Escape(title)}[/]\n[blue]Current:[/] {Markup.Escape(currentPath)}")
                        .PageSize(15)
                        .UseConverter(c => c.IsAction ? $"[green]{Markup.Escape(c.DisplayName)}[/]" : Markup.Escape(c.DisplayName))
                        .AddChoices(choices));

                if (selection.DisplayName == "[Confirm this folder]") return currentPath;
                if (selection.DisplayName == "[Go Up]")
                {
                    var parent = Directory.GetParent(currentPath);
                    if (parent != null) currentPath = parent.FullName;
                }
                else if (selection.DisplayName == "[Create New Folder]")
                {
                    var newFolderName = AnsiConsole.Ask<string>("Enter new folder name:");
                    if (!string.IsNullOrWhiteSpace(newFolderName))
                    {
                        try
                        {
                            var newPath = Path.Combine(currentPath, newFolderName);
                            Directory.CreateDirectory(newPath);
                            currentPath = newPath;
                            AnsiConsole.MarkupLine($"[green]Created folder {Markup.Escape(newFolderName)}[/]");
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[red]Failed to create folder: {Markup.Escape(ex.Message)}[/]");
                        }
                    }
                }
                else
                {
                    currentPath = selection.Path;
                }
            }
        }
    }
}
