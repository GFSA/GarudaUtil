﻿using Apache.Phoenix;
using Garuda.Data.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Garuda.Data
{
    public class PhoenixDataReader : System.Data.Common.DbDataReader
    {
        private PhoenixCommand _command = null;

        private uint _statementId = uint.MaxValue;

        private List<GarudaResultSet> _resultSets = null;

        private int _currentFrameRowNdx = -1;

        private ulong _currentRowCount = 0;

        private int _currentFrame = 0;

        private int _currentResultSet = 0;

        /// <summary>
        /// Gets the PhoenixConnection associated with the SqlDataReader.
        /// </summary>
        public PhoenixConnection Connection {  get { return (PhoenixConnection)_command.Connection; } }

        internal PhoenixDataReader(PhoenixCommand cmd, GarudaExecuteResponse response)
        {
            if(null == cmd)
            {
                throw new ArgumentNullException("cmd");
            }
            if(null == response)
            {
                throw new ArgumentNullException("response");
            }

            _command = cmd;
            _statementId = response.StatementId;

            //_response = response.Response.Results.ToList();
            _resultSets = new List<GarudaResultSet>();
            foreach(var res in response.Response.Results)
            {
                GarudaResultSet grs = new GarudaResultSet(res.Signature, res.FirstFrame);
                _resultSets.Add(grs);
            }
        }

        #region DbDataReader Class

        /// <summary>
        /// Gets the value of the specified column in its native format given the column name.(Overrides DbDataReader.Item[String].)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override object this[string name]
        {
            get
            {
                return this[GetOrdinal(name)];
            }
        }

        /// <summary>
        /// Gets the value of the specified column in its native format given the column ordinal.(Overrides DbDataReader.Item[Int32].)
        /// </summary>
        /// <param name="ordinal"></param>
        /// <returns></returns>
        public override object this[int ordinal]
        {
            get
            {
                return CurrentRowValue(ordinal);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public override int Depth
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the number of columns in the current row.(Overrides DbDataReader.FieldCount.)
        /// </summary>
        public override int FieldCount
        {
            get
            {
                return CurrentResultSet().Signature.Columns.Count;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the SqlDataReader contains one or more rows.(Overrides DbDataReader.HasRows.)
        /// </summary>
        public override bool HasRows
        {
            get
            {
                return CurrentFrame().Rows.Count > 0;
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public override bool IsClosed
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public override int RecordsAffected
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool GetBoolean(int ordinal)
        {
            return (bool)GetValue(ordinal);
        }

        public override byte GetByte(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override char GetChar(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override string GetDataTypeName(int ordinal)
        {
            return CurrentResultSet().Signature.Columns[ordinal].Type.Name;
        }

        public override DateTime GetDateTime(int ordinal)
        {
            return (DateTime)GetValue(ordinal);
        }

        public override decimal GetDecimal(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override double GetDouble(int ordinal)
        {
            return (double)GetValue(ordinal);
        }

        public override IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public override Type GetFieldType(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override float GetFloat(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override Guid GetGuid(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override short GetInt16(int ordinal)
        {
            return (short)GetValue(ordinal);
        }

        public override int GetInt32(int ordinal)
        {
            return (int)GetValue(ordinal);
        }

        public override long GetInt64(int ordinal)
        {
            return (long)GetValue(ordinal);
        }

        public override string GetName(int ordinal)
        {
            return CurrentResultSet().Signature.Columns[ordinal].ColumnName;
        }

        public override int GetOrdinal(string name)
        {
            int ordinal = -1;

            for(int i = 0; i < this.FieldCount; i++)
            {
                var c = CurrentResultSet().Signature.Columns[i];
                if (c.ColumnName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                {
                    ordinal = i;
                    break;
                }
            }

            return ordinal;
        }

        public override string GetString(int ordinal)
        {
            return GetValue(ordinal) as string;
        }

        public override object GetValue(int ordinal)
        {
            object o = null;
            TypedValue val = CurrentRowValue(ordinal);

            switch (val.Type)
            {
                case Rep.STRING:
                    o = val.StringValue;
                    break;

                case Rep.DOUBLE:
                    o = val.DoubleValue;
                    break;

                case Rep.BOOLEAN:
                    o = val.BoolValue;
                    break;

                case Rep.BYTE:
                case Rep.SHORT:
                case Rep.INTEGER:
                case Rep.LONG:
                case Rep.NUMBER:
                    o = val.NumberValue;
                    break;

                case Rep.NULL:
                    o = DBNull.Value;
                    break;

                default:
                    o = val.NumberValue;
                    break;
            }

            switch(GetDataTypeName(ordinal))
            {
                case "DATE":
                    o = FromPhoenixDate(val.NumberValue);
                    break;

                case "TIME":
                    o = FromPhoenixTime(val.NumberValue);
                    break;

                case "TIMESTAMP":
                    o = FromPhoenixTimestamp(val.NumberValue);
                    break;
            }

            return o;
        }

        public override int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        public override bool IsDBNull(int ordinal)
        {
            return CurrentRowValue(ordinal).Null;
        }

        public override bool NextResult()
        {
            bool ok = this._resultSets.Count > _currentResultSet + 1;
            if (ok)
            {
                _currentResultSet++;
            }

            return ok;
        }

        public override bool Read()
        {
            // Crude, but will do for now...
            _currentRowCount++;
            _currentFrameRowNdx++;

            // Load more data?
            bool bInCurrentFrame = CurrentFrame().Rows.Count > _currentFrameRowNdx;
            if (!bInCurrentFrame && !CurrentFrame().Done)
            {
                Task<FetchResponse> tResp = this.Connection.InternalFetchAsync(this._statementId, _currentRowCount - 1, 1000);
                tResp.Wait();

                CurrentResultSet().Frames.Add(tResp.Result.Frame);
                _currentFrame++;
                _currentFrameRowNdx = 0;
            }

            return CurrentFrame().Rows.Count > _currentFrameRowNdx;
        }

        public override void Close()
        {
            this.Connection.InternalCloseStatement(_statementId);

            base.Close();
        }

        #endregion

        private GarudaResultSet CurrentResultSet()
        {
            return _resultSets[_currentResultSet];
        }

        private Frame CurrentFrame()
        {
            return CurrentResultSet().Frames[_currentFrame];
        }

        private Row CurrentRow()
        {
            return CurrentFrame().Rows[_currentFrameRowNdx];
        }

        private TypedValue CurrentRowValue(int ordinal)
        {
            return CurrentRow().Value[ordinal].Value[0];
        }

        private TimeSpan FromPhoenixTime(long time)
        {
            var dtTime = PhoenixParameter.Constants.Epoch.AddMilliseconds(time);
            return dtTime.Subtract(PhoenixParameter.Constants.Epoch);
        }

        private DateTime FromPhoenixDate(long date)
        {
            return PhoenixParameter.Constants.Epoch.AddDays(date);
        }

        private DateTime FromPhoenixTimestamp(long timestamp)
        {
            return PhoenixParameter.Constants.Epoch.AddMilliseconds(timestamp);
        }
    }
}
