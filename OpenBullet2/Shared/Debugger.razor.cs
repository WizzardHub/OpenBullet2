﻿using Microsoft.AspNetCore.Components;
using OpenBullet2.Helpers;
using OpenBullet2.Services;
using RuriLib.Exceptions;
using RuriLib.Logging;
using RuriLib.Models.Configs;
using RuriLib.Models.Debugger;
using RuriLib.Providers.RandomNumbers;
using RuriLib.Providers.UserAgents;
using RuriLib.Services;
using System;
using System.Threading.Tasks;

namespace OpenBullet2.Shared;

public partial class Debugger
{
    private ConfigDebugger debugger;
    private BotLogger logger;
    private BotLoggerViewer loggerViewer;
    private DebuggerOptions options;
    private bool showVariables = false;
    private VariablesViewer variablesViewer;
    [Parameter] public Config Config { get; set; }

    [Inject] private IRandomUAProvider RandomUAProvider { get; set; }
    [Inject] private IRNGProvider RNGProvider { get; set; }
    [Inject] private RuriLibSettingsService RuriLibSettings { get; set; }
    [Inject] private PluginRepository PluginRepo { get; set; }
    [Inject] private VolatileSettingsService VolatileSettings { get; set; }

    protected override void OnInitialized()
    {
        options = VolatileSettings.DebuggerOptions;
        logger = VolatileSettings.DebuggerLog;
    }

    private async Task Run()
    {
        try
        {
            debugger = new ConfigDebugger(Config, options, logger) {
                PluginRepo = PluginRepo,
                RandomUAProvider = RandomUAProvider,
                RNGProvider = RNGProvider,
                RuriLibSettings = RuriLibSettings
            };

            debugger.NewLogEntry += OnNewEntry;
            debugger.StatusChanged += UpdateState;

            await debugger.Run();
        }
        catch (InvalidProxyException)
        {
            await js.AlertError(Loc["InvalidProxy"], Loc["InvalidProxyMessage"]);
            return;
        }
        catch (Exception ex)
        {
            await js.AlertException(ex);
        }
        finally
        {
            debugger.NewLogEntry -= OnNewEntry;
            debugger.StatusChanged -= UpdateState;
        }

        await loggerViewer?.Refresh();
        variablesViewer?.Refresh();
        await InvokeAsync(StateHasChanged);
    }

    private void TakeStep() => debugger?.TryTakeStep();

    private void Stop() => debugger?.Stop();

    private void OnNewEntry(object sender, BotLoggerEntry entry)
        => loggerViewer?.Refresh();

    private void ToggleView()
        => showVariables = !showVariables;

    private void UpdateState(object sender, ConfigDebuggerStatus status) => InvokeAsync(StateHasChanged);
}