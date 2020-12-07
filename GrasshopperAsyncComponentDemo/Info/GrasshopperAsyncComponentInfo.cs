using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace GrasshopperAsyncComponentDemo
{
  public class GrasshopperAsyncComponentInfo : GH_AssemblyInfo
  {
    public override string Name
    {
      get
      {
        return "GrasshopperAsyncComponentDemo";
      }
    }
    public override Bitmap Icon
    {
      get
      {
        //Return a 24x24 pixel bitmap to represent this GHA library.
        return null;
      }
    }
    public override string Description
    {
      get
      {
        //Return a short string describing the purpose of this GHA library.
        return "A base for async & less janky grasshopper components.";
      }
    }
    public override Guid Id
    {
      get
      {
        return new Guid("9c8808bc-ddee-45ca-8c66-05ca3cf4d394");
      }
    }

    public override string AuthorName
    {
      get
      {
        //Return a string identifying you or your company.
        return "";
      }
    }
    public override string AuthorContact
    {
      get
      {
        //Return a string representing your preferred contact details.
        return "";
      }
    }
  }
}
