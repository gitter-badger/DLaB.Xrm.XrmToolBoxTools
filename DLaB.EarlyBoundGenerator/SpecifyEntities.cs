﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using DLaB.XrmToolboxCommon;
using DLaB.Xrm;
using Microsoft.Xrm.Sdk.Metadata;
using XrmToolBox.Extensibility;

namespace DLaB.EarlyBoundGenerator
{
    public partial class SpecifyEntitiesDialog : DialogBase
    {
        public string SpecifiedEntities { get; set; }

        #region Constructor / Load

        public SpecifyEntitiesDialog()
        {
            InitializeComponent();
        }

        public SpecifyEntitiesDialog(PluginControlBase callingControl)
            : base(callingControl)
        {
            InitializeComponent();
        }

        private void SpecifyEntitiesDialog_Load(object sender, EventArgs e)
        {
            Enable(false);

            if (String.IsNullOrWhiteSpace(SpecifiedEntities))
            {
                SpecifiedEntities = String.Empty;
            }
            else
            {

                SpecifiedEntities = SpecifiedEntities.Replace(" ", String.Empty);
                SpecifiedEntities = SpecifiedEntities.Replace("\n", String.Empty);
            }

            RetrieveEntityMetadatasOnLoad(LoadEntities);
        }

        #endregion // Constructor / Load

        private void LoadEntities(IEnumerable<EntityMetadata> entities)
        {
            try
            {
                LstAll.BeginUpdate();
                LstSpecified.BeginUpdate();
                Enable(false);
                LstAll.Items.Clear();
                LstSpecified.Items.Clear();
                var localEntites = entities.ToList(); // Keep from mulitiple Enumerations

                var specified = new HashSet<String>(SpecifiedEntities.Split(new[] {'|'}, StringSplitOptions.RemoveEmptyEntries));
                LstSpecified.Items.AddRange(localEntites.Where(e => specified.Contains(e.LogicalName)).ToObjectCollectionArray());
                LstAll.Items.AddRange(localEntites.Where(e => !specified.Contains(e.LogicalName)).ToObjectCollectionArray());
            }
            finally
            {
                LstAll.EndUpdate();
                LstSpecified.EndUpdate();
                Enable(true);
            }
        }

        private void Enable(bool enable)
        {
            LstAll.Enabled = enable;
            LstSpecified.Enabled = enable;
            BtnAdd.Enabled = enable;
            BtnRemove.Enabled = enable;
            BtnSave.Enabled = enable;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            SpecifiedEntities = String.Join("|", LstSpecified.Items.Cast<ObjectCollectionItem<EntityMetadata>>().Select(i => i.Value.LogicalName));
            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            var values = LstAll.SelectedItems.Cast<object>().ToArray();
            LstSpecified.Items.AddRange(values);
            foreach (var value in values)
            {
                LstAll.Items.Remove(value);
            }
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            var values = LstSpecified.SelectedItems.Cast<object>().ToArray();
            LstAll.Items.AddRange(values);
            foreach (var value in values)
            {
                LstSpecified.Items.Remove(value);
            }
        }
    }
}
