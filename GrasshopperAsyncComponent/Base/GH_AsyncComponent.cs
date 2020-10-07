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

    Action<string> ReportProgress;

    Action<string, GH_RuntimeMessageLevel> ReportError;

    List<(string, GH_RuntimeMessageLevel)> Errors;

    Action Done;

    Timer DisplayProgressTimer;

    int State = 0;

    int Iterations = 0;

    public WorkerInstance BaseWorker { get; set; }

    List<WorkerInstance> Workers;

    List<CancellationTokenSource> CancelationSources;


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
        Message = progress;
        if (!DisplayProgressTimer.Enabled) DisplayProgressTimer.Start();
      };

      ReportError = (error, type) => Errors?.Add((error, type));

      Done = () =>
      {
        State++;

        if (State == Iterations)
        {
          Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
          {
            //State = 1;
            ExpireSolution(true);
          });
        }
      };

      Errors = new List<(string, GH_RuntimeMessageLevel)>();

      Workers = new List<WorkerInstance>();
      CancelationSources = new List<CancellationTokenSource>();
    }

    protected override void BeforeSolveInstance()
    {
      if (State != 0) return;

      foreach (var source in CancelationSources) source.Cancel();

      CancelationSources.Clear();
      Workers.Clear();
      Errors.Clear();

      State = 0;
      Iterations = 0;
      base.BeforeSolveInstance();
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (State == 0)
      {
        Iterations++;
        if (BaseWorker == null)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Worker class not provided.");
          return;
        }

        var CurrentWorker = BaseWorker.Duplicate();
        if (CurrentWorker == null)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not get a worker instance.");
          return;
        }

        // Let the worker collect data.
        CurrentWorker.GetData(DA, Params);

        // Create the task
        var tokenSource = new CancellationTokenSource();
        CurrentWorker.CancellationToken = tokenSource.Token;
        var CurrentRun = new Task(() => CurrentWorker.DoWork(ReportProgress, ReportError, Done), tokenSource.Token, TaskCreationOptions.LongRunning);

        // Add cancelation source to our bag
        CancelationSources.Add(tokenSource);
        // Add the worker to our list
        Workers.Add(CurrentWorker);

        CurrentRun.Start();
        return;
      }

      var test = DA.Iteration;

      Workers[DA.Iteration].SetData(DA);

      if (--State == 0)
      {
        foreach (var (message, type) in Errors)
        {
          AddRuntimeMessage(type, message);
        }

        Message = "Done";
        OnDisplayExpired(true);
        Errors.Clear();
      }
    }
  }
}
