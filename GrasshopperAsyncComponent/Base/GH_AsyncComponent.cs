using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Timer = System.Timers.Timer;

namespace GrasshopperAsyncComponent
{
  public abstract class GH_AsyncComponent : GH_Component
  {
    public override Guid ComponentGuid { get => new Guid("5DBBD498-0326-4E25-83A5-424D8DC493D4"); }

    protected override System.Drawing.Bitmap Icon { get => null; }

    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public IAsyncComponentWorker Worker;

    IAsyncComponentWorker CurrentWorker;

    Task CurrentRun;

    ConcurrentBag<CancellationTokenSource> TokenSources = new ConcurrentBag<CancellationTokenSource>();

    Action<string> ReportProgress;

    Action<string, GH_RuntimeMessageLevel> ReportError;

    List<(string, GH_RuntimeMessageLevel)> Errors;

    Action Done;

    int State = 0;

    Timer DisplayProgressTimer;

    protected GH_AsyncComponent(string name, string nickname, string description, string category, string subCategory) : base(name, nickname, description, category, subCategory)
    {

      DisplayProgressTimer = new Timer(333) { AutoReset = false };
      DisplayProgressTimer.Elapsed += (s, e) =>
      {
        Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
        {
          OnDisplayExpired(true);
        });
      };

      ReportProgress = (progress) =>
      {
        Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
        {
          Message = progress;
          if (!DisplayProgressTimer.Enabled) DisplayProgressTimer.Start();
        });
      };

      ReportError = (error, type) => Errors?.Add((error, type));

      Done = () =>
      {
        Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
        {
          State = 1;
          ExpireSolution(true);
        });
      };

      Errors = new List<(string, GH_RuntimeMessageLevel)>();
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (State == 0)
      {
        if (Worker == null)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Worker class not provided.");
          return;
        }

        CurrentWorker = Worker.GetNewInstance();
        if (CurrentWorker == null)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not get a worker instance.");
          return;
        }

        Errors = new List<(string, GH_RuntimeMessageLevel)>();

        // Request the cancellation of any old tasks.
        CancellationTokenSource oldTokenSource;
        while (TokenSources.TryTake(out oldTokenSource))
        {
          oldTokenSource?.Cancel();
        }

        // Let the worker collect data.
        CurrentWorker.CollectData(DA, Params);

        // Create the task
        var tokenSource = new CancellationTokenSource();
        CurrentRun = new Task(() => CurrentWorker.DoWork(tokenSource.Token, ReportProgress, ReportError, Done), tokenSource.Token);

        // Add cancelation source to our bag
        TokenSources.Add(tokenSource);
        CurrentRun.Start();
        return;
      }

      foreach (var (message, type) in Errors)
      {
        AddRuntimeMessage(type, message);
      }

      OnDisplayExpired(true);
      CurrentWorker.SetData(DA);

      Message = "Done";

      Errors.Clear();
      State = 0;
    }
  }
}
