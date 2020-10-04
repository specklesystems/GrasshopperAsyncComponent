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

    public SampleAsyncComponent() : base("Sample Async Component", "ASYNC", "Meaningless labour.", "Samples", "Async")
    {
      Worker = new SampleAsyncComponentWorker();
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

  public class SampleAsyncComponentWorker : IAsyncComponentWorker
  {
    int MaxIterations { get; set; } = 100;

    public void CollectData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      int _maxIterations = 100;
      DA.GetData(0, ref _maxIterations);
      if (_maxIterations > 1000) MaxIterations = 1000;
      if (_maxIterations < 10) MaxIterations = 10;

      MaxIterations = _maxIterations;
    }

    public void DoWork(CancellationToken token, Action<string> ReportProgress, Action<string, GH_RuntimeMessageLevel> ReportError, Action Done)
    {
      if (token.IsCancellationRequested) return;

      for (int i = 0; i <= MaxIterations; i++)
      {
        var sw = new SpinWait();
        for (int j = 0; j <= 100; j++)
          sw.SpinOnce();
        
        ReportProgress(((double)(i + 1) / (double)MaxIterations).ToString("0.00%"));

        if (token.IsCancellationRequested) return;
      }

      Done();
    }

    public IAsyncComponentWorker GetNewInstance()
    {
      return new SampleAsyncComponentWorker();
    }

    public void SetData(IGH_DataAccess DA)
    {
      DA.SetData(0, "Hello world. I'm done spinning.");
    }
  }
}
