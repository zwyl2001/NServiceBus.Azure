namespace NServiceBus.SagaPersisters.Azure
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using Saga;

    /// <summary>
    /// Saga persister implementation using windows azure storage.
    /// </summary>
    public class AzureSagaPersister : ISagaPersister
    {
        readonly bool autoUpdateSchema;
        readonly CloudTableClient client;
        readonly ConcurrentDictionary<string, bool> tableCreated = new ConcurrentDictionary<string, bool>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="account"></param>
        /// <param name="autoUpdateSchema"></param>
        public AzureSagaPersister(CloudStorageAccount account, bool autoUpdateSchema)
        {
            this.autoUpdateSchema = autoUpdateSchema;
            client = account.CreateCloudTableClient();
        }

        /// <summary>
        /// Gets a saga entity using the given saga id.
        /// </summary>
        /// <param name="sagaId">The saga id to use in the lookup.</param>
        /// <returns>The saga entity if found, otherwise null.</returns>
        public T Get<T>(Guid sagaId) where T : IContainSagaData
        {
            var table = EnsureSagaTableExists<T>();

            var query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "SagaId"),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, sagaId.ToString()))
            );

           return new DictionaryTableEntityConverter().ToEntity<T>(table.ExecuteQuery(query).FirstOrDefault());
        }
       
        T ISagaPersister.Get<T>(string property, object value)
        {
            var table = EnsureSagaTableExists<T>();

            TableQuery<DictionaryTableEntity> query;
            if (IsUniqueProperty<T>(property))
            {
                query =  new TableQuery<DictionaryTableEntity>().Where(TableQuery.CombineFilters(
                                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, property),
                                TableOperators.And,
                                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, value.ToString())));
            }
            else
            {
                query = new DictionaryTableEntityQueryBuilder().Build<T>(property, value);
            }

            try
            {
                return  new DictionaryTableEntityConverter().ToEntity<T>(table.ExecuteQuery(query).FirstOrDefault());
            }
            catch (WebException ex)
            {
                // can occur when table has not yet been created, but already looking for absence of instance
                if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                {
                    var response = (HttpWebResponse) ex.Response;
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return default(T);
                    }
                }

                throw;
            }
            catch (StorageException)
            {
                // can occur when table has not yet been created, but already looking for absence of instance
                return default(T);
            }
        }

        /// <summary>
        /// Saves the given saga entity.
        /// </summary>
        /// <param name="saga">the saga entity that will be saved.</param>
        public void Save(IContainSagaData saga)
        {
            Persist(saga);
        }

        /// <summary>
        /// Updates the given saga entity.
        /// </summary>
        /// <param name="saga">the saga entity that will be updated.</param>
        public void Update(IContainSagaData saga)
        {
            Persist(saga);
        }

        void Persist(IContainSagaData saga)
        {
            var table = EnsureSagaTableExists(saga.GetType());

            var sagaId = saga.Id.ToString();

            Upsert(table, saga, "SagaId", sagaId);

            foreach (var uniqueProperty in UniqueAttribute.GetUniqueProperties(saga))
            {
                Upsert(table, saga, uniqueProperty.Key, uniqueProperty.Value.ToString()); // this may contain invalid characters -> Base64Encode it?
            }
        }

        /// <summary>
        /// Deletes the given saga from the injected session factory's
        /// current session.
        /// </summary>
        /// <param name="saga">The saga entity that will be deleted.</param>
        public void Complete(IContainSagaData saga)
        {
            var table = EnsureSagaTableExists(saga.GetType());

            Delete(table, "SagaId", saga.Id.ToString());

            foreach (var uniqueProperty in UniqueAttribute.GetUniqueProperties(saga))
            {
                Delete(table, uniqueProperty.Key, uniqueProperty.Value.ToString());
            }
        }
        
        CloudTable EnsureSagaTableExists<T>() where T : IContainSagaData
        {
            var table = EnsureSagaTableExists(typeof(T));
            return table;
        }

        CloudTable EnsureSagaTableExists(Type type)
        {
            var tableName = type.Name;
            var table = client.GetTableReference(tableName);
            if (autoUpdateSchema && !tableCreated.ContainsKey(tableName))
            {
                var existed = !table.CreateIfNotExists();
                tableCreated[tableName] = true;
                if (existed) MigrateExistingSagas(type, table);
            }
            return table;
        }

        static void Upsert(CloudTable table, IContainSagaData saga, string partitionKey, string rowkey)
        {
            var batch = new TableBatchOperation();

            AddUpsertToBatch(batch, saga, partitionKey, rowkey);

            table.ExecuteBatch(batch);
        }

        static void AddUpsertToBatch(TableBatchOperation batch, object entity, string partitionKey, string rowkey)
        {
            var type = entity.GetType();

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var toPersist = new DictionaryTableEntityConverter().ToDictionaryTableEntity(entity, partitionKey, rowkey, properties);

            batch.Add(TableOperation.InsertOrReplace(toPersist));
        }

        static void Delete(CloudTable table, string partitionKey, string rowkey)
        {
            var query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterCondition(partitionKey, QueryComparisons.Equal, rowkey));

            var entity = table.ExecuteQuery(query).FirstOrDefault();

            if (entity != null) table.Execute(TableOperation.Delete(entity));
        }

        bool IsUniqueProperty<T>(string property)
        {
            return UniqueAttribute.GetUniqueProperties(typeof(T)).Any(p => p.Name == property);
        }

        void MigrateExistingSagas(Type type, CloudTable table)
        {
            TableContinuationToken token = null;
            var reqOptions = new TableRequestOptions();
            var ctx = new OperationContext { ClientRequestID = "" };
            while (true)
            {
                var query = new TableQuery<DictionaryTableEntity>().Take(100);
                var evt = new System.Threading.ManualResetEvent(false);
                table.BeginExecuteQuerySegmented(query, token, reqOptions, ctx, (o) =>
                {
                    var response = ((CloudTable)o.AsyncState).EndExecuteQuerySegmented<DictionaryTableEntity>(o);
                    token = response.ContinuationToken;

                    foreach (var result in response.Results)
                    {
                        // the old convention
                        if (Equals(result["PartitionKey"], result["RowKey"]))
                        {
                            var e = (IContainSagaData) new DictionaryTableEntityConverter().ToEntity(type, result);
                            Persist(e);
                            table.Execute(TableOperation.Delete(result));
                        }
                    }

                    evt.Set();
                }, table);
                evt.WaitOne();
                if (token == null)
                {
                    break;
                }
            }
        }
    }
}
