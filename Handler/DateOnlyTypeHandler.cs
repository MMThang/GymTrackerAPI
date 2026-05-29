using Dapper;
using System.Data;

namespace GymTracker.Handler
{
    public class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override DateOnly Parse(object value)
        {
            if (value is DateTime dt)
                return DateOnly.FromDateTime(dt);

            throw new DataException($"Cannot convert {value.GetType()} to DateOnly");
        }

        public override void SetValue(IDbDataParameter parameter, DateOnly value)
        {
            parameter.Value = value.ToDateTime(TimeOnly.MinValue);
            parameter.DbType = DbType.Date; // 🔥 IMPORTANT for PostgreSQL
        }
    }
}
