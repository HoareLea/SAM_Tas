namespace SAM.Analytical.Tas.GenOpt
{
    public static partial class Query
    {
        public static string TasGenOptJavaPath()
        {
            return System.IO.Path.Combine(TasGenOptDirectory(), "genopt.jar");
        }
    }
}