﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ICode.Models
{
    public class ServiceResult
    {
        public bool State { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }
}
