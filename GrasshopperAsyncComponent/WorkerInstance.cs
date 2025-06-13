using Grasshopper.Kernel;

namespace GrasshopperAsyncComponent;

/// <summary>
/// A class that holds the actual compute logic and encapsulates the state it needs. Every <see cref="GH_AsyncComponent{T}"/> needs to have one.
/// </summary>
public abstract class WorkerInstance<T>(T parent, string id, CancellationToken cancellationToken)
    where T : GH_Component
{
    /// <summary>
    /// The parent component. Useful for passing state back to the host component.
    /// </summary>
    public T Parent { get; set; } = parent;

    public CancellationToken CancellationToken { get; } = cancellationToken;

    public string Id { get; set; } = id;

    /// <summary>
    /// This is a "factory" method. It should return a fresh instance of this class, but with all the necessary state that you might have passed on directly from your component.
    /// </summary>
    /// <param name="id">A Unique id for the new duplicate</param>
    /// <param name="cancellationToken">A cancellationToken to be passed to the new duplicate</param>
    /// <returns></returns>
    public abstract WorkerInstance<T> Duplicate(string id, CancellationToken cancellationToken);

    /// <summary>
    /// This method is where the actual calculation/computation/heavy lifting should be done.
    /// <b>Make sure you always check as frequently as you can if <see cref="WorkerInstance{T}.CancellationToken"/> is cancelled. For an example, see the PrimeCalculatorWorker example.</b>
    /// </summary>
    /// <remarks>
    /// If you don't need <see langword="async"/> function, then you can simply return <see cref="Task.CompletedTask"/>
    /// Either way, you should be sure to handle exceptions in this function. Otherwise, they will be Unobserved!
    /// You can call <paramref name="done"/> on <see cref="Exception"/>s, but avoid calling it when cancellation is has been observed.
    /// </remarks>
    /// <param name="reportProgress">Call this to report progress up to the parent component.</param>
    /// <param name="done">Call this when everything is <b>done</b>. It will tell the parent component that you're ready to <see cref="SetData(IGH_DataAccess)"/>.</param>
    public abstract Task DoWork(Action<string, double> reportProgress, Action done);

    /// <summary>
    /// Write your data setting logic here. <b>Do not call this function directly from this class. It will be invoked by the parent <see cref="GH_AsyncComponent{T}"/> after you've called `Done` in the <see cref="DoWork"/> function.</b>
    /// </summary>
    /// <param name="da"></param>
    public abstract void SetData(IGH_DataAccess da);

    /// <summary>
    /// Write your data collection logic here. <b>Do not call this method directly. It will be invoked by the parent <see cref="GH_AsyncComponent{T}"/>.</b>
    /// </summary>
    /// <param name="da"></param>
    /// <param name="parameters"></param>
    public abstract void GetData(IGH_DataAccess da, GH_ComponentParamServer parameters);
}
