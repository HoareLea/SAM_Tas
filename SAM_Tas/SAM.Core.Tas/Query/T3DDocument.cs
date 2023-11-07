namespace SAM.Core.Tas
{
    public static partial class Query
    {
        public static TAS3D.T3DDocument T3DDocument()
        {
            TAS3D.T3DDocument t3DDocument = null;

            try
            {
                //object @object = Marshal.GetActiveObject("T3D.Document");
                object @object = Core.Query.ActiveObject("T3D.Document");

                if (@object != null)
                {
                    t3DDocument = @object as TAS3D.T3DDocument;
                    Core.Modify.ReleaseCOMObject(@object);
                    t3DDocument = null;
                }
            }
            catch
            {

            }

            t3DDocument = new TAS3D.T3DDocument();

            return t3DDocument;
        }
    }
}