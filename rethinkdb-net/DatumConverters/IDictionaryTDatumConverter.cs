using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace RethinkDb.DatumConverters
{
	public class IDictionaryTDatumConverterFactory : AbstractDatumConverterFactory
	{
		public static readonly IDictionaryTDatumConverterFactory Instance = new IDictionaryTDatumConverterFactory();

		private IDictionaryTDatumConverterFactory()
		{
		}

		public override bool TryGet<T>(IDatumConverterFactory rootDatumConverterFactory, out IDatumConverter<T> datumConverter)
		{
			datumConverter = null;
			if (rootDatumConverterFactory == null)
				throw new ArgumentNullException("rootDatumConverterFactory");
			bool isGenericColl = (typeof(T).IsGenericType 
				&& typeof(T).GetInterface("IDictionary") != null);

			if(!isGenericColl)
				return false;

			datumConverter = new IDictionaryTConverter<T>(rootDatumConverterFactory);
			return true;
		}

		private class IDictionaryTConverter<T> : AbstractReferenceTypeDatumConverter<T>
		{
			//private readonly IDatumConverter arrayTypeConverterKey;
			private readonly IDatumConverter arrayTypeConverterValue;

			public IDictionaryTConverter(IDatumConverterFactory rootDatumConverterFactory)
			{
				//Type[] arguments = typeof(T).GetGenericArguments();
				//this.arrayTypeConverterKey = rootDatumConverterFactory.Get(typeof(T).GetGenericArguments()[0]);
				this.arrayTypeConverterValue = rootDatumConverterFactory.Get(typeof(T).GetGenericArguments()[1]);
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
					var retColl = (IDictionary)Activator.CreateInstance<T>();
					/*var retColl = (IList)typeof(List<>).MakeGenericType(collItemsType)
						.GetConstructor(Type.EmptyTypes)
							.Invoke(null);*/
					for (int i = 0; i < datum.r_array.Count; i++)
					{
						Console.WriteLine("**Keypair");
						foreach (var assocPair in datum.r_object)
						{
							// left/right for a join
							/*if (assocPair.key == "left")
								item1 = itemConverters[0].ConvertDatum(assocPair.val);*/
							Console.WriteLine("**** assoc key="+assocPair.key+", val="+assocPair.val);
							retColl.Add(assocPair.key, arrayTypeConverterValue.ConvertDatum(assocPair.val));
						}
					}
						//retColl.Add( arrayTypeConverter.ConvertDatum(datum.r_array [i]));
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
				var array = (IDictionary)iCollObject;
				foreach (DictionaryEntry de in array){
					Spec.Datum kvPair = new Spec.Datum{type = Spec.Datum.DatumType.R_OBJECT};
					kvPair.r_object.Add(
						new Spec.Datum.AssocPair{
							key = de.Key.ToString(), 
							val = arrayTypeConverterValue.ConvertObject( de.Value)
						}
					);
					Console.WriteLine("pair = "+de.Key+","+de.Value);
					Console.WriteLine("r_object.Count="+kvPair.r_object.Count);
					retval.r_array.Add(kvPair);
						/*new Spec.Datum( {type = Spec.Datum.DatumType.R_OBJECT,  ConvertObject(obj)+ arrayTypeConverterValue.ConvertObject(obj));
					}*/
				}
				return retval;
			}

			#endregion
		}
	}
}
