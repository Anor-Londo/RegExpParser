using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegExp
{
    public class Set : CollectionBase
    {
        public object this[int index]
        {
            get
            {
                return ((object)List[index]);
            }
            set
            {
                List[index] = value;
            }
        }

        public int AddElement(object element)
        {
            if (Contains(element) == false)
            {
                return List.Add(element);
            }
            return -1;
        }

        public void AddElementRange(object[] arrValue)
        {
            foreach (object obj in arrValue)
            {
                if (Contains(obj) == false)
                {
                    List.Add(obj);
                }
            }
        }

        public void Union(Set setValue)
        {
            foreach (object obj in setValue)
            {
                if (Contains(obj) == false)
                {
                    List.Add(obj);
                }
            }
        }

        public bool IsSubset(Set setValue)
        {
            foreach (object obj in setValue)
            {
                if (Contains(obj) == false)
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsProperSubset(Set setValue)
        {
            if (GetCardinality() > setValue.GetCardinality() && IsSubset(setValue))
            {
                return true;
            }

            return false;

        }

        public void Subtract(Set setValue)
        {
            foreach (object obj in setValue)
            {
                if (Contains(obj) == true)
                {
                    RemoveElement(obj);
                }
            }
        }

        public bool IsEqual(Set setValue)
        {
            if (GetCardinality() == setValue.GetCardinality() && IsSubset(setValue))
            {
                return true;
            }

            return false;

        }

        public bool IsEmpty()
        {
            return List.Count == 0 ? true : false;
        }

        public int GetCardinality()
        {
            return List.Count;
        }

        public bool ElementExist(object value)
        {
            return Contains(value);
        }

        public int IndexOf(object value)
        {
            return (List.IndexOf(value));
        }

        public bool RemoveElement(object value)
        {
            if (Contains(value) == true)
            {
                List.Remove(value);
                return true;
            }

            return false;

        }

        private bool Contains(object value)
        {
            // If value is not of type Int16, this will return false.
            return List.Contains(value);
        }

        protected override void OnInsert(int index, object value)
        {
            if (value == null)
            {
                throw new ArgumentException("Element cannot not be null.");
            }

            if (Contains(value) == true)
            {
                throw new ArgumentException("Element already exists in the set.", "value: " + value.ToString());
            }



        }

        public Array ToArray(Type type)
        {
            return base.InnerList.ToArray(type);
        }


    }
}
