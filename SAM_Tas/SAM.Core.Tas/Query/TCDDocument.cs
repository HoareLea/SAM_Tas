namespace SAM.Core.Tas
{
    public static partial class Query
    {
        public static TCD.Document TCDDocument()
        {
            TCD.Document document = null;

            try
            {
                object @object = Core.Query.ActiveObject("TCD.Document");

                if (@object != null)
                {
                    document = @object as TCD.Document;
                    Core.Modify.ReleaseCOMObject(@object);
                    document = null;
                }
            }
            catch
            {

            }

            document = new TCD.Document();

            return document;
        }
    }
}