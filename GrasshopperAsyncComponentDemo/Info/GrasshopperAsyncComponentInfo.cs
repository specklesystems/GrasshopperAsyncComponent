using Grasshopper.Kernel;

namespace GrasshopperAsyncComponentDemo;

public class GrasshopperAsyncComponentInfo : GH_AssemblyInfo
{
    public override string Name => "GrasshopperAsyncComponentDemo";

    //Return a short string describing the purpose of this GHA library.

    public override string Description => "A base for async & less janky grasshopper components.";

    public override Guid Id => new("9c8808bc-ddee-45ca-8c66-05ca3cf4d394");

    //Return a string identifying you or your company.

    public override string AuthorName => "";

    //Return a string representing your preferred contact details.
    public override string AuthorContact => "";
}
