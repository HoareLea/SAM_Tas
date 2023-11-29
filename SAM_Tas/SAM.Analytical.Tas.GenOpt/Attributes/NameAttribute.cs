using System;

namespace SAM.Analytical.Tas.GenOpt.Attributes
{
    [AttributeUsage(AttributeTargets.All)]
    public class NameAttribute : Attribute
    {
        public string Name { get; }

        public NameAttribute(string name) 
        { 
            Name = name;
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            if (obj is NameAttribute nameAttribute)
            {
                return nameAttribute.Name == Name;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
