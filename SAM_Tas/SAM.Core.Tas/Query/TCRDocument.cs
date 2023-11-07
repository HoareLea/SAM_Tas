namespace SAM.Core.Tas
{
    public static partial class Query
    {
        public static TCR.Document TCRDocument()
        {
            TCR.Document document = null;

            try
            {
                //object @object = Marshal.GetActiveObject("TCR.Document");
                object @object = Core.Query.ActiveObject("TCR.Document");

                if (@object != null)
                {
                    document = @object as TCR.Document;
                    Core.Modify.ReleaseCOMObject(@object);
                    document = null;
                }
            }
            catch
            {

            }

            document = new TCR.Document();

            return document;
        }
    }
}