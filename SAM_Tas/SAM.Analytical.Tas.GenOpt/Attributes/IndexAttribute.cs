using System;

namespace SAM.Analytical.Tas.GenOpt.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class IndexAttribute : Attribute
    {
        public int Index { get; }

        public IndexAttribute(int index) 
        { 
            Index = index;
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            if (obj is IndexAttribute nameAttribute)
            {
                return nameAttribute.Index == Index;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Index.GetHashCode();
        }
    }
}
