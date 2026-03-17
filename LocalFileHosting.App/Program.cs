using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Spectre.Console;
using LocalFileHosting.Core.Models;
using LocalFileHosting.Core.Services;
using LocalFileHosting.Core.Enums;
using LocalFileHosting.Core.Logging;
using LocalFileHosting.Core.Storage;
using LocalFileHosting.Core.Extensions;
using LocalFileHosting.Core.Interfaces;

namespace LocalFileHosting.App
{
    // PARTIAL CLASS
    public partial class Program
    {
        static string _userName = "Anonymous";
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        static async Task Main()
        {
            Console.Clear();
            AnsiConsole.Write(new FigletText("Local File Host").Color(Color.Blue));
            _userName = AnsiConsole.Ask<string>("Enter your [green]display name[/]:");

            while (true)
            {
                var mode = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title($"[bold green]Welcome, {_userName}[/]\nSelect mode:")
                        .AddChoices(["Host a folder", "Connect as Client", "Exit" ]));

                if (mode == "Host a folder") await RunHost();
                else if (mode == "Connect as Client") await RunClient();
                else break;
            }
        }
    }
}