﻿using Grasshopper.Kernel;
using System;
using System.Threading;
using System.Windows.Forms;
using GrasshopperAsyncComponent;


namespace GrasshopperAsyncComponentDemo.SampleImplementations
{
    public class Sample_UselessCyclesAsyncComponent : GH_AsyncComponent
    {
        public override Guid ComponentGuid { get => new Guid("DF2B93E2-052D-4BE4-BC62-90DC1F169BF6"); }

        protected override System.Drawing.Bitmap Icon { get => Properties.Resources.logo32; }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        public Sample_UselessCyclesAsyncComponent() : base("Sample Async Component", "CYCLOMATRON-X", "Spins uselessly.", "Samples", "Async")
        {
            BaseWorker = new UselessCyclesWorker();
        }

        private class UselessCyclesWorker : WorkerInstance
        {
            int MaxIterations { get; set; } = 100;

            public UselessCyclesWorker() : base(null) { }

            public override void DoWork(Action<string, double> ReportProgress, Action Done)
            {
                // Checking for cancellation
                if (CancellationToken.IsCancellationRequested) { return; }

                for (int i = 0; i <= MaxIterations; i++)
                {
                    var sw = new SpinWait();
                    for (int j = 0; j <= 100; j++)
                        sw.SpinOnce();

                    ReportProgress(Id, ((double)(i + 1) / (double)MaxIterations));

                    // Checking for cancellation
                    if (CancellationToken.IsCancellationRequested) { return; }
                }

                Done();
            }

            public override WorkerInstance Duplicate() => new UselessCyclesWorker();

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
            Menu_AppendItem(menu, "Cancel", (s, e) =>
            {
                RequestCancellation();
            });
        }
    }
}
