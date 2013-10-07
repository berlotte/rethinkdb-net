using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace RethinkDb.DatumConverters
{
    /// <summary>
    /// This is a default/fallback converter for Generic Collections.
    /// Anything not handled by an optimized and specialized ConverterFactory before will be handled here.
    /// We accept any type implementing IEnumerable<T> and having a constructor that can initialize the collection
    ///   when provided an IEnumerable<T> parameter. 
    /// Ex: Queue<T> is okay since  new Queue<T>(IEnumerable<T>) exists
    ///     LinkedList is NOT since new LinkedList<T> (IEnumerable<T>) constructor is not implemented   
    /// </summary>
    public class ListTDatumConverterFactory : AbstractDatumConverterFactory
    {
        public static readonly ListTDatumConverterFactory Instance = new ListTDatumConverterFactory();

        private ListTDatumConverterFactory()
        {
        }

        public override bool TryGet<T>(IDatumConverterFactory rootDatumConverterFactory, out IDatumConverter<T> datumConverter)
        {
            datumConverter = null;
            if (rootDatumConverterFactory == null)
                throw new ArgumentNullException("rootDatumConverterFactory");

            Type t = typeof(T);
            bool isColl = (t.IsGenericType && typeof(List<>).IsAssignableFrom(t));   
            if(!isColl)
                return false;
            Type elementType = t.GetGenericArguments()[0];
            datumConverter = (IDatumConverter)new ListTConverter(rootDatumConverterFactory, elementType);
            return true;
        }

        internal class ListTConverter : IDatumConverter
        {
            private readonly IDatumConverter arrayTypeConverter;


            public ListTConverter(IDatumConverterFactory rootDatumConverterFactory, Type elementType)
            {

                this.arrayTypeConverter = rootDatumConverterFactory.Get(elementType);
            }

            #region IDatumConverter<T> Members

            public object ConvertDatum(Spec.Datum datum) 
            {
                if (datum.type == Spec.Datum.DatumType.R_NULL)
                {
                    return default(IList);
                }
                else if (datum.type == Spec.Datum.DatumType.R_ARRAY)
                {
                    var list = new List<T>(datum.r_array.Count);
                    //var retval = Array.CreateInstance(elementType, datum.r_array.Count);
                    for (int i = 0; i < datum.r_array.Count; i++)
                        list.Add((T)arrayTypeConverter.ConvertDatum(datum.r_array [i]));

                    return list;
                }
                else
                {
                    throw new NotSupportedException("Attempted to cast Datum to array, but Datum was unsupported type " + datum.type);
                }
            }

            public Spec.Datum ConvertObject(object iCollObject)
            {
                if (iCollObject == null)
                    return new Spec.Datum() { type = Spec.Datum.DatumType.R_NULL };

                var retval = new Spec.Datum() { type = Spec.Datum.DatumType.R_ARRAY };
                var array = (IEnumerable)iCollObject;
                foreach (var obj in array)
                    retval.r_array.Add(arrayTypeConverter.ConvertObject(obj));
                return retval;
            }

            #endregion
        }
    }
}
