using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Util.Files
{
    public class FileScanner
    {
        private List<FileInfo> _scannedFiles = new List<FileInfo>();
        private List<DirectoryInfo> _scannedFolders = new List<DirectoryInfo>();

        public List<FileInfo> ScannedFiles
        {
            get
            {
                return _scannedFiles;
            }
        }

        public List<DirectoryInfo> ScannedFolders
        {
            get
            {
                return _scannedFolders;
            }
        }

        public void ScanAllFiles(DirectoryInfo dir, string searchPattern)
        {
          
            // list the files
            try
            {
                
                foreach (FileInfo f in dir.GetFiles(searchPattern))
                {
                    //Console.WriteLine("File {0}", f.FullName);
                    _scannedFiles.Add(f);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Directory {0}  \n could not be accessed!!!!", dir.FullName);
                return;  // We alredy got an error trying to access dir so dont try to access it again
            }

            // process each directory
            // If I have been able to see the files in the directory I should also be able 
            // to look at its directories so I dont think I should place this in a try catch block
            foreach (DirectoryInfo d in dir.GetDirectories())
            {
                _scannedFolders.Add(d);
                ScanAllFiles(d, searchPattern);
            }

        }
        public void ScanAllExcelFiles(DirectoryInfo dir)
        {

            // list the files
            try
            {
                foreach (FileInfo f in dir.GetFiles().Where(f=>f.Extension.EndsWith(".xlsx")||f.Extension.EndsWith(".xls")))
                {
                    //Console.WriteLine("File {0}", f.FullName);
                    _scannedFiles.Add(f);
                }
            }
            catch
            {
                Console.WriteLine("Directory {0}  \n could not be accessed!!!!", dir.FullName);
                return;  // We alredy got an error trying to access dir so dont try to access it again
            }
            foreach (DirectoryInfo d in dir.GetDirectories())
            {
                _scannedFolders.Add(d);
                ScanAllExcelFiles(d);
            }




        }




    }
}
