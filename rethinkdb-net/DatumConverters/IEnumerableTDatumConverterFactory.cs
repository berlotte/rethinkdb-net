using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace RethinkDb.DatumConverters
{
	public class IEnumerableTDatumConverterFactory : AbstractDatumConverterFactory
	{
		public static readonly IEnumerableTDatumConverterFactory Instance = new IEnumerableTDatumConverterFactory();

		private IEnumerableTDatumConverterFactory()
		{
		}

		public override bool TryGet<T>(IDatumConverterFactory rootDatumConverterFactory, out IDatumConverter<T> datumConverter)
		{
			datumConverter = null;
			if (rootDatumConverterFactory == null)
				throw new ArgumentNullException("rootDatumConverterFactory");
			bool isColl = (typeof(T).IsArray 
								    || (typeof(T).IsGenericType  			
								    	&& typeof(T).GetInterface("IEnumerable") != null
								    	&& typeof(T).GetInterface("IDictionary") == null
								    	)
			);	

			if(!isColl)
				return false;

			datumConverter = new IEnumerableTConverter<T>(rootDatumConverterFactory);
			return true;
		}

		private class IEnumerableTConverter<T> : AbstractReferenceTypeDatumConverter<T> 
		{
			private readonly IDatumConverter arrayTypeConverter;
			private Type elementType;

			public IEnumerableTConverter(IDatumConverterFactory rootDatumConverterFactory)
			{
				elementType = typeof(T).IsArray ? typeof(T).GetElementType() : typeof(T).GetGenericArguments()[0];
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

					if(typeof(T).IsArray)
						return (T)Convert.ChangeType(retval, typeof(T));
					else
						return (T)Activator.CreateInstance(typeof(T), retval);
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
