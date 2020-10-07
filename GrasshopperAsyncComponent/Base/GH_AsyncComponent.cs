using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Timer = System.Timers.Timer;

namespace GrasshopperAsyncComponent
{
  /// <summary>
  /// Inherit your component from this class to make all the async goodness available.
  /// </summary>
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

    bool SetData = false;

    List<WorkerInstance> Workers;

    List<Task> Tasks;

    List<CancellationTokenSource> CancelationSources;

    /// <summary>
    /// Set this property inside the constructor of your derived component. 
    /// </summary>
    public WorkerInstance BaseWorker { get; set; }

    /// <summary>
    /// Optional: if you have opinions on how the default system task scheduler should treat your workers, set it here.
    /// </summary>
    public TaskCreationOptions? TaskCreationOptions { get; set; } = null;

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

        if (State == Workers.Count)
        {
          SetData = true;
          // We need to reverse the workers list to set the outputs in the same order as the inputs. 
          Workers.Reverse();

          Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
          {
            ExpireSolution(true);
          });
        }
      };

      Errors = new List<(string, GH_RuntimeMessageLevel)>();

      Workers = new List<WorkerInstance>();
      CancelationSources = new List<CancellationTokenSource>();
      Tasks = new List<Task>();
    }

    protected override void BeforeSolveInstance()
    {
      if (State != 0 && SetData) return;

      foreach (var source in CancelationSources) source.Cancel();

      CancelationSources.Clear();
      Workers.Clear();
      Errors.Clear();
      Tasks.Clear();

      State = 0;
    }

    protected override void AfterSolveInstance()
    {
      // We need to start all the tasks as close as possible to each other.
      if (State == 0 && Tasks.Count > 0)
      {
        foreach (var task in Tasks) task.Start();
      }
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (State == 0)
      {
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
        CurrentWorker.Id = DA.Iteration.ToString();

        Task CurrentRun;
        if (TaskCreationOptions != null)
        {
          CurrentRun = new Task(() => CurrentWorker.DoWork(ReportProgress, ReportError, Done), tokenSource.Token, (TaskCreationOptions)TaskCreationOptions);
        }
        else
        {
          CurrentRun = new Task(() => CurrentWorker.DoWork(ReportProgress, ReportError, Done), tokenSource.Token);
        }
        // Add cancelation source to our bag
        CancelationSources.Add(tokenSource);

        // Add the worker to our list
        Workers.Add(CurrentWorker);

        Tasks.Add(CurrentRun);

        return;
      }

      if (SetData)
      {
        if (Workers.Count > 0)
          Workers[--State].SetData(DA);

        if (State == 0)
        {
          foreach (var (message, type) in Errors)
          {
            AddRuntimeMessage(type, message);
          }

          CancelationSources.Clear();
          Workers.Clear();
          Errors.Clear();
          Tasks.Clear();

          SetData = false;

          Message = "Done";
          OnDisplayExpired(true);
        }
      }
    }
  }
}
