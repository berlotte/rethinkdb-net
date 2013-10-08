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
    public class GenericsCollInterfaceDatumConverterFactory : AbstractDatumConverterFactory
    {
        public static readonly GenericsCollInterfaceDatumConverterFactory Instance = new GenericsCollInterfaceDatumConverterFactory();

        private GenericsCollInterfaceDatumConverterFactory()
        {
        }

        public override bool TryGet<T>(IDatumConverterFactory rootDatumConverterFactory, out IDatumConverter<T> datumConverter)
        {
            datumConverter = null;
            if (rootDatumConverterFactory == null)
                throw new ArgumentNullException("rootDatumConverterFactory");

            Type t = typeof(T);
            if ( !t.IsInterface || !t.IsGenericType)
                return false;
                         
            if(t.GetGenericTypeDefinition() != typeof(IList<>) 
                && t.GetGenericTypeDefinition() != typeof(ICollection<>)
                && t.GetGenericTypeDefinition() != typeof(IEnumerable<>)
               )
                return false;

            datumConverter = new GenericsCollInterfaceConverter<T>(rootDatumConverterFactory);
            return true;
        }

        private class GenericsCollInterfaceConverter<T> : AbstractReferenceTypeDatumConverter<T> 
        {
            private readonly IDatumConverter arrayTypeConverter;
            private Type elementType;

            public GenericsCollInterfaceConverter(IDatumConverterFactory rootDatumConverterFactory)
            {
                elementType = typeof(T).GetGenericArguments()[0];
                this.arrayTypeConverter = rootDatumConverterFactory.Get(elementType);
            }

            #region IDatumConverter<T> Members

            public override T ConvertDatum(Spec.Datum datum) 
            {
                if (datum.type == Spec.Datum.DatumType.R_NULL)
                {
                    return default(T);
                }
                else if (datum.type == Spec.Datum.DatumType.R_ARRAY)
                {
                    var retval = Array.CreateInstance(elementType, datum.r_array.Count);
                    for (int i = 0; i < datum.r_array.Count; i++)
                        retval.SetValue(arrayTypeConverter.ConvertDatum(datum.r_array [i]), i);
                    return (T)Activator.CreateInstance(
                        typeof(List<>).MakeGenericType(new Type[]{elementType}), retval);
                }
                else
                {
                    throw new NotSupportedException("Attempted to cast Datum to array, but Datum was unsupported type " + datum.type);
                }
            }

            public override Spec.Datum ConvertObject(T iCollObject)
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
