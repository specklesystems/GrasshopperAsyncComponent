using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GrasshopperAsyncComponent
{
  // TODO: Would an an abstract class be better here? 
  public interface IAsyncComponentWorker
  {

    /// <summary>
    /// This function should return a duplicate instance of your class. Make sure any state is duplicated (or not) properly. 
    /// </summary>
    /// <returns></returns>
    IAsyncComponentWorker GetNewInstance();

    /// <summary>
    /// Here you can safely set the data of your component, just like you would normally. <b>Important: do not call this method directly! When you are ready, call the provided "Done" action from the DoWork function.</b>
    /// </summary>
    /// <param name="DA"></param>
    void SetData(IGH_DataAccess DA);

    /// <summary>
    /// Here you can safely collect the data from your component, just like you would normally. <b>Important: do not call this method directly. It will be invoked by the parent component.</b>
    /// </summary>
    /// <param name="DA">The magic Data Access class.</param>
    /// <param name="Params">The parameters list, in case you need it.</param>
    void CollectData(IGH_DataAccess DA, GH_ComponentParamServer Params);

    /// <summary>
    /// This where the computation happens. Make sure to check and return if the token is cancelled!
    /// </summary>
    /// <param name="token">The cancellation token.</param>
    /// <param name="ReportProgress">Call this action to report progress. It will be displayed in the component's message bar.</param>
    /// <param name="ReportError">Call this to report a warning or an error.</param>
    /// <param name="Done">When you are done computing, call this function to have the parent component invoke the SetData function.</param>
    void DoWork(CancellationToken token, Action<string> ReportProgress, Action<string, GH_RuntimeMessageLevel> ReportError, Action Done);

  }
}
