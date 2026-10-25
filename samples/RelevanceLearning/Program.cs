using Mostlylucid.Relevance.ExperimentDemo;

if (args.Length == 5 && args[0] == "analyse" && args[1] == "--model" && args[3] == "--output")
    await EmailModels.CaptureAsync(args[2], args[4]);
else await EmailExperimentRunner.RunAsync(args);
