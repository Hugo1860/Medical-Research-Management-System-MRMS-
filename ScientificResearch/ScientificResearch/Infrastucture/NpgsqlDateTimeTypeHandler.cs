using System;
using System.Data;
using Dapper;

namespace ScientificResearch.Infrastucture
{
    /// <summary>
    /// Keeps the legacy model contract (DateTime/DateTime?) while PostgreSQL date
    /// columns are exposed by Npgsql as DateOnly.
    /// </summary>
    public sealed class NpgsqlDateTimeTypeHandler : SqlMapper.TypeHandler<DateTime>
    {
        public override DateTime Parse(object value)
        {
            return value switch
            {
                DateOnly date => date.ToDateTime(TimeOnly.MinValue),
                DateTime dateTime => dateTime,
                _ => Convert.ToDateTime(value)
            };
        }

        public override void SetValue(IDbDataParameter parameter, DateTime value)
        {
            parameter.Value = value;
        }
    }
}
