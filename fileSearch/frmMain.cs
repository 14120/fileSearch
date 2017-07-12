#region Namespace
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
#endregion

namespace fileSearch
{
    public partial class frmMain : Form
    {

        #region Declare
        private readonly ListViewColumnSorter lv_sorter = new ListViewColumnSorter();
        private SaveFileDialog sfd = new SaveFileDialog();
        private FolderBrowserDialog fbd = new FolderBrowserDialog();
        private volatile bool _stop = false;
        private int oldWidth = 0;
        #endregion

        #region Init
        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            listView.FullRowSelect = true;
            listView.View = View.Details;
            listView.Columns.Add("Path", 500);
            listView.Columns.Add("Cert", 150);
            oldWidth = Width;
        }

        private void frmMain_Resize(object sender, EventArgs e)
        {
            try
            {
                listView.Columns[0].Width -= oldWidth - Width;
                oldWidth = Width;
            }
            catch
            {

            }
        }

        #endregion

        #region Buttons
        private void btnSearch_Click(object sender, EventArgs e)
        {
            if (btnSearch.Text == "Search")
            {
                _stop = false;
                btnText("Stop");
                listView.Items.Clear();
                Thread t = new Thread(() => Search(txtDir.Text, txtSearch.Text));
                t.Start();
            }
            else
            {
                _stop = true;
            }
            
        }
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txtDir.Text = fbd.SelectedPath;
            }
        }

        private void btnText(string text)
        {
            Invoke(new MethodInvoker(
                () =>
                {
                    btnSearch.Text = text;
                }));
        }
        #endregion

        #region listView
        private void listView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            var lv = (ListView)sender;
            lv.ListViewItemSorter = lv_sorter;

            if (e.Column != lv_sorter.SortColumn)
            {
                lv_sorter.SortColumn = e.Column;
                lv_sorter.Order = SortOrder.Ascending;
            }
            else
                lv_sorter.Order = lv_sorter.Order == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;

            lv.Sort();
        }

        private delegate void addToListViewDelegate(ListView lv, ListViewItem lvi);
        private void addItemToListView(ListView lv, ListViewItem lvi)
        {
            if (lv.InvokeRequired)
            {
                Invoke(new addToListViewDelegate(addItemToListView), new object[] { lv, lvi });
                return;
            }
            lv.Items.Add(lvi);
        }
        #endregion

        #region saveFile
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sfd.InitialDirectory = Environment.CurrentDirectory;
            sfd.Filter = "Text Files (*.txt)|*.txt";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                using (var tw = new StreamWriter(new FileStream(sfd.FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite), Encoding.Default))
                {
                    foreach (ListViewItem lvi in listView.Items)
                    {
                        tw.WriteLine(lvi.Text + ";" + lvi.SubItems[1].Text);
                    }
                }
            }
        }
        #endregion
        
        #region Search

        private void Search(string path, string exts)
        {
            foreach (string file in GetFiles(path, exts))
            {
                if (_stop) break;
                else
                {
                    string[] rows = { file, checkSignature(file).ToString() };
                    ListViewItem lvi = new ListViewItem(rows);
                    addItemToListView(listView, lvi);
                }
            }
            btnText("Search");
            return;
        }

        private IEnumerable<string> GetFiles(string path, string exts)
        {
            Queue<string> q = new Queue<string>();
            q.Enqueue(path);
            while (q.Count > 0)
            {
                path = q.Dequeue();
                try
                {
                    foreach (string subDir in Directory.GetDirectories(path))
                    {
                        q.Enqueue(subDir);
                    }
                }
                catch
                {
                   
                }
                string[] files = null;
                try
                {
                    files = Directory.GetFiles(path, exts);
                }
                catch
                {
                    
                }
                if (files != null)
                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        yield return files[i];
                    }
                }
            }
        }

        #endregion

        #region Signature
        private bool checkSignature(string fileName)
        {
            X509Chain cert_chain = new X509Chain();
            X509Certificate2 cert = default(X509Certificate2);
            bool is_chain_valid = false;
            try
            {
                X509Certificate signer = X509Certificate.CreateFromSignedFile(fileName);
                cert = new X509Certificate2(signer);
            }
            catch
            {
                return is_chain_valid;
            }

            cert_chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
            cert_chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
            cert_chain.ChainPolicy.UrlRetrieval‎Timeout = new TimeSpan(0, 1, 0);
            cert_chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
            is_chain_valid = cert_chain.Build(cert);

            if (is_chain_valid)
            {
                return is_chain_valid;
            }
            else
            {
                return is_chain_valid;
            }
        }
        #endregion


    }

    #region Sorter
    public sealed class ListViewColumnSorter : IComparer
    {
        private int column_to_sort;
        private SortOrder order_of_sort;
        public ListViewColumnSorter()
        {
            column_to_sort = 0;
            order_of_sort = SortOrder.Ascending;
        }
        public int Compare(object x, object y)
        {
            int compare_result = 0;
            ListViewItem lv_x, lv_y;
            lv_x = x as ListViewItem;
            lv_y = y as ListViewItem;

            if (lv_x != lv_y)
            {
                int int_lv_x, int_lv_y;
                Int32.TryParse(lv_x.SubItems[column_to_sort].Text, out int_lv_x);
                Int32.TryParse(lv_y.SubItems[column_to_sort].Text, out int_lv_y);

                if (int_lv_x > 0 && int_lv_y > 0)
                    compare_result = Decimal.Compare(int_lv_x, int_lv_y);
                else
                    compare_result = String.CompareOrdinal(lv_x.SubItems[column_to_sort].Text, lv_y.SubItems[column_to_sort].Text);
            }

            if (order_of_sort == SortOrder.Ascending)
                return compare_result;

            return -compare_result;
        }

        public int SortColumn
        {
            set
            {
                column_to_sort = value;
            }
            get
            {
                return column_to_sort;
            }
        }
        public SortOrder Order
        {
            set
            {
                order_of_sort = value;
            }
            get
            {
                return order_of_sort;
            }
        }
    }
    #endregion

    #region DoubleBuffer
    public class DoubleBufferListView : ListView
    {
        public DoubleBufferListView()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.EnableNotifyMessage, true);
        }
        protected override void OnNotifyMessage(Message m)
        {
            if (m.Msg != 0x14)
            {
                base.OnNotifyMessage(m);
            }
        }
    }
    #endregion
}
