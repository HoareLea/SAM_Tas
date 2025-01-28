using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static tpdSizeMethod ToTPD(this SizeMethod sizeMethod)
        {
            switch (sizeMethod)
            {
                case SizeMethod.Normal:
                    return tpdSizeMethod.tpdSizeMethodNormal;

                case SizeMethod.AddLoadLocal:
                    return tpdSizeMethod.tpdSizeMethodAddLoadLocal;

                case SizeMethod.AddLoadAllAttachedDHW:
                    return tpdSizeMethod.tpdSizeMethodAddLoadAllAttachedDHW;

                case SizeMethod.AddLoadAllAttached:
                    return tpdSizeMethod.tpdSizeMethodAddLoadAllAttached;

                case SizeMethod.AddLoadAllAttachedChiller:
                    return tpdSizeMethod.tpdSizeMethodAddLoadAllAttachedChiller;

                case SizeMethod.AddLoadAllAttachedHeating:
                    return tpdSizeMethod.tpdSizeMethodAddLoadAllAttachedHeating;

                case SizeMethod.DesignFlow:
                    return tpdSizeMethod.tpdSizeMethodDesignFlow;

                case SizeMethod.Sized:
                    return tpdSizeMethod.tpdSizeMethodSized;
            }

            throw new System.NotImplementedException();
        }
    }
}
