﻿using Apache.Phoenix;
using Garuda.Data.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Garuda.Data
{
    /// <summary>
    /// Encapsulates a parameter for a Phoenix command.
    /// </summary>
    public sealed class PhoenixParameter : System.Data.Common.DbParameter
    {
        /// <summary>
        /// Structure to encapsulate constants
        /// </summary>
        public struct Constants
        {
            /// <summary>
            /// The string format associated with the TIMESTAMP data type.
            /// </summary>
            public const string TimestampFormat = "yyyy-MM-dd hh:mm:ss.fffffff";

            /// <summary>
            /// The Epoch value January 1, 1970
            /// </summary>
            public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        /// <summary>
        /// Creates a new instance of a Phoenix parameter with the specified value.
        /// </summary>
        public PhoenixParameter()
        {
        }

        /// <summary>
        /// Creates a new instance of a Phoenix parameter with the specified value.
        /// </summary>
        /// <param name="value"></param>
        public PhoenixParameter(object value) : this()
        {
            this.Value = value;
        }

        /// <summary>
        /// Gets or sets the DbType of the parameter.
        /// </summary>
        public override DbType DbType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the parameter is input-only, output-only, bidirectional, or a stored procedure return value parameter.
        /// </summary>
        public override ParameterDirection Direction { get; set; }

        /// <summary>
        /// Gets a value indicating whether the parameter accepts null values.
        /// </summary>
        public override bool IsNullable { get; set; }

        /// <summary>
        /// Gets or sets the name of the IDataParameter.
        /// </summary>
        public override string ParameterName { get; set; }

        /// <summary>
        /// The size of the parameter.
        /// </summary>
        public override int Size { get; set; }

        /// <summary>
        /// Gets or sets the name of the source column that is mapped to the DataSet and used for loading or returning the Value.
        /// </summary>
        public override string SourceColumn { get; set; }

        /// <summary>
        /// Unused
        /// </summary>
        public override bool SourceColumnNullMapping { get; set; }

        /// <summary>
        /// Gets or sets the value of the parameter.
        /// </summary>
        public override object Value { get; set; }

        /// <summary>
        /// Not implemented
        /// </summary>
        public override void ResetDbType()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Converts this parameter to and returns the Apache Phoenix TypedValue structure.
        /// </summary>
        /// <returns></returns>
        public TypedValue AsPhoenixTypedValue()
        {
            return PhoenixParameter.AsPhoenixTypedValue(this);
        }

        /// <summary>
        /// Converts the specified parameter to and returns the Apache Phoenix TypedValue structure.
        /// </summary>
        /// <returns></returns>
        public static TypedValue AsPhoenixTypedValue(PhoenixParameter parameter)
        {
            return PhoenixParameter.AsPhoenixTypedValue(parameter.Value);
        }

        /// <summary>
        /// Converts this parameter to and returns the Apache Phoenix TypedValue structure.
        /// </summary>
        /// <returns></returns>
        public static TypedValue AsPhoenixTypedValue(object val)
        {
            var tv = new TypedValue();

            if(val == null)
            {
                tv.Null = true;
                tv.Type = GarudaRep.Null;
            }
            else
            {
                Type pt = val.GetType();
                if (pt == typeof(int) || 
                    pt == typeof(long) ||
                    pt == typeof(uint) || 
                    pt == typeof(short) ||
                    pt == typeof(ushort))
                {
                    tv.NumberValue = Convert.ToInt64(val);
                    tv.Type = GarudaRep.Long;
                }
                else if (pt == typeof(ulong))
                {
                    ulong ulVal =  Convert.ToUInt64(val);
                    tv.NumberValue = (long)ulVal;
                    tv.Type = GarudaRep.Long;
                }
                else if(pt == typeof(byte[]))
                {
                    tv.BytesValue = Google.Protobuf.ByteString.CopyFrom(val as byte[]);
                    tv.Type = GarudaRep.ByteString;
                }
                else if(pt == typeof(float) || pt == typeof(double))
                {
                    tv.DoubleValue = Convert.ToDouble(val);
                    tv.Type = GarudaRep.Double;
                }
                else if (pt == typeof(string))
                {
                    tv.StringValue = Convert.ToString(val);
                    tv.Type = GarudaRep.String;
                }
                else if(pt == typeof(DateTime))
                {
                    tv.NumberValue = Convert.ToInt64(Convert.ToDateTime(val).Subtract(PhoenixParameter.Constants.Epoch).TotalMilliseconds);
                    tv.Type = GarudaRep.JavaSqlTimestamp;
                }
                else if (val == DBNull.Value)
                {
                    tv.Null = true;
                    tv.Type = GarudaRep.Null;
                }
            }

            return tv;
        }
    }
}
