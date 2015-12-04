namespace Riskmap.DB
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Range_Backup
    {
        public long ID { get; set; }

        public long RiskID { get; set; }

        [Column(TypeName = "real")]
        public double PointLatitude { get; set; }

        [Column(TypeName = "real")]
        public double PointLongitude { get; set; }

        public virtual Risk_Backup Risk_Backup { get; set; }
    }
}
