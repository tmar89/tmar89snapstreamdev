using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace BTVTrailerDownloader
{
    public partial class dBEditor : Form
    {
        List<string> downloadedTrailers;
        ObjectToSerialize trailerDBobject;

        public dBEditor()
        {
            InitializeComponent();
            loadList();
        }

        private void Close_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        public void loadList()
        {
            // Get history            
            downloadedTrailers = new List<string>();  
            trailerDBobject = new ObjectToSerialize();
            Serializer serializer = new Serializer();
            trailerDBobject = serializer.DeSerializeObject(Application.StartupPath + "\\trailerDB.txt");
            downloadedTrailers = trailerDBobject.DownloadedTrailers;

            historyListBox.DataSource = downloadedTrailers;
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            downloadedTrailers.RemoveAt(historyListBox.SelectedIndex);
            historyListBox.DataSource = null;
            historyListBox.DataSource = downloadedTrailers;

            // Save new list
            downloadedTrailers = (List<string>)historyListBox.DataSource;
            Serializer serializer = new Serializer();
            serializer.SerializeObject(Application.StartupPath + "\\trailerDB.txt", trailerDBobject);
        }
    }
}