﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFWagons.Entities
{
    [Table("NUM_VAG.STRANA")]
    public partial class NumVagStrana
    {
        [Key]
        //[Column(TypeName = "numeric")]
        public decimal KOD_STRAN { get; set; }

        //[StringLength(10)]
        public string ABREV_STRAN { get; set; }

        //[StringLength(50)]
        public string NAME { get; set; }
    }
}
