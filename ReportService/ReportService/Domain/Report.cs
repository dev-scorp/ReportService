using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ReportService.Domain
{
    public class Report
    {
        public string S { get; set; }
        private string _filePath { get; set; }

        public void Save()
        {
            _filePath = Path.GetTempPath()+ "\\report_"+ Guid.NewGuid() + ".txt";
            File.WriteAllText(_filePath, S);
        }

        public byte[] ReadFile()
        {
            return File.ReadAllBytes(_filePath);
        }
    }
}
