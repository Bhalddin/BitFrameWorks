﻿using System;
using System.Collections.Generic;
using System.Text;

namespace BIT.Data.Services
{
    
    public interface IResolver<T>
    {
        
        T GetById(string Id);
       
    }
}
