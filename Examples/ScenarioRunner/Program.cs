#nullable enable
using System.Collections.ObjectModel;
using System.CommandLine;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drivers;
using UICatalog;

namespace ScenarioRunner;

/// <summary>
///     Command-line tool for running and benchmarking Terminal.Gui scenarios.
/// </summary>
public static class Program
{
    private static LoggingLevelSwitch LogLevelSwitch { get; } = new ();
    private const string LOGFILE_LOCATION = "logs";
    private static string LogFilePath { get; set; } = string.Empty;

    public static int Main (string [] args)
    {
        Console.OutputEncoding = Encoding.Default;

        // Process command line args
        // If no driver is provided, the default driver is used.
        // Get allowed driver names
        string? [] allowedDrivers = DriverRegistry.GetDriverNames ().ToArray ();

        Option<string> driverOption = new Option<string> ("--driver") { Description = "The IDriver to use.", DefaultValueFactory = _ => string.Empty };
        driverOption.AcceptOnlyFromAmong (allowedDrivers!);
        driverOption.Aliases.Add ("-d");

        Option<bool> disableConfigManagement = new ("--disable-cm") { Description = "Skip applying CLI driver / 16-color overlays via TuiConfigurationBuilder.RuntimeConfig." };
        disableConfigManagement.Aliases.Add ("-dcm");

        Option<bool> force16Colors = new ("--force-16-colors") { Description = "Forces the driver to use 16-color mode instead of TrueColor." };
        force16Colors.Aliases.Add ("-16");

        Option<uint> benchmarkTimeout = new ("--timeout")
        {
            Description = $"The maximum time in milliseconds to run a benchmark. Default is {Scenario.BenchmarkTimeout}ms.",
            DefaultValueFactory = _ => Scenario.BenchmarkTimeout
        };
        benchmarkTimeout.Aliases.Add ("-t");

        Option<string> resultsFile = new ("--file") { Description = "The file to save benchmark results to." };
        resultsFile.Aliases.Add ("-f");

        LogFilePath = $"{LOGFILE_LOCATION}/{Assembly.GetExecutingAssembly ().GetName ().Name}";

        Option<string> debugLogLevel = new Option<string> ("--debug-log-level")
        {
            Description = "The level to use for logging.", DefaultValueFactory = _ => "Warning"
        };
        debugLogLevel.AcceptOnlyFromAmong (Enum.GetNames<LogLevel> ());
        debugLogLevel.Aliases.Add ("-dl");

        // List command
        Command listCommand = new ("list", "List all available scenarios");

        listCommand.SetAction (_ =>
                               {
                                   ObservableCollection<Scenario> scenarios = Scenario.GetScenarios ();

                                   Console.WriteLine (@$"Available scenarios ({scenarios.Count})");
                                   Console.WriteLine ();

                                   foreach (Scenario s in scenarios)
                                   {
                                       Console.WriteLine (@$"  {s.GetName (),-30} {s.GetDescription ()}");
                                   }
                               });

        // Run command
        Argument<string> scenarioArgument = new ("scenario") { Description = "The name of the Scenario to run." };

        Command runCommand = new ("run", "Run a specific scenario") { scenarioArgument };

        runCommand.Options.Add (driverOption);
        runCommand.Options.Add (disableConfigManagement);
        runCommand.Options.Add (force16Colors);
        runCommand.Options.Add (debugLogLevel);

        runCommand.SetAction (parseResult =>
                              {
                                  // Extract the values ​​using parseResult.
                                  string scenarioName = parseResult.GetRequiredValue (scenarioArgument);
                                  string driver = parseResult.GetRequiredValue (driverOption);
                                  bool disableCm = parseResult.GetRequiredValue (disableConfigManagement);
                                  bool force16 = parseResult.GetRequiredValue (force16Colors);
                                  string logLevel = parseResult.GetRequiredValue (debugLogLevel);

                                  // Executing the original logic
                                  SetupLogging (logLevel);

                                  Runner runner = new ();

                                  if (!disableCm)
                                  {
                                      runner.SetRuntimeConfig (driver, force16 ? true : null);
                                  }

                                  Scenario? scenario = FindScenario (scenarioName);

                                  if (scenario is null)
                                  {
                                      Console.Error.WriteLine ($"Scenario '{scenarioName}' not found.");

                                      return; // SetAction returns void, so the empty return value works.
                                  }

                                  // Pass force16 only if explicitly set (default false means not set)
                                  runner.RunScenario (scenarioName, false);
                              });

        // Benchmark command
        Argument<string?> benchmarkScenarioArgument = new ("scenario")
        {
            Description = "The name of the Scenario to benchmark. If not specified, all scenarios are benchmarked.", DefaultValueFactory = _ => null
        };

        Command benchmarkCommand = new ("benchmark", "Benchmark scenarios") { benchmarkScenarioArgument };

        benchmarkCommand.Options.Add (driverOption);
        benchmarkCommand.Options.Add (disableConfigManagement);
        benchmarkCommand.Options.Add (force16Colors);
        benchmarkCommand.Options.Add (benchmarkTimeout);
        benchmarkCommand.Options.Add (resultsFile);
        benchmarkCommand.Options.Add (debugLogLevel);

        benchmarkCommand.SetAction (parseResult =>
                                    {
                                        // Extract the values ​​using parseResult.
                                        string scenarioName = parseResult.GetRequiredValue (scenarioArgument);
                                        string driver = parseResult.GetRequiredValue (driverOption);
                                        bool disableCm = parseResult.GetRequiredValue (disableConfigManagement);
                                        bool force16 = parseResult.GetRequiredValue (force16Colors);
                                        uint timeout = parseResult.GetRequiredValue (benchmarkTimeout);
                                        string file = parseResult.GetRequiredValue (resultsFile);
                                        string logLevel = parseResult.GetRequiredValue (debugLogLevel);

                                        SetupLogging (logLevel);
                                        Scenario.BenchmarkTimeout = timeout;

                                        Runner runner = new ();

                                        if (!disableCm)
                                        {
                                            // Pass force16 only if explicitly set
                                            runner.SetRuntimeConfig (driver, force16 ? true : null);
                                        }

                                        List<BenchmarkResults> results;

                                        if (string.IsNullOrEmpty (scenarioName))
                                        {
                                            // Benchmark all scenarios
                                            ObservableCollection<Scenario> scenarios = Scenario.GetScenarios ();
                                            results = runner.BenchmarkAllScenarios (scenarios);
                                        }
                                        else
                                        {
                                            // Benchmark single scenario
                                            Scenario? scenario = FindScenario (scenarioName);

                                            if (scenario is null)
                                            {
                                                Console.Error.WriteLine ($"Scenario '{scenarioName}' not found.");

                                                return;
                                            }

                                            BenchmarkResults? result = runner.RunScenario (scenarioName, true);
                                            results = result is { } ? [result] : [];
                                        }

                                        if (results.Count == 0)
                                        {
                                            Console.WriteLine (@"No benchmark results collected.");

                                            return;
                                        }

                                        if (!string.IsNullOrEmpty (file))
                                        {
                                            Runner.SaveResultsToFile (results, file);
                                            Console.WriteLine (@$"Results saved to {file}");
                                        }
                                        else
                                        {
                                            // Display in UI
                                            Runner.DisplayResultsUI (results);
                                        }
                                    });

        RootCommand rootCommand = new ("Terminal.Gui Scenario Runner - Run and benchmark Terminal.Gui scenarios") { listCommand, runCommand, benchmarkCommand };

        return rootCommand.Parse (args).Invoke ();
    }

