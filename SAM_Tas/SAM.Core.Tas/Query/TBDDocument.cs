namespace SAM.Core.Tas
{
    public static partial class Query
    {
        public static TBD.TBDDocument TBDDocument()
        {
            TBD.TBDDocument tBDDocument = null;

            try
            {
                //object @object = Marshal.GetActiveObject("Document");
                object @object = Core.Query.ActiveObject("Document");

                if (@object != null)
                {
                    tBDDocument = @object as TBD.TBDDocument;
                    Core.Modify.ReleaseCOMObject(@object);
                    tBDDocument = null;
                }
            }
            catch
            {

            }

            tBDDocument = new TBD.TBDDocument();

            return tBDDocument;
        }
    }
}