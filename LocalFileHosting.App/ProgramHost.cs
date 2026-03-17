using LocalFileHosting.Core.Interfaces;
using LocalFileHosting.Core.Logging;
using LocalFileHosting.Core.Services;
using LocalFileHosting.Core.Storage;
using LocalFileHosting.Core.Extensions;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LocalFileHosting.App
{
    // Must have the same namespace and class name
    public partial class Program
    {
        static async Task RunHost()
        {
            {

                var folderPath = ConsoleExplorer.BrowseDirectory("Select folder to host:");
                var port = AnsiConsole.Ask<int>("Enter [green]port[/] to listen on (e.g., 5050):", 5050);
                // AllowEmpty means they can just press Enter to skip having a password
                var _roomPassword = AnsiConsole.Prompt(
                    new TextPrompt<string>("Set a room [green]password[/] (Press Enter to leave open):")
                        .AllowEmpty()
                        .Secret()); // Secret() hides the typing with asterisks!
                var logger = new ConsoleLogger();
                var storage = new LocalStorageProvider(long.MaxValue);
                var service = new FileHostingService(logger, storage);

                void RefreshHostedFiles()
                {
                    var files = Directory.GetFiles(folderPath);
                    foreach (var f in files)
                    {
                        var info = new FileInfo(f);
                        if (!storage.GetAllFiles().Any(x => x.Name == info.Name))
                        {
                            service.AddFiles(FileCategorizer.Categorize(f));
                        }
                    }
                }
                RefreshHostedFiles();

                using var listener = new HttpListener();

                // Try to bind to all interfaces, fallback to localhost if Access Denied
                try
                {
                    listener.Prefixes.Add($"http://*:{port}/");
                    listener.Start();
                }
                catch (HttpListenerException ex) when (ex.ErrorCode == 5) // Access Denied
                {
                    AnsiConsole.MarkupLine("[yellow]Admin rights required to listen on all interfaces. Falling back to localhost only.[/]");
                    listener.Prefixes.Clear();
                    listener.Prefixes.Add($"http://localhost:{port}/");
                    listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                    listener.Start();
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Failed to start server: {Markup.Escape(ex.Message)}[/]");
                    return;
                }

                AnsiConsole.MarkupLine($"[green]Hosting started on port {port}.[/]");

                using var scanner = new NetworkScanner(Environment.MachineName, port);
                scanner.StartBroadcasting();
                AnsiConsole.MarkupLine("[blue]Network discovery broadcast started.[/]");

                var listenTask = Task.Run(async () =>
                {
                    while (listener.IsListening)
                    {
                        var context = await listener.GetContextAsync();

                        // FIRE AND FORGET - Do not 'await' this here!
                        _ = Task.Run(async () => {
                            try
                            {
                                await HandleRequest(context, folderPath, storage, _roomPassword);
                            }
                            catch (Exception ex)
                            {
                                // This forces a log even if the menu is active
                                Console.Title = $"Last Error: {ex.Message}";
                            }
                        });
                    }
                });

                while (true)
                {
                    var action = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Host Dashboard:")
                            .AddChoices("View Statistics", "List Hosted Files", "Refresh Folder", "Stop Hosting"));

                    if (action == "Stop Hosting")
                    {
                        listener.Stop();
                        break;
                    }
                    else if (action == "View Statistics")
                    {
                        var stats = service.GetStats();
                        var table = new Table();
                        table.AddColumn("Metric");
                        table.AddColumn("Value");
                        table.AddRow("Total Files", stats.TotalFiles.ToString());
                        table.AddRow("Total Size", $"{stats.TotalSize / 1024.0 / 1024.0:F2} MB");
                        table.AddRow("Last Updated", stats.LastUpdated.ToString("T"));
                        AnsiConsole.Write(table);
                    }
                    else if (action == "List Hosted Files")
                    {
                        var table = new Table();
                        table.AddColumn("Name");
                        table.AddColumn("Type");
                        table.AddColumn("Size");
                        foreach (var f in storage.GetAllFiles())
                        {
                            table.AddRow(Markup.Escape(f.Name), f.GetType().Name, f.ToHumanReadableSize());
                        }
                        AnsiConsole.Write(table);
                    }
                    else if (action == "Refresh Folder")
                    {
                        RefreshHostedFiles();
                        AnsiConsole.MarkupLine("[green]Folder refreshed.[/]");
                    }
                }
            }
        }

        static async Task HandleRequest(HttpListenerContext context, string folderPath, IStorageProvider storage, string? roomPassword)
        {
            var req = context.Request;
            var res = context.Response;
            var clientEndPoint = req.RemoteEndPoint?.ToString() ?? "Unknown";

            try
            {
                // Log to Title bar so we don't mess with the Spectre Menu
                Console.Title = $"[REQ] {req.Url?.AbsolutePath} from {clientEndPoint}";

                // 1. Password check
                string? clientPass = req.Headers["X-Room-Password"];
                if (!string.IsNullOrEmpty(roomPassword) && clientPass != roomPassword)
                {
                    res.StatusCode = 401;
                    res.Close();
                    return;
                }

                if (req.Url!.AbsolutePath == "/api/files")
                {
                    // TO LIST ensures we don't hold the disk open
                    var files = storage.GetAllFiles().ToList();
                    var dto = files.Select(f => new { f.Name, Type = f.GetType().Name, f.Size }).ToList();

                    byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(dto);

                    res.ContentType = "application/json";
                    res.ContentLength64 = bytes.Length;
                    res.StatusCode = 200;

                    // USE THE BASE STREAM
                    await res.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                    await res.OutputStream.FlushAsync();
                }
                else if (req.Url.AbsolutePath == "/api/download")
                {
                    var fileName = req.QueryString["name"];
                    var filePath = Path.Combine(folderPath, fileName ?? "");

                    if (File.Exists(filePath))
                    {
                        res.ContentType = "application/octet-stream";
                        res.AddHeader("Content-Disposition", $"attachment; filename=\"{Uri.EscapeDataString(fileName!)}\"");
                        using var fs = File.OpenRead(filePath);
                        res.ContentLength64 = fs.Length;
                        await fs.CopyToAsync(res.OutputStream);
                        await res.OutputStream.FlushAsync();
                    }
                    else { res.StatusCode = 404; }
                }
            }
            catch (Exception ex)
            {
                res.StatusCode = 500;
            }
            finally
            {
                // THIS IS THE ONLY WAY TO PREVENT THE HANG
                try { res.Close(); } catch { }
            }
        }
    }
}