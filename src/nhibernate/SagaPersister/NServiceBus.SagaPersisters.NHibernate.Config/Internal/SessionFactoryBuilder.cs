namespace NServiceBus.SagaPersisters.NHibernate.Config.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using AutoPersistence;
    using global::NHibernate;
    using global::NHibernate.Cfg.MappingSchema;
    using global::NHibernate.Context;
    using global::NHibernate.Mapping.ByCode;
    using Configuration = global::NHibernate.Cfg.Configuration;

    /// <summary>
    /// Builder class for the NHibernate Session Factory
    /// </summary>
    public class SessionFactoryBuilder
    {
        private readonly IEnumerable<Type> typesToScan;

        /// <summary>
        /// Constructor that accepts the types to scan for saga data classes
        /// </summary>
        /// <param name="typesToScan"></param>
        public SessionFactoryBuilder(IEnumerable<Type> typesToScan)
        {
            this.typesToScan = typesToScan;
        }

        /// <summary>
        /// Builds the session factory with the given properties. Database is updated if updateSchema is set
        /// </summary>
        /// <returns></returns>
        public ISessionFactory Build(Configuration nhibernateConfiguration)
        {
            var scannedAssemblies = typesToScan.Select(t => t.Assembly).Distinct();

            foreach (var assembly in scannedAssemblies)
                nhibernateConfiguration.AddAssembly(assembly);

            var modelMapper =
                new SagaModelMapper(typesToScan.Except(nhibernateConfiguration.ClassMappings.Select(x => x.MappedClass)));

            var mapping = modelMapper.Compile();
            var s = mapping.AsString();
            
            nhibernateConfiguration.AddMapping(mapping);

            //nhibernateConfiguration.CreateMappings(HbmDialectScope).

            ApplyDefaultsTo(nhibernateConfiguration);

            try
            {
                return nhibernateConfiguration.BuildSessionFactory();
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                    throw new ConfigurationErrorsException(e.InnerException.Message, e);

                throw;
            }
        }

        private static void ApplyDefaultsTo(Configuration configuration)
        {
            configuration.SetProperty("current_session_context_class",
                                      typeof (ThreadStaticSessionContext).AssemblyQualifiedName);
        }
    }
}