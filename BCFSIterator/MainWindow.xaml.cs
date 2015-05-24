using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using BCFileSearch;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace BCFSIterator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            CommonOpenFileDialog cofd = new CommonOpenFileDialog();
            cofd.IsFolderPicker = true;
            if (cofd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                txtSearchFor.Text = cofd.FileName;
            }

        }
        private void FileFound(FileSearchResult fr)
        {
            lvwResults.Items.Add(fr);
        }
        private void cmdSearch_Click(object sender, RoutedEventArgs e)
        {
            String SearchLocation = txtSearchFor.Text;
            
                String searchfor = txtSearchFor.Text;
                /*foreach(var iterate in BCFileSearch.FileFinder.Enumerate(SearchLocation, searchfor, (FileFinder.SearchFilter)null, chkrecursive.IsChecked.HasValue && chkrecursive.IsChecked.Value))
                {
                    lvwResults.Items.Add(iterate);
                }*/
                    
            
        }
    }
}
