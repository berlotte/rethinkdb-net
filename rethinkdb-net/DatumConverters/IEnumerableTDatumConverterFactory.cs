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
			bool isGenericColl = (typeof(T).IsGenericType  			// first, fastest check
				&& typeof(T).GetInterface("IEnumerable") != null
				&& typeof(T).GetInterface("IDictionary") == null);	// Dictionary has to be handled separately

			Console.WriteLine("Type '"+typeof(T).GetGenericTypeDefinition().Name+"' Is Generic IEnumerableT : "+isGenericColl);
			//if(!typeof(IEnumerableT<>).IsAssignableFrom(typeof(T).GetGenericTypeDefinition()))
			if(!isGenericColl)
				return false;

			datumConverter = new IEnumerableTConverter<T>(rootDatumConverterFactory);
			return true;
		}

		private class IEnumerableTConverter<T> : AbstractReferenceTypeDatumConverter<T>
		{
			private readonly IDatumConverter arrayTypeConverter;

			public IEnumerableTConverter(IDatumConverterFactory rootDatumConverterFactory)
			{
				this.arrayTypeConverter = rootDatumConverterFactory.Get(typeof(T).GetGenericArguments()[0]);
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
					var retColl = (IList)Activator.CreateInstance<T>();
					/*var retColl = (IList)typeof(List<>).MakeGenericType(collItemsType)
						.GetConstructor(Type.EmptyTypes)
							.Invoke(null);*/
					for (int i = 0; i < datum.r_array.Count; i++)
						retColl.Add( arrayTypeConverter.ConvertDatum(datum.r_array [i]));
					return (T)retColl;
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
