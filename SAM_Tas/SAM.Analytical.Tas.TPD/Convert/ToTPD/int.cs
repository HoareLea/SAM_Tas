namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static int ToTPD(this bool @bool)
        {
            return @bool ? 1 : 0;
        }
    }
}
