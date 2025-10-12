using System;
using System.Windows.Forms;

namespace DispensePath
{
    public static class CUtil
    {
        public static bool ShowMessageBox(string strHead, string strMessage, FormPopup_MessageBox.MESSAGEBOX_TYPE type = FormPopup_MessageBox.MESSAGEBOX_TYPE.OKCANCEL)
        {
            try
            {
                FormPopup_MessageBox FrmMessageBox = new FormPopup_MessageBox(strHead, strMessage, type);
                FrmMessageBox.TopMost = true;

                if (FrmMessageBox.ShowDialog() == DialogResult.OK)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
