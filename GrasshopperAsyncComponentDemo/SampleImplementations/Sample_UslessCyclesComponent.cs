using System.Windows.Forms;
using Grasshopper.Kernel;
using GrasshopperAsyncComponent;

namespace GrasshopperAsyncComponentDemo.SampleImplementations;

public class Sample_UselessCyclesAsyncComponent : GH_AsyncComponent
{
    public override Guid ComponentGuid => new Guid("DF2B93E2-052D-4BE4-BC62-90DC1F169BF6");

    protected override System.Drawing.Bitmap Icon => Properties.Resources.logo32;

    public override GH_Exposure Exposure => GH_Exposure.primary;

    public Sample_UselessCyclesAsyncComponent()
        : base("Sample Async Component", "CYCLOMATRON-X", "Spins uselessly.", "Samples", "Async")
    {
        BaseWorker = new UselessCyclesWorker(this);
    }

    private sealed class UselessCyclesWorker(
        GH_Component? parent,
        string id = "baseworker",
        CancellationToken cancellationToken = default
    ) : WorkerInstance(parent, id, cancellationToken)
    {
        private int MaxIterations { get; set; } = 100;

        public override void DoWork(Action<string, double> reportProgress, Action done)
        {
            // Checking for cancellation
            if (CancellationToken.IsCancellationRequested)
            {
                return;
            }

            for (int i = 0; i <= MaxIterations; i++)
            {
                var sw = new SpinWait();
                for (int j = 0; j <= 100; j++)
                {
                    sw.SpinOnce();
                }

                reportProgress(Id, (i + 1) / (double)MaxIterations);

                // Checking for cancellation
                if (CancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }

            done();
        }

        public override WorkerInstance Duplicate(string id, CancellationToken cancellationToken) =>
            new UselessCyclesWorker(Parent, id, cancellationToken);

        public override void GetData(IGH_DataAccess da, GH_ComponentParamServer parameters)
        {
            if (CancellationToken.IsCancellationRequested)
            {
                return;
            }

            int maxIterations = 100;
            da.GetData(0, ref maxIterations);
            if (maxIterations > 1000)
            {
                maxIterations = 1000;
            }

            if (maxIterations < 10)
            {
                maxIterations = 10;
            }

            MaxIterations = maxIterations;
        }

        public override void SetData(IGH_DataAccess da)
        {
            if (CancellationToken.IsCancellationRequested)
            {
                return;
            }

            da.SetData(0, $"Hello world. Worker {Id} has spun for {MaxIterations} iterations.");
        }
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddIntegerParameter("N", "N", "Number of spins.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Output", "O", "Nothing really interesting.", GH_ParamAccess.item);
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
        base.AppendAdditionalMenuItems(menu);
        Menu_AppendItem(
            menu,
            "Cancel",
            (s, e) =>
            {
                RequestCancellation();
            }
        );
    }
}