    private static Scenario? FindScenario (string name)
    {
        ObservableCollection<Scenario> scenarios = Scenario.GetScenarios ();

        return scenarios.FirstOrDefault (s => s.GetName ().Equals (name, StringComparison.OrdinalIgnoreCase));
    }

    private static void SetupLogging (string logLevelName)
    {
        var logLevel = Enum.Parse<LogLevel> (logLevelName);
        LogLevelSwitch.MinimumLevel = LogLevelToLogEventLevel (logLevel);

        Log.Logger = new LoggerConfiguration ().MinimumLevel.ControlledBy (LogLevelSwitch)
                                               .Enrich.FromLogContext ()
                                               .WriteTo.Debug ()
                                               .WriteTo.File (LogFilePath,
                                                              rollingInterval: RollingInterval.Day,
                                                              outputTemplate:
                                                              "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                                               .CreateLogger ();

        using ILoggerFactory loggerFactory = LoggerFactory.Create (builder => { builder.AddSerilog (dispose: true).SetMinimumLevel (LogLevel.Trace); });

        Logging.Logger = loggerFactory.CreateLogger ("ScenarioRunner");
    }

    private static LogEventLevel LogLevelToLogEventLevel (LogLevel logLevel) =>
        logLevel switch
        {
            LogLevel.Trace => LogEventLevel.Verbose,
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            _ => LogEventLevel.Fatal
        };
}
