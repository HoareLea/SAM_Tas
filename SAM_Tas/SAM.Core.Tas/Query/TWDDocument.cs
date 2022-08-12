using System;

namespace SAM.Core.Tas
{
    public static partial class Query
    {
        public static TWD.Document TWDDocument()
        {
            TWD.Document document = null;

            try
            {
                object @object = null;//Marshal.GetActiveObject("Document");

                if (@object != null)
                {
                    document = @object as TWD.Document;
                    Core.Modify.ReleaseCOMObject(@object);
                    document = null;
                }
            }
            catch(Exception exception)
            {
                string message = exception.Message;
            }

            document = new TWD.Document();

            return document;
        }
    }
}