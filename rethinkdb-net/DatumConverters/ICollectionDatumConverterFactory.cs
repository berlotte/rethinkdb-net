using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace RethinkDb.DatumConverters
{
    public class ICollectionTDatumConverterFactory: AbstractDatumConverterFactory 
    {
        public static readonly ICollectionTDatumConverterFactory Instance = new ICollectionTDatumConverterFactory();

        private ICollectionTDatumConverterFactory()
        {
        }

        public override bool TryGet<T>(IDatumConverterFactory rootDatumConverterFactory, out IDatumConverter<T> datumConverter) 
        {
            datumConverter = null;
            if (rootDatumConverterFactory == null)
                throw new ArgumentNullException("rootDatumConverterFactory");

            Type t = typeof(T);
            bool isColl = (t.IsGenericType && typeof(ICollection<>).IsAssignableFrom(t)
                           && !typeof(IDictionary).IsAssignableFrom(t));
            if(!isColl)
                return false;

            datumConverter = new ICollectionTConverter<T>(rootDatumConverterFactory);
            return true;
        }

        private class ICollectionTConverter<T> : AbstractReferenceTypeDatumConverter<T> 
        {
            private readonly IDatumConverter arrayTypeConverter;
            private Type elementType;

            public ICollectionTConverter(IDatumConverterFactory rootDatumConverterFactory)
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
                    var reflectionInstance = (T)Activator.CreateInstance(typeof(T));
                    //var retColl = (typeof(ICollection<>).GetType())reflectionInstance ;
                    //var retval = Array.CreateInstance(elementType, datum.r_array.Count);
                    for (int i = 0; i < datum.r_array.Count; i++)
                        retColl.Add(arrayTypeConverter.ConvertDatum(datum.r_array [i]));
                        //retval.SetValue(arrayTypeConverter.ConvertDatum(datum.r_array [i]), i);


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
