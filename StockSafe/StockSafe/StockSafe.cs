using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StockSafe
{
    public partial class StockSafe : ServiceBase
    {
        public StockSafe()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Thread thread = new Thread((new Main()).DoWork);
            thread.Start(this);
        }

        protected override void OnStop()
        {
        }
    }
}
