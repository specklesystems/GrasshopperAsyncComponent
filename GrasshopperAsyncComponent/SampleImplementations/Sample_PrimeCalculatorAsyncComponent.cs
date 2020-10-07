using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GrasshopperAsyncComponent.SampleImplementations
{
  public class Sample_PrimeCalculatorAsyncComponent : GH_AsyncComponent
  {
    public override Guid ComponentGuid { get => new Guid("DF2B93E2-052D-4BE4-BC62-90DC1F169BF6"); }

    protected override System.Drawing.Bitmap Icon { get => null; }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    public Sample_PrimeCalculatorAsyncComponent() : base("Sample Async Component", "PRIME", "Calculates the nth prime number.", "Samples", "Async")
    {
      BaseWorker = new PrimeCalculatorWorker();
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddIntegerParameter("N", "N", "Which n-th prime number. Minimum 1, maximum one million. Take care, it can burn your CPU.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter("Output", "O", "The n-th prime number.", GH_ParamAccess.item);
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

  public class PrimeCalculatorWorker : WorkerInstance
  {
    int TehNthPrime { get; set; } = 100;
    long ThePrime { get; set; } = -1;

    public override void DoWork(Action<string> ReportProgress, Action<string, GH_RuntimeMessageLevel> ReportError, Action Done)
    {
      if (CancellationToken.IsCancellationRequested) return;

      int count = 0;
      long a = 2;

      // Thanks Steak Overflow (TM) https://stackoverflow.com/a/13001749/
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

    public override WorkerInstance Duplicate() => new PrimeCalculatorWorker();

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      int _nthPrime = 100;
      DA.GetData(0, ref _nthPrime);
      if (_nthPrime > 1000000) _nthPrime = 1000000;
      if (_nthPrime < 1) _nthPrime = 1;

      TehNthPrime = _nthPrime;
    }

    public override void SetData(IGH_DataAccess DA)
    {
      if (CancellationToken.IsCancellationRequested) return;
      DA.SetData(0, $"W_ID {Id}: {TehNthPrime}th prime is: {ThePrime}");
    }
  }

}
