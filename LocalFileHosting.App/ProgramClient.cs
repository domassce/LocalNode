using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace LocalFileHosting.App
{
    public partial class Program
    {
        static async Task RunClient()
        {
            string serverUrl = "";
            string selectedHostDisplayName = "Manual Entry"; // Define it here so it's "in scope" later
            using var scanner = new NetworkScanner(Environment.MachineName, 0);
            scanner.StartListening();

            AnsiConsole.MarkupLine("[yellow]Scanning for local hosts...[/]");

            var hosts = new List<DiscoveredHost>();

            await AnsiConsole.Status()
                .StartAsync("Scanning...", async ctx =>
                {
                    for (int i = 0; i < 30; i++)
                    {
                        await Task.Delay(100);
                        hosts = scanner.GetActiveHosts();
                        if (hosts.Count > 0) break;
                    }
                });

            if (hosts.Count > 0)
            {
                var choices = new List<string> { "Enter URL manually" };
                choices.AddRange(hosts.Select(h => $"{h.Name} ({h.Url})"));

                // Store the result in our variable defined outside this block
                selectedHostDisplayName = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select a discovered host or enter manually:")
                        .UseConverter(c => Markup.Escape(c))
                        .AddChoices(choices));

                if (selectedHostDisplayName != "Enter URL manually")
                {
                    serverUrl = hosts.First(h => $"{h.Name} ({h.Url})" == selectedHostDisplayName).Url;
                }
            }

            if (string.IsNullOrEmpty(serverUrl))
            {
                serverUrl = AnsiConsole.Ask<string>("Enter [green]Server URL[/]:");
            }
            AnsiConsole.Clear();
            // Use 'Justify.Left' specifically from Spectre.Console
            var rule = new Rule($"[bold green]Connected to: {Markup.Escape(selectedHostDisplayName)}[/]");
            rule.Justification = Justify.Left;
            AnsiConsole.Write(rule);
            string? currentPassword = null;
            using var client = new HttpClient();

            while (true)
            {

                // ... inside the while(true) loop ...

                AnsiConsole.MarkupLine("[yellow]Fetching file list...[/]");
                List<FileDto> files;
                // Inside the while(true) loop in RunClient
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, $"{serverUrl.TrimEnd('/')}/api/files");

                    if (!string.IsNullOrEmpty(currentPassword))
                    {
                        request.Headers.Add("X-Room-Password", currentPassword);
                    }

                    // 1. Send the request
                    var response = await client.SendAsync(request);

                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        // If this prompt triggers, the code "stops" here until the user types
                        currentPassword = AnsiConsole.Prompt(
                            new TextPrompt<string>("[red]Password required![/] Enter room password:")
                                .Secret());
                        continue; // Jump back to top of 'while' loop to try again with password
                    }

                    // 3. Ensure we got a 200 OK
                    response.EnsureSuccessStatusCode();
                    var json = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[DEBUG] Client received JSON: {json.Length} characters.");   

                    // 5. Deserialize
                    files = JsonSerializer.Deserialize<List<FileDto>>(json, _jsonOptions) ?? new();
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Client Error:[/] {Markup.Escape(ex.Message)}");
                    break;
                }
                if (files == null || files.Count == 0)
                {
                    AnsiConsole.MarkupLine("[red]No files found on the server.[/]");
                    break;
                }

                var choices = new List<FileChoice>
                {
                    new () { DisplayName = "[Download All Files]", IsAction = true },
                    new () { DisplayName = "[Back to Main Menu]", IsAction = true }
                };

                foreach (var f in files)
                {
                    choices.Add(new FileChoice { DisplayName = $"{f.Name} ({f.Type}, {f.Size / 1024} KB)", FileName = f.Name, IsAction = false });
                }

                var selectedFile = AnsiConsole.Prompt(
                    new SelectionPrompt<FileChoice>()
                        .Title("Select a file to [green]download[/]:")
                        .PageSize(15)
                        .UseConverter(c => c.IsAction ? $"[green]{Markup.Escape(c.DisplayName)}[/]" : Markup.Escape(c.DisplayName))
                        .AddChoices(choices));

                if (selectedFile.DisplayName == "[Back to Main Menu]") break;

                var downloadDir = ConsoleExplorer.BrowseDirectory("Select destination folder to save the file(s):");

                if (selectedFile.DisplayName == "[Download All Files]")
                {
                    await AnsiConsole.Progress()
                        .StartAsync(async ctx =>
                        {
                            var tasks = new List<Task>();
                            foreach (var f in files)
                            {
                                var task = ctx.AddTask($"[green]Downloading {Markup.Escape(f.Name)}[/]");
                                tasks.Add(DownloadFileAsync(client, serverUrl, f.Name, downloadDir, task, currentPassword));
                            }
                            await Task.WhenAll(tasks);
                        });
                    AnsiConsole.MarkupLine($"[bold green]Successfully downloaded all files to {Markup.Escape(downloadDir)}[/]");
                }
                else
                {
                    await AnsiConsole.Progress()
                        .StartAsync(async ctx =>
                        {
                            var task = ctx.AddTask($"[green]Downloading {Markup.Escape(selectedFile.FileName ?? "Unknown File")}[/]");
                            await DownloadFileAsync(client, serverUrl, selectedFile.FileName ?? "Unknown File", downloadDir, task, currentPassword);
                        });
                    AnsiConsole.MarkupLine($"[bold green]Successfully downloaded to {Markup.Escape(Path.Combine(downloadDir, selectedFile.FileName ?? "Unknown File"))}[/]");
                }

                AnsiConsole.MarkupLine("\nPress any key to continue...");
                Console.ReadKey(true);
            }
        }

        static async Task DownloadFileAsync(HttpClient client, string serverUrl, string fileName, string downloadDir, ProgressTask task, string? password)
        {
            var savePath = Path.Combine(downloadDir, fileName);

            // Use HttpRequestMessage instead of client.GetAsync to add the header
            var request = new HttpRequestMessage(HttpMethod.Get, $"{serverUrl.TrimEnd('/')}/api/download?name={Uri.EscapeDataString(fileName)}");

            if (!string.IsNullOrEmpty(password))
            {
                request.Headers.Add("X-Room-Password", password);
            }

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            task.MaxValue = totalBytes > 0 ? totalBytes : 100;

            using var stream = await response.Content.ReadAsStreamAsync();
            using var fs = File.Create(savePath);

            var buffer = new byte[8192];
            int bytesRead;
            long totalRead = 0;
            while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
            {
                await fs.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalRead += bytesRead;
                if (totalBytes > 0) task.Value = totalRead;
            }
            task.Value = task.MaxValue;
        }
        public class FileDto
        {
            public required string Name { get; set; }
            public required string Type { get; set; }
            public long Size { get; set; }
        }

        public class FileChoice
        {
            public required string DisplayName { get; set; }
            public  string? FileName { get; set; }
            public bool IsAction { get; set; }
        }

    }
}