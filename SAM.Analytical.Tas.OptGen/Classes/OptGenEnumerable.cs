﻿using SAM.Analytical.Tas.OptGen.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.OptGen
{
    public class OptGenEnumerable<T> : OptGenObject, IEnumerable<T> where T : IOptGenObject
    {
        private List<T> values;

        public IEnumerator<T> GetEnumerator()
        {
            return values == null ? (new List<T>() ).GetEnumerator(): values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected override string GetText()
        {
            if(values == null)
            {
                return null;
            }

            List<string> texts = new List<string>();
            foreach(T value in values)
            {
                texts.Add(value.Text);
            }


            return string.Format("\n", texts);
        }
    }
}
