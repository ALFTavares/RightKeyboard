using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using RightKeyboard.Win32;

namespace RightKeyboard.Forms
{
    public partial class LayoutSelectionDialog : Form
    {
        #region properties

        private int _recentLayoutsCount;
        private bool _okPressed;

        public new Layout Layout { get; private set; }

        #endregion

        #region CTOR

        public LayoutSelectionDialog()
        {
            InitializeComponent();
            LoadLanguageList();
        }

        #endregion

        #region private methods

        private void LoadLanguageList()
        {
            lbLayouts.Items.Clear();
            _recentLayoutsCount = 0;

            var installedLayouts = API.GetKeyboardLayoutList();

            foreach (var layout in Layout.GetLayouts())
            {
                foreach (var installedLayout in installedLayouts)
                {
                    var languageId = unchecked((ushort)installedLayout.ToInt32());

                    if (layout.Identifier == languageId)
                        lbLayouts.Items.Add(layout);
                }
            }


            //Layout.GetLayouts().ToList().ForEach(layout => installedLayouts.ForEach(installedLayout =>
            //{
            //    var languageId = unchecked((ushort)installedLayout.ToInt32());

            //    if (layout.Identifier == languageId)
            //        lbLayouts.Items.Add(layout);
            //}));

            lbLayouts.SelectedIndex = 0;
        }

        private void btOk_Click(object sender, EventArgs e)
        {
            Layout = (Layout)lbLayouts.SelectedItem;
            _okPressed = true;
            Close();
        }

        private void lbLayouts_SelectedIndexChanged(object sender, EventArgs e)
        {
            btOk.Enabled = lbLayouts.SelectedIndex != _recentLayoutsCount || _recentLayoutsCount == 0;
        }

        private void lbLayouts_DoubleClick(object sender, EventArgs e)
        {
            if (btOk.Enabled)
            {
                btOk_Click(this, EventArgs.Empty);
            }
        }

        #endregion

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = !_okPressed;
            _okPressed = false;
            base.OnClosing(e);
        }
    }
}