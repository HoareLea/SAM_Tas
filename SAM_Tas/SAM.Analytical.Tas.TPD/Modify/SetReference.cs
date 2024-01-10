using SAM.Core;
using SAM.Core.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        public static bool SetReference(this ISystemObject systemObject, string reference)
        { 
            if(systemObject == null)
            {
                return false;
            }

            IParameterizedSAMObject parameterizedSAMObject = systemObject as IParameterizedSAMObject;
            if(parameterizedSAMObject == null)
            {
                return false;
            }

            if(reference == null)
            {
                return parameterizedSAMObject.RemoveValue(SystemObjectParameter.Reference);
            }
            else
            {
                if (!parameterizedSAMObject.SetValue(SystemObjectParameter.Reference, reference))
                {
                    return false;
                }
            }

            return true;

        }
    }
}