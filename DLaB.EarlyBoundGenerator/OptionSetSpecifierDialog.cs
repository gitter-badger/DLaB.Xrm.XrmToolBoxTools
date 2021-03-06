﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using DLaB.Xrm;
using DLaB.XrmToolboxCommon;
using PropertyInterface = DLaB.XrmToolboxCommon.PropertyInterface;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using XrmToolBox.Extensibility;

namespace DLaB.EarlyBoundGenerator
{
    public partial class OptionSetSpecifierDialog : DialogBase
    {
        #region Properties

        public string AttributeSchemaName { get; set; }
        public bool SelectOptionSetsForEntity { get; set; }

        #endregion // Properties

        #region Constructor / Load

        public OptionSetSpecifierDialog()
        {
            InitializeComponent();
        }

        public OptionSetSpecifierDialog(PluginControlBase callingControl, bool selectOptionSetsForEntity)
            : base(callingControl)
        {
            InitializeComponent();
            SelectOptionSetsForEntity = selectOptionSetsForEntity;
        }

        private void LocalOptionSetSpecifierDialog_Load(object sender, EventArgs e)
        {
            MakeLocalVisible(false);
            LblOptionSet.Visible = false;
            CmbOptionSets.Visible = false;

            if (SelectOptionSetsForEntity)
            {
                LoadOrRetrieveEntities();
                tableLayoutPanel2.ColumnStyles.Cast<ColumnStyle>().First().Width = 0F;
            }
        }

        #endregion // Constructor / Load

        private void CmbEntities_SelectedIndexChanged(object sender, EventArgs e)
        {
            Enable(false);
            ExecuteMethod(RetrieveAttributes);
        }

        private void CmbAttributes_SelectedIndexChanged(object sender, EventArgs e)
        {
            var attribute = CmbAttributes.SelectedItem as ObjectCollectionItem<AttributeMetadata>;
            if (attribute == null)
            {
                return;
            }

            if (SelectOptionSetsForEntity)
            {
                AttributeSchemaName = attribute.Value.EntityLogicalName + "." + attribute.Value.SchemaName.ToLower();
            }
            else
            {
                AttributeSchemaName = attribute.Value.EntityLogicalName + "_" + attribute.Value.SchemaName.ToLower();
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrWhiteSpace(AttributeSchemaName))
            {
                MessageBox.Show(("No OptionSet Attribute Schema Name specified"),
                "No OptionSet Name!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else 
            { 
                DialogResult = DialogResult.OK;
                Close();
            };
        }

        private void LoadEntities(IEnumerable<EntityMetadata> entities)
        {
            try
            {
                CmbEntities.BeginUpdate();
                CmbEntities.Items.Clear();
                CmbEntities.Text = null;
                CmbEntities.Items.AddRange(entities.ToObjectCollectionArray());
            }
            finally
            {
                CmbEntities.EndUpdate();
                Enable(true);
            }
        }

        public void RetrieveAttributes()
        {
            var entity = CmbEntities.SelectedItem as ObjectCollectionItem<EntityMetadata>;
            if (entity == null)
            {
                return;
            }

            WorkAsync("Retrieving Attributes...", e =>
            {
                e.Result = Service.Execute(new RetrieveEntityRequest()
                {
                    LogicalName = entity.Value.LogicalName,
                    EntityFilters = EntityFilters.Attributes,
                    RetrieveAsIfPublished = true
                });
            }, e =>
            {
                try
                {
                    CmbAttributes.BeginUpdate();
                    CmbAttributes.Items.Clear();
                    CmbAttributes.Text = null;

                    var result = ((RetrieveEntityResponse) e.Result).EntityMetadata.Attributes.
                                 Where(a => 
                                    (SelectOptionSetsForEntity && a.AttributeType == AttributeTypeCode.Picklist)
                                    || a.IsLocalOptionSetAttribute()).
                                 Select(a => new ObjectCollectionItem<AttributeMetadata>(a.SchemaName + " (" + a.LogicalName + ")", a)).
                                 OrderBy(r => r.DisplayName);

                    CmbAttributes.Items.AddRange(result.ToArray());
                }
                finally
                {
                    CmbAttributes.EndUpdate();
                    Enable(true);
                }
            });            
        }

        private void Enable(bool enable)
        {
            CmbEntities.Enabled = enable;
            CmbAttributes.Enabled = enable;
            CmbOptionSets.Enabled = enable;
            RdoLocal.Enabled = enable;
            RdoGlobal.Enabled = enable;
            BtnAdd.Enabled = enable;
        }

        public void RetrieveOptionSets()
        {
            WorkAsync("Retrieving OptionSets...", e =>
            {
                e.Result = ((RetrieveAllOptionSetsResponse)Service.Execute(new RetrieveAllOptionSetsRequest())).OptionSetMetadata;
            }, e =>
            {
                var entityContainer = ((PropertyInterface.IGlobalOptionSets)CallingControl);
                entityContainer.GlobalOptionSets = ((IEnumerable<OptionSetMetadataBase>)e.Result);
                LoadOptionSets(entityContainer.GlobalOptionSets);
            });
        }

        private void LoadOptionSets(IEnumerable<OptionSetMetadataBase> optionSets)
        {
            try
            {
                CmbOptionSets.BeginUpdate();
                CmbOptionSets.Items.Clear();
                CmbOptionSets.Text = null;

                var values = optionSets.
                    Select(e => new ObjectCollectionItem<OptionSetMetadataBase>(e.Name, e)).
                    OrderBy(r => r.DisplayName).ToList();

                CmbOptionSets.Items.AddRange(values.ToArray());
            }
            finally
            {
                CmbOptionSets.EndUpdate();
                Enable(true);
            }
        }

        private void RdoLocal_CheckedChanged(object sender, EventArgs e)
        {
            if (!RdoLocal.Checked) { return; }

            LoadOrRetrieveEntities();
        }

        private void LoadOrRetrieveEntities()
        {
            AttributeSchemaName = String.Empty;
            MakeLocalVisible(true);
            Enable(false);
            RetrieveEntityMetadatasOnLoad(LoadEntities);
        }

        private void RdoGlobal_CheckedChanged(object sender, EventArgs e)
        {
            if (!RdoGlobal.Checked) { return; }

            AttributeSchemaName = String.Empty;
            MakeLocalVisible(false);
            Enable(false);
            var optionSets = ((PropertyInterface.IGlobalOptionSets)CallingControl).GlobalOptionSets;

            if (optionSets == null)
            {
                ExecuteMethod(RetrieveOptionSets);
            }
            else
            {
                LoadOptionSets(optionSets);
            }
        }

        private void MakeLocalVisible(Boolean value)
        {
            LblOptionSet.Visible = !value;
            CmbOptionSets.Visible = !value;

            LblEntity.Visible = value;
            CmbEntities.Visible = value;
            LblAttribute.Visible = value;
            CmbAttributes.Visible = value;
        }

        private void CmbOptionSets_SelectedIndexChanged(object sender, EventArgs e)
        {
            var optionSet = CmbOptionSets.SelectedItem as ObjectCollectionItem<OptionSetMetadataBase>;
            if (optionSet == null)
            {
                return;
            }

            if (optionSet.Value.IsGlobal.GetValueOrDefault(false))
            {
                AttributeSchemaName = optionSet.Value.Name.ToLower();
            }
        }
    }
}
