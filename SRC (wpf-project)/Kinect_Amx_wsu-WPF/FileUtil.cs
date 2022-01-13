using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Microsoft.Samples.Kinect.SpeechBasics
{
    public class FileUtil
    {
        private String path;

        public FileUtil()
        {
            this.path = Directory.GetCurrentDirectory();
            this.path = Path.Combine(this.path, "classroom_IP.txt");
        }

        public string Read_File()
        {
            String fileIP = String.Empty;

            using (var fileStream = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                try
                {
                    using (StreamReader sr = new StreamReader(path, true))
                    {
                        fileIP = sr.ReadLine();
                    }
                }catch(IOException e)
                {
                    MessageBox.Show("Error Accessing ip config file \n" + e.Message, "Error Acessing File", MessageBoxButton.OK, MessageBoxImage.Error);
                }           
            }
            
            fileIP.Trim();
            if(fileIP == string.Empty)
                fileIP = "000.00.000.00";
            return fileIP;
        }

        internal void writeToFile(string controler_IP)
        {
            FileInfo fi = new FileInfo(this.path);
            using (TextWriter txtWriter = new StreamWriter(fi.Open(FileMode.Truncate)))
            {
                txtWriter.Write(controler_IP);
            }                       
        }
    }
}
