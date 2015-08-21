using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Spraylight.MurlTools
{
    public partial class DlgRemoveDelete : DialogWindow
    {
        private int result = CANCEL;
        public const int CANCEL = 0;
        public const int REMOVE = 1;
        public const int DELETE = 2;

        public DlgRemoveDelete()
        {
            InitializeComponent();
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            result = REMOVE;
            Close();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            result = DELETE;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            result = CANCEL;
            Close();
        }

        public int getResult()
        {
            return result;
        }
    }
}
