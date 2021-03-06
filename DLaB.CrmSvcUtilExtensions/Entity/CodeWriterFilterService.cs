﻿using System.Diagnostics;
using DLaB.CrmSvcUtilExtensions.OptionSet;
using Microsoft.Crm.Services.Utility;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;

namespace DLaB.CrmSvcUtilExtensions.Entity
{
    class CodeWriterFilterService  : ICodeWriterFilterService
    {
        private ICodeWriterFilterService DefaultService { get; set; }
        public static Dictionary<Guid, OptionSetMetadataBase> OptionSetMetadata { get; set; }
        /// <summary>
        /// Contains Meta Data for entities, key'd by logical name
        /// </summary>
        public static Dictionary<String, EntityMetadata> EntityMetadata { get; set; }
        public FilterOptionSetEnums EnumFilter { get; set; }
        public HashSet<String> EntitiesToSkip { get; set; }

        static CodeWriterFilterService()
        {
            OptionSetMetadata = new Dictionary<Guid, OptionSetMetadataBase>();
            EntityMetadata = new Dictionary<String, EntityMetadata>();

        }

        public CodeWriterFilterService(ICodeWriterFilterService defaultService)
        {
            DefaultService = defaultService;
            EnumFilter = new FilterOptionSetEnums(defaultService);
            EntitiesToSkip = ConfigHelper.GetHashSet("EntitiesToSkip");
        }

        #region ICodeWriterFilterService Members

        #region Pass Through Implementations

        public bool GenerateAttribute(AttributeMetadata attributeMetadata, IServiceProvider services)
        {
            return DefaultService.GenerateAttribute(attributeMetadata, services);
        }
         
        public bool GenerateOption(OptionMetadata optionMetadata, IServiceProvider services)
        {
            return DefaultService.GenerateOption(optionMetadata, services);
        }

        public bool GenerateRelationship(RelationshipMetadataBase relationshipMetadata, EntityMetadata otherEntityMetadata, IServiceProvider services)
        {
            return DefaultService.GenerateRelationship(relationshipMetadata, otherEntityMetadata, services);
        }

        public bool GenerateServiceContext(IServiceProvider services)
        {
            return DefaultService.GenerateServiceContext(services);
        }

        #endregion // Pass Through Implementations
        
        public bool GenerateEntity(EntityMetadata entityMetadata, IServiceProvider services)
        {
            if (!DefaultService.GenerateEntity(entityMetadata, services)) { return false; }

            if (!EntityMetadata.ContainsKey(entityMetadata.LogicalName))
            {
                EntityMetadata.Add(entityMetadata.LogicalName, entityMetadata);
            }
            return !EntitiesToSkip.Contains(entityMetadata.LogicalName);
        }

        public bool GenerateOptionSet(OptionSetMetadataBase optionSetMetadata, IServiceProvider services)
        {
            if (EnumFilter.GenerateOptionSet(optionSetMetadata, services))
            {
                OptionSetMetadata.Add(optionSetMetadata.MetadataId.GetValueOrDefault(), optionSetMetadata);
            }
            return DefaultService.GenerateOptionSet(optionSetMetadata, services);
        }

        #endregion
    }
}
