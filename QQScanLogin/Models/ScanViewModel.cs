using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace QQScanLogin.Models
{
    public class ScanResult
    {
        public ScanResult()
        {
            
        }

        public ScanResult(string number, string nick)
        {
            Number = number;
            Nick = nick;
        }

        public string Number { get; set; }
        public string Nick { get; set; }
    }

    public class ScanViewModel
    {
        [Required]
        public string Number { get; set; }

        [Required]
        public string Nick { get; set; }
    }
}
