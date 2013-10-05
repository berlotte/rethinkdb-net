using System;
using System.Linq;

namespace RethinkDb.DatumConverters
{
    public class EnumDatumConverterFactory : AbstractDatumConverterFactory
    {
        public static readonly EnumDatumConverterFactory Instance = new EnumDatumConverterFactory();

        public EnumDatumConverterFactory()
        {
        }

        public override bool TryGet<T>(IDatumConverterFactory rootDatumConverterFactory, out IDatumConverter<T> datumConverter)
        {
            datumConverter = null;
            if (typeof(T).IsEnum)
                datumConverter = (IDatumConverter<T>)EnumDatumConverter<T>.Instance.Value;
            return datumConverter != null;
		}
    }

	public class EnumDatumConverter<T> : AbstractValueTypeDatumConverter<T>
    {
        public static readonly Lazy<EnumDatumConverter<T>> Instance = new Lazy<EnumDatumConverter<T>>(() => new EnumDatumConverter<T>());

        #region IDatumConverter<Enum> Members

        public override T ConvertDatum(Spec.Datum datum)
        {
			int val = PrimitiveDatumConverterFactory.IntDatumConverter.Instance.Value.ConvertDatum(datum);
			return (T)Enum.ToObject(typeof(T), val);
        }

        public override Spec.Datum ConvertObject(T en)
        {                
			int val = Convert.ToInt32(en);
            return new Spec.Datum() { type = Spec.Datum.DatumType.R_NUM, r_num =  val };
        }

        #endregion
    }
}
