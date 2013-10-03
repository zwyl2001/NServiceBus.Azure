namespace NServiceBus.SagaPersisters.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public class DictionaryTableEntityConverter
    {
        public object ToEntity(Type entityType, DictionaryTableEntity entity)
        {
            if (entity == null) return null;

            var toCreate = Activator.CreateInstance(entityType);

            CopyProperties(entity, entityType, toCreate);

            return toCreate;
        }

        public T ToEntity<T>(DictionaryTableEntity entity)
        {
            if (entity == null) return default(T);

            var toCreate = Activator.CreateInstance<T>();
            var entityType = typeof(T);

            CopyProperties(entity, entityType, toCreate);

            return toCreate;
        }

        static void CopyProperties<T>(DictionaryTableEntity entity, Type entityType, T toCreate)
        {
            foreach (var propertyInfo in entityType.GetProperties())
            {
                if (entity.ContainsKey(propertyInfo.Name))
                {
                    if (propertyInfo.PropertyType == typeof(byte[]))
                    {
                        propertyInfo.SetValue(toCreate, entity[propertyInfo.Name].BinaryValue, null);
                    }
                    else if (propertyInfo.PropertyType == typeof(bool))
                    {
                        var boolean = entity[propertyInfo.Name].BooleanValue;
                        propertyInfo.SetValue(toCreate, boolean.HasValue && boolean.Value, null);
                    }
                    else if (propertyInfo.PropertyType == typeof(DateTime))
                    {
                        var dateTimeOffset = entity[propertyInfo.Name].DateTimeOffsetValue;
                        propertyInfo.SetValue(toCreate,
                            dateTimeOffset.HasValue ? dateTimeOffset.Value.DateTime : default(DateTime), null);
                    }
                    else if (propertyInfo.PropertyType == typeof(Guid))
                    {
                        var guid = entity[propertyInfo.Name].GuidValue;
                        propertyInfo.SetValue(toCreate, guid.HasValue ? guid.Value : default(Guid), null);
                    }
                    else if (propertyInfo.PropertyType == typeof(Int32))
                    {
                        var int32 = entity[propertyInfo.Name].Int32Value;
                        propertyInfo.SetValue(toCreate, int32.HasValue ? int32.Value : default(Int32), null);
                    }
                    else if (propertyInfo.PropertyType == typeof(Double))
                    {
                        var d = entity[propertyInfo.Name].DoubleValue;
                        propertyInfo.SetValue(toCreate, d.HasValue ? d.Value : default(Int64), null);
                    }
                    else if (propertyInfo.PropertyType == typeof(Int64))
                    {
                        var int64 = entity[propertyInfo.Name].Int64Value;
                        propertyInfo.SetValue(toCreate, int64.HasValue ? int64.Value : default(Int64), null);
                    }
                    else if (propertyInfo.PropertyType == typeof(string))
                    {
                        propertyInfo.SetValue(toCreate, entity[propertyInfo.Name].StringValue, null);
                    }
                    else
                    {
                        throw new NotSupportedException(
                            string.Format("The property type '{0}' is not supported in windows azure table storage",
                                propertyInfo.PropertyType.Name));
                    }
                }
            }
        }

        public DictionaryTableEntity ToDictionaryTableEntity(object entity, string partitionKey, string rowkey, IEnumerable<PropertyInfo> properties)
        {
            var toPersist = new DictionaryTableEntity
            {
                PartitionKey = partitionKey,
                RowKey = rowkey
            };

            foreach (var propertyInfo in properties)
            {
                if (propertyInfo.PropertyType == typeof(byte[]))
                {
                    toPersist.Add(propertyInfo.Name, (byte[])propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(bool))
                {
                    toPersist.Add(propertyInfo.Name, (bool)propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(DateTime))
                {
                    toPersist.Add(propertyInfo.Name, (DateTime)propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(Guid))
                {
                    toPersist.Add(propertyInfo.Name, (Guid)propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(Int32))
                {
                    toPersist.Add(propertyInfo.Name, (Int32)propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(Int64))
                {
                    toPersist.Add(propertyInfo.Name, (Int64)propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(Double))
                {
                    toPersist.Add(propertyInfo.Name, (Double)propertyInfo.GetValue(entity, null));
                }
                else if (propertyInfo.PropertyType == typeof(string))
                {
                    toPersist.Add(propertyInfo.Name, (string)propertyInfo.GetValue(entity, null));
                }
                else
                {
                    throw new NotSupportedException(
                        string.Format("The property type '{0}' is not supported in windows azure table storage",
                            propertyInfo.PropertyType.Name));
                }
            }
            return toPersist;
        }

    }
}