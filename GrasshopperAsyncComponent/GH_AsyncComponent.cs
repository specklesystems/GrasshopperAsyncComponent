using System.Collections.Concurrent;
using System.Diagnostics;
using Grasshopper.Kernel;
using Timer = System.Timers.Timer;

namespace GrasshopperAsyncComponent;

public sealed class Worker<T> : IDisposable
    where T : GH_Component
{
    public required WorkerInstance<T> Instance { get; init; }

    public required Task Task { get; init; }

    public required CancellationTokenSource CancellationSource { get; init; }

    public void Cancel() => CancellationSource.Cancel();

    public void Dispose()
    {
        if (Task.IsCompleted)
        {
            Task.Dispose();
        }
        CancellationSource.Dispose();
    }
}

/// <summary>
/// Inherit your component from this class to make all the async goodness available.
/// </summary>
public abstract class GH_AsyncComponent<T> : GH_Component, IDisposable
    where T : GH_Component
{
    //List<(string, GH_RuntimeMessageLevel)> Errors;

    private readonly Action<string, double> _reportProgress;

    public ConcurrentDictionary<string, double> ProgressReports { get; protected set; }

    private readonly Timer _displayProgressTimer;

    //JEDD: appears to be the number of workers that have completed
    private volatile int _state;

    //JEDD: boolean, 1 if this class needs to set the data of the workers...
    private volatile int _setData;

    public List<Worker<T>> Workers { get; }

    /// <summary>
    /// Set this property inside the constructor of your derived component.
    /// </summary>
    public WorkerInstance<T>? BaseWorker { get; set; }

    /// <summary>
    /// Optional: if you have opinions on how the default system task scheduler should treat your workers, set it here.
    /// </summary>
    public TaskCreationOptions TaskCreationOptions { get; set; } = TaskCreationOptions.None;

    protected GH_AsyncComponent(string name, string nickname, string description, string category, string subCategory)
        : base(name, nickname, description, category, subCategory)
    {
        Workers = new List<Worker<T>>();

        ProgressReports = new ConcurrentDictionary<string, double>();

        _displayProgressTimer = new Timer(333) { AutoReset = false };
        _displayProgressTimer.Elapsed += DisplayProgress;

        _reportProgress = (id, value) =>
        {
            ProgressReports[id] = value;
            if (!_displayProgressTimer.Enabled)
            {
                _displayProgressTimer.Start();
            }
        };
    }

    private void Done()
    {
        Interlocked.Increment(ref _state);
        if (_state == Workers.Count && _setData == 0)
        {
            Interlocked.Exchange(ref _setData, 1);

            // We need to reverse the workers list to set the outputs in the same order as the inputs.
            Workers.Reverse();

            Rhino.RhinoApp.InvokeOnUiThread(
                (Action)
                    delegate
                    {
                        ExpireSolution(true);
                    }
            );
        }
    }

    public virtual void DisplayProgress(object sender, System.Timers.ElapsedEventArgs e)
    {
        if (Workers.Count == 0 || ProgressReports.Values.Count == 0)
        {
            return;
        }

        if (Workers.Count == 1)
        {
            Message = ProgressReports.Values.Last().ToString("0.00%");
        }
        else
        {
            double total = 0;
            foreach (var kvp in ProgressReports)
            {
                total += kvp.Value;
            }

            Message = (total / Workers.Count).ToString("0.00%");
        }

        Rhino.RhinoApp.InvokeOnUiThread(
            (Action)
                delegate
                {
                    OnDisplayExpired(true);
                }
        );
    }

    protected override void BeforeSolveInstance()
    {
        if (_state != 0 && _setData == 1)
        {
            return;
        }

        Debug.WriteLine("Killing");

        ResetState();
    }

    protected override void AfterSolveInstance()
    {
        Debug.WriteLine("After solve instance was called " + _state + " ? " + Workers.Count);
        // We need to start all the tasks as close as possible to each other.
        if (_state == 0 && Workers.Count > 0 && _setData == 0)
        {
            Debug.WriteLine("After solve INVOCATION");
            foreach (var worker in Workers)
            {
                worker.Task.Start();
            }
        }
    }

    protected override void ExpireDownStreamObjects()
    {
        // Prevents the flash of null data until the new solution is ready
        if (_setData == 1)
        {
            base.ExpireDownStreamObjects();
        }
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        //return;
        if (_state == 0)
        {
            if (BaseWorker == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Worker class not provided.");
                return;
            }

            // Add cancellation source to our bag
            var tokenSource = new CancellationTokenSource();

            var currentWorker = BaseWorker.Duplicate($"Worker-{da.Iteration}", tokenSource.Token);

            // Let the worker collect data.
            currentWorker.GetData(da, Params);

            var currentRun = new Task<Task>(
                async () =>
                {
                    await currentWorker.DoWork(_reportProgress, Done).ConfigureAwait(true);
                },
                tokenSource.Token,
                TaskCreationOptions
            );

            // Add the worker to our list
            Workers.Add(
                new()
                {
                    Instance = currentWorker,
                    Task = currentRun,
                    CancellationSource = tokenSource,
                }
            );

            return;
        }

        if (_setData == 0)
        {
            return;
        }

        if (Workers.Count > 0)
        {
            Interlocked.Decrement(ref _state);
            Workers[_state].Instance.SetData(da);
        }

        if (_state != 0)
        {
            return;
        }

        foreach (var worker in Workers)
        {
            worker?.Dispose();
        }

        ResetState();

        Message = "Done";
        OnDisplayExpired(true);
    }

    private void ResetState()
    {
        Workers.Clear();
        ProgressReports.Clear();

        Interlocked.Exchange(ref _state, 0);
        Interlocked.Exchange(ref _setData, 0);
    }

    public void RequestCancellation()
    {
        foreach (var worker in Workers)
        {
            worker.Cancel();
        }

        ResetState();
        Message = "Cancelled";
        OnDisplayExpired(true);
    }

    private bool _isDisposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            _isDisposed = true;

            if (disposing)
            {
                foreach (var worker in Workers)
                {
                    worker?.Dispose();
                }
                _displayProgressTimer?.Dispose();
            }
        }
    }
}
