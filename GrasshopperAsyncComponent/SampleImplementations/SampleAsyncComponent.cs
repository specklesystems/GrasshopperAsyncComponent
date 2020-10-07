using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GrasshopperAsyncComponent.SampleImplementations
{
  public class SampleAsyncComponent : GH_AsyncComponent
  {
    public override Guid ComponentGuid { get => new Guid("DF2B93E2-052D-4BE4-BC62-90DC1F169BF6"); }

    protected override System.Drawing.Bitmap Icon { get => null; }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    public SampleAsyncComponent() : base("Sample Async Component", "CYCLOMAXOTRON", "Meaningless labour.", "Samples", "Async")
    {
      BaseWorker = new PrimeCalculator();
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddIntegerParameter("Max iterations", "M", "How many useless cycles should we spin. Minimum 10, maximum 1000.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter("Output", "O", "Will just say hello world after spinning.", GH_ParamAccess.item);
    }
  }

  public class SampleAsyncWorker : WorkerInstance
  {
    int MaxIterations { get; set; } = 100;
  
    public override void DoWork(Action<string> ReportProgress, Action<string, GH_RuntimeMessageLevel> ReportError, Action Done)
    {
      if (CancellationToken.IsCancellationRequested) return;

      for (int i = 0; i <= MaxIterations; i++)
      {
        var sw = new SpinWait();
        for (int j = 0; j <= 100; j++)
          sw.SpinOnce();

        ReportProgress(((double)(i + 1) / (double)MaxIterations).ToString("0.00%"));

        if (CancellationToken.IsCancellationRequested) return;
      }

      Done();
    }

    public override WorkerInstance Duplicate() => new SampleAsyncWorker();

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      if (CancellationToken.IsCancellationRequested) return;
      
      int _maxIterations = 100;
      DA.GetData(0, ref _maxIterations);
      if (_maxIterations > 1000) _maxIterations = 1000;
      if (_maxIterations < 10) _maxIterations = 10;

      MaxIterations = _maxIterations;
    }

    public override void SetData(IGH_DataAccess DA)
    {
      if (CancellationToken.IsCancellationRequested) return;
      DA.SetData(0, $"Hello world. Worker {Id} has spun for {MaxIterations} iterations.");
    }
  }

  public class PrimeCalculator : WorkerInstance
  {
    int TehNthPrime { get; set; } = 100;
    long ThePrime { get; set; } = -1;

    public override void DoWork(Action<string> ReportProgress, Action<string, GH_RuntimeMessageLevel> ReportError, Action Done)
    {
      if (CancellationToken.IsCancellationRequested) return;

      int count = 0;
      long a = 2;

      while (count < TehNthPrime)
      {
        if (CancellationToken.IsCancellationRequested) return;

        long b = 2;
        int prime = 1;// to check if found a prime
        while (b * b <= a)
        {

          if (CancellationToken.IsCancellationRequested) return;

          if (a % b == 0)
          {
            prime = 0;
            break;
          }
          b++;
        }

        ReportProgress(((double)(count) / (double)TehNthPrime).ToString("0.00%"));

        if (prime > 0)
        {
          count++;
        }
        a++;
      }

      ThePrime = --a;
      Done();
    }

    public override WorkerInstance Duplicate() => new PrimeCalculator();

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      if (CancellationToken.IsCancellationRequested) return;

      int _maxIterations = 100;
      DA.GetData(0, ref _maxIterations);
      if (_maxIterations > 1000000) _maxIterations = 1000000;
      if (_maxIterations < 10) _maxIterations = 10;

      TehNthPrime = _maxIterations;
    }

    public override void SetData(IGH_DataAccess DA)
    {
      if (CancellationToken.IsCancellationRequested) return;
      DA.SetData(0, $"Hello world. Worker {Id} has found for that the {TehNthPrime}th prime is: {ThePrime}");
    }
  }

}
