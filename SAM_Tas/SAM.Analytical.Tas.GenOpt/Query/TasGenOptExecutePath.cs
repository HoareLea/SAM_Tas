namespace SAM.Analytical.Tas.GenOpt
{
    public static partial class Query
    {
        public static string TasGenOptExecutePath()
        {
            return System.IO.Path.Combine(TasGenOptDirectory(), "TasGenExecute.exe");
        }
    }
}