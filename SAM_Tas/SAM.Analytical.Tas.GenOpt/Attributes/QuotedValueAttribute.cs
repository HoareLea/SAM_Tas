using System;
namespace SAM.Analytical.Tas.GenOpt.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class QuotedValueAttribute : Attribute
    {
        public QuotedValueAttribute() 
        { 
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            if (obj is QuotedValueAttribute)
            {
                return true;
            }

            return false;
        }
    }
}
