using System;

namespace SAM.Core.Tas
{
    public static partial class Query
    {
        public static TSD.TSDDocument TSDDocument()
        {
            TSD.TSDDocument tSDDocument = null;

            try
            {
                //object @object = Marshal.GetActiveObject("Document");
                object @object = Core.Query.ActiveObject("Document");

                if (@object != null)
                {
                    tSDDocument = @object as TSD.TSDDocument;
                    Core.Modify.ReleaseCOMObject(@object);
                    tSDDocument = null;
                }
            }
            catch(Exception exception)
            {
                string message = exception.Message;
            }

            tSDDocument = new TSD.TSDDocument();

            return tSDDocument;
        }
    }
}