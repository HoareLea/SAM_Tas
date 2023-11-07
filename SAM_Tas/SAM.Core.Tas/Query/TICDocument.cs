namespace SAM.Core.Tas
{
    public static partial class Query
    {
        public static TIC.Document TICDocument()
        {
            TIC.Document document = null;

            try
            {
                //object @object = Marshal.GetActiveObject("TIC.Document");
                object @object = Core.Query.ActiveObject("TIC.Document");

                if (@object != null)
                {
                    document = @object as TIC.Document;
                    Core.Modify.ReleaseCOMObject(@object);
                    document = null;
                }
            }
            catch
            {

            }

            document = new TIC.Document();

            return document;
        }
    }
}