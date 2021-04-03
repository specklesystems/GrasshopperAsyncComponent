using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GrasshopperAsyncComponent;
using System.Windows.Forms;

namespace GrasshopperAsyncComponentDemo.SampleImplementations
{
  public class Sample_PrimeCalculatorAsyncComponent : GH_AsyncComponent
  {
    public override Guid ComponentGuid { get => new Guid("22C612B0-2C57-47CE-B9FE-E10621F18933"); }

    protected override System.Drawing.Bitmap Icon { get => Properties.Resources.logo32; }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    public Sample_PrimeCalculatorAsyncComponent() : base("The N-th Prime Calculator", "PRIME", "Calculates the nth prime number.", "Samples", "Async")
    {
      BaseWorker = new PrimeCalculatorWorker();
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddIntegerParameter("N", "N", "Which n-th prime number. Minimum 1, maximum one million. Take care, it can burn your CPU.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddNumberParameter("Output", "O", "The n-th prime number.", GH_ParamAccess.item);
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);
      Menu_AppendItem(menu, "Cancel", (s, e) =>
      {
        RequestCancellation();
      });
    }
  }

  public class PrimeCalculatorWorker : WorkerInstance
  {
    int TheNthPrime { get; set; } = 100;
    long ThePrime { get; set; } = -1;

    public PrimeCalculatorWorker() : base(null) { }

    public override void DoWork(Action<string, double> ReportProgress, Action Done)
    {
      // 👉 Checking for cancellation!
      if (CancellationToken.IsCancellationRequested) { return; }

      int count = 0;
      long a = 2;

      // Thanks Steak Overflow (TM) https://stackoverflow.com/a/13001749/
      while (count < TheNthPrime)
      {
        // 👉 Checking for cancellation!
        if (CancellationToken.IsCancellationRequested) {  return; }

        long b = 2;
        int prime = 1;// to check if found a prime
        while (b * b <= a)
        {
          // 👉 Checking for cancellation!
          if (CancellationToken.IsCancellationRequested) {return; }

          if (a % b == 0)
          {
            prime = 0;
            break;
          }
          b++;
        }

        ReportProgress(Id, ((double)count) / TheNthPrime);

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

      TheNthPrime = _nthPrime;
    }

    public override void SetData(IGH_DataAccess DA)
    {
      // 👉 Checking for cancellation!
      if (CancellationToken.IsCancellationRequested) { return; }

      DA.SetData(0, ThePrime);
    }
  }

}
