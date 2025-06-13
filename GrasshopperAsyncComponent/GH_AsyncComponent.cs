using System.Collections.Concurrent;
using System.Diagnostics;
using Grasshopper.Kernel;
using Timer = System.Timers.Timer;

namespace GrasshopperAsyncComponent;

/// <summary>
/// Inherit your component from this class to make all the async goodness available.
/// </summary>
public abstract class GH_AsyncComponent : GH_Component, IDisposable
{
    //List<(string, GH_RuntimeMessageLevel)> Errors;

    private readonly Action<string, double> _reportProgress;

    public ConcurrentDictionary<string, double> ProgressReports { get; protected set; }

    private readonly Action _done;

    private readonly Timer _displayProgressTimer;

    private int _state;

    private int _setData;

    public List<WorkerInstance> Workers { get; protected set; }

    private readonly List<Task> _tasks;

    public List<CancellationTokenSource> CancellationSources { get; }

    /// <summary>
    /// Set this property inside the constructor of your derived component.
    /// </summary>
    public WorkerInstance? BaseWorker { get; set; }

    /// <summary>
    /// Optional: if you have opinions on how the default system task scheduler should treat your workers, set it here.
    /// </summary>
    public TaskCreationOptions? TaskCreationOptions { get; set; }

    protected GH_AsyncComponent(string name, string nickname, string description, string category, string subCategory)
        : base(name, nickname, description, category, subCategory)
    {
        Workers = new List<WorkerInstance>();
        CancellationSources = new List<CancellationTokenSource>();
        _tasks = new List<Task>();
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

        _done = () =>
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
        };
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

        foreach (var source in CancellationSources)
        {
            source.Cancel();
        }

        CancellationSources.Clear();
        Workers.Clear();
        ProgressReports.Clear();
        _tasks.Clear();

        Interlocked.Exchange(ref _state, 0);
    }

    protected override void AfterSolveInstance()
    {
        System.Diagnostics.Debug.WriteLine("After solve instance was called " + _state + " ? " + Workers.Count);
        // We need to start all the tasks as close as possible to each other.
        if (_state == 0 && _tasks.Count > 0 && _setData == 0)
        {
            System.Diagnostics.Debug.WriteLine("After solve INVOKATION");
            foreach (var task in _tasks)
            {
                task.Start();
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
            CancellationSources.Add(tokenSource);

            var currentWorker = BaseWorker.Duplicate($"Worker-{da.Iteration}", tokenSource.Token);
            if (currentWorker == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not get a worker instance.");
                return;
            }

            // Let the worker collect data.
            currentWorker.GetData(da, Params);

            var currentRun =
                TaskCreationOptions != null
                    ? new Task(
                        () => currentWorker.DoWork(_reportProgress, _done),
                        tokenSource.Token,
                        (TaskCreationOptions)TaskCreationOptions
                    )
                    : new Task(() => currentWorker.DoWork(_reportProgress, _done), tokenSource.Token);

            // Add the worker to our list
            Workers.Add(currentWorker);

            _tasks.Add(currentRun);

            return;
        }

        if (_setData == 0)
        {
            return;
        }

        if (Workers.Count > 0)
        {
            Interlocked.Decrement(ref _state);
            Workers[_state].SetData(da);
        }

        if (_state != 0)
        {
            return;
        }

        CancellationSources.Clear();
        Workers.Clear();
        ProgressReports.Clear();
        _tasks.Clear();

        Interlocked.Exchange(ref _setData, 0);

        Message = "Done";
        OnDisplayExpired(true);
    }

    public void RequestCancellation()
    {
        foreach (var source in CancellationSources)
        {
            source.Cancel();
        }

        CancellationSources.Clear();
        Workers.Clear();
        ProgressReports.Clear();
        _tasks.Clear();

        Interlocked.Exchange(ref _state, 0);
        Interlocked.Exchange(ref _setData, 0);
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
                _displayProgressTimer?.Dispose();
            }
        }
    }
}
