namespace NServiceBus.SagaPersisters.Azure
{
    using System;
    using Microsoft.WindowsAzure.Storage.Table;

    public class DictionaryTableEntityQueryBuilder
    {
        public TableQuery<DictionaryTableEntity> Build<T>(string property, object value)
        {
            var type = typeof(T);
            TableQuery<DictionaryTableEntity> query;

            var propertyInfo = type.GetProperty(property);

            if (propertyInfo.PropertyType == typeof(byte[]))
            {
                query =
                    new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForBinary(property,
                        QueryComparisons.Equal, (byte[])value));
            }
            else if (propertyInfo.PropertyType == typeof(bool))
            {
                query =
                    new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForBool(property,
                        QueryComparisons.Equal, (bool)value));
            }
            else if (propertyInfo.PropertyType == typeof(DateTime))
            {
                query =
                    new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForDate(property,
                        QueryComparisons.Equal, (DateTime)value));
            }
            else if (propertyInfo.PropertyType == typeof(Guid))
            {
                query =
                    new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForGuid(property,
                        QueryComparisons.Equal, (Guid)value));
            }
            else if (propertyInfo.PropertyType == typeof(Int32))
            {
                query =
                    new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForInt(property,
                        QueryComparisons.Equal, (int)value));
            }
            else if (propertyInfo.PropertyType == typeof(Int64))
            {
                query =
                    new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForLong(property,
                        QueryComparisons.Equal, (long)value));
            }
            else if (propertyInfo.PropertyType == typeof(Double))
            {
                query =
                    new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForDouble(property,
                        QueryComparisons.Equal, (double)value));
            }
            else if (propertyInfo.PropertyType == typeof(string))
            {
                query =
                    new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterCondition(property,
                        QueryComparisons.Equal, (string)value));
            }
            else
            {
                throw new NotSupportedException(
                    string.Format("The property type '{0}' is not supported in windows azure table storage",
                        propertyInfo.PropertyType.Name));
            }
            return query;
        }
    }
}