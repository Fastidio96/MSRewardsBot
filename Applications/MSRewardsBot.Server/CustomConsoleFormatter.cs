using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;


namespace MSRewardsBot.Server
{
    public sealed class CustomConsoleFormatter : ConsoleFormatter
    {
        private CustomConsoleOptions _options;
        private TextWriter _writer;

        private string _previousCategory = null;
        private bool _isSameCategory = false;

        string AnsiFg(ConsoleColor color) => color switch
        {
            ConsoleColor.Black => "\u001b[30m",
            ConsoleColor.DarkRed => "\u001b[31m",
            ConsoleColor.DarkGreen => "\u001b[32m",
            ConsoleColor.DarkYellow => "\u001b[33m",
            ConsoleColor.DarkBlue => "\u001b[34m",
            ConsoleColor.DarkMagenta => "\u001b[35m",
            ConsoleColor.DarkCyan => "\u001b[36m",
            ConsoleColor.Gray => "\u001b[37m",

            ConsoleColor.DarkGray => "\u001b[90m",
            ConsoleColor.Red => "\u001b[91m",
            ConsoleColor.Green => "\u001b[92m",
            ConsoleColor.Yellow => "\u001b[93m",
            ConsoleColor.Blue => "\u001b[94m",
            ConsoleColor.Magenta => "\u001b[95m",
            ConsoleColor.Cyan => "\u001b[96m",
            ConsoleColor.White => "\u001b[97m",

            _ => ""
        };

        private static readonly Regex AnsiRegex = new Regex(@"\u001b\[[0-9;]*m", RegexOptions.Compiled);

        const string ResetColor = "\u001b[0m";

        public CustomConsoleFormatter(IOptions<CustomConsoleOptions> options) : base(nameof(CustomConsoleFormatter))
        {
            _options = options.Value;
        }

        public override void Write<TState>(
            in LogEntry<TState> logEntry,
            IExternalScopeProvider scopeProvider,
            TextWriter textWriter)
        {
            _writer = textWriter;
            LogLevel logLevel = logEntry.LogLevel;

            if (_options.GroupedCategories)
            {
                if (_previousCategory == null)
                {
                    _previousCategory = logEntry.Category;
                    _isSameCategory = false;
                }
                else
                {
                    _isSameCategory = logEntry.Category == _previousCategory;
                    if (!_isSameCategory)
                    {
                        _previousCategory = logEntry.Category;
                    }
                }
            }

            string category = FormatCategory(logEntry.Category);
            string message = logEntry.Formatter(logEntry.State, logEntry.Exception);

            string timestamp = DateTime.Now.ToString(_options.TimestampFormat ?? "HH:mm:ss");


            string consoleMsg = $"{timestamp} [{logLevel}] [{category}]: {message}";
            if (_options.GroupedCategories)
            {
                consoleMsg = $"{timestamp} ";
                consoleMsg += ApplyColor(logLevel, $"[{logLevel}]: {message}");
                consoleMsg = consoleMsg.PadLeft(consoleMsg.Length + 3);

                if (!_isSameCategory)
                {
                    string toWrite = ApplyColor(null, $"[{category}]");
                    _writer.WriteLine(toWrite);
                    WriteOnFile(toWrite);
                }
            }

            _writer.WriteLine(consoleMsg);
            WriteOnFile(consoleMsg);

            if (logEntry.Exception != null)
            {
                string toWrite = ApplyColor(logLevel, logEntry.Exception.Message);
                _writer.WriteLine(toWrite);
                WriteOnFile(toWrite);
            }
        }

        private string FormatCategory(string category)
        {
            int lastDot = category.LastIndexOf('.');
            if (lastDot >= 0)
            {
                category = category.Substring(lastDot + 1);
            }

            return category;
        }

        private string ApplyColor(LogLevel? logLevel, string message)
        {
            if (_options.UseColors)
            {
                ConsoleColor color;

                if (!logLevel.HasValue)
                {
                    color = ConsoleColor.White;
                }
                else
                {
                    switch (logLevel)
                    {
                        case LogLevel.Trace:
                            { color = ConsoleColor.DarkGray; break; }
                        case LogLevel.Debug:
                            { color = ConsoleColor.Gray; break; }
                        case LogLevel.Information:
                            { color = ConsoleColor.Green; break; }
                        case LogLevel.Warning:
                            { color = ConsoleColor.Yellow; break; }
                        case LogLevel.Error:
                            { color = ConsoleColor.Red; break; }
                        case LogLevel.Critical:
                            { color = ConsoleColor.DarkRed; break; }
                        default:
                            { color = ConsoleColor.DarkGray; break; }
                    }
                }

                message = AnsiFg(color) + message + ResetColor;
            }

            return message;
        }

        private bool WriteOnFile(string log)
        {
            if (!_options.WriteOnFile)
            {
                return true;
            }

            string path = Utils.GetLogFile();

            try
            {
                log = AnsiRegex.Replace(log, "");

                using (FileStream fs = File.Open(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    sw.WriteLine(log);
                }

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(ApplyColor(LogLevel.Critical, e.Message));
                return false;
            }
        }
    }

    public class CustomConsoleOptions : ConsoleFormatterOptions
    {
        public bool UseColors { get; set; } = true;
        public bool GroupedCategories { get; set; } = false;
        public bool WriteOnFile { get; set; } = true;
    }
}
