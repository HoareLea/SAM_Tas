using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SizeMethod ToSAM(this global::TPD.tpdSizeMethod tpdSizeMethod)
        {
            switch(tpdSizeMethod)
            {
                case global::TPD.tpdSizeMethod.tpdSizeMethodSized:
                    return SizeMethod.Sized;

                case global::TPD.tpdSizeMethod.tpdSizeMethodAddLoadLocal:
                    return SizeMethod.AddLoadLocal;

                case global::TPD.tpdSizeMethod.tpdSizeMethodNormal:
                    return SizeMethod.Normal;

                case global::TPD.tpdSizeMethod.tpdSizeMethodAddLoadAllAttached:
                    return SizeMethod.AddLoadAllAttached;

                case global::TPD.tpdSizeMethod.tpdSizeMethodAddLoadAllAttachedChiller:
                    return SizeMethod.AddLoadAllAttachedChiller;

                case global::TPD.tpdSizeMethod.tpdSizeMethodAddLoadAllAttachedHeating:
                    return SizeMethod.AddLoadAllAttachedHeating;

                case global::TPD.tpdSizeMethod.tpdSizeMethodAddLoadAllAttachedDHW:
                    return SizeMethod.AddLoadAllAttachedDHW;

                case global::TPD.tpdSizeMethod.tpdSizeMethodDesignFlow:
                    return SizeMethod.DesignFlow;
            }

            throw new System.NotImplementedException();
        }
    }
}
