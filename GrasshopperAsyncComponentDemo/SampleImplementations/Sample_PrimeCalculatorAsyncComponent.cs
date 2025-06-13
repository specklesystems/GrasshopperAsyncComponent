using System.Windows.Forms;
using Grasshopper.Kernel;
using GrasshopperAsyncComponent;

namespace GrasshopperAsyncComponentDemo.SampleImplementations;

public class Sample_PrimeCalculatorAsyncComponent : GH_AsyncComponent<Sample_PrimeCalculatorAsyncComponent>
{
    public override Guid ComponentGuid => new Guid("22C612B0-2C57-47CE-B9FE-E10621F18933");

    protected override System.Drawing.Bitmap Icon => Properties.Resources.logo32;

    public override GH_Exposure Exposure => GH_Exposure.primary;

    public Sample_PrimeCalculatorAsyncComponent()
        : base("The N-th Prime Calculator", "PRIME", "Calculates the nth prime number.", "Samples", "Async")
    {
        BaseWorker = new PrimeCalculatorWorker(this);
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddIntegerParameter(
            "N",
            "N",
            "Which n-th prime number. Minimum 1, maximum one million. Take care, it can burn your CPU.",
            GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddNumberParameter("Output", "O", "The n-th prime number.", GH_ParamAccess.item);
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

    private sealed class PrimeCalculatorWorker : WorkerInstance<Sample_PrimeCalculatorAsyncComponent>
    {
        private int TheNthPrime { get; set; } = 100;
        private long ThePrime { get; set; } = -1;

        public PrimeCalculatorWorker(
            Sample_PrimeCalculatorAsyncComponent parent,
            string id = "baseworker",
            CancellationToken cancellationToken = default
        )
            : base(parent, id, cancellationToken) { }

        public override Task DoWork(Action<string, double> reportProgress, Action done)
        {
            try
            {
                CalculatePrimes(reportProgress);
                done();
            }
            catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
            {
                //No need to call `done()` - GrasshopperAsyncComponent assumes immediate cancel,
                //thus it has already performed clean-up actions that would normally be done on `done()`
            }

            return Task.CompletedTask;
        }

        public void CalculatePrimes(Action<string, double> reportProgress)
        {
            // 👉 Checking for cancellation!
            CancellationToken.ThrowIfCancellationRequested();

            int count = 0;
            long a = 2;

            // Thanks Steak Overflow (TM) https://stackoverflow.com/a/13001749/
            while (count < TheNthPrime)
            {
                // 👉 Checking for cancellation!
                CancellationToken.ThrowIfCancellationRequested();

                long b = 2;
                int prime = 1; // to check if found a prime
                while (b * b <= a)
                {
                    // 👉 Checking for cancellation!
                    CancellationToken.ThrowIfCancellationRequested();

                    if (a % b == 0)
                    {
                        prime = 0;
                        break;
                    }
                    b++;
                }

                reportProgress(Id, (double)count / TheNthPrime);

                if (prime > 0)
                {
                    count++;
                }
                a++;
            }

            ThePrime = --a;
        }

        public override WorkerInstance<Sample_PrimeCalculatorAsyncComponent> Duplicate(
            string id,
            CancellationToken cancellationToken
        ) => new PrimeCalculatorWorker(Parent, id, cancellationToken);

        public override void GetData(IGH_DataAccess da, GH_ComponentParamServer parameters)
        {
            int nthPrime = 100;
            da.GetData(0, ref nthPrime);
            if (nthPrime > 1000000)
            {
                nthPrime = 1000000;
            }

            if (nthPrime < 1)
            {
                nthPrime = 1;
            }

            TheNthPrime = nthPrime;
        }

        public override void SetData(IGH_DataAccess da)
        {
            da.SetData(0, ThePrime);
        }
    }
}
